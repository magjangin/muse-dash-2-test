namespace muse_dash_2_test.LogicTests
{
    public static class GameVersionCheckTests
    {
        public static void Evaluate_SameVersion_IsMatch()
        {
            Assert.Equal(GameVersionCheck.Result.Match, GameVersionCheck.Evaluate("6.7.0", "6.7.0"));
            Assert.Equal(GameVersionCheck.Result.Match, GameVersionCheck.Evaluate(" 6.7.0 ", "6.7.0"));
        }

        public static void Evaluate_MissingTrailingZero_IsMatch()
        {
            Assert.Equal(GameVersionCheck.Result.Match, GameVersionCheck.Evaluate("6.7", "6.7.0"));
        }

        public static void Evaluate_NewerVersion_IsNewer()
        {
            Assert.Equal(GameVersionCheck.Result.Newer, GameVersionCheck.Evaluate("6.8.0", "6.7.0"));
            // 문자열 비교였다면 "6.10.0" < "6.7.0"으로 틀립니다.
            Assert.Equal(GameVersionCheck.Result.Newer, GameVersionCheck.Evaluate("6.10.0", "6.7.0"));
        }

        public static void Evaluate_OlderVersion_IsOlder()
        {
            Assert.Equal(GameVersionCheck.Result.Older, GameVersionCheck.Evaluate("6.6.2", "6.7.0"));
        }

        public static void Evaluate_EmptyOrNull_IsUnknown()
        {
            Assert.Equal(GameVersionCheck.Result.Unknown, GameVersionCheck.Evaluate(null, "6.7.0"));
            Assert.Equal(GameVersionCheck.Result.Unknown, GameVersionCheck.Evaluate("  ", "6.7.0"));
        }

        public static void Evaluate_NonNumeric_IsDifferent()
        {
            Assert.Equal(GameVersionCheck.Result.Different, GameVersionCheck.Evaluate("6.7.0b", "6.7.0"));
        }

        public static void Describe_Match_ReturnsNull()
        {
            Assert.Null(GameVersionCheck.Describe(GameVersionCheck.Result.Match, "6.7.0", "6.7.0"));
        }

        public static void Describe_Newer_MentionsBothVersions()
        {
            string text = GameVersionCheck.Describe(GameVersionCheck.Result.Newer, "6.8.0", "6.7.0");
            Assert.True(text.Contains("6.8.0") && text.Contains("6.7.0"), text);
        }

        public static void VerifiedVersion_IsNumeric()
        {
            Assert.True(GameVersionCheck.TryParseNumericVersion(GameVersionCheck.VerifiedGameVersion, out _),
                "VerifiedGameVersion은 숫자 버전이어야 비교가 됩니다.");
        }
    }
}
