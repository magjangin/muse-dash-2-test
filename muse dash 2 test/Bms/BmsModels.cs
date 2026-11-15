using System;
using System.Collections.Generic;

namespace muse_dash_2_test
{
    public enum BmsLane
    {
        Note = 0,
        BossInOut = 2,   // 보스 등장/퇴장
        BossAction = 3,  // 보스 액션/패턴
        Unknown = -1
    }

    public sealed class BmsChart
    {
        internal BmsChart(string sourcePath)
        {
            SourcePath = sourcePath;
            Notes = new List<BmsNote>();
            BpmChanges = new List<BpmChange>();
            BpmDefinitions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            WavInfoCache = new Dictionary<string, BmsWavInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public string SourcePath { get; }
        public string Title { get; internal set; }
        public string Artist { get; internal set; }
        public float DefaultBpm { get; internal set; }
        public IReadOnlyDictionary<string, float> BpmDefinitions { get; internal set; }
        public Dictionary<string, string> Metadata { get; internal set; }
        public IReadOnlyList<BpmChange> BpmChanges { get; internal set; }
        public IReadOnlyList<BmsNote> Notes { get; internal set; }
        internal Dictionary<string, BmsWavInfo> WavInfoCache { get; }
    }

    public sealed class BmsNote
    {
        public int Measure { get; internal set; }
        public int Channel { get; internal set; }
        public int CellIndex { get; internal set; }
        public BmsLane Lane { get; internal set; }
        public string RawValue { get; internal set; }
        public float Tick { get; internal set; }
        public float Time { get; internal set; }
    }

    public sealed class BpmChange
    {
        public float Tick { get; internal set; }
        public float Bpm { get; internal set; }
        public string Source { get; internal set; }

        // 같은 틱에 여러 변경이 모였을 때의 적용 순서(작을수록 먼저). 기본 BPM이 0번이고 채보에서 읽은 변경이
        // 선언 순서대로 그 뒤를 잇습니다. 이름 문자열로 정렬하면 0틱 변경이 기본 BPM보다 앞서 적용되고 곧바로 덮였습니다.
        internal int Order { get; set; }
    }
}
