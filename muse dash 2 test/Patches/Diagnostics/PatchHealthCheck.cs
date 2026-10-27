using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace muse_dash_2_test
{
    /// <summary>
    /// 모드 어셈블리의 모든 [HarmonyPatch] 대상이 현재 게임 빌드에서 실제로 해석되는지 시작 시 점검합니다.
    /// 게임 업데이트로 타입/메서드가 사라지면 어떤 패치가 비활성화될지 한눈에 요약 로그로 보여줍니다.
    /// (패치 적용 여부와 무관하게, AccessTools로 대상 존재만 독립적으로 검사하므로 호출 시점 제약이 없습니다.)
    /// </summary>
    public static class PatchHealthCheck
    {
        public static void Run()
        {
            try
            {
                var missing = new List<string>();
                var unresolvable = new List<string>();
                var unreadable = new List<string>();
                var dynamicTargets = new List<string>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                int total = 0;

                // 게임 업데이트로 타입이 사라졌을 때가 이 점검이 가장 필요한 순간인데, 예전에는 그때 통째로 죽었습니다.
                // [HarmonyPatch(typeof(사라진타입))] 클래스 하나에서 GetCustomAttributes가 TypeLoadException을 던지면
                // 바깥 catch로 빠져 나머지 클래스는 점검조차 안 됐습니다(2026-09-27 실측, .NET 6/10 동일).
                // 그래서 타입 목록은 MelonLoader와 같은 GetValidTypes로 받고, 클래스마다 따로 격리합니다.
                foreach (var type in Assembly.GetExecutingAssembly().GetValidTypes())
                {
                    try
                    {
                        CheckPatchClass(type, seen, missing, unresolvable, dynamicTargets, ref total);
                    }
                    catch (Exception ex)
                    {
                        unreadable.Add($"{type.Name}: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                if (missing.Count == 0)
                {
                    ModLogger.Msg($"[PatchHealth] 패치 대상 {total}개 전부 정상 해석되었습니다.");
                }
                else
                {
                    ModLogger.Warning($"[PatchHealth] {missing.Count}/{total}개 패치 대상이 현재 게임 빌드에서 해석되지 않았습니다(해당 기능 비활성 가능):");
                    foreach (var m in missing)
                    {
                        ModLogger.Warning($"[PatchHealth]   - {m}");
                    }
                }

                if (unreadable.Count > 0)
                {
                    ModLogger.Warning($"[PatchHealth] 어트리뷰트를 읽지 못한 패치 클래스 {unreadable.Count}개(대상 타입이 게임에서 사라졌을 수 있음):");
                    foreach (var u in unreadable)
                    {
                        ModLogger.Warning($"[PatchHealth]   - {u}");
                    }
                }

                if (unresolvable.Count > 0)
                {
                    ModLogger.Warning($"[PatchHealth] 대상 타입을 알 수 없어 점검하지 못한 패치 {unresolvable.Count}개:");
                    foreach (var u in unresolvable)
                    {
                        ModLogger.Warning($"[PatchHealth]   - {u}");
                    }
                }

                if (dynamicTargets.Count > 0)
                {
                    ModLogger.Msg($"[PatchHealth] 대상을 런타임에 정하는 패치 {dynamicTargets.Count}개는 정적 점검 대상이 아닙니다: {string.Join(", ", dynamicTargets)}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[PatchHealth] 패치 점검 중 예외: {ex}");
            }
        }

        /// <summary>패치 클래스 하나의 대상들을 점검해 결과 목록에 더합니다. 패치 클래스가 아니면 아무것도 하지 않습니다.</summary>
        private static void CheckPatchClass(
            Type type,
            HashSet<string> seen,
            List<string> missing,
            List<string> unresolvable,
            List<string> dynamicTargets,
            ref int total)
        {
            var classAttrs = type.GetCustomAttributes(typeof(HarmonyPatch), true);
            if (classAttrs == null || classAttrs.Length == 0) return;

            // 대상을 런타임에 계산하는 패치(TargetMethod/TargetMethods)는 정적 점검이 불가능합니다.
            // 조용히 빼면 "전부 정상"에 묻히므로, 못 봤다는 사실을 따로 남깁니다.
            if (HasDynamicTargets(type))
            {
                dynamicTargets.Add(type.Name);
                return;
            }

            PatchTarget classTarget = Merge(default(PatchTarget), classAttrs);

            foreach (var target in EnumerateTargets(type, classTarget))
            {
                string label = $"{type.Name} → {target.Describe()}";
                if (!seen.Add(label)) continue;

                if (target.DeclaringType == null)
                {
                    unresolvable.Add(label);
                    continue;
                }

                total++;
                if (!TargetExists(target.DeclaringType, target.MethodName, target.ArgumentTypes, target.MethodType))
                {
                    missing.Add(label);
                }
            }
        }

        /// <summary>점검 대상 한 건. Harmony가 클래스/메서드 어트리뷰트를 합쳐 계산하는 그 대상입니다.</summary>
        private struct PatchTarget
        {
            public Type DeclaringType;
            public string MethodName;
            public Type[] ArgumentTypes;
            public MethodType MethodType;

            public string Describe()
            {
                string name = string.IsNullOrEmpty(MethodName)
                    ? MethodType.ToString()
                    : (MethodType == MethodType.Normal ? MethodName : $"{MethodName}[{MethodType}]");

                string args = string.Empty;
                if (ArgumentTypes != null && ArgumentTypes.Length > 0)
                {
                    var names = new string[ArgumentTypes.Length];
                    for (int i = 0; i < ArgumentTypes.Length; i++)
                    {
                        names[i] = ArgumentTypes[i]?.Name ?? "?";
                    }
                    args = "(" + string.Join(", ", names) + ")";
                }

                return $"{DeclaringType?.Name ?? "(타입 미상)"}.{name}{args}";
            }
        }

        /// <summary>어트리뷰트 없이 이름만으로도 Harmony가 패치 메서드로 인정하는 이름들입니다.</summary>
        private static readonly string[] PatchMethodNames =
        {
            "Prefix", "Postfix", "Transpiler", "Finalizer", "ReversePatch"
        };

        private static readonly Type[] PatchMethodAttributes =
        {
            typeof(HarmonyPrefix), typeof(HarmonyPostfix), typeof(HarmonyTranspiler),
            typeof(HarmonyFinalizer), typeof(HarmonyReversePatch)
        };

        /// <summary>
        /// 한 패치 클래스가 실제로 노리는 대상들을 열거합니다.
        ///
        /// <para><b>왜 클래스 레벨만 보면 안 되는가</b>: Harmony는 클래스 레벨 <c>[HarmonyPatch]</c>와
        /// <b>각 패치 메서드에 붙은</b> <c>[HarmonyPatch]</c>를 합쳐 대상을 정합니다. 예전 점검 코드는
        /// 클래스 레벨 어트리뷰트만 읽어서 두 방향으로 틀렸습니다.</para>
        ///
        /// <list type="number">
        /// <item><description><b>오진</b>: <c>[HarmonyPatch(typeof(X))]</c> + 메서드마다
        /// <c>[HarmonyPatch("M")]</c>를 쓰는 형태에서는 메서드명을 못 구해
        /// <c>X.Normal 해석 실패</c>라는 없는 메서드 이름으로 경고를 냈습니다.
        /// (2026-08-27 실측 로그: <c>PnlInputMobile_LifecyclePatch → PnlInputMobile.Normal</c>.
        ///  실제로는 Awake/SetAutoFever/SetTouchReverse/SetLeftRight 전부 멀쩡했습니다.)</description></item>
        /// <item><description><b>사각지대</b>: 맨 <c>[HarmonyPatch]</c> 클래스는 declaringType이 없다고
        /// 통째로 건너뛰어 총계에도 안 잡혔습니다. 그래서 <c>MouseTouchBridgePatch</c>의 입력 후킹
        /// 8개(<c>StandloneController.GetButton*</c>, <c>InputManager.*</c>, <c>HideCursor.Update</c>)처럼
        /// <b>게임 업데이트에 가장 잘 깨지는 패치들이 점검에서 빠진 채 "전부 정상"으로 보였습니다.</b>
        /// 실측 당시 119개를 셌지만 메서드 레벨 지정 47개가 통째로 빠져 있었습니다.</description></item>
        /// </list>
        /// </summary>
        private static IEnumerable<PatchTarget> EnumerateTargets(Type patchClass, PatchTarget classTarget)
        {
            bool foundPatchMethod = false;

            foreach (var method in patchClass.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (!IsPatchMethod(method)) continue;
                foundPatchMethod = true;

                var methodAttrs = method.GetCustomAttributes(typeof(HarmonyPatch), true);

                // 메서드 레벨 지정이 없으면 클래스 레벨이 곧 대상입니다(가장 흔한 형태).
                yield return (methodAttrs == null || methodAttrs.Length == 0)
                    ? classTarget
                    : Merge(classTarget, methodAttrs);
            }

            // 패치 메서드를 못 찾았으면 클래스 레벨 지정만으로 판단합니다.
            if (!foundPatchMethod)
            {
                yield return classTarget;
            }
        }

        /// <summary>Harmony가 패치 메서드로 취급하는 메서드인지 확인합니다. <see cref="PatchInstaller"/>도 이 기준을 씁니다.</summary>
        internal static bool IsPatchMethod(MethodInfo method)
        {
            for (int i = 0; i < PatchMethodNames.Length; i++)
            {
                if (string.Equals(method.Name, PatchMethodNames[i], StringComparison.Ordinal)) return true;
            }

            for (int i = 0; i < PatchMethodAttributes.Length; i++)
            {
                if (method.IsDefined(PatchMethodAttributes[i], true)) return true;
            }

            return false;
        }

        /// <summary>대상을 런타임에 계산하는 패치(TargetMethod/TargetMethods)인지 확인합니다.</summary>
        private static bool HasDynamicTargets(Type patchClass)
        {
            foreach (var method in patchClass.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (string.Equals(method.Name, "TargetMethod", StringComparison.Ordinal)
                    || string.Equals(method.Name, "TargetMethods", StringComparison.Ordinal)
                    || method.IsDefined(typeof(HarmonyTargetMethod), true)
                    || method.IsDefined(typeof(HarmonyTargetMethods), true))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>여러 [HarmonyPatch] 어트리뷰트를 기준 대상 위에 순서대로 덮어씁니다.</summary>
        private static PatchTarget Merge(PatchTarget baseTarget, object[] attrs)
        {
            PatchTarget result = baseTarget;

            foreach (HarmonyPatch attr in attrs)
            {
                var info = attr?.info;
                if (info == null) continue;
                if (info.declaringType != null) result.DeclaringType = info.declaringType;
                if (!string.IsNullOrEmpty(info.methodName)) result.MethodName = info.methodName;
                if (info.argumentTypes != null) result.ArgumentTypes = info.argumentTypes;
                if (info.methodType.HasValue) result.MethodType = info.methodType.Value;
            }

            return result;
        }

        private static bool TargetExists(Type declaringType, string methodName, Type[] argumentTypes, MethodType methodType)
        {
            try
            {
                switch (methodType)
                {
                    case MethodType.Constructor:
                        return AccessTools.Constructor(declaringType, argumentTypes) != null;
                    case MethodType.Getter:
                        return !string.IsNullOrEmpty(methodName) && AccessTools.PropertyGetter(declaringType, methodName) != null;
                    case MethodType.Setter:
                        return !string.IsNullOrEmpty(methodName) && AccessTools.PropertySetter(declaringType, methodName) != null;
                    default:
                        if (string.IsNullOrEmpty(methodName)) return false;
                        return (argumentTypes != null
                            ? AccessTools.Method(declaringType, methodName, argumentTypes)
                            : AccessTools.Method(declaringType, methodName)) != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
