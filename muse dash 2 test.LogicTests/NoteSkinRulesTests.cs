using System.Collections.Generic;
using System.IO;

namespace muse_dash_2_test.LogicTests
{
    // NoteSkinRules 검증.
    // 세트 이름을 잘못 얻으면 곡 폴더의 스킨을 못 찾거나(조용히 원본 노트), 엉뚱한 세트를 씌웁니다.
    // 계약 검사를 잘못하면 애니메이션이 빠진 스켈레톤이 들어가 노트가 멈추거나 사라집니다.
    public static class NoteSkinRulesTests
    {
        public static void BaseName_StripsSkeletonDataSuffix()
        {
            Assert.Equal("0411_road", NoteSkinRules.BaseNameFromAssetName("0411_road_SkeletonData"));
            Assert.Equal("0510_road", NoteSkinRules.BaseNameFromAssetName("0510_road_skeletondata"));
            Assert.Equal("0503_air", NoteSkinRules.BaseNameFromAssetName("  0503_air_SkeletonData "));
        }

        public static void BaseName_KeepsNameWithoutSuffix()
        {
            Assert.Equal("0411_road", NoteSkinRules.BaseNameFromAssetName("0411_road"));
        }

        public static void BaseName_RejectsEmptyAndPathEscapes()
        {
            Assert.Null(NoteSkinRules.BaseNameFromAssetName(null));
            Assert.Null(NoteSkinRules.BaseNameFromAssetName(""));
            Assert.Null(NoteSkinRules.BaseNameFromAssetName("_SkeletonData"));
            Assert.Null(NoteSkinRules.BaseNameFromAssetName(".."));
            Assert.Null(NoteSkinRules.BaseNameFromAssetName("a/b_SkeletonData"));
        }

        public static void SetPaths_UseBaseNameForAllThreeFiles()
        {
            string dir = Path.Combine("skin test", "0411_road");
            NoteSkinRules.GetSetPaths(dir, "0411_road", out string png, out string atlas, out string json);
            Assert.Equal(Path.Combine(dir, "0411_road.png"), png);
            Assert.Equal(Path.Combine(dir, "0411_road.atlas"), atlas);
            Assert.Equal(Path.Combine(dir, "0411_road.json"), json);
        }

        public static void AtlasTextureName_UsesFirstPageLineWithoutExtension()
        {
            // Spine 3.8 에디터가 내보낸 아틀라스는 빈 줄로 시작합니다(0411_road.atlas 실물과 같은 모양).
            string atlas = "\n0411_road.png\nsize: 162,144\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n0411/images_road/fire1\n  rotate: false\n";
            Assert.Equal("0411_road", NoteSkinRules.AtlasTextureName(atlas, "fallback"));
            Assert.Equal("char_1_rock", NoteSkinRules.AtlasTextureName("\r\nchar_1_rock.png\r\nsize: 1621,528\r\n", "x"));
        }

        public static void AtlasTextureName_FollowsPageLineEvenIfPngFileWasRenamed()
        {
            // 파일 이름만 바꾸고 아틀라스 첫 줄을 안 고쳤다면 페이지 이름이 맞아야 아틀라스가 만들어집니다.
            Assert.Equal("old_name", NoteSkinRules.AtlasTextureName("old_name.png\nsize: 2,2\n", "new_name"));
        }

        public static void AtlasTextureName_FallsBackWhenNoPageLine()
        {
            Assert.Equal("fb", NoteSkinRules.AtlasTextureName(null, "fb"));
            Assert.Equal("fb", NoteSkinRules.AtlasTextureName("\n\n", "fb"));
            Assert.Equal("fb", NoteSkinRules.AtlasTextureName("size: 2,2\n", "fb"));
            Assert.Equal("page", NoteSkinRules.AtlasTextureName("﻿page.png\n", "fb"));
        }

        public static void RuntimeScale_DoublesOriginalLikeCharacterSkins()
        {
            // 커스텀 캐릭터 스킨은 원본 0.005에 0.01 고정으로 맞춰 왔습니다. 노트도 같은 관행이어야 같은 작업 흐름의 스킨이 원래 크기로 나옵니다.
            Assert.Equal(0.01f, NoteSkinRules.RuntimeScale(0.005f));
            Assert.Equal(0.02f, NoteSkinRules.RuntimeScale(0.01f));
        }

        public static void MissingAnimations_ListsOnlyAbsentNamesOnceInOrder()
        {
            var have = new HashSet<string> { "in_nor_44", "out_g" };
            var required = new[] { "in_nor_44", "out_p", "", null, "out_g", "out_p", "hurt_1" };

            List<string> missing = NoteSkinRules.FindMissingAnimations(required, have.Contains);

            Assert.Equal(2, missing.Count);
            Assert.Equal("out_p", missing[0]);
            Assert.Equal("hurt_1", missing[1]);
        }

        public static void MissingAnimations_EmptyWhenAllPresent()
        {
            var have = new HashSet<string> { "in_nor_38", "out_g", "out_p" };
            var missing = NoteSkinRules.FindMissingAnimations(new[] { "in_nor_38", "out_g", "out_p" }, have.Contains);
            Assert.Equal(0, missing.Count);
        }

        public static void MissingAnimations_IsCaseSensitiveLikeSpine()
        {
            var have = new HashSet<string> { "Out_G" };
            var missing = NoteSkinRules.FindMissingAnimations(new[] { "out_g" }, have.Contains);
            Assert.Equal(1, missing.Count);
        }
    }
}
