using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RetargetScanner
{
    /// <summary>
    /// 1편에서 뽑은 훅·API 목록을 다른 빌드(2편)의 Il2CppAssemblies에 대조해 마크다운 보고서를 만듭니다.
    /// 문제가 있는 줄에는 "어디로 갔을지" 후보를 붙입니다: 같은 이름의 타입(네임스페이스 이동), 같은 이름의 멤버를 선언한 타입.
    /// </summary>
    internal static class RetargetChecker
    {
        private static readonly Dictionary<ResolveStatus, string> HookLabels = new()
        {
            [ResolveStatus.Ok] = "그대로",
            [ResolveStatus.Ambiguous] = "모호(오버로드 여럿, 인자 지정 필요)",
            [ResolveStatus.OverloadMismatch] = "오버로드 불일치",
            [ResolveStatus.OnlyInBase] = "부모 클래스로 이동(Harmony는 못 검)",
            [ResolveStatus.MemberMissing] = "메서드 없음",
            [ResolveStatus.TypeMissing] = "타입 없음",
        };

        public static string Check(List<HookRow> hooks, List<ApiRefRow> refs, GameAssemblies game, string gameDir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 1편 → 대상 빌드 재타깃팅 보고서");
            sb.AppendLine();
            sb.AppendLine($"- 대상: `{gameDir}`");
            sb.AppendLine($"- 생성: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"- 읽은 게임 어셈블리: {string.Join(", ", game.LoadedAssemblies)}");
            sb.AppendLine();

            var hookLines = new List<(ResolveStatus Status, string Line)>();
            int changedSig = 0, dynamicCount = 0;

            foreach (HookRow row in hooks)
            {
                if (row.PatchMethod == "(dynamic)") { dynamicCount++; continue; }

                ResolveStatus st;
                string detail;
                MethodBase found = null;
                if (!game.ByFullName.TryGetValue(row.DeclaringType, out Type t))
                {
                    st = ResolveStatus.TypeMissing;
                    string shortName = row.DeclaringType.Split('.', '+').Last();
                    game.BySimpleName.TryGetValue(shortName, out var sameName);
                    detail = sameName != null
                        ? $"같은 이름 타입: {GameAssemblies.Candidates(sameName)}"
                        : MemberCandidates(game, row.MethodName);
                }
                else
                {
                    st = HarmonyRules.Resolve(t, row, out found, out detail);
                    if (st == ResolveStatus.MemberMissing) detail = Join(SimilarInType(t, row.MethodName), MemberCandidates(game, row.MethodName));
                }

                string sig = found != null ? HarmonyRules.Signature(found) : "";
                bool sigChanged = st == ResolveStatus.Ok && !string.IsNullOrEmpty(row.Md1Signature)
                                  && !row.Md1Signature.StartsWith("(") && sig != row.Md1Signature;
                if (sigChanged)
                {
                    changedSig++;
                    detail = $"1편 `{row.Md1Signature}` → 지금 `{sig}` (패치 매개변수·`__instance`·반환값 확인)";
                }

                string label = sigChanged ? "시그니처 변경" : HookLabels[st];
                hookLines.Add((sigChanged ? ResolveStatus.OverloadMismatch : st,
                    $"| {label} | `{row.Describe()}` | {row.PatchClass}.{row.PatchMethod} | {Escape(detail)} |"));
            }

            int okHooks = hookLines.Count(h => h.Status == ResolveStatus.Ok);
            sb.AppendLine("## 요약");
            sb.AppendLine();
            sb.AppendLine("| 구분 | 수 |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| 훅 대상 전체 | {hookLines.Count} |");
            sb.AppendLine($"| 훅 그대로 | {okHooks} |");
            foreach (var g in hookLines.Where(h => h.Status != ResolveStatus.Ok).GroupBy(h => h.Line.Split('|')[1].Trim()))
            {
                sb.AppendLine($"| 훅 {g.Key} | {g.Count()} |");
            }
            sb.AppendLine($"| 런타임 대상 패치(점검 불가) | {dynamicCount} |");

            List<string> refProblems = CheckRefs(refs, game, out int okRefs);
            sb.AppendLine($"| 게임 API 참조 전체 / 그대로 / 문제 | {refs.Count} / {okRefs} / {refProblems.Count} |");
            sb.AppendLine();

            sb.AppendLine("## 훅 — 손봐야 할 것");
            sb.AppendLine();
            sb.AppendLine("| 상태 | 1편 대상 | 1편 패치 | 단서 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var h in hookLines.Where(h => h.Status != ResolveStatus.Ok).OrderBy(h => h.Status).ThenBy(h => h.Line))
            {
                sb.AppendLine(h.Line);
            }
            sb.AppendLine();

            sb.AppendLine("## 게임 API 참조 — 손봐야 할 것 (빌드 전에 보는 컴파일 오류 목록)");
            sb.AppendLine();
            sb.AppendLine("| 상태 | 1편 타입 | 멤버 | 단서 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (string line in refProblems) sb.AppendLine(line);
            sb.AppendLine();

            sb.AppendLine("## 훅 — 그대로인 것");
            sb.AppendLine();
            sb.AppendLine("| 상태 | 1편 대상 | 1편 패치 | 단서 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var h in hookLines.Where(h => h.Status == ResolveStatus.Ok).OrderBy(h => h.Line)) sb.AppendLine(h.Line);

            return sb.ToString();
        }

        private static List<string> CheckRefs(List<ApiRefRow> refs, GameAssemblies game, out int ok)
        {
            ok = 0;
            var problems = new List<string>();
            var missingTypes = new HashSet<string>(StringComparer.Ordinal);

            foreach (ApiRefRow r in refs)
            {
                if (!game.ByFullName.TryGetValue(r.Type, out Type t))
                {
                    // 타입이 없으면 그 타입의 멤버 줄은 다 같이 깨지므로 타입 줄 하나로 묶습니다.
                    if (missingTypes.Add(r.Type))
                    {
                        string shortName = r.Type.Split('.', '+').Last();
                        game.BySimpleName.TryGetValue(shortName, out var sameName);
                        string hint = sameName != null ? $"같은 이름 타입: {GameAssemblies.Candidates(sameName)}" : "";
                        int members = refs.Count(x => x.Type == r.Type && x.Member != "");
                        problems.Add($"| 타입 없음 | `{r.Type}` | (멤버 {members}개 참조) | {Escape(hint)} |");
                    }
                    continue;
                }

                if (r.Kind == "type" || r.Member == ".ctor" || HarmonyRules.HasInheritedMember(t, r.Member))
                {
                    ok++;
                    continue;
                }

                string hintText = Join(SimilarInType(t, r.Member), MemberCandidates(game, r.Member));
                problems.Add($"| 멤버 없음 | `{r.Type}` | `{r.Member}` | {Escape(hintText)} |");
            }

            return problems;
        }

        private static string MemberCandidates(GameAssemblies game, string member)
        {
            if (string.IsNullOrEmpty(member)) return "";
            return game.ByDeclaredMemberName.TryGetValue(member, out var types)
                ? $"같은 이름 멤버를 선언한 타입: {GameAssemblies.Candidates(types)}"
                : "같은 이름 멤버 없음 — 기능 재분석";
        }

        /// <summary>
        /// 같은 타입 안에서 이름이 비슷한 멤버(한쪽이 다른 쪽을 포함하거나 앞 6글자 이상이 같음)를 찾습니다.
        /// 이름만 살짝 바뀐 경우(접미사 추가, 오타 수정)를 잡기 위한 것이라 판정이 아니라 단서입니다.
        /// </summary>
        private static string SimilarInType(Type t, string member)
        {
            if (string.IsNullOrEmpty(member)) return "";
            string bare = member.StartsWith("get_") || member.StartsWith("set_") ? member.Substring(4) : member;
            string[] names;
            try
            {
                names = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Select(m => m.Name).Distinct().ToArray();
            }
            catch { return ""; }

            var similar = names.Where(n => n != member && (
                    n.Contains(bare, StringComparison.OrdinalIgnoreCase)
                    || (n.Length >= 6 && bare.Contains(n, StringComparison.OrdinalIgnoreCase))
                    || CommonPrefix(n, bare) >= 6))
                .OrderBy(n => Math.Abs(n.Length - bare.Length)).Take(5).ToList();
            return similar.Count == 0 ? "" : $"같은 타입의 비슷한 이름: {string.Join(", ", similar)}";
        }

        private static int CommonPrefix(string a, string b)
        {
            int i = 0;
            while (i < a.Length && i < b.Length && char.ToLowerInvariant(a[i]) == char.ToLowerInvariant(b[i])) i++;
            return i;
        }

        private static string Join(params string[] parts) => string.Join(" / ", parts.Where(p => !string.IsNullOrEmpty(p)));

        private static string Escape(string s) => (s ?? "").Replace("|", "\\|");
    }
}
