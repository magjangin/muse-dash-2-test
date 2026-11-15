namespace muse_dash_2_test
{
    /// <summary>
    /// 게임의 히든 해금 데이터(<c>hideMusic</c> 설정의 <c>invokeType</c> / <c>diffIndex</c>)를
    /// 곡 선택창에 띄울 한 줄 문구로 바꾸는 순수 규칙입니다. 게임에 의존하지 않으므로
    /// 로직 테스트(<c>HiddenUnlockTextTests</c>)로 검증합니다.
    ///
    /// <para><b>데이터에 있는 것만 적습니다.</b> 게임 6.7.0의 <c>hideMusic</c>은 119곡이고 분포는
    /// 여러 번 누르기 88 / 길게 누르기 16 / 기타 14 / 난이도 전환 1, 대상은 Master 104 / Hard 6 /
    /// 난이도 버튼 아님(-1) 9입니다(2026-09-27, 에셋 번들에서 직접 추출). 정확한 횟수나 "기타"의 내용은
    /// 데이터에 없고 게임 로직 안에 있으므로 지어내지 않습니다. 공식 힌트(<c>hideMusicTip</c>)는
    /// 수수께끼형 문구라 쓰지 않기로 했습니다.</para>
    /// </summary>
    public static class HiddenUnlockText
    {
        /// <summary>게임 enum <c>HideMapInvokeType</c>의 값입니다(None=0, LongPress=1, DifficultySwitch=2, MultiClick=3, Other=4).</summary>
        internal const int InvokeNone = 0;
        internal const int InvokeLongPress = 1;
        internal const int InvokeDifficultySwitch = 2;
        internal const int InvokeMultiClick = 3;
        internal const int InvokeOther = 4;

        private const string Prefix = "히든 해금: ";

        /// <summary>
        /// 곡 하나의 해금 조건 문구를 만듭니다. 히든이 없는 곡(<see cref="InvokeNone"/>)은 null을 돌려줍니다.
        /// 게임 업데이트로 모르는 방식 값이 생겨도 숨기지 않고 번호를 그대로 보여 줍니다.
        /// </summary>
        public static string Describe(int invokeType, int diffIndex)
        {
            string button = DescribeButton(diffIndex);

            switch (invokeType)
            {
                case InvokeNone:
                    return null;
                case InvokeMultiClick:
                    return Prefix + (button != null ? $"{button} 버튼 여러 번 연타" : "여러 번 연타 (난이도 버튼 아님)");
                case InvokeLongPress:
                    return Prefix + (button != null ? $"{button} 버튼 길게 누르기" : "길게 누르기 (난이도 버튼 아님)");
                case InvokeDifficultySwitch:
                    return Prefix + (button != null ? $"난이도 전환 조작 ({button} 버튼)" : "난이도 전환 조작");
                case InvokeOther:
                    return Prefix + "특수 조건 (곡마다 다름)";
                default:
                    return Prefix + $"알 수 없는 방식 (invokeType={invokeType})";
            }
        }

        /// <summary>
        /// 게임의 난이도 번호(<c>MusicInfo.difficulty1~5</c>의 숫자)를 버튼 이름으로 바꿉니다.
        /// 0 이하(데이터상 -1)는 난이도 버튼이 대상이 아니라는 뜻이라 null입니다.
        /// </summary>
        internal static string DescribeButton(int diffIndex)
        {
            switch (diffIndex)
            {
                case 1: return "Easy";
                case 2: return "Hard";
                case 3: return "Master";
                default: return diffIndex > 0 ? $"{diffIndex}번 난이도" : null;
            }
        }
    }
}
