using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace RetargetScanner
{
    /// <summary>
    /// 모드 DLL의 [HarmonyPatch] 선언을 <see cref="CustomAttributeData"/>로 읽어 패치 대상 목록을 만듭니다.
    /// 어트리뷰트 인스턴스를 만들지 않으므로 모드 코드는 실행되지 않습니다. <c>GameBindings</c> 같은 상수 문자열도
    /// 컴파일 때 어트리뷰트에 박혀 있어 그대로 읽힙니다(소스 정규식으로는 못 따라가는 부분).
    /// </summary>
    internal static class HookExtractor
    {
        private const string HarmonyPatchAttr = "HarmonyLib.HarmonyPatch";

        private static readonly string[] PatchMethodNames = { "Prefix", "Postfix", "Transpiler", "Finalizer", "ReversePatch" };

        private static readonly Dictionary<string, string> PatchMethodAttrs = new()
        {
            ["HarmonyLib.HarmonyPrefix"] = "Prefix",
            ["HarmonyLib.HarmonyPostfix"] = "Postfix",
            ["HarmonyLib.HarmonyTranspiler"] = "Transpiler",
            ["HarmonyLib.HarmonyFinalizer"] = "Finalizer",
            ["HarmonyLib.HarmonyReversePatch"] = "ReversePatch",
        };

        private const BindingFlags StaticDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public static List<HookRow> Extract(Assembly mod, GameAssemblies md1)
        {
            var rows = new List<HookRow>();

            foreach (Type type in GameAssemblies.GetTypesSafe(mod).OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                IList<CustomAttributeData> classAttrs;
                try { classAttrs = Patches(type.GetCustomAttributesData()); }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[경고] {type.Name}: 어트리뷰트를 못 읽음 ({ex.Message})");
                    continue;
                }
                if (classAttrs.Count == 0) continue;

                MethodInfo[] methods = type.GetMethods(StaticDeclared);
                if (methods.Any(m => m.Name is "TargetMethod" or "TargetMethods"))
                {
                    rows.Add(new HookRow { PatchClass = type.Name, PatchMethod = "(dynamic)", DeclaringType = "", MethodName = "", MethodType = "", ArgTypes = null });
                    continue;
                }

                var classTarget = Merge(new HookRow { MethodType = "Normal" }, classAttrs);

                foreach (MethodInfo m in methods)
                {
                    string kind = PatchKind(m);
                    if (kind == null) continue;

                    HookRow row = Merge(Clone(classTarget), Patches(m.GetCustomAttributesData()));
                    row.PatchClass = type.Name;
                    row.PatchMethod = kind;
                    row.Md1Signature = ResolveInMd1(md1, row);
                    rows.Add(row);
                }
            }

            return rows;
        }

        private static string ResolveInMd1(GameAssemblies md1, HookRow row)
        {
            if (string.IsNullOrEmpty(row.DeclaringType) || !md1.ByFullName.TryGetValue(row.DeclaringType, out Type t))
            {
                return "(1편에서 해석 실패)";
            }
            ResolveStatus st = HarmonyRules.Resolve(t, row, out MethodBase found, out _);
            return st == ResolveStatus.Ok ? HarmonyRules.Signature(found) : $"(1편에서 {st})";
        }

        private static IList<CustomAttributeData> Patches(IEnumerable<CustomAttributeData> attrs) =>
            attrs.Where(a => a.AttributeType.FullName == HarmonyPatchAttr).ToList();

        private static string PatchKind(MethodInfo m)
        {
            foreach (CustomAttributeData a in m.GetCustomAttributesData())
            {
                if (PatchMethodAttrs.TryGetValue(a.AttributeType.FullName ?? "", out string kind)) return kind;
            }
            return PatchMethodNames.Contains(m.Name) ? m.Name : null;
        }

        private static HookRow Clone(HookRow r) => new()
        {
            DeclaringType = r.DeclaringType, MethodName = r.MethodName, MethodType = r.MethodType, ArgTypes = r.ArgTypes
        };

        /// <summary>Harmony처럼 어트리뷰트를 순서대로 덮어씁니다. 생성자 인자는 타입으로 구분합니다.</summary>
        private static HookRow Merge(HookRow row, IEnumerable<CustomAttributeData> attrs)
        {
            foreach (CustomAttributeData a in attrs)
            {
                var strings = new List<string>();
                foreach (CustomAttributeTypedArgument arg in a.ConstructorArguments)
                {
                    string argType = arg.ArgumentType.FullName;
                    if (argType == "System.Type" && arg.Value is Type t)
                    {
                        row.DeclaringType = t.FullName;
                    }
                    else if (argType == "System.String" && arg.Value is string s)
                    {
                        strings.Add(s);
                    }
                    else if (argType == "HarmonyLib.MethodType")
                    {
                        row.MethodType = Enum.ToObject(arg.ArgumentType, arg.Value).ToString();
                    }
                    else if (argType == "System.Type[]" && arg.Value is ReadOnlyCollection<CustomAttributeTypedArgument> items)
                    {
                        row.ArgTypes = string.Join(",", items.Select(i => ((Type)i.Value).ToString()));
                    }
                    // ArgumentType[](ref/out 표시)은 대상 찾기에 쓰이지 않아 무시합니다.
                }

                // HarmonyPatch(string methodName) 또는 HarmonyPatch(string typeName, string methodName)
                if (strings.Count == 1) row.MethodName = strings[0];
                else if (strings.Count >= 2) { row.DeclaringType = strings[0].Split(',')[0].Trim(); row.MethodName = strings[1]; }
            }
            return row;
        }
    }
}
