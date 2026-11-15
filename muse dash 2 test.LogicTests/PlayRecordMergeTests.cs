namespace muse_dash_2_test.LogicTests
{
    // PlayRecordMerge의 판정 규칙 검증.
    // 이 규칙은 "최고 기록"과 "달성 배지(FC/AP)"를 한 플래그로 겸하다가 양방향으로 기록이
    // 망가진 사고에서 나왔습니다. 아래 두 테스트(LowerScoringAllPerfect~, HigherScoringNonFullCombo~)가
    // 그 두 사고를 각각 못 박습니다. 지우지 마세요.
    public static class PlayRecordMergeTests
    {
        // 기록이 없으면 이번 판이 그대로 최고 기록이 됩니다.
        public static void Merge_TreatsFirstPlayAsHighScore()
        {
            var r = PlayRecordMerge.Merge(
                hasExisting: false,
                existingScore: 0, existingMaxCombo: 0, existingAccuracy: 0f,
                existingIsFullCombo: false, existingIsAllPerfect: false,
                newScore: 50000, newMaxCombo: 120, newAccuracy: 0.93f,
                newIsFullCombo: false, newIsAllPerfect: false);

            Assert.True(r.IsNewHighScore);
            Assert.Equal(50000, r.Score);
            Assert.Equal(120, r.MaxCombo);
            Assert.Equal(0.93f, r.Accuracy);
        }

        public static void Merge_TakesHigherScore()
        {
            var r = PlayRecordMerge.Merge(
                hasExisting: true,
                existingScore: 100000, existingMaxCombo: 200, existingAccuracy: 0.95f,
                existingIsFullCombo: false, existingIsAllPerfect: false,
                newScore: 120000, newMaxCombo: 250, newAccuracy: 0.97f,
                newIsFullCombo: false, newIsAllPerfect: false);

            Assert.True(r.IsNewHighScore);
            Assert.Equal(120000, r.Score);
            Assert.Equal(0.97f, r.Accuracy);
        }

        public static void Merge_KeepsExistingScoreWhenNewRunIsWorse()
        {
            var r = PlayRecordMerge.Merge(
                hasExisting: true,
                existingScore: 120000, existingMaxCombo: 250, existingAccuracy: 0.97f,
                existingIsFullCombo: false, existingIsAllPerfect: false,
                newScore: 80000, newMaxCombo: 90, newAccuracy: 0.80f,
                newIsFullCombo: false, newIsAllPerfect: false);

            Assert.False(r.IsNewHighScore);
            Assert.Equal(120000, r.Score);
            Assert.Equal(0.97f, r.Accuracy);
        }

        // 사고 ①: 점수가 더 낮은 AP 판이 최고점을 덮어썼습니다.
        // (뮤즈 대시 점수는 피버 사용에 좌우되므로 AP여도 점수가 낮을 수 있고,
        //  이 모드의 '피버충전금지'를 켜면 바로 재현됩니다)
        // AP 배지는 새로 달되, 점수는 예전 최고점이 그대로 남아야 합니다.
        public static void Merge_LowerScoringAllPerfectDoesNotLowerTheHighScore()
        {
            var r = PlayRecordMerge.Merge(
                hasExisting: true,
                existingScore: 150000, existingMaxCombo: 300, existingAccuracy: 0.96f,
                existingIsFullCombo: false, existingIsAllPerfect: false,
                newScore: 90000, newMaxCombo: 300, newAccuracy: 1.0f,
                newIsFullCombo: true, newIsAllPerfect: true);

            Assert.False(r.IsNewHighScore, "AP 달성은 최고 기록 판정 사유가 아닙니다.");
            Assert.Equal(150000, r.Score, "더 낮은 AP 판 점수가 최고점을 덮으면 안 됩니다.");
            Assert.Equal(0.96f, r.Accuracy, "정확도도 최고 기록 판의 값을 유지해야 합니다.");
            Assert.True(r.IsAllPerfect, "점수와 무관하게 AP 배지는 달려야 합니다.");
            Assert.True(r.IsFullCombo, "AP 판이면 FC 배지도 함께 달립니다.");
        }

        // 사고 ②: 점수만 높은 비-FC 판이 이미 달성한 FC/AP 배지를 지웠습니다.
        // 점수는 새 값으로 갱신하되, 배지는 유지되어야 합니다.
        public static void Merge_HigherScoringNonFullComboKeepsEarnedBadges()
        {
            var r = PlayRecordMerge.Merge(
                hasExisting: true,
                existingScore: 100000, existingMaxCombo: 300, existingAccuracy: 1.0f,
                existingIsFullCombo: true, existingIsAllPerfect: true,
                newScore: 130000, newMaxCombo: 210, newAccuracy: 0.94f,
                newIsFullCombo: false, newIsAllPerfect: false);

            Assert.True(r.IsNewHighScore);
            Assert.Equal(130000, r.Score);
            Assert.True(r.IsFullCombo, "이미 달성한 FC 배지가 지워지면 안 됩니다.");
            Assert.True(r.IsAllPerfect, "이미 달성한 AP 배지가 지워지면 안 됩니다.");
        }

        public static void Merge_BreaksScoreTieByAccuracy()
        {
            var better = PlayRecordMerge.Merge(
                hasExisting: true,
                existingScore: 100000, existingMaxCombo: 200, existingAccuracy: 0.90f,
                existingIsFullCombo: false, existingIsAllPerfect: false,
                newScore: 100000, newMaxCombo: 200, newAccuracy: 0.95f,
                newIsFullCombo: false, newIsAllPerfect: false);

            Assert.True(better.IsNewHighScore);
            Assert.Equal(0.95f, better.Accuracy);

            var worse = PlayRecordMerge.Merge(
                hasExisting: true,
                existingScore: 100000, existingMaxCombo: 200, existingAccuracy: 0.95f,
                existingIsFullCombo: false, existingIsAllPerfect: false,
                newScore: 100000, newMaxCombo: 200, newAccuracy: 0.90f,
                newIsFullCombo: false, newIsAllPerfect: false);

            Assert.False(worse.IsNewHighScore);
            Assert.Equal(0.95f, worse.Accuracy);
        }

        // 최고 콤보는 최고 기록 판이 아니라 역대 최댓값입니다.
        // 그러지 않으면 FC 배지는 켜져 있는데 maxCombo는 그보다 작은 기록이 나옵니다.
        public static void Merge_KeepsBestMaxComboRegardlessOfHighScore()
        {
            var r = PlayRecordMerge.Merge(
                hasExisting: true,
                existingScore: 100000, existingMaxCombo: 300, existingAccuracy: 0.90f,
                existingIsFullCombo: true, existingIsAllPerfect: false,
                newScore: 130000, newMaxCombo: 150, newAccuracy: 0.94f,
                newIsFullCombo: false, newIsAllPerfect: false);

            Assert.True(r.IsNewHighScore);
            Assert.Equal(300, r.MaxCombo, "최고 콤보는 역대 최댓값이어야 합니다.");
        }

        // 배지는 한 번 달면 이후 어떤 판을 해도 유지됩니다.
        public static void Merge_BadgesStayEarnedAcrossLaterRuns()
        {
            var r = PlayRecordMerge.Merge(
                hasExisting: true,
                existingScore: 100000, existingMaxCombo: 300, existingAccuracy: 1.0f,
                existingIsFullCombo: true, existingIsAllPerfect: true,
                newScore: 10, newMaxCombo: 1, newAccuracy: 0.01f,
                newIsFullCombo: false, newIsAllPerfect: false);

            Assert.False(r.IsNewHighScore);
            Assert.Equal(100000, r.Score);
            Assert.True(r.IsFullCombo);
            Assert.True(r.IsAllPerfect);
        }
    }
}
