namespace muse_dash_2_test.LogicTests
{
    // BmsBossSwapPlanner: 노트 → WAV 정보 해석/캐싱, out→in 보스 교체 이벤트 조립 검증.
    public static class BmsBossSwapPlannerTests
    {
        public static void ResolveWavInfo_ReadsWavMetadataAndCachesPerRawValue()
        {
            var chart = BmsParser.ParseText("""
                #BPM 120
                #WAV01 010101_보스 등장.wav
                #00115:01
                """);

            var note = chart.Notes[0];
            var first = BmsBossSwapPlanner.ResolveWavInfo(chart, note);
            var second = BmsBossSwapPlanner.ResolveWavInfo(chart, note);

            Assert.NotNull(first);
            Assert.Equal("010101", first.Uid);
            Assert.Equal("in", first.BossTransition);
            Assert.Same(first, second, "같은 RawValue는 캐시된 인스턴스를 재사용해야 합니다.");
        }

        public static void ResolveWavInfo_FallsBackToRawValueAsFileName()
        {
            var chart = BmsParser.ParseText("""
                #BPM 120
                #00115:01
                """);

            var info = BmsBossSwapPlanner.ResolveWavInfo(chart, chart.Notes[0]);

            Assert.NotNull(info);
            Assert.Equal("01.wav", info.RawWavName);
            Assert.Null(info.Uid);
        }

        public static void ResolveWavInfo_ReturnsNullForMissingArguments()
        {
            var chart = BmsParser.ParseText("#BPM 120\n#00115:01");

            Assert.Null(BmsBossSwapPlanner.ResolveWavInfo(null, chart.Notes[0]));
            Assert.Null(BmsBossSwapPlanner.ResolveWavInfo(chart, null));
        }

        public static void BuildSwapAction_RequiresBossNameAndScene()
        {
            Assert.Equal(string.Empty, BmsBossSwapPlanner.BuildSwapAction(null));
            Assert.Equal(string.Empty, BmsBossSwapPlanner.BuildSwapAction(new BmsWavInfo { BossScene = 4 }));
            Assert.Equal(string.Empty, BmsBossSwapPlanner.BuildSwapAction(new BmsWavInfo { BossName = "0401_boss" }));
            Assert.Equal("swap:0401_boss:4", BmsBossSwapPlanner.BuildSwapAction(new BmsWavInfo { BossName = "0401_boss", BossScene = 4 }));
        }

        public static void BuildSwapEvents_PairsOutNoteWithFollowingInNote()
        {
            var chart = BmsParser.ParseText("""
                #BPM 120
                #WAV01 010102_보스 퇴장.wav
                #WAV02 040101_보스 등장.wav
                #00115:0100
                #00215:0200
                """);

            var events = BmsBossSwapPlanner.BuildSwapEvents(chart);

            Assert.Equal(1, events.Count);
            Assert.Equal("swap:0401_boss:4", events[0].BossAction);
            Assert.Equal("out", events[0].OutWav.BossTransition);
            Assert.Equal("in", events[0].InWav.BossTransition);

            // 1틱(=2초)과 2틱(=4초) 사이 간격
            Assert.Equal(2.0f, events[0].DelaySeconds);
        }

        public static void BuildSwapEvents_IgnoresInNoteWithoutPrecedingOutNote()
        {
            var chart = BmsParser.ParseText("""
                #BPM 120
                #WAV01 040101_보스 등장.wav
                #00115:01
                """);

            Assert.Equal(0, BmsBossSwapPlanner.BuildSwapEvents(chart).Count);
        }

        public static void BuildSwapEvents_ReturnsEmptyListForEmptyChart()
        {
            Assert.Equal(0, BmsBossSwapPlanner.BuildSwapEvents(null).Count);
            Assert.Equal(0, BmsBossSwapPlanner.BuildSwapEvents(BmsParser.ParseText("#BPM 120")).Count);
        }
    }
}
