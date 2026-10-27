using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RetargetScanner
{
    /// <summary>Harmony 패치 대상 한 건. TSV 한 줄과 같습니다.</summary>
    internal sealed class HookRow
    {
        public string PatchClass;
        public string PatchMethod;     // Prefix / Postfix / ... / (dynamic)
        public string DeclaringType;   // 게임 타입 전체 이름 (dynamic이면 빈칸)
        public string MethodName;
        public string MethodType;      // Normal / Getter / Setter / Constructor ...
        public string ArgTypes;        // 쉼표 구분 전체 이름. null이면 지정 안 함(이름만으로 찾음)
        public string Md1Signature;    // 1편에서 실제로 걸린 메서드의 시그니처

        public const string Header = "PatchClass\tPatchMethod\tDeclaringType\tMethodName\tMethodType\tArgTypes\tMd1Signature";

        public string ToTsv() => string.Join("\t", PatchClass, PatchMethod, DeclaringType, MethodName, MethodType,
            ArgTypes ?? "*", Md1Signature ?? "");

        public static HookRow FromTsv(string line)
        {
            string[] c = line.Split('\t');
            return new HookRow
            {
                PatchClass = c[0], PatchMethod = c[1], DeclaringType = c[2], MethodName = c[3], MethodType = c[4],
                ArgTypes = c[5] == "*" ? null : c[5], Md1Signature = c.Length > 6 ? c[6] : ""
            };
        }

        public string Describe()
        {
            string member = MethodType == "Normal" ? MethodName : $"{MethodName}[{MethodType}]";
            string args = ArgTypes == null ? "" : $"({ArgTypes})";
            return $"{DeclaringType}.{member}{args}";
        }
    }

    internal enum ResolveStatus { Ok, Ambiguous, OverloadMismatch, OnlyInBase, MemberMissing, TypeMissing }

    /// <summary>
    /// Harmony가 대상을 찾는 규칙을 흉내 냅니다: <b>그 타입에 직접 선언된 멤버만</b> 보고(부모 클래스 멤버는 못 검),
    /// 인자 타입을 지정하지 않았는데 같은 이름 오버로드가 여럿이면 모호해서 실패합니다.
    /// 1편 PatchHealthCheck는 부모까지 찾아서 "부모로 옮겨진 메서드"에 초록불을 켰습니다. 여기서는 따로 구분합니다.
    /// </summary>
    internal static class HarmonyRules
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                              | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                         | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        public static ResolveStatus Resolve(Type type, HookRow row, out MethodBase found, out string detail)
        {
            found = null;
            detail = "";
            string[] wanted = row.ArgTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            List<MethodBase> declared = DeclaredCandidates(type, row);
            if (declared.Count == 0)
            {
                bool inBase = InheritedCandidates(type, row).Any();
                return inBase ? ResolveStatus.OnlyInBase : ResolveStatus.MemberMissing;
            }

            if (wanted != null)
            {
                found = declared.FirstOrDefault(m => ParamTypes(m).SequenceEqual(wanted, StringComparer.Ordinal));
                if (found != null) return ResolveStatus.Ok;
                detail = string.Join(" | ", declared.Select(Signature));
                return ResolveStatus.OverloadMismatch;
            }

            if (row.MethodType == "Constructor")
            {
                // 인자 미지정 생성자는 Harmony가 매개변수 없는 생성자를 찾습니다.
                found = declared.FirstOrDefault(m => m.GetParameters().Length == 0);
                if (found != null) return ResolveStatus.Ok;
                detail = string.Join(" | ", declared.Select(Signature));
                return ResolveStatus.OverloadMismatch;
            }

            if (declared.Count > 1 && row.MethodType == "Normal")
            {
                detail = string.Join(" | ", declared.Select(Signature));
                return ResolveStatus.Ambiguous;
            }

            found = declared[0];
            return ResolveStatus.Ok;
        }

        private static List<MethodBase> DeclaredCandidates(Type type, HookRow row) => Candidates(type, row, Declared);

        private static IEnumerable<MethodBase> InheritedCandidates(Type type, HookRow row)
        {
            for (Type t = type.BaseType; t != null; t = t.BaseType)
            {
                foreach (MethodBase m in Candidates(t, row, Declared)) yield return m;
            }
        }

        private static List<MethodBase> Candidates(Type type, HookRow row, BindingFlags flags)
        {
            var list = new List<MethodBase>();
            try
            {
                switch (row.MethodType)
                {
                    case "Getter":
                    case "Setter":
                        PropertyInfo p = type.GetProperties(flags).FirstOrDefault(x => x.Name == row.MethodName);
                        MethodInfo acc = row.MethodType == "Getter" ? p?.GetGetMethod(true) : p?.GetSetMethod(true);
                        if (acc != null) list.Add(acc);
                        break;
                    case "Constructor":
                        list.AddRange(type.GetConstructors(flags & ~BindingFlags.Static));
                        break;
                    case "StaticConstructor":
                        if (type.TypeInitializer != null) list.Add(type.TypeInitializer);
                        break;
                    default:
                        list.AddRange(type.GetMethods(flags).Where(m => m.Name == row.MethodName));
                        break;
                }
            }
            catch
            {
                // 타입의 멤버를 못 읽으면 없는 것으로 칩니다(보고서에 "멤버 없음"으로 나와 사람이 확인).
            }
            return list;
        }

        public static IEnumerable<string> ParamTypes(MethodBase m) => m.GetParameters().Select(p => p.ParameterType.ToString());

        public static string Signature(MethodBase m)
        {
            string ret = m is MethodInfo mi ? mi.ReturnType.ToString() : "void";
            string stat = m.IsStatic ? "static " : "";
            return $"{stat}{ret} {m.Name}({string.Join(", ", ParamTypes(m))})";
        }

        public static bool HasInheritedMember(Type type, string name)
        {
            try { return type.GetMember(name, All).Length > 0; }
            catch { return false; }
        }
    }
}
