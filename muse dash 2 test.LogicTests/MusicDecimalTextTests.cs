using System.Globalization;

namespace muse_dash_2_test.LogicTests
{
    // MusicDecimalText의 문화권 폴백 검증.
    //
    // 이 규칙은 "문자열을 만드는 쪽(IL2CPP 런타임)과 읽는 쪽(관리 런타임)의 문화권이 다를 수 있다"는
    // 사실에서 나왔습니다. 그래서 테스트는 실제 시스템 로케일에 기대지 않고 1순위 문화권을 주입합니다.
    // (테스트 프로젝트는 InvariantGlobalization=true라 new CultureInfo("de-DE")로는 쉼표 소수점을
    //  재현할 수 없습니다. NumberFormatInfo를 직접 만들면 그 설정과 무관하게 동작합니다.)
    public static class MusicDecimalTextTests
    {
        /// <summary>소수점이 쉼표인 문화권. 실제 로케일 없이 쉼표 표기를 재현합니다.</summary>
        private static NumberFormatInfo CommaDecimal()
        {
            return new NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = " ",
            };
        }

        public static void Parse_ReadsInvariantFormatUnderInvariantCulture()
        {
            Assert.Equal(15.5, MusicDecimalText.Parse("15.5", CultureInfo.InvariantCulture));
        }

        // 쉼표 로케일에서 게임이 "15,5"를 내놓는 경우. 1순위 문화권으로 바로 읽혀야 합니다.
        public static void Parse_ReadsCommaFormatUnderCommaCulture()
        {
            Assert.Equal(15.5, MusicDecimalText.Parse("15,5", CommaDecimal()));
        }

        // ★ 이 테스트가 규칙의 존재 이유입니다.
        // 문자열은 관리 런타임 문화권(쉼표)이 아니라 IL2CPP 쪽에서 점(.)으로 나왔는데,
        // 1순위 문화권만 쓰면 파싱에 실패해 0.0이 됩니다. InvariantCulture 재시도가 이를 건집니다.
        public static void Parse_FallsBackToInvariantWhenRuntimeCulturesDisagree()
        {
            double result = MusicDecimalText.Parse("15.5", CommaDecimal());
            Assert.Equal(15.5, result, "쉼표 문화권이 1순위여도 점 표기 문자열을 살려내야 합니다.");
        }

        // 반대 방향도 성립해야 합니다. Invariant가 1순위인데 문자열이 쉼표로 나온 경우는
        // 재시도로도 살릴 수 없으므로(둘 다 실패) 0.0으로 degrade하고 예외는 던지지 않습니다.
        public static void Parse_DegradesToZeroWhenBothCulturesFail()
        {
            Assert.Equal(0.0, MusicDecimalText.Parse("소수점아님", CultureInfo.InvariantCulture));
        }

        public static void Parse_ReturnsZeroForEmptyInput()
        {
            Assert.Equal(0.0, MusicDecimalText.Parse(null, CultureInfo.InvariantCulture));
            Assert.Equal(0.0, MusicDecimalText.Parse("", CultureInfo.InvariantCulture));
        }

        public static void Parse_HandlesNegativeAndIntegerValues()
        {
            Assert.Equal(-2.25, MusicDecimalText.Parse("-2.25", CultureInfo.InvariantCulture));
            Assert.Equal(7.0, MusicDecimalText.Parse("7", CultureInfo.InvariantCulture));
            Assert.Equal(-2.25, MusicDecimalText.Parse("-2,25", CommaDecimal()));
        }

        // 공개 오버로드(현재 문화권 사용)도 최소한 불변 표기는 읽어야 합니다.
        public static void Parse_PublicOverloadReadsPlainValue()
        {
            Assert.Equal(3.5, MusicDecimalText.Parse("3.5"));
        }
    }
}
