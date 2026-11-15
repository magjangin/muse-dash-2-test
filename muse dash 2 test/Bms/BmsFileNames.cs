using System;
using System.IO;

namespace muse_dash_2_test
{
    /// <summary>
    /// BMS 파일 이름에 대한 순수 판정 규칙. 게임에 의존하지 않으므로 로직 테스트로 검증합니다.
    /// </summary>
    public static class BmsFileNames
    {
        /// <summary>낱말로 쓰였을 때만 임시 파일로 볼 표식들.</summary>
        private static readonly string[] TempWords = { "temp", "tmp" };

        /// <summary>
        /// 편집기/동기화 도구가 만든 임시 파일이라 채보 후보에서 제외해야 하는지 판정합니다.
        ///
        /// <para><b>판정을 좁게 잡는 이유</b>: 예전에는 파일명 아무 곳에나 "temp"/"tmp"가 들어가면
        /// 임시로 봤습니다. 그래서 <c>tempo.bms</c>, <c>attempt.bms</c> 같은 정상 채보가
        /// <b>아무 메시지 없이</b> 무시됐습니다. 여기서 잘못 걸러내면 "채보가 그냥 안 뜬다"는
        /// 원인 찾기 어려운 증상이 되므로, 임시 파일을 하나 더 읽는 쪽보다 정상 파일을
        /// 놓치지 않는 쪽으로 기울여 판정합니다.</para>
        /// </summary>
        public static bool IsTempFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return true;

            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName)) return true;

            // 편집기·동기화 도구는 임시본임을 이름 '앞'에 표시합니다.
            if (fileName.StartsWith("~", StringComparison.Ordinal)) return true;   // 오피스/메모장 계열 잠금·임시본
            if (fileName.StartsWith("___", StringComparison.Ordinal)) return true; // 모드가 쓰는 표식
            if (fileName.StartsWith(".", StringComparison.Ordinal)) return true;   // 숨김 파일·동기화 부산물

            // vim/gedit 계열 백업본(chart.bms~).
            if (fileName.EndsWith("~", StringComparison.Ordinal)) return true;

            string baseName = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(baseName)) return true;

            return IsTempWord(baseName);
        }

        /// <summary>
        /// info.txt에 적힌 채보 파일 이름을 곡 폴더 기준 전체 경로로 바꿉니다. 파일이 실제로 있는지는 보지 않습니다.
        ///
        /// <para>곡 폴더 안의 <c>.bms</c>만 받습니다. 곡은 남이 만든 폴더째로 받기도 하므로, info.txt 한 줄이
        /// 폴더 밖 파일을 읽게 하지 않습니다. 파일 변경 감지도 곡 폴더 안의 <c>.bms</c>만 봅니다.</para>
        /// </summary>
        public static bool TryResolveInSongFolder(string songDir, string configuredName, out string fullPath)
        {
            fullPath = null;
            if (string.IsNullOrWhiteSpace(songDir) || string.IsNullOrWhiteSpace(configuredName)) return false;

            string name = configuredName.Trim();
            if (Path.IsPathRooted(name)) return false;
            if (!string.Equals(Path.GetExtension(name), ".bms", StringComparison.OrdinalIgnoreCase)) return false;

            try
            {
                string root = Path.GetFullPath(songDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(Path.Combine(root, name));
                if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;

                fullPath = candidate;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        /// 이름이 "temp"/"tmp" 그 자체이거나, 그 뒤에 글자가 아닌 문자가 이어질 때만 참입니다.
        /// (<c>temp</c>·<c>tmp_1</c>·<c>temp-backup</c>은 임시, <c>tempo</c>·<c>attempt</c>는 정상 채보)
        /// </summary>
        private static bool IsTempWord(string baseName)
        {
            foreach (string word in TempWords)
            {
                if (baseName.Equals(word, StringComparison.OrdinalIgnoreCase)) return true;

                if (baseName.Length > word.Length
                    && baseName.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                    && !char.IsLetter(baseName[word.Length]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
