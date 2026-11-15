using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace muse_dash_2_test
{
    // BMS 텍스트 → BmsChart 변환 본체.
    // 저수준 토큰 파싱(정규식/셀 처리)은 BmsParser.Lexer.cs, 데이터 모델은 BmsModels.cs 참고.
    public static partial class BmsParser
    {
        private static readonly Dictionary<int, BmsLane> ChannelToLaneMap = new Dictionary<int, BmsLane>
        {
            { 0x13, BmsLane.Note },
            { 0x14, BmsLane.Note },
            { 0x15, BmsLane.BossInOut },
            { 0x18, BmsLane.BossAction },
        };

        public static BmsChart ParseFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath is null or empty.", nameof(filePath));
            }

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                return ParseText(sr.ReadToEnd(), filePath);
            }
        }

        public static BmsChart ParseText(string text, string sourcePath = null)
        {
            var chart = new BmsChart(sourcePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                return chart;
            }

            var measureEvents = new List<RawMeasureEvent>();
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var bpmDefinitions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            float defaultBpm = 120f;
            bool sawNonPositiveHeaderBpm = false;

            using (var reader = new StringReader(text))
            {
                string rawLine;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    var trimmedLine = rawLine.Trim();
                    if (trimmedLine.Length == 0 || IsCommentLine(trimmedLine))
                    {
                        continue;
                    }

                    // 마디·BPM 줄은 형식이 정해져 있어 뒤에 붙은 주석을 떼어 내고 읽습니다.
                    // 곡 정보(아래 TryParseMetadata)는 자유 텍스트라 떼지 않은 trimmedLine을 씁니다.
                    var line = StripComments(trimmedLine).Trim();

                    // Check TryParseMeasureLine first as 95%+ lines in BMS are measure lines
                    if (TryParseMeasureLine(line, out int measure, out int channel, out string data))
                    {
                        measureEvents.Add(new RawMeasureEvent
                        {
                            Measure = measure,
                            Channel = channel,
                            Data = NormalizeHexData(data),
                        });
                        continue;
                    }

                    if (TryParseHeaderBpm(line, out float headerBpm))
                    {
                        // #BPM 0을 받아들이면 아래 시간 계산이 60 / 0이 되어 모든 노트 시간이 무한대가 됩니다.
                        if (headerBpm > 0f)
                        {
                            defaultBpm = headerBpm;
                        }
                        else
                        {
                            sawNonPositiveHeaderBpm = true;
                        }
                        continue;
                    }

                    if (TryParseBpmAlias(line, out string bpmKey, out float bpmValue))
                    {
                        bpmDefinitions[bpmKey] = bpmValue;
                        continue;
                    }

                    if (TryParseMetadata(trimmedLine, out string metaKey, out string metaValue))
                    {
                        string normalizedMetaValue = TextNormalizer.NormalizeEnglishContractions(metaValue);
                        metadata[metaKey] = normalizedMetaValue;
                        if (string.Equals(metaKey, "TITLE", StringComparison.OrdinalIgnoreCase))
                        {
                            chart.Title = normalizedMetaValue;
                        }
                        else if (string.Equals(metaKey, "ARTIST", StringComparison.OrdinalIgnoreCase))
                        {
                            chart.Artist = normalizedMetaValue;
                        }
                        continue;
                    }
                }
            }

            chart.DefaultBpm = defaultBpm;
            chart.BpmDefinitions = bpmDefinitions;
            chart.Metadata = metadata;
            int noteCellWidth = DetectWavCellWidth(metadata);

            var bpmChanges = new List<BpmChange>
            {
                new BpmChange
                {
                    Tick = 0f,
                    Bpm = defaultBpm,
                    Source = "default",
                    Order = 0
                }
            };

            bool sawDirectBpmChannel = false;
            foreach (var evt in measureEvents)
            {
                if (evt.Channel == 0x08)
                {
                    AddBpmReferenceChanges(evt, bpmDefinitions, bpmChanges);
                }
                else if (evt.Channel == 0x03)
                {
                    sawDirectBpmChannel = true;
                }
            }

            // 채널 03(직접 BPM 변경)은 지원하지 않습니다. BPM 변경은 #BPMxx 선언 + 채널 08로 적어 주세요.
            // 조용히 무시하면 "BPM이 왜 안 바뀌지"가 원인 찾기 어려운 증상이 되므로 한 줄 남깁니다.
            if (sawDirectBpmChannel)
            {
                ModLogger.Warning(
                    $"[BmsParser] 채널 03(직접 BPM 변경)은 지원하지 않아 무시했습니다: {sourcePath ?? "(경로 없음)"}. "
                    + "BPM 변경은 '#BPMxx nnn' 선언과 채널 08로 적어 주세요.");
            }

            // 무한대 노트 시간은 모드가 게임 노트 값(Decimal)으로 옮기는 순간 예외가 되어 차트 주입이 통째로 실패합니다.
            // 원인이 로그에 드러나도록 한 줄 남깁니다.
            if (sawNonPositiveHeaderBpm)
            {
                ModLogger.Warning(
                    $"[BmsParser] 0 이하인 #BPM 줄을 무시했습니다 (적용 BPM: {defaultBpm.ToString("0.###", CultureInfo.InvariantCulture)}): {sourcePath ?? "(경로 없음)"}. "
                    + "'#BPM 120'처럼 0보다 큰 값을 적어 주세요.");
            }

            var notes = new List<BmsNote>();
            foreach (var evt in measureEvents)
            {
                if (!ChannelToLaneMap.TryGetValue(evt.Channel, out BmsLane lane))
                {
                    continue;
                }

                AddNoteEvents(evt, lane, notes, noteCellWidth);
            }

            // 같은 틱이면 선언 순서대로 적용합니다(기본 BPM이 항상 첫 항목).
            // 예전에는 Source 이름으로 정렬해서 0틱의 "BPM01"이 "default"보다 먼저 적용되고
            // 곧바로 기본 BPM에 덮여, 첫 마디의 BPM 변경이 통째로 사라졌습니다.
            bpmChanges.Sort((left, right) =>
            {
                int tickCompare = left.Tick.CompareTo(right.Tick);
                return tickCompare != 0
                    ? tickCompare
                    : left.Order.CompareTo(right.Order);
            });

            // O(N + M) single sweep to calculate time for all notes
            notes.Sort((left, right) =>
            {
                int tickCompare = left.Tick.CompareTo(right.Tick);
                return tickCompare != 0 ? tickCompare : left.Channel.CompareTo(right.Channel);
            });
            float time = 0f;
            float currentTick = 0f;
            float currentBpm = (bpmChanges.Count > 0 && bpmChanges[0].Bpm > 0f) ? bpmChanges[0].Bpm : defaultBpm;
            int bpmIndex = 0;

            foreach (var note in notes)
            {
                while (bpmIndex < bpmChanges.Count)
                {
                    var change = bpmChanges[bpmIndex];
                    if (change.Tick > note.Tick)
                    {
                        break;
                    }

                    if (change.Tick > currentTick)
                    {
                        time += (change.Tick - currentTick) * 4f * (60f / currentBpm);
                        currentTick = change.Tick;
                    }

                    currentBpm = change.Bpm > 0f ? change.Bpm : currentBpm;
                    bpmIndex++;
                }

                float noteTime = time;
                if (note.Tick > currentTick)
                {
                    noteTime += (note.Tick - currentTick) * 4f * (60f / currentBpm);
                }
                note.Time = noteTime;
            }

            chart.BpmChanges = bpmChanges;
            chart.Notes = notes;
            return chart;
        }

        public static float CalculateTime(float tick, IReadOnlyList<BpmChange> bpmChanges)
        {
            if (bpmChanges == null || bpmChanges.Count == 0)
            {
                return tick * 4f * (60f / 120f);
            }

            float time = 0f;
            float currentTick = 0f;
            float currentBpm = bpmChanges[0].Bpm <= 0f ? 120f : bpmChanges[0].Bpm;

            foreach (var change in bpmChanges)
            {
                if (change.Tick <= currentTick)
                {
                    currentBpm = change.Bpm > 0f ? change.Bpm : currentBpm;
                    continue;
                }

                if (change.Tick > tick)
                {
                    break;
                }

                time += (change.Tick - currentTick) * 4f * (60f / currentBpm);
                currentTick = change.Tick;
                currentBpm = change.Bpm > 0f ? change.Bpm : currentBpm;
            }

            if (tick > currentTick)
            {
                time += (tick - currentTick) * 4f * (60f / currentBpm);
            }

            return time;
        }

        private static void AddBpmReferenceChanges(RawMeasureEvent evt, Dictionary<string, float> bpmDefinitions, List<BpmChange> bpmChanges)
        {
            int cellWidth = 2;
            int cellCount = GetCellCount(evt.Data, cellWidth);
            if (cellCount == 0)
            {
                return;
            }

            for (int i = 0; i < cellCount; i++)
            {
                int offset = i * cellWidth;
                if (IsZeroCell(evt.Data, offset, cellWidth))
                {
                    continue;
                }

                string cell = evt.Data.Substring(offset, cellWidth);
                if (!bpmDefinitions.TryGetValue(cell, out float bpm))
                {
                    continue;
                }

                bpmChanges.Add(new BpmChange
                {
                    Tick = evt.Measure + (i / (float)cellCount),
                    Bpm = bpm,
                    Source = $"BPM{cell}",
                    Order = bpmChanges.Count
                });
            }
        }

        private static void AddNoteEvents(RawMeasureEvent evt, BmsLane lane, List<BmsNote> notes, int cellWidth)
        {
            int width = Math.Max(2, cellWidth);
            int cellCount = GetCellCount(evt.Data, width);
            if (cellCount == 0)
            {
                return;
            }

            for (int i = 0; i < cellCount; i++)
            {
                int offset = i * width;
                if (IsZeroCell(evt.Data, offset, width))
                {
                    continue;
                }

                string cell = evt.Data.Substring(offset, width);
                notes.Add(new BmsNote
                {
                    Measure = evt.Measure,
                    Channel = evt.Channel,
                    CellIndex = i,
                    Lane = lane,
                    RawValue = cell,
                    Tick = evt.Measure + (i / (float)cellCount)
                });
            }
        }

        private sealed class RawMeasureEvent
        {
            public int Measure;
            public int Channel;
            public string Data;
        }
    }
}
