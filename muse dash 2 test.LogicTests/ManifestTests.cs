
namespace muse_dash_2_test.LogicTests
{
    internal static class ManifestTests
    {
        public static void FirstSeparatorPreservesAllRemainingSeparators()
        {
            foreach (string separator in new[] { "=", ":", "：" })
            {
                Assert.True(HwaManifestLoader.TryParseManifestLine($"곡제목 {separator} Re:Zero：A=B // 주석", out var key, out var value));
                Assert.Equal("곡제목", key);
                Assert.Equal("Re:Zero：A=B", value);
            }
        }

        public static void CustomTitleAliasesPreserveHostSong()
        {
            foreach (string alias in new[] { "커스텀 곡 이름", "커스텀 곡명", "custom song name", "커스텀 곡제목", "custom title" })
            {
                var manifest = new HwaManifest();
                HwaManifestLoader.ApplyManifestValue(manifest, "가져올곡", "원본");
                Assert.True(HwaManifestLoader.TryParseManifestLine($"{alias} = Re:Zero", out var key, out var value));
                HwaManifestLoader.ApplyManifestValue(manifest, key, value);
                Assert.Equal("원본", manifest.Title);
                Assert.Equal("Re:Zero", manifest.CustomTitle);
            }
        }

        public static void ChristmasSceneOptionParsesWithoutStealingOtherKeys()
        {
            foreach (string alias in new[] { "크리스마스 배경", "크리스마스", "크리스마스 활성화", "christmas scene", "xmas" })
            {
                var manifest = new HwaManifest();
                Assert.True(HwaManifestLoader.TryParseManifestLine($"{alias} = 활성화", out var key, out var value));
                HwaManifestLoader.ApplyManifestValue(manifest, key, value);
                Assert.Equal(true, manifest.ChristmasScene);
                Assert.Equal(null, manifest.Scene);
            }

            // 실제 사용 중인 info.txt 줄(2026-11-11): 값이 true여도 켜집니다. 노트 스킨도 이 값으로 곡을 고릅니다.
            var userLine = new HwaManifest();
            Assert.True(HwaManifestLoader.TryParseManifestLine("크리스마스 활성화 = true", out var userKey, out var userValue));
            HwaManifestLoader.ApplyManifestValue(userLine, userKey, userValue);
            Assert.Equal(true, userLine.ChristmasScene);

            var off = new HwaManifest();
            HwaManifestLoader.ApplyManifestValue(off, "크리스마스 배경", "비활성화");
            Assert.Equal(false, off.ChristmasScene);

            var other = new HwaManifest();
            HwaManifestLoader.ApplyManifestValue(other, "씬번호", "5");
            HwaManifestLoader.ApplyManifestValue(other, "커스텀 곡 고스트 노트 보이기", "활성화");
            Assert.Equal(null, other.ChristmasScene);
            Assert.Equal(5, other.Scene);
        }

        public static void CommentsAndInvalidLinesRemainIgnored()
        {
            foreach (string line in new[] { "", "// offset: 1", "제목", "= 값", "offset=" })
                Assert.False(HwaManifestLoader.TryParseManifestLine(line, out _, out _));
            Assert.True(HwaManifestLoader.TryParseManifestLine("곡제목 = https://example.com/a:b", out _, out var value));
            Assert.Equal("https://example.com/a:b", value);
        }
    }
}
