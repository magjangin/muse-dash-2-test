using MelonLoader;
using System;
using System.IO;

namespace muse_dash_2_test
{
    internal static class HwaManifestLoader
    {
        internal static HwaManifest LoadHwaManifest(string folderPath)
        {
            try
            {
                ModLogger.Verbose($"[HwaResourceManager] manifest 탐색 시작: folder={folderPath}");

                string[] txtFiles = Directory.GetFiles(folderPath, "*.txt", SearchOption.AllDirectories);
                if (txtFiles == null || txtFiles.Length == 0)
                {
                    ModLogger.Warning($"[HwaResourceManager] 하위 폴더까지 스캔했지만 txt 파일이 없습니다: folder={folderPath}");
                    return null;
                }

                ModLogger.Verbose($"[HwaResourceManager] txt 파일 {txtFiles.Length}개 발견(하위 폴더 포함): {string.Join(", ", Array.ConvertAll(txtFiles, file => GetRelativeHwaPath(folderPath, file)))}");

                Array.Sort(txtFiles, StringComparer.OrdinalIgnoreCase);
                string preferred = null;
                foreach (var file in txtFiles)
                {
                    string fileName = Path.GetFileName(file);
                    if (string.Equals(fileName, "info.txt", StringComparison.OrdinalIgnoreCase) || string.Equals(fileName, "info1.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        preferred = file;
                        break;
                    }

                    if (preferred == null)
                    {
                        preferred = file;
                    }
                }

                if (string.IsNullOrWhiteSpace(preferred) || !File.Exists(preferred))
                {
                    ModLogger.Warning($"[HwaResourceManager] 선택할 txt 파일이 없습니다: folder={folderPath}");
                    return null;
                }

                ModLogger.Verbose($"[HwaResourceManager] manifest 읽기 대상: {preferred}");

                var manifest = new HwaManifest { SourcePath = preferred };
                foreach (var rawLine in File.ReadAllLines(preferred))
                {
                    if (TryParseManifestLine(rawLine, out string key, out string value))
                    {
                        ModLogger.Verbose($"[HwaResourceManager] manifest line parsed: key={key}, value={value}");
                        ApplyManifestValue(manifest, key, value);
                    }
                }

                if (string.IsNullOrWhiteSpace(manifest.Uid) && string.IsNullOrWhiteSpace(manifest.Title) && string.IsNullOrWhiteSpace(manifest.Artist))
                {
                    ModLogger.Warning($"[HwaResourceManager] manifest 파싱은 했지만 핵심 값이 비어 있습니다: {DescribeManifest(manifest)}");
                    return null;
                }

                ModLogger.Verbose($"[HwaResourceManager] manifest 파싱 완료: {DescribeManifest(manifest)}");

                return manifest;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[HwaResourceManager] manifest 읽기 실패: {ex}");
                return null;
            }
        }

        internal static string GetRelativeHwaPath(string rootPath, string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(filePath))
                {
                    return Path.GetFileName(filePath);
                }

                string root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string full = Path.GetFullPath(filePath);
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return full.Substring(root.Length);
                }
            }
            catch (Exception ex) { ModLogger.Verbose($"[HwaManifestLoader.GetRelativeHwaPath] 무시된 예외: {ex.Message}"); }

            return Path.GetFileName(filePath);
        }

        internal static bool TryParseManifestLine(string rawLine, out string key, out string value)
        {
            key = null;
            value = null;

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                return false;
            }

            string line = rawLine.Trim();

            // '//'로 시작하는 줄은 주석입니다.
            //
            // 예전에는 '//'만 벗겨내고 남은 텍스트를 설정으로 파싱했습니다. 그래서
            // "// 씬번호: 9" 같은 줄이 그대로 적용됐고, 진짜 설정 아래에 주석이 있으면
            // 주석 쪽이 이기기까지 했습니다(설정을 끄려고 주석 처리하면 오히려 켜짐).
            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                return false;
            }

            // 값 뒤에 붙인 '//' 주석을 잘라냅니다. CUSTOM_CHART_GUIDE.md의 예시가
            // "씬번호: 4    // 플레이할 인게임 배경 테마 번호" 형식인데, 예전에는 이 주석이
            // 값에 그대로 눌어붙어 씬번호가 int 파싱에 실패하고(=미설정) 곡 제목·아티스트에는
            // 설명문이 통째로 들어갔습니다.
            line = StripTrailingComment(line);

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            int separatorIndex = line.IndexOfAny(new[] { ':', '=', '：' });

            if (separatorIndex < 0)
            {
                return false;
            }

            key = line.Substring(0, separatorIndex).Trim();
            value = line.Substring(separatorIndex + 1).Trim();
            return !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// 값 뒤에 붙은 <c>//</c> 주석을 잘라냅니다.
        /// <para>앞에 공백이 있는 <c>//</c>만 주석으로 봅니다. 값 안에 <c>//</c>가 붙어 나오는
        /// 경우(경로·URL 등)를 실수로 자르지 않기 위해서입니다. 문서의 예시도 전부
        /// "값<i>공백</i>// 설명" 형태입니다.</para>
        /// </summary>
        internal static string StripTrailingComment(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;

            for (int i = 1; i < line.Length - 1; i++)
            {
                if (line[i] == '/' && line[i + 1] == '/' && char.IsWhiteSpace(line[i - 1]))
                {
                    return line.Substring(0, i).TrimEnd();
                }
            }

            return line;
        }

        internal static void ApplyManifestValue(HwaManifest manifest, string key, string value)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string normalizedKey = NormalizeManifestKey(key);

            // "난이도1 bms"는 "난이도1"을 포함하므로 레벨 숫자 줄보다 먼저 봐야 합니다.
            if (TryApplyDifficultyBms(manifest, normalizedKey, value)) return;
            if (TryApplyBool(normalizedKey, value, v => manifest.ShowGhostNotes = v, "고스트노트보이기", "고스트보이기", "노트보이기", "ghostnote")) return;
            if (TryApplyBool(normalizedKey, value, v => manifest.FixDtToRecommended = v, "권장표", "권장dt", "dt보정", "recommendeddt", "fixdt")) return;
            if (TryApplyBool(normalizedKey, value, v => manifest.ChristmasScene = v, "크리스마스", "christmas", "xmas")) return;
            if (TryApplyString(normalizedKey, value, v => manifest.Album = v, "가져올앨범", "앨범", "album")) return;
            if (TryApplyString(normalizedKey, value, v => manifest.Uid = v, "uid")) return;
            if (TryApplyString(normalizedKey, value, v => manifest.CustomArtist = v, "커스텀아티스트", "customartist", "customauthor")) return;
            if (TryApplyString(normalizedKey, value, v => manifest.LevelDesigner = v, "레벨디자이너", "leveldesigner")) return;
            if (TryApplyString(normalizedKey, value, v => manifest.Artist = v, "artist", "아티스트", "author")) return;
            if (TryApplyString(normalizedKey, value, v => manifest.CustomTitle = v, "커스텀곡제목", "커스텀곡이름", "커스텀곡명", "customsongtitle", "customsongname", "customtitle")) return;
            if (TryApplyString(normalizedKey, value, v => manifest.Title = v, "곡이름", "곡명", "곡제목", "가져올곡", "song", "title", "music")) return;
            if (TryApplyInt(normalizedKey, value, v => manifest.Scene = v, "씬번호", "scene")) return;
            if (TryApplyInt(normalizedKey, value, v => manifest.Difficulty1 = v, "난이도1", "difficulty1")) return;
            if (TryApplyInt(normalizedKey, value, v => manifest.Difficulty2 = v, "난이도2", "difficulty2")) return;
            if (TryApplyInt(normalizedKey, value, v => manifest.Difficulty3 = v, "난이도3", "difficulty3")) return;
            if (TryApplyInt(normalizedKey, value, v => manifest.Difficulty4 = v, "난이도4", "difficulty4")) return;
            if (TryApplyInt(normalizedKey, value, v => manifest.Difficulty5 = v, "난이도5", "difficulty5")) return;
            if (TryApplyDouble(normalizedKey, value, v => manifest.Delay = v, "delay", "지연")) return;
            if (TryApplyDouble(normalizedKey, value, v => manifest.Offset = v, "offset", "오프셋", "싱크")) return;

            // 어떤 항목에도 걸리지 않은 줄은 오타일 가능성이 높습니다. 예전에는 조용히 버려서
            // "설정을 적었는데 반영이 안 된다"는 증상만 남았습니다.
            ModLogger.Warning($"[HwaResourceManager] info.txt에서 알 수 없는 설정 키를 건너뜁니다: '{key}' (값: '{value}')");
        }

        private static bool TryApplyDifficultyBms(HwaManifest manifest, string normalizedKey, string value)
        {
            for (int difficulty = 1; difficulty <= BmsChartSet.MaxDifficulty; difficulty++)
            {
                if (!ContainsAny(normalizedKey, $"난이도{difficulty}bms", $"난이도{difficulty}채보", $"difficulty{difficulty}bms", $"difficulty{difficulty}chart"))
                {
                    continue;
                }

                // 다른 난이도 줄과 같이 'null'은 적지 않은 것으로 봅니다.
                string fileName = value.Trim();
                manifest.DifficultyBmsFiles[difficulty] = fileName.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : fileName;
                return true;
            }

            return false;
        }

        private static bool TryApplyString(string normalizedKey, string value, Action<string> apply, params string[] tokens)
        {
            if (!ContainsAny(normalizedKey, tokens)) return false;
            apply(TextNormalizer.NormalizeEnglishContractions(value));
            return true;
        }

        private static bool TryApplyInt(string normalizedKey, string value, Action<int?> apply, params string[] tokens)
        {
            if (!ContainsAny(normalizedKey, tokens)) return false;
            apply(TryParseNullableInt(value));
            return true;
        }

        private static bool TryApplyBool(string normalizedKey, string value, Action<bool?> apply, params string[] tokens)
        {
            if (!ContainsAny(normalizedKey, tokens)) return false;
            apply(TryParseNullableBool(value));
            return true;
        }

        private static bool TryApplyDouble(string normalizedKey, string value, Action<double?> apply, params string[] tokens)
        {
            if (!ContainsAny(normalizedKey, tokens)) return false;
            apply(TryParseNullableDouble(value));
            return true;
        }

        /// <summary>
        /// 키에 토큰 중 하나라도 <b>포함</b>되면 참입니다(정확 일치가 아닙니다).
        /// <para>실제 사용 중인 info.txt가 "내가 가져올 uid", "커스텀 곡 고스트 보이기"처럼
        /// 문장형 키를 쓰기 때문에 부분일치가 필요합니다. 대신 <see cref="ApplyManifestValue"/>의
        /// <b>검사 순서가 곧 우선순위</b>가 되므로, 더 구체적인 키를 반드시 먼저 두어야 합니다.
        /// (예: "커스텀아티스트"는 "아티스트"를 포함하므로 앞에 와야 합니다.)
        /// 새 키를 추가할 때는 기존 토큰의 부분 문자열이 되지 않는지 확인하세요.</para>
        /// </summary>
        private static bool ContainsAny(string normalizedKey, params string[] tokens)
        {
            foreach (string token in tokens)
            {
                if (normalizedKey.Contains(token)) return true;
            }

            return false;
        }

        internal static string NormalizeManifestKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(text.Length);
            foreach (char ch in text)
            {
                if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_' || ch == '/' || ch == '·' || ch == '.' || ch == '(' || ch == ')' || ch == '[' || ch == ']')
                {
                    continue;
                }

                builder.Append(char.ToLowerInvariant(ch));
            }

            return builder.ToString();
        }

        internal static int? TryParseNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (int.TryParse(value.Trim(), out int parsed))
            {
                return parsed;
            }

            return null;
        }

        /// <summary>'활성화/비활성화'를 우선으로, true/false·on/off·켜기/끄기·1/0도 받습니다. 못 읽으면 null(= 전역 설정 따름).</summary>
        internal static bool? TryParseNullableBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            switch (NormalizeManifestKey(value))
            {
                case "활성화":
                case "켜기":
                case "켜짐":
                case "true":
                case "on":
                case "yes":
                case "1":
                    return true;

                case "비활성화":
                case "끄기":
                case "끔":
                case "false":
                case "off":
                case "no":
                case "0":
                    return false;

                default:
                    return null;
            }
        }

        internal static double? TryParseNullableDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (double.TryParse(value.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }

            return null;
        }

        internal static string DescribeManifest(HwaManifest manifest)
        {
            if (manifest == null)
            {
                return "(null)";
            }

            return "path=" + (manifest.SourcePath ?? "(null)")
                + ", uid=" + (manifest.Uid ?? "(null)")
                + ", title=" + (manifest.Title ?? "(null)")
                + ", customTitle=" + (manifest.CustomTitle ?? "(null)")
                + ", artist=" + (manifest.Artist ?? "(null)")
                + ", customArtist=" + (manifest.CustomArtist ?? "(null)")
                + ", levelDesigner=" + (manifest.LevelDesigner ?? "(null)")
                + ", album=" + (manifest.Album ?? "(null)")
                + ", scene=" + (manifest.Scene.HasValue ? manifest.Scene.Value.ToString() : "(null)")
                + ", diff1=" + (manifest.Difficulty1.HasValue ? manifest.Difficulty1.Value.ToString() : "(null)")
                + ", diff2=" + (manifest.Difficulty2.HasValue ? manifest.Difficulty2.Value.ToString() : "(null)")
                + ", diff3=" + (manifest.Difficulty3.HasValue ? manifest.Difficulty3.Value.ToString() : "(null)")
                + ", diff4=" + (manifest.Difficulty4.HasValue ? manifest.Difficulty4.Value.ToString() : "(null)")
                + ", diff5=" + (manifest.Difficulty5.HasValue ? manifest.Difficulty5.Value.ToString() : "(null)")
                + ", diffBms=[" + string.Join(", ", Array.ConvertAll(manifest.DifficultyBmsFiles, file => file ?? "-")) + "]"
                + ", delay=" + (manifest.Delay.HasValue ? manifest.Delay.Value.ToString("F7") : "(null)")
                + ", offset=" + (manifest.Offset.HasValue ? manifest.Offset.Value.ToString("F7") : "(null)")
                + ", showGhostNotes=" + (manifest.ShowGhostNotes.HasValue ? manifest.ShowGhostNotes.Value.ToString() : "(null)")
                + ", fixDtToRecommended=" + (manifest.FixDtToRecommended.HasValue ? manifest.FixDtToRecommended.Value.ToString() : "(null)")
                + ", christmasScene=" + (manifest.ChristmasScene.HasValue ? manifest.ChristmasScene.Value.ToString() : "(null)");
        }
    }
}
