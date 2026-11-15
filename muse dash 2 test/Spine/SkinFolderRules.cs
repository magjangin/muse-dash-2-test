using System;
using System.IO;

namespace muse_dash_2_test
{
    /// <summary>커스텀 Spine 스킨의 종류. 종류마다 <c>skin test</c> 아래 폴더가 따로 있습니다.</summary>
    public enum SkinKind
    {
        /// <summary>캐릭터·고스트 스킨. <c>skin test/character/{세트}/</c></summary>
        Character,
        /// <summary>노트 스킨. <c>skin test/note/{원본 스켈레톤 이름}/</c></summary>
        Note,
    }

    /// <summary>
    /// 스킨 세트 폴더를 고르는 규칙입니다(게임에 의존하지 않아 로직 테스트로 검증합니다).
    ///
    /// <para><b>배치</b>: <c>skin test/character/{세트}/{세트}.png·.atlas·.json</c>,
    /// <c>skin test/note/{세트}/{세트}.png·.atlas·.json</c>. 노트 세트가 스무 개 가까이 되면서 캐릭터 세트와 한 폴더에 섞여
    /// 찾기 어려워져 종류별로 나눴습니다(2026-11-11).</para>
    ///
    /// <para><b>옛 배치</b>: 예전에는 <c>skin test/{세트}/</c>에 바로 두었습니다. 새 위치에 세트가 없고 옛 위치에 있으면
    /// 옛 위치를 씁니다. 이미 만들어 둔 스킨이 업데이트 뒤 조용히 사라지지 않게 하려는 것입니다. 둘 다 있으면 새 위치가 이깁니다.</para>
    /// </summary>
    public static class SkinFolderRules
    {
        public const string CharacterFolderName = "character";
        public const string NoteFolderName = "note";

        /// <summary>종류별 폴더 이름입니다.</summary>
        public static string KindFolderName(SkinKind kind) =>
            kind == SkinKind.Note ? NoteFolderName : CharacterFolderName;

        /// <summary><c>{root}/character</c> 또는 <c>{root}/note</c>.</summary>
        public static string KindDirectory(string root, SkinKind kind) => Path.Combine(root, KindFolderName(kind));

        /// <summary>새 배치의 세트 폴더입니다(존재 여부는 보지 않습니다).</summary>
        public static string PreferredSetDirectory(string root, SkinKind kind, string baseName) =>
            Path.Combine(KindDirectory(root, kind), baseName);

        /// <summary>옛 배치(<c>{root}/{세트}</c>)의 세트 폴더입니다.</summary>
        public static string LegacySetDirectory(string root, string baseName) => Path.Combine(root, baseName);

        /// <summary>
        /// 실제로 읽을 세트 폴더입니다. 새 위치에 <c>{세트}.json</c>이 있으면 새 위치, 없고 옛 위치에 있으면 옛 위치,
        /// 둘 다 없으면 새 위치(없다는 로그가 새 위치를 가리키도록)입니다. <paramref name="usedLegacy"/>는 옛 위치를 골랐는지입니다.
        /// </summary>
        public static string ResolveSetDirectory(string root, SkinKind kind, string baseName, Func<string, bool> fileExists, out bool usedLegacy)
        {
            usedLegacy = false;
            string preferred = PreferredSetDirectory(root, kind, baseName);
            if (fileExists == null) return preferred;

            if (fileExists(Path.Combine(preferred, baseName + ".json"))) return preferred;

            string legacy = LegacySetDirectory(root, baseName);
            if (fileExists(Path.Combine(legacy, baseName + ".json")))
            {
                usedLegacy = true;
                return legacy;
            }

            return preferred;
        }
    }
}
