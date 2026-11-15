using System;
using System.Collections.Generic;

namespace muse_dash_2_test
{
    /// <summary>
    /// 숙주 곡에서 어느 난이도의 차트 에셋을 불러올지 정합니다. 숙주 차트는 노트를 복제할 틀로만 쓰이므로 어느 난이도여도 됩니다.
    ///
    /// <para><b>왜 필요한가</b>: 게임은 고른 난이도로 <c>&lt;곡&gt;_map&lt;N&gt;</c> 에셋을 불러옵니다. 숙주 곡에 그 난이도가 없으면
    /// null을 받아 <c>GetMusicDataFromStageInfo</c>에서 NRE가 나고 보라색 화면에서 멈춥니다.
    /// (2026-11-05 Android 실측: 숙주 0-4는 <c>difficulty3</c>이 "0"이라 Master로 시작할 수 없었습니다.)</para>
    /// </summary>
    public static class BmsHostMaps
    {
        /// <summary>게임의 난이도 문자열로 그 난이도 에셋이 있는지 봅니다. 없는 난이도는 "0"이나 빈 값이고, "?" 같은 표기는 있는 것입니다.</summary>
        public static bool HasMapForLevel(string level)
        {
            string text = level?.Trim();
            return !string.IsNullOrEmpty(text) && text != "0";
        }

        /// <summary>
        /// 숙주 곡의 난이도 문자열 목록(<c>difficulty1~5</c> 순서)에서 실제로 있는 난이도 번호를 뽑습니다.
        /// </summary>
        public static int[] AvailableDifficulties(IReadOnlyList<string> levels)
        {
            var available = new List<int>();
            for (int i = 0; i < levels.Count; i++)
            {
                if (HasMapForLevel(levels[i])) available.Add(i + 1);
            }
            return available.ToArray();
        }

        /// <summary>
        /// 불러올 차트 에셋의 난이도 번호입니다. 고른 난이도가 있으면 그대로, 없으면 그보다 낮은 것 중 가장 높은 것,
        /// 그것도 없으면 가장 낮은 것입니다. 숙주 정보를 모르면(빈 목록) 고른 난이도를 그대로 돌려줍니다.
        /// </summary>
        public static int PickMap(int[] available, int difficulty)
        {
            if (available == null || available.Length == 0 || Array.IndexOf(available, difficulty) >= 0)
            {
                return difficulty;
            }

            int lowest = int.MaxValue;
            int highestBelow = -1;
            foreach (int candidate in available)
            {
                lowest = Math.Min(lowest, candidate);
                if (candidate < difficulty) highestBelow = Math.Max(highestBelow, candidate);
            }
            return highestBelow >= 0 ? highestBelow : lowest;
        }
    }
}
