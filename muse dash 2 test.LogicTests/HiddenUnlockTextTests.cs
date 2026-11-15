namespace muse_dash_2_test.LogicTests
{
    // HiddenUnlockText 검증. 입력 조합은 게임 6.7.0 hideMusic 설정(119곡)에서 실제로 나온 것만 씁니다:
    //   invokeType × diffIndex = {1,2,3,4} × {-1,2,3}  (2026-09-27, 에셋 번들 추출)
    public static class HiddenUnlockTextTests
    {
        // 88곡이 이 형태입니다(대부분 Master).
        public static void Describe_MultiClickOnMaster()
        {
            Assert.Equal("히든 해금: Master 버튼 여러 번 연타", HiddenUnlockText.Describe(3, 3));
        }

        public static void Describe_LongPressOnHard()
        {
            Assert.Equal("히든 해금: Hard 버튼 길게 누르기", HiddenUnlockText.Describe(1, 2));
        }

        // diffIndex -1(9곡)은 난이도 버튼이 대상이 아닙니다. 없는 버튼 이름을 붙이면 안 됩니다.
        public static void Describe_NonDifficultyTargetSaysSo()
        {
            string text = HiddenUnlockText.Describe(1, -1);
            Assert.Equal("히든 해금: 길게 누르기 (난이도 버튼 아님)", text);
            Assert.False(text.Contains("Master"), "대상이 난이도 버튼이 아닌데 버튼 이름이 붙었습니다.");
        }

        // "기타"는 데이터에 내용이 없습니다. 난이도가 있어도 구체적인 조작을 지어내지 않습니다.
        public static void Describe_OtherDoesNotInventDetails()
        {
            Assert.Equal("히든 해금: 특수 조건 (곡마다 다름)", HiddenUnlockText.Describe(4, 3));
            Assert.Equal("히든 해금: 특수 조건 (곡마다 다름)", HiddenUnlockText.Describe(4, -1));
        }

        public static void Describe_DifficultySwitchNamesTheButton()
        {
            Assert.Equal("히든 해금: 난이도 전환 조작 (Master 버튼)", HiddenUnlockText.Describe(2, 3));
        }

        // HideMapInvokeType.None은 히든이 없다는 뜻이라 아무것도 띄우지 않습니다.
        public static void Describe_NoneShowsNothing()
        {
            Assert.Null(HiddenUnlockText.Describe(0, 3));
        }

        // 게임 업데이트로 새 방식이 생겨도 조용히 사라지지 않고 번호를 보여 줍니다.
        public static void Describe_UnknownInvokeTypeStillShows()
        {
            Assert.Equal("히든 해금: 알 수 없는 방식 (invokeType=9)", HiddenUnlockText.Describe(9, 3));
        }

        // 실제 데이터에 나온 조합은 전부 문구가 나와야 합니다.
        public static void Describe_EveryCombinationInGameDataProducesText()
        {
            foreach (int invokeType in new[] { 1, 2, 3, 4 })
            {
                foreach (int diffIndex in new[] { -1, 2, 3 })
                {
                    string text = HiddenUnlockText.Describe(invokeType, diffIndex);
                    Assert.NotNull(text, $"invokeType={invokeType}, diffIndex={diffIndex}");
                    Assert.True(text.StartsWith("히든 해금: "), text);
                }
            }
        }

        public static void DescribeButton_MapsGameDifficultyNumbers()
        {
            Assert.Equal("Easy", HiddenUnlockText.DescribeButton(1));
            Assert.Equal("Hard", HiddenUnlockText.DescribeButton(2));
            Assert.Equal("Master", HiddenUnlockText.DescribeButton(3));
            Assert.Equal("5번 난이도", HiddenUnlockText.DescribeButton(5));
            Assert.Null(HiddenUnlockText.DescribeButton(-1));
            Assert.Null(HiddenUnlockText.DescribeButton(0));
        }
    }
}
