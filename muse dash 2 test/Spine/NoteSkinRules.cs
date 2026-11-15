using System;
using System.Collections.Generic;
using System.IO;

namespace muse_dash_2_test
{
    /// <summary>
    /// 노트 스킨 주입의 규칙 중 게임에 의존하지 않는 부분입니다. 로직 테스트(<c>NoteSkinRulesTests</c>)로 검증합니다.
    ///
    /// <para><b>파일 배치</b>: 게임 루트 <c>skin test/note/{세트 이름}/</c>에 Spine 3.8 에디터가 내보낸 세트를 둡니다(캐릭터 스킨은 <c>skin test/character/</c>, 폴더 규칙은 <see cref="SkinFolderRules"/>).
    /// 세트 이름은 <b>바꿀 원본 스켈레톤 이름</b>입니다. 원본 에셋 이름이 <c>0411_road_SkeletonData</c>면
    /// <c>skin test/note/0411_road/0411_road.json</c>·<c>.atlas</c>·<c>.png</c>입니다. 에디터에서 원본 <c>.skel</c>을 가져와 고쳤다면
    /// 스켈레톤 이름이 이미 원본과 같으므로 내보낸 파일을 그대로 넣으면 됩니다.</para>
    ///
    /// <para>프리팹 이름이 아니라 스켈레톤 이름으로 고르는 이유: 한 스켈레톤을 여러 프리팹(속도 1·2·3, 위·아래)이
    /// 같이 쓰므로, 스켈레톤 하나를 고치면 그 스켈레톤을 쓰는 노트가 모두 바뀌는 쪽이 에디터에서 작업한 단위와 맞습니다.
    /// 오브젝트 이름은 <c>{프리팹}(Clone){objId}</c>로 바뀌어 완전 일치로는 고를 수 없습니다.</para>
    /// </summary>
    public static class NoteSkinRules
    {
        private const string SkeletonDataSuffix = "_SkeletonData";

        /// <summary>
        /// 스킨 파일 좌표가 원본의 몇 분의 1인지에 대한 관행값입니다. 커스텀 캐릭터 스킨과 같은 관행을 따릅니다.
        /// <para>원본 <c>.skel</c>을 Spine 에디터로 가져오는 작업 흐름에서 좌표가 정확히 절반이 됩니다
        /// (2026-11-11 실측: 원본 <c>0411_road.skel</c>의 뼈대 위치 대비 스킨 JSON이 모든 뼈대에서 ½).
        /// 캐릭터 주입기는 원본 0.005의 2배인 0.01로 고정해 이것을 맞춰 왔으므로, 노트도 원본 배율에 2를 곱합니다.</para>
        /// </summary>
        public const float HalfImportFactor = 2f;

        /// <summary>커스텀 스킨을 만들 때 쓸 배율입니다. 원본 에셋 배율 × <see cref="HalfImportFactor"/> (씬 04·05 노트: 0.005 → 0.01).</summary>
        public static float RuntimeScale(float originalScale) => originalScale * HalfImportFactor;

        /// <summary>
        /// 원본 <c>SkeletonDataAsset</c> 이름에서 세트 이름을 얻습니다. <c>0411_road_SkeletonData</c> → <c>0411_road</c>.
        /// 꼬리가 없으면 이름 그대로입니다. 비어 있으면 null입니다.
        /// </summary>
        public static string BaseNameFromAssetName(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName)) return null;

            string name = assetName.Trim();
            if (name.EndsWith(SkeletonDataSuffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - SkeletonDataSuffix.Length);
            }

            if (name.Length == 0) return null;

            // 세트 이름이 경로를 벗어나지 못하게 합니다(에셋 이름은 게임 데이터지만 파일 경로로 쓰이므로).
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..") return null;

            return name;
        }

        /// <summary>
        /// 아틀라스 첫 페이지의 텍스처 이름(확장자 제외)입니다. 첫 번째 비어 있지 않은 줄이 페이지 파일 이름입니다.
        /// <c>SpineAtlasAsset.CreateRuntimeInstance</c>는 페이지를 <c>Texture2D.name</c>으로 찾으므로, 텍스처 이름을 이 값으로 맞춰야 합니다.
        /// 다르면 "Could not find matching atlas page in the texture array"로 실패합니다(2026-11-11 실측: 이름을 <c>note:0411_road</c>로 줘서 실패).
        /// 페이지 줄을 찾지 못하면 <paramref name="fallback"/>입니다.
        /// </summary>
        public static string AtlasTextureName(string atlasText, string fallback)
        {
            if (string.IsNullOrEmpty(atlasText)) return fallback;

            using (var reader = new StringReader(atlasText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim().TrimStart((char)0xFEFF);
                    if (trimmed.Length == 0) continue;
                    if (trimmed.IndexOf(':') >= 0) return fallback; // 페이지 이름 없이 속성 줄부터 나오면 형식이 다릅니다.
                    string name = Path.GetFileNameWithoutExtension(trimmed);
                    return string.IsNullOrEmpty(name) ? fallback : name;
                }
            }

            return fallback;
        }

        /// <summary>세트 파일 세 개의 경로입니다(존재 여부는 보지 않습니다).</summary>
        public static void GetSetPaths(string skinDir, string baseName, out string png, out string atlas, out string json)
        {
            png = Path.Combine(skinDir, baseName + ".png");
            atlas = Path.Combine(skinDir, baseName + ".atlas");
            json = Path.Combine(skinDir, baseName + ".json");
        }

        /// <summary>
        /// 프리팹이 부르는 애니메이션 이름 중 새 스켈레톤에 없는 것을 돌려줍니다(중복·빈 이름 제외, 처음 나온 순서).
        /// 하나라도 있으면 그 노트는 그 동작에서 멈추거나 사라지므로, 호출자는 주입을 건너뜁니다.
        /// </summary>
        public static List<string> FindMissingAnimations(IEnumerable<string> required, Func<string, bool> hasAnimation)
        {
            var missing = new List<string>();
            if (required == null || hasAnimation == null) return missing;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in required)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (!seen.Add(name)) continue;
                if (!hasAnimation(name)) missing.Add(name);
            }

            return missing;
        }
    }
}
