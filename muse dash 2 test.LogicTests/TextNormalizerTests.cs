namespace muse_dash_2_test.LogicTests
{
    public static class TextNormalizerTests
    {
        public static void Normalize_FixesYouAreContractionWithMultipleSpaces()
        {
            Assert.Equal("you're", TextNormalizer.NormalizeEnglishContractions("you'  re"));
            Assert.Equal("You're", TextNormalizer.NormalizeEnglishContractions("You'   re"));
        }

        public static void Normalize_FixesSpacesAroundApostrophe()
        {
            Assert.Equal("you're", TextNormalizer.NormalizeEnglishContractions("you ' re"));
            Assert.Equal("you're", TextNormalizer.NormalizeEnglishContractions("you  '   re"));
        }

        public static void Normalize_FixesUnicodeApostrophes()
        {
            // Curly apostrophe (’ U+2019) and Fullwidth apostrophe (＇ U+FF07)
            Assert.Equal("you're", TextNormalizer.NormalizeEnglishContractions("you’  re"));
            Assert.Equal("it's", TextNormalizer.NormalizeEnglishContractions("it＇  s"));
            Assert.Equal("I'm", TextNormalizer.NormalizeEnglishContractions("I`  m"));
        }

        public static void Normalize_FixesNegativeContractions()
        {
            Assert.Equal("don't", TextNormalizer.NormalizeEnglishContractions("don'  t"));
            Assert.Equal("don't", TextNormalizer.NormalizeEnglishContractions("don ' t"));
            Assert.Equal("can't", TextNormalizer.NormalizeEnglishContractions("can '  t"));
            Assert.Equal("won't", TextNormalizer.NormalizeEnglishContractions("won ' t"));
            Assert.Equal("didn't", TextNormalizer.NormalizeEnglishContractions("didn '   t"));
            Assert.Equal("isn't", TextNormalizer.NormalizeEnglishContractions("isn'  t"));
        }

        public static void Normalize_FixesVariousPronounContractions()
        {
            Assert.Equal("it's", TextNormalizer.NormalizeEnglishContractions("it'  s"));
            Assert.Equal("I'm", TextNormalizer.NormalizeEnglishContractions("I'  m"));
            Assert.Equal("they've", TextNormalizer.NormalizeEnglishContractions("they '  ve"));
            Assert.Equal("we'll", TextNormalizer.NormalizeEnglishContractions("we '  ll"));
            Assert.Equal("he'd", TextNormalizer.NormalizeEnglishContractions("he '  d"));
            Assert.Equal("Let's", TextNormalizer.NormalizeEnglishContractions("Let '  s"));
        }

        public static void Normalize_FixesUpperCaseContractions()
        {
            Assert.Equal("YOU'RE", TextNormalizer.NormalizeEnglishContractions("YOU ' RE"));
            Assert.Equal("DON'T", TextNormalizer.NormalizeEnglishContractions("DON ' T"));
            Assert.Equal("IT'S", TextNormalizer.NormalizeEnglishContractions("IT ' S"));
            Assert.Equal("WE'LL", TextNormalizer.NormalizeEnglishContractions("WE ' LL"));
            Assert.Equal("THEY'VE", TextNormalizer.NormalizeEnglishContractions("THEY ' VE"));
        }

        public static void Normalize_PreservesAlreadyCorrectContractions()
        {
            Assert.Equal("you're not here", TextNormalizer.NormalizeEnglishContractions("you're not here"));
            Assert.Equal("don't stop the music", TextNormalizer.NormalizeEnglishContractions("don't stop the music"));
            Assert.Equal("it's fine", TextNormalizer.NormalizeEnglishContractions("it's fine"));
        }

        public static void Normalize_FastPathWithoutApostropheReturnsExactInstance()
        {
            string plainText = "070101_road_nor_1.wav";
            string result = TextNormalizer.NormalizeEnglishContractions(plainText);
            Assert.True(object.ReferenceEquals(plainText, result));
        }

        public static void Normalize_PreservesQuotedWords()
        {
            Assert.Equal("'Rock' and 'Roll'", TextNormalizer.NormalizeEnglishContractions("'Rock' and 'Roll'"));
        }

        public static void Normalize_HandlesNullAndEmpty()
        {
            Assert.Null(TextNormalizer.NormalizeEnglishContractions(null));
            Assert.Equal("", TextNormalizer.NormalizeEnglishContractions(""));
        }

        public static void Normalize_PreservesKoreanAndMixedText()
        {
            string mixed = "너는 you'  re 최고야 (don ' t forget)";
            string expected = "너는 you're 최고야 (don't forget)";
            Assert.Equal(expected, TextNormalizer.NormalizeEnglishContractions(mixed));
        }

        public static void BmsParser_NormalizesTitleAndArtistContractions()
        {
            string bmsText = "#TITLE you'  re my star\n#ARTIST don ' t cry\n#BPM 120\n#00111:01";
            var chart = BmsParser.ParseText(bmsText);
            Assert.Equal("you're my star", chart.Title);
            Assert.Equal("don't cry", chart.Artist);
        }
    }
}
