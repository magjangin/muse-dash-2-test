using System;

namespace muse_dash_2_test
{
    /// <summary>
    /// 지금 실행 중인 게임 버전이 이 모드를 실기로 검증한 버전과 같은지 시작할 때 한 번 판정합니다.
    ///
    /// <para><b>왜 필요한가</b>: 모드의 훅·필드 오프셋·dt 표·현지화 행 번호 같은 사실은 전부 게임 6.7.0에서
    /// 실측한 값입니다. 게임이 업데이트되면 <see cref="PatchInstaller"/>와 <c>PatchHealthCheck</c>가
    /// "대상 메서드가 사라졌는지"는 잡지만, <b>메서드는 그대로인데 동작이 바뀐 경우</b>(예: 6.7.0에서
    /// <c>BGM_OneShot</c> 소스가 새로 생겨 배틀 BGM이 겹친 사고, CHECKLIST 참고)는 아무 경고도 남지 않습니다.
    /// 그래서 버전이 다르면 로그 맨 앞쪽에 한 줄 경고를 남겨, 이상 동작을 볼 때 업데이트부터 의심하게 합니다.
    /// 판정만 하고 기능은 끄지 않습니다(작은 패치 업데이트에서도 대부분의 기능은 그대로 동작합니다).</para>
    ///
    /// <para>순수 로직(<see cref="Evaluate"/>)은 Unity 없이 LogicTests에서 검사합니다.</para>
    /// </summary>
    public static class GameVersionCheck
    {
        /// <summary>이 모드를 실기로 검증한 게임 버전입니다. 새 게임 버전에서 실기 검증을 마치면 함께 올리십시오.
        /// 2편은 아직 실기 검증한 버전이 없어 "0.0.0"으로 둡니다(어떤 버전이든 "검증 버전과 다름" 경고가 나도록).
        /// 위 설명의 6.7.0 사례는 1편 기록입니다. 출시 후 2편 버전을 확인하면 여기를 채우십시오.</summary>
        public const string VerifiedGameVersion = "0.0.0";

        public enum Result
        {
            /// <summary>검증한 버전과 같습니다.</summary>
            Match,
            /// <summary>버전 문자열을 읽지 못했습니다(빈 값).</summary>
            Unknown,
            /// <summary>검증한 버전보다 새 버전입니다(게임 업데이트).</summary>
            Newer,
            /// <summary>검증한 버전보다 옛 버전입니다(다운그레이드된 사본 등).</summary>
            Older,
            /// <summary>숫자 버전으로 비교할 수 없는 다른 문자열입니다.</summary>
            Different,
        }

        /// <summary>게임 버전 문자열을 검증 버전과 비교합니다. 앞뒤 공백은 무시합니다.</summary>
        public static Result Evaluate(string runtimeVersion, string verifiedVersion = VerifiedGameVersion)
        {
            string runtime = runtimeVersion?.Trim();
            if (string.IsNullOrEmpty(runtime)) return Result.Unknown;

            string verified = (verifiedVersion ?? string.Empty).Trim();
            if (string.Equals(runtime, verified, StringComparison.OrdinalIgnoreCase)) return Result.Match;

            if (TryParseNumericVersion(runtime, out Version runtimeParsed)
                && TryParseNumericVersion(verified, out Version verifiedParsed))
            {
                int cmp = runtimeParsed.CompareTo(verifiedParsed);
                if (cmp == 0) return Result.Match;
                return cmp > 0 ? Result.Newer : Result.Older;
            }

            return Result.Different;
        }

        /// <summary>
        /// "6.7", "6.7.0", "6.7.0.1" 같은 숫자 버전을 읽습니다. 빠진 자리는 0으로 채워 "6.7"과 "6.7.0"을 같게 봅니다.
        /// </summary>
        internal static bool TryParseNumericVersion(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string[] parts = text.Trim().Split('.');
            if (parts.Length < 1 || parts.Length > 4) return false;

            var numbers = new int[4];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out numbers[i]))
                {
                    return false;
                }
            }

            version = new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
            return true;
        }

        /// <summary>판정 결과를 사람이 읽는 한 줄 설명으로 바꿉니다. null이면 남길 말이 없다는 뜻입니다.</summary>
        public static string Describe(Result result, string runtimeVersion, string verifiedVersion = VerifiedGameVersion)
        {
            switch (result)
            {
                case Result.Match:
                    return null;
                case Result.Unknown:
                    return $"게임 버전을 읽지 못했습니다. 이 모드는 {verifiedVersion}에서 검증되었습니다.";
                case Result.Newer:
                    return $"게임이 {runtimeVersion}으로 업데이트되었습니다. 이 모드는 {verifiedVersion}에서 검증되었습니다. " +
                           "이상 동작을 보면 [PatchInstaller]/[PatchHealth] 경고와 docs/guides/CHECKLIST.md의 '게임 업데이트 후' 항목부터 확인하십시오.";
                case Result.Older:
                    return $"게임 버전 {runtimeVersion}은(는) 검증 버전 {verifiedVersion}보다 옛 버전입니다. 없는 메서드를 쓰는 기능은 꺼질 수 있습니다.";
                default:
                    return $"게임 버전 '{runtimeVersion}'을(를) 검증 버전 {verifiedVersion}과 비교할 수 없습니다.";
            }
        }
    }
}
