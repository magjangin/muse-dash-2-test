using System;
using System.Collections.Generic;

namespace muse_dash_2_test
{
    public sealed class BmsBossSwapEvent
    {
        public BmsNote OutNote { get; set; }
        public BmsNote InNote { get; set; }
        public BmsWavInfo OutWav { get; set; }
        public BmsWavInfo InWav { get; set; }
        public string BossAction { get; set; }
        public float DelaySeconds => InNote != null && OutNote != null ? InNote.Time - OutNote.Time : 0f;
    }

    public static class BmsBossSwapPlanner
    {
        public static List<BmsBossSwapEvent> BuildSwapEvents(BmsChart chart)
        {
            var events = new List<BmsBossSwapEvent>();
            if (chart == null || chart.Notes == null || chart.Notes.Count == 0)
            {
                return events;
            }

            BmsNote pendingOutNote = null;
            BmsWavInfo pendingOutWav = null;

            foreach (var note in chart.Notes)
            {
                var wavInfo = ResolveWavInfo(chart, note);
                if (wavInfo == null)
                {
                    continue;
                }

                if (string.Equals(wavInfo.BossTransition, "out", StringComparison.OrdinalIgnoreCase))
                {
                    pendingOutNote = note;
                    pendingOutWav = wavInfo;
                    continue;
                }

                if (pendingOutNote != null && string.Equals(wavInfo.BossTransition, "in", StringComparison.OrdinalIgnoreCase))
                {
                    string swapAction = BuildSwapAction(wavInfo);
                    if (!string.IsNullOrWhiteSpace(swapAction))
                    {
                        events.Add(new BmsBossSwapEvent
                        {
                            OutNote = pendingOutNote,
                            InNote = note,
                            OutWav = pendingOutWav,
                            InWav = wavInfo,
                            BossAction = swapAction
                        });
                    }

                    pendingOutNote = null;
                    pendingOutWav = null;
                }
            }

            return events;
        }

        public static BmsWavInfo ResolveWavInfo(BmsChart chart, BmsNote note)
        {
            if (chart == null || note == null)
            {
                return null;
            }

            string rawValue = note.RawValue ?? string.Empty;
            if (chart.WavInfoCache.TryGetValue(rawValue, out var cachedWavInfo))
            {
                return cachedWavInfo;
            }

            string wavKey = "WAV" + rawValue.ToUpperInvariant();
            string wavName = null;
            if (chart.Metadata != null && chart.Metadata.TryGetValue(wavKey, out string metadataWavName))
            {
                wavName = metadataWavName;
            }

            if (string.IsNullOrWhiteSpace(wavName))
            {
                wavName = rawValue + ".wav";
            }

            var wavInfo = BmsWavParser.ParseWavName(wavName);
            chart.WavInfoCache[rawValue] = wavInfo;
            return wavInfo;
        }

        public static string BuildSwapAction(BmsWavInfo inWav)
        {
            if (inWav == null || string.IsNullOrWhiteSpace(inWav.BossName) || inWav.BossScene < 0)
            {
                return string.Empty;
            }

            return $"swap:{inWav.BossName}:{inWav.BossScene}";
        }
    }
}
