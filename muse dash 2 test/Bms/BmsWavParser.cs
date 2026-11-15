using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace muse_dash_2_test
{
    public sealed class BmsWavInfo
    {
        public string RawWavName { get; set; }
        public string Uid { get; set; }
        public string PrefabName { get; set; }
        public double Dt { get; set; } = -1.0;
        public int NoteType { get; set; } = 1; // Default to normal note
        public string KeyAudio { get; set; }
        public string BossAction { get; set; }
        public string BossTransition { get; set; }
        public string BossName { get; set; }
        public int BossScene { get; set; } = -1;
    }

    public static class BmsWavParser
    {
        private static readonly Regex DtRegex = new Regex(@"_dt([0-9]+(?:\.[0-9]+)?)(?:\.wav)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UidRegex = new Regex(@"^([0-9]{6})", RegexOptions.Compiled);

        // 보스를 동반한 발사체/톱니 노트에서 파일명에 _dt 선언이 없을 때 쓰는 기본 선행 표시 시간.
        private const double BossProjectileDefaultDt = 0.7;

        // UID 앞 4자리 → NoteType (접두사 우선 매핑)
        private static readonly Dictionary<string, (int noteType, string keyAudio)> UidPrefixNoteType =
            new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase)
        {
            { "0002", (6, "sfx_hp") },    // HP / Heart
            { "0003", (7, "sfx_score") }, // Score Note
            { "0004", (9, null) },        // 씬 전환 토글: xx=04가 샌드백과 충돌하므로 prefix로 우선 분류
        };

        // UID xx(2~3번째 자리) → NoteType
        private static readonly Dictionary<string, int> XxNoteType =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "02", 3 }, // Hold / Long note
            { "03", 2 }, // Obstacle / Boss Gear
            { "04", 8 }, // Sandbag / Multi-hit
            { "09", 2 }, // Boss Gear
            { "17", 4 }, // Ghost
        };

        // xxyy → (BossAction, BossTransition?) — xx=01 보스 전환 테이블
        private static readonly Dictionary<string, (string action, string transition)> XxyyTransitionMap =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            { "0101", ("in",  "in") },
            { "0102", ("out", "out") },
            { "0107", ("boss_far_atk_1_start", null) },
            { "0108", ("boss_far_atk_1_end",   null) },
            { "0109", ("boss_far_atk_2_start", null) },
            { "0110", ("boss_far_atk_2_end",   null) },
            { "0113", ("multi_atk_48",          null) },
            { "0114", ("multi_atk_48_end",      null) },
        };

        // xxyy → BossAction — 보스 발사체/톱니 자동 매핑 테이블
        private static readonly Dictionary<string, string> XxyyProjectileAction =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "0601", "boss_far_atk_1_R" }, { "0902", "boss_far_atk_1_R" }, { "0903", "boss_far_atk_1_R" },
            { "0604", "boss_far_atk_1_L" }, { "0906", "boss_far_atk_1_L" },
            { "0701", "boss_far_atk_2" }, { "0704", "boss_far_atk_2" },
            { "0801", "boss_far_atk_2" }, { "0804", "boss_far_atk_2" },
            { "0908", "boss_far_atk_2" }, { "0909", "boss_far_atk_2" },
            { "0911", "boss_far_atk_2" }, { "0912", "boss_far_atk_2" },
        };

        public static BmsWavInfo ParseWavName(string wavName)
        {
            if (string.IsNullOrWhiteSpace(wavName))
            {
                return null;
            }

            var info = new BmsWavInfo { RawWavName = wavName };
            string nameWithoutExt = Path.GetFileNameWithoutExtension(wavName);

            // 1. Parse UID (6 digits at the start of filename, e.g., 051001)
            var uidMatch = UidRegex.Match(nameWithoutExt);
            if (uidMatch.Success)
            {
                info.Uid = uidMatch.Groups[1].Value;
                info.PrefabName = NormalizePrefabName(nameWithoutExt, info.Uid);
            }

            // 2. Parse dt (e.g. _dt0.7 or _dt1.2)
            var dtMatch = DtRegex.Match(nameWithoutExt);
            if (dtMatch.Success)
            {
                if (double.TryParse(dtMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedDt))
                {
                    info.Dt = Math.Round(parsedDt, 3, MidpointRounding.AwayFromZero);
                }

                info.PrefabName = NormalizePrefabName(nameWithoutExt, info.Uid);
            }

            // 3. Skeleton mapping for special gameplay notes & audios
            string lowerName = nameWithoutExt.ToLowerInvariant();

            // UID의 xx 자리(zzxxyy 구조)를 확인합니다
            bool typeResolvedFromUid = false;
            if (info.Uid != null && info.Uid.Length == 6)
            {
                string xx = info.Uid.Substring(2, 2);
                string xxyy = info.Uid.Substring(2, 4);
                string prefix4 = info.Uid.Substring(0, 4);

                if (UidPrefixNoteType.TryGetValue(prefix4, out var prefixEntry))
                {
                    info.NoteType = prefixEntry.noteType;
                    info.KeyAudio = prefixEntry.keyAudio;
                    typeResolvedFromUid = true;
                }
                else if (XxNoteType.TryGetValue(xx, out int xxType))
                {
                    info.NoteType = xxType;
                    typeResolvedFromUid = true;
                }

                if (XxyyTransitionMap.ContainsKey(xxyy))
                {
                    typeResolvedFromUid = true;
                }
                ApplyBossTransitionFromXxyy(info, xxyy);

                // 4. 보스 발사체 (xx=06/07/08, Type 1) 및 보스 톱니 (xx=09, Type 2) 처리
                if (xx == "06" || xx == "07" || xx == "08" || xx == "09")
                {
                    typeResolvedFromUid = true;
                }
                ApplyBossProjectileAction(info, lowerName, xx, xxyy);
            }

            // String-based pattern matching and overrides for fallbacks
            ApplyFallbackNoteType(info, lowerName, nameWithoutExt, typeResolvedFromUid);

            return info;
        }

        /// <summary>
        /// 보스 발사체(xx=06/07/08)의 속도 2·3 uid를 같은 레인의 속도 1 xxyy로 바꿉니다(yy 02·03 → 01, 05·06 → 04).
        /// 매핑 표는 속도 1 번호만 알아서, 속도 2·3 발사체에 _boss를 붙여도 보스 동작이 비어 보스가 멈춰 있었습니다
        /// (2026-10-23, Star Trace: 020702·040703 등). 공식 표에서도 속도를 바꾼 발사체의 boss_action이 다른 경우는
        /// 없었습니다(docs/experiments/NOTE_SPEED_DT.md 3절 확인 기록 4). 보스 톱니(xx=09)는 표가 속도별 번호를
        /// 따로 갖고 있어 그대로 둡니다.
        /// </summary>
        private static string SpeedOneProjectileXxyy(string xx, string xxyy)
        {
            if (xx != "06" && xx != "07" && xx != "08") return xxyy;
            if (!int.TryParse(xxyy.Substring(2, 2), out int yy) || yy < 1 || yy > 6) return xxyy;

            return xx + (yy <= 3 ? "01" : "04");
        }

        private static void ApplyBossProjectileAction(BmsWavInfo info, string lowerName, string xx, string xxyy)
        {
            bool isBossProjectile = xx == "06" || xx == "07" || xx == "08";
            bool isBossGear = xx == "09";
            if (!isBossProjectile && !isBossGear)
            {
                return;
            }

            if (lowerName.Contains("_boss") || lowerName.Contains("_atk"))
            {
                if (lowerName.Contains("boss_far_atk_1_r"))
                    info.BossAction = "boss_far_atk_1_R";
                else if (lowerName.Contains("boss_far_atk_1_l"))
                    info.BossAction = "boss_far_atk_1_L";
                else if (lowerName.Contains("boss_far_atk_2"))
                    info.BossAction = "boss_far_atk_2";
                else if (XxyyProjectileAction.TryGetValue(SpeedOneProjectileXxyy(xx, xxyy), out string mappedAction))
                    info.BossAction = mappedAction;

                // 보스의 사전 전조 애니메이션 시간을 확보하려고 dt를 0.7초로 당겨 주는 자리입니다.
                // 예전에는 파일명에 적은 _dt를 무조건 덮어써서, 실제 차트의 _boss 항목 66개에
                // 선언된 _dt0.8이 배치되는 순간 전부 0.7이 됐습니다. 선언이 없을 때만 기본값을 씁니다.
                if (info.Dt < 0.0)
                {
                    info.Dt = BossProjectileDefaultDt;
                }
            }
            else
            {
                info.BossAction = "";
            }
        }

        /// <summary>
        /// 파일명의 영문 키워드로 노트 타입을 추정합니다. UID로 타입이 정해지지 않은 경우에만 쓰는
        /// <b>폴백</b>입니다.
        ///
        /// <para><paramref name="typeResolvedFromUid"/>가 참이면 타입 추정 부분을 건너뜁니다.
        /// 예전에는 이 함수가 UID 매핑 뒤에 무조건 실행되어 <b>폴백이 아니라 override로</b>
        /// 동작했습니다. else-if 순서상 "note"가 xx=="02"(홀드)보다 먼저 걸리기 때문에,
        /// 예를 들어 <c>010201_hold_note_start.wav</c>가 홀드(3)가 아니라 음표(7)로 파싱됐습니다.
        /// 샌드백(8)·고스트(4)도 같은 방식으로 오염됐고, 타입이 7이 되면 BmsNoteMatcher의 짝
        /// 매칭에서도 빠지는데 "짝 없음" 경고조차 뜨지 않아 완전히 조용히 실패했습니다.
        /// (한글 파일명 차트는 영문 키워드에 걸리지 않아 우연히 피해 있었습니다.)</para>
        ///
        /// <para>보스 관련 분기(<c>boss_swap</c>/<c>boss_out</c>/<c>boss_in</c>)는 파일명에 명시적으로
        /// 적는 영문 표식이므로 UID 해석 여부와 무관하게 계속 존중합니다.</para>
        /// </summary>
        private static void ApplyFallbackNoteType(BmsWavInfo info, string lowerName, string nameWithoutExt, bool typeResolvedFromUid)
        {
            // 씬 전환 노트(0004xx)는 UID 중간 2자리가 "04"라 아래 샌드백 규칙(Substring(2,2)=="04")과
            // 충돌합니다. prefix "0004"를 가장 먼저 가로채 SceneToggle(type 9)로 고정하고 조기 반환합니다.
            if (info.Uid != null && info.Uid.StartsWith("0004"))
            {
                info.NoteType = 9; // SceneToggle (= NoteTypes.SceneToggle)
                return;
            }

            // 보스 표식은 UID 해석 여부와 무관하게 처리합니다.
            if (lowerName.Contains("boss_swap"))
            {
                info.NoteType = 0;
                info.PrefabName = "empty_000";
                info.BossAction = "swap:0401_boss:4"; // Skeleton default swap redirection
                return;
            }
            if (lowerName.Contains("boss_out"))
            {
                info.NoteType = 0;
                info.PrefabName = "empty_000";
                info.BossAction = "out";
                info.BossTransition = "out";
                return;
            }
            if (lowerName.Contains("boss_in"))
            {
                info.NoteType = 0;
                info.PrefabName = "empty_000";
                info.BossAction = "in";
                info.BossTransition = "in";
                ApplyBossTargetFromName(info, nameWithoutExt);
                return;
            }

            // 여기부터는 UID가 타입을 정하지 못했을 때만 쓰는 이름 기반 추정입니다.
            if (typeResolvedFromUid)
            {
                return;
            }

            if (lowerName.Contains("heart") || lowerName.Contains("hp") || (info.Uid != null && info.Uid.StartsWith("0002")))
            {
                info.NoteType = 6;
                info.KeyAudio = "sfx_hp";
            }
            else if (lowerName.Contains("score") || lowerName.Contains("note") || (info.Uid != null && info.Uid.StartsWith("0003")))
            {
                info.NoteType = 7;
                info.KeyAudio = "sfx_score";
            }
            else if (lowerName.Contains("sandbag") || (info.Uid != null && info.Uid.Substring(2, 2) == "04"))
            {
                info.NoteType = 8;
            }
            else if (lowerName.Contains("hold") || lowerName.Contains("long") || (info.Uid != null && info.Uid.Substring(2, 2) == "02"))
            {
                info.NoteType = 3;
            }
            else if (info.Uid != null && info.Uid.Substring(2, 2) == "17")
            {
                info.NoteType = 4; // Ghost
            }
        }

        private static void ApplyBossTargetFromName(BmsWavInfo info, string nameWithoutExt)
        {
            if (info == null || string.IsNullOrWhiteSpace(nameWithoutExt))
            {
                return;
            }

            var bossNameMatch = Regex.Match(nameWithoutExt, @"([0-9]{4}_boss)", RegexOptions.IgnoreCase);
            if (bossNameMatch.Success)
            {
                info.BossName = bossNameMatch.Groups[1].Value.ToLowerInvariant();
            }

            var sceneMatch = Regex.Match(nameWithoutExt, @"(?:scene|sc|s)([0-9]{1,2})", RegexOptions.IgnoreCase);
            if (sceneMatch.Success && int.TryParse(sceneMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int scene))
            {
                info.BossScene = scene;
            }

            if (string.IsNullOrWhiteSpace(info.BossName) && !string.IsNullOrWhiteSpace(info.Uid) && info.Uid.Length >= 2)
            {
                info.BossName = info.Uid.Substring(0, 2) + "01_boss";
            }

            if (info.BossScene < 0 && !string.IsNullOrWhiteSpace(info.Uid) && info.Uid.Length >= 2)
            {
                if (int.TryParse(info.Uid.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int uidScene))
                {
                    info.BossScene = uidScene;
                }
            }
        }

        private static string NormalizePrefabName(string nameWithoutExt, string uid)
        {
            if (string.IsNullOrWhiteSpace(nameWithoutExt))
            {
                return uid;
            }

            string prefabName = nameWithoutExt;
            int dtIdx = prefabName.IndexOf("_dt", StringComparison.OrdinalIgnoreCase);
            if (dtIdx > 0)
            {
                prefabName = prefabName.Substring(0, dtIdx);
            }

            if (string.IsNullOrWhiteSpace(uid))
            {
                return prefabName;
            }

            if (ContainsHumanLabel(prefabName))
            {
                return uid;
            }

            return prefabName;
        }

        private static bool ContainsHumanLabel(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            foreach (char ch in text)
            {
                if (ch > 127 || char.IsWhiteSpace(ch) || ch == '(' || ch == ')' || ch == '[' || ch == ']')
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyBossTransitionFromXxyy(BmsWavInfo info, string xxyy)
        {
            if (info == null || string.IsNullOrWhiteSpace(xxyy)) return;
            if (!XxyyTransitionMap.TryGetValue(xxyy, out var entry)) return;

            info.NoteType = 0;
            info.PrefabName = "empty_000";
            info.BossAction = entry.action;
            if (entry.transition != null)
                info.BossTransition = entry.transition;
            ApplyBossTargetFromUid(info);
        }

        private static void ApplyBossTargetFromUid(BmsWavInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Uid) || info.Uid.Length < 2)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(info.BossName))
            {
                info.BossName = info.Uid.Substring(0, 2) + "01_boss";
            }

            if (info.BossScene < 0 && int.TryParse(info.Uid.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int scene))
            {
                info.BossScene = scene;
            }
        }
    }
}
