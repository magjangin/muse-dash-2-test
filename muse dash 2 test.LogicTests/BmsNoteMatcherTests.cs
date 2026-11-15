namespace muse_dash_2_test.LogicTests
{
    // BmsNoteMatcher: 채널별로 홀드(NoteType 3)/샌드백(NoteType 8) 시작-끝 노트를 짝짓는 로직 검증.
    // 홀드 WAV 이름은 실제 차트(hwa2.bms)의 010201 시작/끝 노트를 씁니다.
    public static class BmsNoteMatcherTests
    {
        private const string HoldWav = @"#WAV00C 1번 씬 wav폴더\010201_홀드 지상 시작 노트_dt1.47.wav";
        private const string HoldEndWav = @"#WAV00B 1번 씬 wav폴더\010201_홀드 지상 끝 노트_dt1.47.wav";

        public static void MatchSpecialNotes_PairsHoldStartAndEndOnSameChannel()
        {
            var chart = BmsParser.ParseText($"""
                #BPM 115
                {HoldWav}
                {HoldEndWav}
                #00113:00C00B
                """);

            var pairs = BmsNoteMatcher.MatchSpecialNotes(chart.Notes, chart);

            Assert.Equal(1, pairs.Count);
            Assert.Equal(BmsSpecialNoteType.Hold, pairs[0].Type);
            Assert.Equal(1.0f, pairs[0].StartNote.Tick);
            Assert.Equal(1.5f, pairs[0].EndNote.Tick);
            Assert.Equal(0.5f, pairs[0].LengthInTicks);
            Assert.Equal(BmsLane.Note, pairs[0].Lane);
        }

        public static void MatchSpecialNotes_PairsByOrderEvenWhenEndNoteComesFirst()
        {
            // 차트에서 "끝 노트"를 먼저 배치하면 파서는 그것을 시작으로 간주합니다.
            // 이름이 아니라 등장 순서로만 짝을 짓기 때문입니다.
            var chart = BmsParser.ParseText($"""
                #BPM 115
                {HoldWav}
                {HoldEndWav}
                #00113:00B00C
                """);

            var pairs = BmsNoteMatcher.MatchSpecialNotes(chart.Notes, chart);

            Assert.Equal(1, pairs.Count);
            Assert.Equal("00B", pairs[0].StartNote.RawValue);
            Assert.Equal("00C", pairs[0].EndNote.RawValue);
        }

        public static void MatchSpecialNotes_PairsSandbagStartAndEnd()
        {
            var chart = BmsParser.ParseText("""
                #BPM 115
                #WAV001 010401_샌드백.wav
                #00113:001000001
                """);

            var pairs = BmsNoteMatcher.MatchSpecialNotes(chart.Notes, chart);

            Assert.Equal(1, pairs.Count);
            Assert.Equal(BmsSpecialNoteType.Sandbag, pairs[0].Type);
        }

        public static void MatchSpecialNotes_DoesNotPairAcrossGroundAndAirChannels()
        {
            // 지상(13)과 공중(14)은 별개 채널이므로 서로 짝이 되면 안 됩니다.
            var chart = BmsParser.ParseText($"""
                #BPM 115
                {HoldWav}
                {HoldEndWav}
                #00113:00C
                #00114:00B
                """);

            Assert.Equal(2, chart.Notes.Count);
            Assert.Equal(0, BmsNoteMatcher.MatchSpecialNotes(chart.Notes, chart).Count);
        }

        public static void MatchSpecialNotes_LeavesTrailingUnpairedNoteUnmatched()
        {
            var chart = BmsParser.ParseText($"""
                #BPM 115
                {HoldWav}
                {HoldEndWav}
                #00113:00C00B00C
                """);

            var pairs = BmsNoteMatcher.MatchSpecialNotes(chart.Notes, chart);

            // 홀드 3개 → 앞의 두 개만 짝이 되고 마지막 하나는 짝 없이 남습니다.
            Assert.Equal(3, chart.Notes.Count);
            Assert.Equal(1, pairs.Count);
        }

        public static void MatchSpecialNotes_IgnoresNormalNotes()
        {
            var chart = BmsParser.ParseText("""
                #BPM 115
                #WAV001 1번 씬 wav폴더\011001_일반 노트1 지상 노멀_dt1.48.wav
                #00113:001000001
                """);

            Assert.Equal(0, BmsNoteMatcher.MatchSpecialNotes(chart.Notes, chart).Count);
        }

        public static void MatchSpecialNotes_ReturnsEmptyListForMissingInput()
        {
            var chart = BmsParser.ParseText("#BPM 115\n#00113:01");

            Assert.Equal(0, BmsNoteMatcher.MatchSpecialNotes(null, chart).Count);
            Assert.Equal(0, BmsNoteMatcher.MatchSpecialNotes(chart.Notes, null).Count);
            Assert.Equal(0, BmsNoteMatcher.MatchSpecialNotes(Array.Empty<BmsNote>(), chart).Count);
        }
    }
}
