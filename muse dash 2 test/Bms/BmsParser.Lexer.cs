using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace muse_dash_2_test
{
    // BMS 한 줄을 토큰으로 해석하는 저수준 헬퍼 모음(정규식 매칭, 셀 폭/제로셀 판정, 주석 제거 등).
    public static partial class BmsParser
    {
        private static readonly Regex HeaderBpmRegex = new Regex(@"^#BPM\s+([0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        // 표준 BMS는 "#BPM01 240"처럼 공백으로 값을 적습니다. 모드의 경고문과 BMS_PARSING.md도 그렇게 안내하므로
        // 공백 구분을 ':'/'=' 구분과 함께 받습니다. ':'와 '='만 받던 시절에는 이 줄이 일반 메타데이터로 새어 나가
        // 채널 08이 참조할 선언이 사라졌고, BPM 변경이 경고 없이 없던 일이 됐습니다.
        private static readonly Regex BpmAliasRegex = new Regex(@"^#BPM([0-9A-Fa-f]{2})(?:\s*[:=]\s*|\s+)([0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MeasureLineRegex = new Regex(@"^#(?<measure>\d{3})(?<channel>[0-9A-Fa-f]{2})\s*:\s*(?<data>[0-9A-Za-z\s]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MetadataRegex = new Regex(@"^#(?<key>[A-Za-z0-9_]+)\s+(?<value>.+)$", RegexOptions.Compiled);

        private static bool TryParseHeaderBpm(string line, out float bpm)
        {
            bpm = 0f;
            var match = HeaderBpmRegex.Match(line);
            if (!match.Success)
            {
                return false;
            }

            return float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out bpm);
        }

        private static bool TryParseBpmAlias(string line, out string key, out float bpm)
        {
            key = null;
            bpm = 0f;

            var match = BpmAliasRegex.Match(line);
            if (!match.Success)
            {
                return false;
            }

            key = match.Groups[1].Value.ToUpperInvariant();
            return float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out bpm);
        }

        private static bool TryParseMetadata(string line, out string key, out string value)
        {
            key = null;
            value = null;

            if (!line.StartsWith("#", StringComparison.Ordinal))
            {
                return false;
            }

            // Other types of # lines (measure lines, header bpms, and bpm aliases) are already checked before
            // calling TryParseMetadata in the loop, so we don't need redundant Regex IsMatch checks.
            var match = MetadataRegex.Match(line);
            if (!match.Success)
            {
                return false;
            }

            key = match.Groups["key"].Value.Trim();
            value = match.Groups["value"].Value.Trim();
            return !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryParseMeasureLine(string line, out int measure, out int channel, out string data)
        {
            measure = 0;
            channel = 0;
            data = null;

            var match = MeasureLineRegex.Match(line);
            if (!match.Success)
            {
                return false;
            }

            if (!int.TryParse(match.Groups["measure"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out measure))
            {
                return false;
            }

            if (!int.TryParse(match.Groups["channel"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out channel))
            {
                return false;
            }

            data = match.Groups["data"].Value;
            return !string.IsNullOrWhiteSpace(data);
        }

        private static int GetCellCount(string data, int cellWidth)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return 0;
            }

            int width = Math.Max(2, cellWidth);
            return data.Length / width;
        }

        private static int DetectWavCellWidth(IReadOnlyDictionary<string, string> metadata)
        {
            int width = 2;
            if (metadata == null || metadata.Count == 0)
            {
                return width;
            }

            foreach (var key in metadata.Keys)
            {
                if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("WAV", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string rawKey = key.Substring(3);
                if (rawKey.Length > width)
                {
                    width = rawKey.Length;
                }
            }

            return width;
        }

        private static bool IsZeroCell(string data, int offset, int width)
        {
            for (int i = offset; i < offset + width; i++)
            {
                if (data[i] != '0')
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeHexData(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                char ch = data[i];
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                }
            }

            return builder.ToString();
        }

        /// <summary>줄 전체가 주석('//' 또는 ';'로 시작)인지 판정합니다. 앞뒤 공백을 걷어 낸 줄을 넘겨 주세요.</summary>
        private static bool IsCommentLine(string trimmedLine)
        {
            return trimmedLine.StartsWith("//", StringComparison.Ordinal)
                || trimmedLine.StartsWith(";", StringComparison.Ordinal);
        }

        /// <summary>
        /// 줄 뒤에 붙은 '//' 또는 ';' 주석을 떼어 냅니다. 마디·BPM 줄에만 씁니다.
        /// <para>두 표식이 섞여 있으면 먼저 나온 쪽에서 자릅니다. 예전에는 '//'를 늘 먼저 찾아서
        /// "01 ; 메모 // 메모"가 ';' 뒤까지 남았고, 그 줄은 마디 줄로 읽히지 않았습니다.</para>
        /// </summary>
        private static string StripComments(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return string.Empty;
            }

            int slashIndex = line.IndexOf("//", StringComparison.Ordinal);
            int semicolonIndex = line.IndexOf(';');

            int commentIndex = slashIndex < 0
                ? semicolonIndex
                : (semicolonIndex < 0 ? slashIndex : Math.Min(slashIndex, semicolonIndex));

            return commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
        }
    }
}
