namespace muse_dash_2_test.LogicTests
{
    internal static class BmsDtTableTests
    {
        public static void ClassifiesEachFamily()
        {
            Assert.Equal(DtFamily.Normal, BmsDtTable.Classify("051001", 1, ""));
            Assert.Equal(DtFamily.Normal, BmsDtTable.Classify("051007", 1, ""));      // Up
            Assert.Equal(DtFamily.Normal, BmsDtTable.Classify("000301", 7, ""));      // 음표
            Assert.Equal(DtFamily.Hold, BmsDtTable.Classify("050203", 3, ""));
            Assert.Equal(DtFamily.BossProjectile, BmsDtTable.Classify("020702", 1, "boss_far_atk_2"));
            Assert.Equal(DtFamily.FixedEvent, BmsDtTable.Classify("000401", 9, ""));
            Assert.Equal(DtFamily.FixedEvent, BmsDtTable.Classify("050101", 0, "in"));
            Assert.Equal(DtFamily.None, BmsDtTable.Classify("050401", 8, ""));        // 샌드백
            Assert.Equal(DtFamily.None, BmsDtTable.Classify("090908", 2, ""));        // 보스 톱니
        }

        public static void RecommendedValuesMatchConfirmedTable()
        {
            AssertRecommended(DtFamily.Normal, new[] { 1.48, 1.28, 1.18 });
            AssertRecommended(DtFamily.Hold, new[] { 1.47, 1.26, 1.10 });
            AssertRecommended(DtFamily.BossProjectile, new[] { 0.8, 0.76, 0.6 });
            Assert.True(BmsDtTable.TryGetRecommended(DtFamily.FixedEvent, 0, out double fixedDt));
            Assert.Equal(0.0, fixedDt);
            Assert.False(BmsDtTable.TryGetRecommended(DtFamily.None, 1, out _));
        }

        public static void NormalFamilyAcceptsCharterRange()
        {
            Assert.True(BmsDtTable.IsAllowed(DtFamily.Normal, 1, 1.48, out _));
            Assert.False(BmsDtTable.IsAllowed(DtFamily.Normal, 1, 1.38, out _));
            Assert.True(BmsDtTable.IsAllowed(DtFamily.Normal, 2, 1.18, out _));
            Assert.True(BmsDtTable.IsAllowed(DtFamily.Normal, 2, 1.38, out _));
            Assert.False(BmsDtTable.IsAllowed(DtFamily.Normal, 2, 1.40, out string expected2));
            Assert.Equal("1.18~1.38", expected2);
            Assert.True(BmsDtTable.IsAllowed(DtFamily.Normal, 3, 0.98, out _));
            Assert.True(BmsDtTable.IsAllowed(DtFamily.Normal, 3, 1.28, out _));
            Assert.False(BmsDtTable.IsAllowed(DtFamily.Normal, 3, 0.9, out _));
        }

        public static void OtherFamiliesAcceptOnlyRecommended()
        {
            Assert.True(BmsDtTable.IsAllowed(DtFamily.Hold, 3, 1.10, out _));
            Assert.False(BmsDtTable.IsAllowed(DtFamily.Hold, 3, 1.07, out _));
            Assert.False(BmsDtTable.IsAllowed(DtFamily.BossProjectile, 1, 0.7, out _));
            // dt를 적지 않은 노트와 권장표 밖 계열은 판정하지 않습니다.
            Assert.True(BmsDtTable.IsAllowed(DtFamily.Hold, 3, -1.0, out _));
            Assert.True(BmsDtTable.IsAllowed(DtFamily.None, 1, 5.0, out _));
        }

        public static void ManifestKeyFromHwaInfoTurnsFixOn()
        {
            var manifest = new HwaManifest();
            Assert.True(HwaManifestLoader.TryParseManifestLine("dt를 권장 표로 고치기 = 활성화", out var key, out var value));
            HwaManifestLoader.ApplyManifestValue(manifest, key, value);
            Assert.True(manifest.FixDtToRecommended == true);
            Assert.True(manifest.ShowGhostNotes == null);

            HwaManifestLoader.ApplyManifestValue(manifest, "fix dt", "off");
            Assert.True(manifest.FixDtToRecommended == false);
        }

        private static void AssertRecommended(DtFamily family, double[] expected)
        {
            for (int speed = 1; speed <= 3; speed++)
            {
                Assert.True(BmsDtTable.TryGetRecommended(family, speed, out double dt));
                Assert.Equal(expected[speed - 1], dt);
            }
        }

        // 5번 씬 발사체2·3은 비행 애니메이션이 7~8프레임 길어(in_boss_2_31/28/24) 0.8을 쓰면 판정이 노트보다 먼저 옵니다.
        // 2026-11-11 summertime에서 속도 1을 1.07로 실기 확인했습니다. 권장표로 덮어쓸 때 다시 0.8이 되면 안 됩니다.
        public static void Scene05LongProjectilesUseTheirOwnRow()
        {
            foreach (string uid in new[] { "050701", "050704", "050801", "050804" })
            {
                Assert.True(BmsDtTable.TryGetRecommended(DtFamily.BossProjectile, 1, out double dt, uid));
                Assert.Equal(1.07, dt);
                Assert.True(BmsDtTable.IsAllowed(DtFamily.BossProjectile, 1, 1.07, out _, uid));
                Assert.False(BmsDtTable.IsAllowed(DtFamily.BossProjectile, 1, 0.8, out _, uid));
            }

            Assert.True(BmsDtTable.TryGetRecommended(DtFamily.BossProjectile, 2, out double s2, "050702"));
            Assert.Equal(1.03, s2);
            Assert.True(BmsDtTable.TryGetRecommended(DtFamily.BossProjectile, 3, out double s3, "050806"));
            Assert.Equal(0.83, s3);

            // 5번 씬 발사체1과 다른 씬 발사체2는 기본 표 그대로입니다.
            Assert.True(BmsDtTable.TryGetRecommended(DtFamily.BossProjectile, 1, out double p1, "050601"));
            Assert.Equal(0.8, p1);
            Assert.True(BmsDtTable.TryGetRecommended(DtFamily.BossProjectile, 1, out double other, "010701"));
            Assert.Equal(0.8, other);
        }
    }
}
