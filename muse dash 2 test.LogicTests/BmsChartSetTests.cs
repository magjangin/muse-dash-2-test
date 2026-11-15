namespace muse_dash_2_test.LogicTests
{
    // 난이도별 채보 선택은 틀려도 게임이 멀쩡히 돌아가서(다른 채보가 조용히 나옵니다) 규칙을 테스트로 고정합니다.
    public static class BmsChartSetTests
    {
        private static BmsChart Chart(string path, int noteCount = 1)
        {
            var notes = new List<BmsNote>();
            for (int i = 0; i < noteCount; i++) notes.Add(new BmsNote());
            return new BmsChart(path) { Notes = notes };
        }

        public static void ForDifficulty_UsesExplicitChartThenDefault()
        {
            var defaultChart = Chart("chart.bms");
            var master = Chart("master.bms");
            var set = new BmsChartSet(defaultChart);
            set.SetForDifficulty(3, master);

            Assert.True(ReferenceEquals(master, set.ForDifficulty(3)));
            Assert.True(ReferenceEquals(defaultChart, set.ForDifficulty(1)));
            Assert.True(ReferenceEquals(defaultChart, set.ForDifficulty(2)));
        }

        // 게임은 튜토리얼에서 -1을 쓰고, 값을 못 읽으면 0이 올 수 있습니다.
        public static void ForDifficulty_OutOfRangeUsesDefault()
        {
            var defaultChart = Chart("chart.bms");
            var set = new BmsChartSet(defaultChart);
            set.SetForDifficulty(1, Chart("easy.bms"));
            set.SetForDifficulty(6, Chart("ignored.bms"));

            Assert.True(ReferenceEquals(defaultChart, set.ForDifficulty(-1)));
            Assert.True(ReferenceEquals(defaultChart, set.ForDifficulty(0)));
            Assert.True(ReferenceEquals(defaultChart, set.ForDifficulty(6)));
        }

        public static void IsEmpty_TrueOnlyWithoutAnyChart()
        {
            Assert.True(new BmsChartSet(null).IsEmpty);
            Assert.False(new BmsChartSet(Chart("chart.bms")).IsEmpty);

            var onlyExplicit = new BmsChartSet(null);
            onlyExplicit.SetForDifficulty(2, Chart("hard.bms"));
            Assert.False(onlyExplicit.IsEmpty);
        }

        public static void MergeReload_TakesFreshChartsWithoutRetry()
        {
            var current = new BmsChartSet(Chart("chart.bms"));
            current.SetForDifficulty(2, Chart("hard.bms"));
            var freshDefault = Chart("chart.bms");
            var freshHard = Chart("hard.bms");
            var fresh = new BmsChartSet(freshDefault);
            fresh.SetForDifficulty(2, freshHard);

            var merged = BmsChartSet.MergeReload(fresh, current, _ => true, out bool keptOld);

            Assert.False(keptOld);
            Assert.True(ReferenceEquals(freshDefault, merged.Default));
            Assert.True(ReferenceEquals(freshHard, merged.ExplicitFor(2)));
        }

        // 한 난이도 파일을 저장하는 중이어도 그 칸만 옛 채보로 남고, 나머지는 새 채보로 바뀌어야 합니다.
        public static void MergeReload_KeepsOnlyUnreadableSlotAndAsksRetry()
        {
            var oldHard = Chart("hard.bms");
            var current = new BmsChartSet(Chart("chart.bms"));
            current.SetForDifficulty(2, oldHard);
            var freshDefault = Chart("chart.bms");
            var fresh = new BmsChartSet(freshDefault);
            fresh.SetForDifficulty(2, Chart("hard.bms", noteCount: 0));

            var merged = BmsChartSet.MergeReload(fresh, current, _ => true, out bool keptOld);

            Assert.True(keptOld);
            Assert.True(ReferenceEquals(freshDefault, merged.Default));
            Assert.True(ReferenceEquals(oldHard, merged.ExplicitFor(2)));
        }

        // 난이도 파일을 지우면 그 난이도는 기본 채보로 돌아가야 합니다. 옛 채보가 남으면 지운 채보가 계속 나옵니다.
        public static void MergeReload_DropsSlotWhoseFileIsGone()
        {
            var current = new BmsChartSet(Chart("chart.bms"));
            current.SetForDifficulty(3, Chart("master.bms"));
            var freshDefault = Chart("chart.bms");

            var merged = BmsChartSet.MergeReload(new BmsChartSet(freshDefault), current, path => path != "master.bms", out bool keptOld);

            Assert.False(keptOld);
            Assert.True(merged.ExplicitFor(3) == null);
            Assert.True(ReferenceEquals(freshDefault, merged.ForDifficulty(3)));
        }

        public static void MergeReload_EmptyWhenEveryFileIsGone()
        {
            var current = new BmsChartSet(Chart("chart.bms"));

            var merged = BmsChartSet.MergeReload(null, current, _ => false, out bool keptOld);

            Assert.False(keptOld);
            Assert.True(merged.IsEmpty);
        }

        public static void Describe_NamesOnlyAssignedSlots()
        {
            var set = new BmsChartSet(Chart(@"H:\songs\a\chart.bms"));
            set.SetForDifficulty(3, Chart(@"H:\songs\a\master.bms"));

            Assert.True(set.Describe() == "기본=chart.bms, 3=master.bms", set.Describe());
        }

        // 실제 게임 값: 0-4는 difficulty1="1", difficulty2="4", difficulty3="0", 나머지는 null입니다.
        public static void HostMaps_ReadsOnlyDifficultiesThatExist()
        {
            int[] available = BmsHostMaps.AvailableDifficulties(new[] { "1", "4", "0", null, "" });
            Assert.Equal(2, available.Length);
            Assert.Equal(1, available[0]);
            Assert.Equal(2, available[1]);

            Assert.True(BmsHostMaps.HasMapForLevel("?"));
            Assert.False(BmsHostMaps.HasMapForLevel(" 0 "));
        }

        public static void HostMaps_PicksExistingMap()
        {
            int[] oneTwo = { 1, 2 };
            Assert.Equal(2, BmsHostMaps.PickMap(oneTwo, 3));
            Assert.Equal(2, BmsHostMaps.PickMap(oneTwo, 2));
            Assert.Equal(1, BmsHostMaps.PickMap(oneTwo, 1));
            Assert.Equal(2, BmsHostMaps.PickMap(oneTwo, 5));
            Assert.Equal(2, BmsHostMaps.PickMap(new[] { 2, 3 }, 1));
        }

        public static void HostMaps_UnknownHostKeepsDifficulty()
        {
            Assert.Equal(3, BmsHostMaps.PickMap(new int[0], 3));
            Assert.Equal(3, BmsHostMaps.PickMap(null, 3));
        }

        public static void TryResolveInSongFolder_AcceptsFilesInsideSongFolder()
        {
            string dir = Path.Combine(Path.GetTempPath(), "song");

            Assert.True(BmsFileNames.TryResolveInSongFolder(dir, "easy.bms", out string direct));
            Assert.True(direct == Path.Combine(dir, "easy.bms"), direct);

            Assert.True(BmsFileNames.TryResolveInSongFolder(dir, "  charts/hard.BMS ", out string nested));
            Assert.True(nested == Path.GetFullPath(Path.Combine(dir, "charts", "hard.BMS")), nested);
        }

        public static void TryResolveInSongFolder_RejectsEverythingElse()
        {
            string dir = Path.Combine(Path.GetTempPath(), "song");

            Assert.False(BmsFileNames.TryResolveInSongFolder(dir, @"..\other\chart.bms", out _));
            Assert.False(BmsFileNames.TryResolveInSongFolder(dir, "../other/chart.bms", out _));
            Assert.False(BmsFileNames.TryResolveInSongFolder(dir, Path.Combine(Path.GetTempPath(), "chart.bms"), out _));
            Assert.False(BmsFileNames.TryResolveInSongFolder(dir, "info.txt", out _));
            Assert.False(BmsFileNames.TryResolveInSongFolder(dir, "easy", out _));
            Assert.False(BmsFileNames.TryResolveInSongFolder(dir, "   ", out _));
            Assert.False(BmsFileNames.TryResolveInSongFolder(null, "easy.bms", out _));
        }

        private static HwaManifest ParseManifest(params string[] lines)
        {
            var manifest = new HwaManifest();
            foreach (string line in lines)
            {
                if (HwaManifestLoader.TryParseManifestLine(line, out string key, out string value))
                {
                    HwaManifestLoader.ApplyManifestValue(manifest, key, value);
                }
            }
            return manifest;
        }

        // "난이도1 bms"에는 "난이도1"이 들어 있어, 순서가 틀리면 파일 이름이 레벨 숫자 칸으로 들어갑니다.
        public static void Manifest_DifficultyBmsDoesNotTouchLevelNumbers()
        {
            var manifest = ParseManifest(
                "난이도 1 : 4",
                "난이도 1 bms : easy.bms",
                "난이도2 채보 = hard.bms // 하드 채보",
                "difficulty3 bms: master.bms",
                "difficulty_4_chart: another.bms");

            Assert.True(manifest.Difficulty1 == 4);
            Assert.True(manifest.DifficultyBmsFiles[1] == "easy.bms");
            Assert.True(manifest.DifficultyBmsFiles[2] == "hard.bms");
            Assert.True(manifest.DifficultyBmsFiles[3] == "master.bms");
            Assert.True(manifest.DifficultyBmsFiles[4] == "another.bms");
            Assert.True(manifest.DifficultyBmsFiles[5] == null);
            Assert.False(manifest.Difficulty2.HasValue);
        }

        public static void Manifest_NullValueMeansUnset()
        {
            var manifest = ParseManifest("난이도 2 bms : null", "난이도 2 : 9");

            Assert.True(manifest.DifficultyBmsFiles[2] == null);
            Assert.True(manifest.GetDifficultyLevel(2) == 9);
        }

        public static void Manifest_ExistingKeysStillWork()
        {
            var manifest = ParseManifest("내가 가져올 uid : 0-4", "난이도 3 : 11", "씬 번호 : 7");

            Assert.True(manifest.Uid == "0-4");
            Assert.True(manifest.Difficulty3 == 11);
            Assert.True(manifest.Scene == 7);
            for (int i = 0; i < manifest.DifficultyBmsFiles.Length; i++)
            {
                Assert.True(manifest.DifficultyBmsFiles[i] == null);
            }
        }
    }
}
