using System.Collections.Generic;
using System.IO;

namespace muse_dash_2_test.LogicTests
{
    // SkinFolderRules 검증.
    // 폴더를 잘못 고르면 스킨이 조용히 원본으로 돌아가거나(새 배치를 못 찾음), 옛 배치로 만든 스킨이 업데이트 뒤 사라집니다.
    public static class SkinFolderRulesTests
    {
        private static readonly string Root = Path.Combine("game", "skin test");

        public static void KindFolders_AreCharacterAndNote()
        {
            Assert.Equal(Path.Combine(Root, "character"), SkinFolderRules.KindDirectory(Root, SkinKind.Character));
            Assert.Equal(Path.Combine(Root, "note"), SkinFolderRules.KindDirectory(Root, SkinKind.Note));
        }

        public static void Resolve_PrefersNewLayoutWhenSetExists()
        {
            var files = new HashSet<string>
            {
                Path.Combine(Root, "note", "0411_road", "0411_road.json"),
                Path.Combine(Root, "0411_road", "0411_road.json"),
            };

            string dir = SkinFolderRules.ResolveSetDirectory(Root, SkinKind.Note, "0411_road", files.Contains, out bool legacy);

            Assert.Equal(Path.Combine(Root, "note", "0411_road"), dir);
            Assert.False(legacy);
        }

        public static void Resolve_FallsBackToLegacyFlatLayout()
        {
            var files = new HashSet<string> { Path.Combine(Root, "char_3_black", "char_3_black.json") };

            string dir = SkinFolderRules.ResolveSetDirectory(Root, SkinKind.Character, "char_3_black", files.Contains, out bool legacy);

            Assert.Equal(Path.Combine(Root, "char_3_black"), dir);
            Assert.True(legacy);
        }

        public static void Resolve_PointsAtNewLayoutWhenNothingExists()
        {
            string dir = SkinFolderRules.ResolveSetDirectory(Root, SkinKind.Character, "char_1_rock", _ => false, out bool legacy);

            Assert.Equal(Path.Combine(Root, "character", "char_1_rock"), dir);
            Assert.False(legacy);
        }

        public static void Resolve_DoesNotCrossKinds()
        {
            // 노트 세트가 character/에 잘못 들어가 있어도 노트로 읽지 않습니다.
            var files = new HashSet<string> { Path.Combine(Root, "character", "0411_road", "0411_road.json") };

            string dir = SkinFolderRules.ResolveSetDirectory(Root, SkinKind.Note, "0411_road", files.Contains, out bool legacy);

            Assert.Equal(Path.Combine(Root, "note", "0411_road"), dir);
            Assert.False(legacy);
        }
    }
}
