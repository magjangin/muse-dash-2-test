namespace muse_dash_2_test.LogicTests
{
    // 실제 차트(H:\muse dash hwa\hwa\아기상어\hwa2.bms)와 같은 형태를 축소해서 통째로 통과시키는 테스트입니다.
    // 실제 차트의 특징을 그대로 재현합니다:
    //   - BPM 고정(#BPM 115), 채널 13/14/15/18만 사용
    //   - 3자리 base-36 WAV 키(001 ~ 0HS), 즉 G~Z가 들어가는 키/셀
    //   - WAV 값에 한글 폴더 경로가 붙음
    //   - 값이 빈 헤더(#GENRE), 노트가 아닌 BGM 항목(music.ogg)
    public static class RealChartTests
    {
        // 115 BPM에서 1마디(=1틱) 길이
        private const float MeasureSeconds = 4f * (60f / 115f);

        private const string Chart = """
            #TITLE test
            #ARTIST 화영왕
            #GENRE
            #BPM 115
            #PLAYER 2

            #WAV001 10번 씬 wav폴더\000201_하트 지상_dt1.48.wav
            #WAV003 10번 씬 wav폴더\000301_음표 지상_dt1.48.wav
            #WAV006 1번 씬 wav폴더\010102_1번 씬 보스 퇴장_dt0.wav
            #WAV00B 1번 씬 wav폴더\010201_홀드 지상 끝 노트_dt1.47.wav
            #WAV00C 1번 씬 wav폴더\010201_홀드 지상 시작 노트_dt1.47.wav
            #WAV00H 1번 씬 wav폴더\010601_보스 발사체1 보스 액션 사용 지상_boss_dt0.8.wav
            #WAV00I 1번 씬 wav폴더\010601_보스 발사체1 보스 없이 지상_dt0.8.wav
            #WAV00T 1번 씬 wav폴더\011001_일반 노트1 지상 노멀_dt1.48.wav
            #WAV01J 1번 씬 wav폴더\011701_고스트 지상_dt1.48.wav
            #WAV0B8 8번 씬 wav폴더\080101_8번 씬 보스 등장_dt0.wav
            #WAV0HG 씬 전환 wav 폴더\000401_1번 씬 전환_dt0.wav
            #WAV0HS music.ogg

            #00113:00T00001J
            #00114:00T00T
            #00213:00C00B
            #00315:006000
            #00415:0B8000
            #00518:0HG
            #00613:00H00I
            #00713:001003
            """;

        private static BmsChart Parse() => BmsParser.ParseText(Chart);

        public static void RealChart_ParsesHeaderWithFixedBpm()
        {
            var chart = Parse();

            Assert.Equal("test", chart.Title);
            Assert.Equal("화영왕", chart.Artist);
            Assert.Equal(115f, chart.DefaultBpm);

            // 값이 비어 있는 헤더는 조용히 무시됩니다.
            Assert.False(chart.Metadata.ContainsKey("GENRE"));

            // BPM 변화가 없는 차트이므로 기본 BPM 하나만 남습니다.
            Assert.Equal(1, chart.BpmChanges.Count);
            Assert.Equal(115f, chart.BpmChanges[0].Bpm);
        }

        public static void RealChart_ReadsBase36WavKeysAsThreeCharacterCells()
        {
            var chart = Parse();

            // WAV 키가 3자리이므로 노트 데이터도 3자리 단위입니다.
            // "01J" / "0HG" / "0B8" 처럼 16진 범위를 넘는 base-36 셀도 그대로 다뤄져야 합니다.
            Assert.Equal(13, chart.Notes.Count);

            var ghost = chart.Notes.First(n => n.RawValue == "01J");
            Assert.Equal(0x13, ghost.Channel);
            Assert.Equal(1f + (2f / 3f), ghost.Tick);

            Assert.Equal(1, chart.Notes.Count(n => n.RawValue == "0HG"));
            Assert.Equal(1, chart.Notes.Count(n => n.RawValue == "0B8"));
        }

        public static void RealChart_MapsChannelsToLanes()
        {
            var chart = Parse();

            // 13/14는 둘 다 Note 레인입니다(지상/공중 구분은 UID의 yy가 담당).
            Assert.Equal(BmsLane.Note, chart.Notes.First(n => n.Channel == 0x13).Lane);
            Assert.Equal(BmsLane.Note, chart.Notes.First(n => n.Channel == 0x14).Lane);
            Assert.Equal(BmsLane.BossInOut, chart.Notes.First(n => n.Channel == 0x15).Lane);
            Assert.Equal(BmsLane.BossAction, chart.Notes.First(n => n.Channel == 0x18).Lane);
        }

        public static void RealChart_ComputesNoteTimesFromFixedBpm()
        {
            var chart = Parse();

            var first = chart.Notes[0];
            Assert.Equal(1.0f, first.Tick);
            Assert.Equal(MeasureSeconds, first.Time);

            var last = chart.Notes[chart.Notes.Count - 1];
            Assert.Equal(7.5f, last.Tick);
            Assert.Equal(7.5f * MeasureSeconds, last.Time);
        }

        public static void RealChart_ResolvesEveryNoteToAWavInfoWithUid()
        {
            var chart = Parse();

            foreach (var note in chart.Notes)
            {
                var info = BmsBossSwapPlanner.ResolveWavInfo(chart, note);
                Assert.NotNull(info, $"셀 '{note.RawValue}'의 WAV 정보를 찾지 못했습니다.");
                Assert.NotNull(info.Uid, $"셀 '{note.RawValue}'({info.RawWavName})에서 UID를 못 읽었습니다.");
            }
        }

        public static void RealChart_ClassifiesNoteTypes()
        {
            var chart = Parse();
            var counts = new SortedDictionary<int, int>();

            foreach (var note in chart.Notes)
            {
                int type = BmsBossSwapPlanner.ResolveWavInfo(chart, note).NoteType;
                counts[type] = counts.TryGetValue(type, out int existing) ? existing + 1 : 1;
            }

            Assert.Equal(2, counts[0]);  // 보스 퇴장 / 보스 등장
            Assert.Equal(5, counts[1]);  // 일반 노트 3 + 발사체 2
            Assert.Equal(2, counts[3]);  // 홀드 시작/끝
            Assert.Equal(1, counts[4]);  // 고스트
            Assert.Equal(1, counts[6]);  // 하트
            Assert.Equal(1, counts[7]);  // 음표
            Assert.Equal(1, counts[9]);  // 씬 전환
        }

        public static void RealChart_BuildsBossSwapAcrossScenes()
        {
            var chart = Parse();
            var events = BmsBossSwapPlanner.BuildSwapEvents(chart);

            // 1번 씬 보스 퇴장(3틱) → 8번 씬 보스 등장(4틱)
            Assert.Equal(1, events.Count);
            Assert.Equal("swap:0801_boss:8", events[0].BossAction);
            Assert.Equal(3.0f, events[0].OutNote.Tick);
            Assert.Equal(4.0f, events[0].InNote.Tick);
            Assert.Equal(MeasureSeconds, events[0].DelaySeconds);
        }

        public static void RealChart_MatchesHoldPairByOrderNotByName()
        {
            var chart = Parse();
            var pairs = BmsNoteMatcher.MatchSpecialNotes(chart.Notes, chart);

            Assert.Equal(1, pairs.Count);
            Assert.Equal(BmsSpecialNoteType.Hold, pairs[0].Type);

            // 차트에서 "시작 노트"(00C)가 먼저, "끝 노트"(00B)가 뒤에 옵니다.
            // 파서는 한글 라벨을 읽지 않고 등장 순서로만 짝을 짓습니다.
            Assert.Equal("00C", pairs[0].StartNote.RawValue);
            Assert.Equal("00B", pairs[0].EndNote.RawValue);
            Assert.Equal(0.5f, pairs[0].LengthInTicks);
            Assert.Equal(0.5f * MeasureSeconds, pairs[0].Duration);
        }

        public static void RealChart_KeepsDeclaredProjectileDtOnBothMarkers()
        {
            var chart = Parse();

            // 같은 UID(010601)에 같은 _dt0.8 선언이고, _boss 유무는 보스 액션만 가릅니다.
            // 예전에는 _boss 쪽 dt만 0.7로 덮어써져서 선언값이 사라졌습니다.
            var withBoss = BmsBossSwapPlanner.ResolveWavInfo(chart, chart.Notes.First(n => n.RawValue == "00H"));
            var withoutBoss = BmsBossSwapPlanner.ResolveWavInfo(chart, chart.Notes.First(n => n.RawValue == "00I"));

            Assert.Equal(withBoss.Uid, withoutBoss.Uid);
            Assert.Equal(0.8, withBoss.Dt, "_boss 항목의 선언 dt(0.8)를 그대로 써야 합니다.");
            Assert.Equal(0.8, withoutBoss.Dt);
            Assert.Equal("boss_far_atk_1_R", withBoss.BossAction);
            Assert.Equal("", withoutBoss.BossAction);
        }
    }
}
