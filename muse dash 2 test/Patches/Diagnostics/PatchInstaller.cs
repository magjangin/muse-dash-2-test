using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace muse_dash_2_test
{
    /// <summary>
    /// 이 모드의 Harmony 패치를 MelonLoader 대신 직접 겁니다(<c>[assembly: HarmonyDontPatchAll]</c>, MainMod.cs).
    /// 걸기 전에 패치 메서드를 미리 JIT 해 보고, 현재 게임 빌드에서 컴파일되지 않는 클래스는 <b>걸지 않습니다</b>.
    ///
    /// <para><b>왜 필요한가</b>: 게임이 업데이트되면 MelonLoader가 Il2CppAssemblies를 새로 만듭니다. 패치 본문이
    /// 쓰던 게임 멤버가 사라졌으면 그 패치 메서드는 첫 호출 때 JIT에 실패해 <c>MissingMethodException</c>을
    /// 던집니다. 이 예외는 <b>패치 메서드 안의 try/catch로 잡히지 않습니다</b> — 메서드가 컴파일되기 전에 호출
    /// 지점에서 나기 때문입니다(2026-09-27 실측, .NET 6/10 동일). 그러면 Il2CppInterop 트램펄린이 예외를 받아
    /// "During invoking native->managed trampoline"을 <b>호출마다</b> 남기고 기본값(null/0/false)을 돌려주며,
    /// Prefix였다면 게임 원본 메서드는 아예 실행되지 않습니다. 기능 하나가 꺼지는 데서 끝나지 않고 게임 쪽이
    /// 망가집니다.</para>
    ///
    /// <para><see cref="PatchHealthCheck"/>는 "대상 메서드가 있는가"만 봅니다. 대상이 멀쩡해도 본문이 깨질 수
    /// 있으므로 초록불이 이 경우를 막아주지 못했습니다. 여기서 시작 시점에 걸러 두면, 깨진 기능만 빠지고 게임
    /// 원본은 손대지 않은 채로 돌며, 무엇이 사라졌는지 로그 한 줄로 남습니다.</para>
    ///
    /// <para><b>MelonLoader와 같은 점</b>: 0.7.3의 <c>MelonBase.HarmonyInit</c>과 같은 순서
    /// (<c>GetValidTypes</c>)로 모든 타입에 <c>CreateClassProcessor(type, false).Patch()</c>를 부르고, 클래스
    /// 단위로 예외를 격리합니다. 호출 시점도 사실상 같습니다(MelonHarmonyInit 직후 OnApplicationStart, 그 사이에
    /// 게임 코드는 돌지 않습니다). 달라진 건 JIT 사전 점검 하나뿐입니다. 사전 JIT는 어차피 첫 호출 때 할 일을
    /// 앞당길 뿐이고, 정적 생성자는 실행하지 않습니다(같은 실측).</para>
    ///
    /// <para><b>잡지 못하는 것</b>: 패치 메서드가 부르는 <b>다른</b> 메서드(헬퍼, 람다)의 JIT 실패는 여기서 안 보입니다.
    /// 그건 런타임에 호출 지점에서 나므로 패치 본문의 try/catch가 잡습니다. 게임 멤버를 만지는 코드를 별도
    /// 메서드로 빼 두면 그 try가 실제로 작동합니다(1편 <c>GameBgmSource</c> 참고).</para>
    /// </summary>
    public static class PatchInstaller
    {
        public static void ApplyAll(HarmonyLib.Harmony harmony, Assembly assembly)
        {
            int applied = 0;
            var skipped = new List<string>();
            var failed = new List<string>();

            foreach (Type type in assembly.GetValidTypes())
            {
                try
                {
                    bool isPatchClass = type.IsDefined(typeof(HarmonyAttribute), true);
                    if (isPatchClass && TryFindUncompilablePatchMethod(type, out MethodInfo broken, out Exception jitError))
                    {
                        skipped.Add(type.Name);
                        ModLogger.Warning(
                            $"[PatchInstaller] '{type.Name}' 패치를 걸지 않았습니다. {broken.Name}이(가) 현재 게임 빌드에서 " +
                            $"컴파일되지 않습니다(게임 업데이트로 참조 멤버가 사라졌을 수 있음): {jitError.GetType().Name}: {jitError.Message}");
                        continue;
                    }

                    harmony.CreateClassProcessor(type, allowUnannotatedType: false).Patch();
                    if (isPatchClass) applied++;
                }
                catch (Exception ex)
                {
                    failed.Add(type.Name);
                    ModLogger.Error($"[PatchInstaller] '{type.FullName}' 패치 적용 실패: {ex}");
                }
            }

            if (skipped.Count == 0 && failed.Count == 0)
            {
                ModLogger.Msg($"[PatchInstaller] 패치 클래스 {applied}개를 적용했습니다.");
                return;
            }

            ModLogger.Warning(
                $"[PatchInstaller] 패치 클래스 {applied}개 적용, {skipped.Count}개 제외(JIT 실패), {failed.Count}개 적용 실패. " +
                "제외·실패한 기능만 꺼지고 게임 원본 동작은 그대로입니다.");
            if (skipped.Count > 0) ModLogger.Warning($"[PatchInstaller]   제외: {string.Join(", ", skipped)}");
            if (failed.Count > 0) ModLogger.Warning($"[PatchInstaller]   실패: {string.Join(", ", failed)}");
        }

        /// <summary>
        /// 패치 클래스의 패치 메서드(Prefix/Postfix/Finalizer 등, <see cref="PatchHealthCheck.IsPatchMethod"/>) 중
        /// 현재 로드된 게임 어셈블리로 JIT 되지 않는 것이 있으면 그 메서드와 예외를 돌려줍니다.
        /// 로그를 남기지 않는 순수 점검이라 게임 밖에서도 부를 수 있습니다.
        /// </summary>
        internal static bool TryFindUncompilablePatchMethod(Type patchClass, out MethodInfo method, out Exception error)
        {
            foreach (MethodInfo candidate in patchClass.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (candidate.IsAbstract || candidate.ContainsGenericParameters) continue;
                if (!PatchHealthCheck.IsPatchMethod(candidate)) continue;

                try
                {
                    RuntimeHelpers.PrepareMethod(candidate.MethodHandle);
                }
                catch (Exception ex)
                {
                    method = candidate;
                    error = ex;
                    return true;
                }
            }

            method = null;
            error = null;
            return false;
        }
    }
}
