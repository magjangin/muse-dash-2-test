using HarmonyLib;
using MelonLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace muse_dash_2_test
{
    /// <summary>
    /// <c>UserData/CallProbe.txt</c>에 적은 게임 메서드가 언제, 어떤 순서로, 어떤 인자로 불리는지 로그로 남기는 관찰 전용 탐침입니다.
    /// 다시 빌드하지 않고 파일만 고쳐 게임을 재시작하면 대상이 바뀝니다. 파일이 없거나 비어 있으면 아무것도 걸지 않습니다.
    ///
    /// <para><b>왜 필요한가</b>: 1편 기능 대부분은 "덤프에서 후보를 고른 뒤 Postfix 로그로 호출 시점을 확인"하는 단계를 거쳤고,
    /// 후보를 바꿀 때마다 빌드·배포를 다시 했습니다. 2편 첫 주는 이 단계가 대부분이라 재빌드를 없앴습니다.</para>
    ///
    /// <para><b>안전 범위</b>: Prefix는 void라 원본을 막지 않습니다. 인자는 문자열·숫자·enum만 값으로 찍고, 게임 객체는 타입 이름만
    /// 찍습니다. 1편에서 객체를 리플렉션으로 깊게 훑던 진단이 노트 많은 곡에서 프로세스를 날린 적이 있어(CHECKLIST) 일부러 얕게 둡니다.
    /// <c>Type::*</c>는 virtual·ref/out 매개변수·제네릭 메서드를 뺍니다(1편에서 virtual, private+out 패치가 로그 없이 네이티브 크래시를 낸 전례).
    /// 이름을 직접 적으면 그런 메서드도 걸지만 경고를 남깁니다.</para>
    /// </summary>
    [HarmonyPatch]
    internal static class CallProbe
    {
        public const string FileName = "CallProbe.txt";

        private static readonly List<MethodBase> Targets = new List<MethodBase>();
        private static readonly Dictionary<MethodBase, int> Counts = new Dictionary<MethodBase, int>();
        private static readonly Dictionary<MethodBase, int> CountsSinceFlush = new Dictionary<MethodBase, int>();
        private static bool _loaded;
        private static int _logFirstCalls = 5;
        private static bool _logArgs = true;
        private static long _sequence;

        public static string FilePath => Path.Combine(MelonEnvironment.UserDataDirectory, FileName);

        private static bool Prepare()
        {
            if (!_loaded)
            {
                _loaded = true;
                try { Load(); }
                catch (Exception ex) { ModLogger.Error($"[CallProbe] 설정을 읽지 못했습니다: {ex}"); Targets.Clear(); }
            }
            return Targets.Count > 0;
        }

        private static IEnumerable<MethodBase> TargetMethods() => Targets;

        private static void Prefix(MethodBase __originalMethod, object[] __args)
        {
            try
            {
                Counts.TryGetValue(__originalMethod, out int n);
                Counts[__originalMethod] = ++n;
                CountsSinceFlush.TryGetValue(__originalMethod, out int m);
                CountsSinceFlush[__originalMethod] = m + 1;

                // 처음 몇 번은 전부, 그 뒤로는 10·100·1000…번째만 찍습니다. 매 프레임 불리는 메서드가 로그를 덮지 않게 합니다.
                if (n > _logFirstCalls && !IsPowerOfTen(n)) return;

                string args = _logArgs && __args != null && __args.Length > 0
                    ? "(" + string.Join(", ", __args.Select(FormatArg)) + ")"
                    : "";
                ModLogger.Msg($"[CallProbe] #{++_sequence} f{Frame()} {Describe(__originalMethod)}{args} — {n}번째");
            }
            catch (Exception ex)
            {
                ModLogger.Verbose($"[CallProbe] 기록 실패: {ex.Message}");
            }
        }

        /// <summary>지난 요약 이후 불린 대상과 횟수를 많이 불린 순으로 남깁니다. 씬이 바뀔 때 MainMod가 부릅니다.</summary>
        public static void FlushSummary(string reason)
        {
            if (CountsSinceFlush.Count == 0) return;
            string list = string.Join(", ", CountsSinceFlush.OrderByDescending(kv => kv.Value).Select(kv => $"{Describe(kv.Key)}×{kv.Value}"));
            ModLogger.Msg($"[CallProbe] 요약({reason}): {list}");
            CountsSinceFlush.Clear();
        }

        private static void Load()
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                File.WriteAllText(path, Template);
                ModLogger.Msg($"[CallProbe] 설정 파일 견본을 만들었습니다(대상 없음): {path}");
                return;
            }

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                if (line.StartsWith("limit=", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(line.Substring(6), out int limit)) _logFirstCalls = Math.Max(0, limit);
                    continue;
                }
                if (line.Equals("args=off", StringComparison.OrdinalIgnoreCase))
                {
                    _logArgs = false;
                    continue;
                }

                AddTargets(line);
            }

            ModLogger.Msg($"[CallProbe] 대상 {Targets.Count}개를 겁니다(처음 {_logFirstCalls}번은 매번 기록, 인자 기록 {(_logArgs ? "켬" : "끔")}).");
        }

        private static void AddTargets(string line)
        {
            int sep = line.LastIndexOf("::", StringComparison.Ordinal);
            if (sep <= 0)
            {
                ModLogger.Warning($"[CallProbe] '타입::메서드' 형식이 아닙니다: {line}");
                return;
            }

            string typeName = line.Substring(0, sep).Trim();
            string methodName = line.Substring(sep + 2).Trim();
            Type type = FindType(typeName);
            if (type == null)
            {
                ModLogger.Warning($"[CallProbe] 타입을 찾지 못했습니다: {typeName}");
                return;
            }

            bool wildcard = methodName == "*";
            // Harmony와 같이 그 타입에 직접 선언된 메서드만 봅니다(부모 메서드는 걸리지 않음).
            var found = AccessTools.GetDeclaredMethods(type)
                .Where(m => wildcard || m.Name == methodName)
                .Where(m => !m.IsAbstract && !m.ContainsGenericParameters)
                .ToList();

            if (found.Count == 0)
            {
                ModLogger.Warning($"[CallProbe] {typeName}에 직접 선언된 '{methodName}'이(가) 없습니다(부모 클래스에 있을 수 있음).");
                return;
            }

            foreach (MethodInfo m in found)
            {
                bool risky = m.IsVirtual || m.GetParameters().Any(p => p.ParameterType.IsByRef);
                if (risky && wildcard) continue;
                if (risky) ModLogger.Warning($"[CallProbe] {Describe(m)}는 virtual이거나 ref/out 매개변수가 있습니다. 1편에서 이런 패치가 네이티브 크래시를 낸 적이 있습니다.");
                if (!Targets.Contains(m)) Targets.Add(m);
            }
        }

        /// <summary>
        /// 전체 이름으로만 찾습니다. <c>AccessTools.TypeByName</c>은 못 찾으면 모든 어셈블리의 타입을 훑는데,
        /// 1편에서 시험해 보니 그때 UnityEngine.UIElementsModule의 TypeLoadException 경고가 30줄 넘게 쏟아졌습니다.
        /// </summary>
        private static Type FindType(string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type t = asm.GetType(fullName, throwOnError: false);
                    if (t != null) return t;
                }
                catch
                {
                    // 동적 어셈블리 등 GetType이 안 되는 어셈블리는 건너뜁니다.
                }
            }
            return null;
        }

        private static string FormatArg(object a)
        {
            switch (a)
            {
                case null: return "null";
                case string s: return s.Length > 60 ? $"\"{s.Substring(0, 60)}…\"" : $"\"{s}\"";
                case bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or char:
                    return Convert.ToString(a, System.Globalization.CultureInfo.InvariantCulture);
                case Enum e: return e.ToString();
                default: return a.GetType().Name;
            }
        }

        private static string Describe(MethodBase m)
        {
            string ps = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name));
            return $"{m.DeclaringType?.Name}.{m.Name}({ps})";
        }

        private static int Frame()
        {
            try { return UnityEngine.Time.frameCount; }
            catch { return -1; }
        }

        private static bool IsPowerOfTen(int n)
        {
            while (n >= 10 && n % 10 == 0) n /= 10;
            return n == 1;
        }

        private const string Template =
@"# CallProbe 대상 목록. 한 줄에 하나, 게임을 재시작하면 적용됩니다(모드 재빌드 불필요).
#   타입전체이름::메서드이름   그 이름의 오버로드 전부
#   타입전체이름::*            그 타입에 직접 선언된 메서드 전부(virtual, ref/out, 제네릭 제외)
# 타입 이름은 Decompiled/의 네임스페이스\파일 이름과 같습니다(예: Il2CppAssets.Scripts.Database.DBStageInfo).
# limit=5     처음 N번은 매번 기록(그 뒤는 10·100·1000번째만)
# args=off    인자 기록 끄기
#
# 예(1편 이름):
# Il2CppAssets.Scripts.Database.DBStageInfo::SetRuntimeMusicData
# Il2CppAssets.Scripts.UI.Panels.PnlStage::*
";
    }
}
