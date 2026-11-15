namespace muse_dash_2_test.LogicTests
{
    // 임시 파일 판정은 "잘못 걸러내면 채보가 아무 말 없이 사라지는" 규칙이라,
    // 정상 파일이 통과하는지를 특히 촘촘히 확인합니다.
    public static class BmsFileNamesTests
    {
        public static void IsTempFile_TreatsEditorPrefixesAsTemp()
        {
            Assert.True(BmsFileNames.IsTempFile("~chart.bms"));
            Assert.True(BmsFileNames.IsTempFile("~$chart.bms"));
            Assert.True(BmsFileNames.IsTempFile("___chart.bms"));
            Assert.True(BmsFileNames.IsTempFile(".chart.bms"));
        }

        public static void IsTempFile_TreatsBackupSuffixAsTemp()
        {
            Assert.True(BmsFileNames.IsTempFile("chart.bms~"));
        }

        public static void IsTempFile_TreatsTempWordAsTemp()
        {
            Assert.True(BmsFileNames.IsTempFile("temp.bms"));
            Assert.True(BmsFileNames.IsTempFile("tmp.bms"));
            Assert.True(BmsFileNames.IsTempFile("TEMP.bms"));
            Assert.True(BmsFileNames.IsTempFile("temp_1.bms"));
            Assert.True(BmsFileNames.IsTempFile("tmp-backup.bms"));
            Assert.True(BmsFileNames.IsTempFile("temp.old.bms"));
        }

        // 예전 규칙(부분 일치)에서 조용히 사라지던 정상 채보들입니다.
        public static void IsTempFile_KeepsNormalChartsThatMerelyContainTempSubstring()
        {
            Assert.False(BmsFileNames.IsTempFile("tempo.bms"));
            Assert.False(BmsFileNames.IsTempFile("attempt.bms"));
            Assert.False(BmsFileNames.IsTempFile("Tempest.bms"));
            Assert.False(BmsFileNames.IsTempFile("contempt.bms"));
            Assert.False(BmsFileNames.IsTempFile("tmpfile.bms"));
        }

        public static void IsTempFile_KeepsOrdinaryChartNames()
        {
            Assert.False(BmsFileNames.IsTempFile("chart.bms"));
            Assert.False(BmsFileNames.IsTempFile("main.bms"));
            Assert.False(BmsFileNames.IsTempFile("아기상어.bms"));
            Assert.False(BmsFileNames.IsTempFile(@"H:\muse dash hwa\hwa\아기상어\hwa2.bms"));
        }

        public static void IsTempFile_HonorsFullPathsNotJustFileNames()
        {
            // 폴더 이름에 temp가 들어가도 파일 자체는 정상이어야 합니다.
            Assert.False(BmsFileNames.IsTempFile(@"C:\temp\chart.bms"));
            Assert.True(BmsFileNames.IsTempFile(@"C:\songs\~chart.bms"));
        }

        public static void IsTempFile_TreatsBlankPathAsTemp()
        {
            Assert.True(BmsFileNames.IsTempFile(null));
            Assert.True(BmsFileNames.IsTempFile(""));
            Assert.True(BmsFileNames.IsTempFile("   "));
        }
    }
}
