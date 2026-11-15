using System;
using System.Collections.Generic;
using System.IO;

namespace muse_dash_2_test
{
    /// <summary>
    /// 한 곡의 채보 묶음입니다. 난이도마다 따로 지정한 채보와, 지정이 없는 난이도가 함께 쓰는 기본 채보를 담습니다.
    /// 다 만든 뒤에는 고치지 않고 통째로 바꿔 끼우므로, 읽는 쪽이 중간 상태를 보지 않습니다.
    /// </summary>
    public sealed class BmsChartSet
    {
        /// <summary>게임의 난이도 번호는 1~5입니다(<c>MusicInfo.difficulty1~5</c>).</summary>
        public const int MaxDifficulty = 5;

        private readonly BmsChart[] byDifficulty = new BmsChart[MaxDifficulty + 1];

        public BmsChartSet(BmsChart defaultChart)
        {
            Default = defaultChart;
        }

        public BmsChart Default { get; }

        public bool IsEmpty
        {
            get
            {
                if (Default != null) return false;
                for (int i = 1; i <= MaxDifficulty; i++)
                {
                    if (byDifficulty[i] != null) return false;
                }
                return true;
            }
        }

        public void SetForDifficulty(int difficulty, BmsChart chart)
        {
            if (IsValidDifficulty(difficulty))
            {
                byDifficulty[difficulty] = chart;
            }
        }

        /// <summary>이 난이도에 따로 지정된 채보만 돌려줍니다. 없으면 null입니다.</summary>
        public BmsChart ExplicitFor(int difficulty)
        {
            return IsValidDifficulty(difficulty) ? byDifficulty[difficulty] : null;
        }

        /// <summary>이 난이도에서 플레이할 채보입니다. 따로 지정된 채보가 없으면 기본 채보를 씁니다.</summary>
        public BmsChart ForDifficulty(int difficulty)
        {
            return ExplicitFor(difficulty) ?? Default;
        }

        /// <summary>로그용 요약입니다. 예: <c>기본=chart.bms, 1=easy.bms, 3=master.bms</c></summary>
        public string Describe()
        {
            var parts = new List<string> { "기본=" + NameOf(Default) };
            for (int i = 1; i <= MaxDifficulty; i++)
            {
                if (byDifficulty[i] != null)
                {
                    parts.Add(i + "=" + NameOf(byDifficulty[i]));
                }
            }
            return string.Join(", ", parts);
        }

        /// <summary>
        /// 파일 변경으로 다시 읽은 결과를 지금 쓰는 묶음과 합칩니다. 칸마다 따로 판단합니다.
        /// 새로 읽은 채보가 있으면 그것을, 못 읽었지만 옛 파일이 남아 있으면(저장 도중일 수 있습니다) 옛 채보를 쓰고,
        /// 옛 파일까지 사라졌으면 비웁니다. 저장 중인 난이도 하나 때문에 다른 난이도 채보가 흔들리지 않습니다.
        /// </summary>
        /// <param name="keptOld">옛 채보를 남긴 칸이 있으면 참입니다. 호출자는 잠시 뒤 다시 읽어야 합니다.</param>
        public static BmsChartSet MergeReload(BmsChartSet fresh, BmsChartSet current, Func<string, bool> fileExists, out bool keptOld)
        {
            keptOld = false;
            var merged = new BmsChartSet(PickReloaded(fresh?.Default, current?.Default, fileExists, ref keptOld));
            for (int i = 1; i <= MaxDifficulty; i++)
            {
                merged.SetForDifficulty(i, PickReloaded(fresh?.ExplicitFor(i), current?.ExplicitFor(i), fileExists, ref keptOld));
            }
            return merged;
        }

        private static BmsChart PickReloaded(BmsChart fresh, BmsChart current, Func<string, bool> fileExists, ref bool keptOld)
        {
            if (fresh?.Notes != null && fresh.Notes.Count > 0)
            {
                return fresh;
            }

            if (current == null || string.IsNullOrEmpty(current.SourcePath) || !fileExists(current.SourcePath))
            {
                return null;
            }

            keptOld = true;
            return current;
        }

        private static bool IsValidDifficulty(int difficulty)
        {
            return difficulty >= 1 && difficulty <= MaxDifficulty;
        }

        private static string NameOf(BmsChart chart)
        {
            return chart == null ? "(없음)" : Path.GetFileName(chart.SourcePath) ?? "(이름 없음)";
        }
    }
}
