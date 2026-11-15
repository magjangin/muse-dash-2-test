using System.Text.RegularExpressions;

namespace muse_dash_2_test
{
    /// <summary>
    /// 외부 BMS 차트 메타데이터(#TITLE, #ARTIST)나 사용자 작성 info.txt에서 발생하기 쉬운
    /// 영문 축약형의 비정상적 공백 분리(예: "you'  re", "don ' t", "it ’ s") 및
    /// 유니코드 특수 따옴표를 표준 영문 축약형으로 자동 교정하는 유틸리티입니다.
    /// </summary>
    public static class TextNormalizer
    {
        // 따옴표 및 변형 문자 집합 (고속 fast-path 검사용)
        private static readonly char[] ApostropheChars = { '\'', '’', '＇', '`', '´' };

        // 1. 일반 축약형 접미사: 're, 've, 'll, 'd, 'm, 's
        // 패턴: 단어 + 0개 이상 공백 + 따옴표 계열 + 0개 이상 공백 + 축약어 접미사
        private static readonly Regex ContractionSuffixRegex = new Regex(
            @"\b([A-Za-z]+)\s*['’＇`´]\s*(re|ve|ll|d|m|s)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 2. 부정 축약형 n't (don't, can't, won't, didn't, isn't, aren't 등):
        // 패턴: n으로 끝나는 단어 + 0개 이상 공백 + 따옴표 계열 + 0개 이상 공백 + t/T
        private static readonly Regex NegativeContractionRegex = new Regex(
            @"\b([A-Za-z]+?n)\s*['’＇`´]\s*(t)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 텍스트 내 영문 축약형의 비정상적 공백 분리(예: "you'   re" -> "you're", "don ' t" -> "don't")를
        /// 올바른 표준 축약형으로 자동 교정합니다.
        /// 따옴표 문자가 전혀 포함되지 않은 텍스트는 즉시 반환하여 정규식 평가 및 문자열 할당을 차단합니다.
        /// </summary>
        public static string NormalizeEnglishContractions(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 따옴표 계열 문자가 아예 없으면 정규식 평가를 건너뛰고 원본 반환 (BMS 수백 라인 메타데이터 파싱 병목 및 GC 방지)
            if (text.IndexOfAny(ApostropheChars) < 0) return text;

            // 1) n't 부정 축약형 교정 (don '  t -> don't, can ' t -> can't, DON ' T -> DON'T)
            string result = NegativeContractionRegex.Replace(text, "$1'$2");

            // 2) 일반 축약형 교정 (you '  re -> you're, it ’  s -> it's, I ` m -> I'm, YOU ' RE -> YOU'RE)
            result = ContractionSuffixRegex.Replace(result, "$1'$2");

            return result;
        }
    }
}
