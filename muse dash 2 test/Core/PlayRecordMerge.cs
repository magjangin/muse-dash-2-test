using System;

namespace muse_dash_2_test
{
    /// <summary>
    /// 이전 기록과 방금 끝난 한 판을 어떻게 합칠지 정하는 순수 판정 규칙입니다.
    /// 게임에 의존하지 않으므로 로직 테스트(<c>PlayRecordMergeTests</c>)로 검증합니다.
    ///
    /// <para><b>핵심은 "최고 기록"과 "달성 배지"가 서로 다른 개념이라는 것입니다.</b>
    /// 예전에는 <c>CustomRecordStore.SaveResult</c> 안에서 <c>isNewHighScore</c> 플래그 하나가
    /// 둘을 겸했고, 그래서 기록이 <b>양방향으로</b> 망가졌습니다.</para>
    ///
    /// <list type="number">
    /// <item><description><b>낮은 점수가 높은 점수를 덮어씀</b>: FC/AP를 처음 달성하면 점수와 무관하게
    /// "신규 최고 기록"으로 쳐서 <c>finalScore = score</c>가 됐습니다. 뮤즈 대시 점수는 피버 사용에
    /// 좌우되므로 AP를 내고도 점수가 낮을 수 있고, 이 모드의 <c>피버충전금지</c> 옵션을 켜면
    /// (피버 게이지가 0으로 고정되어) 아주 쉽게 재현됩니다.</description></item>
    /// <item><description><b>이미 딴 배지가 지워짐</b>: 반대로 FC가 아닌 판이 점수만 높으면
    /// <c>isNewHighScore</c>가 서면서 <c>finalIsFullCombo = isFullCombo</c>(=false)가 되어
    /// 예전에 달성한 FC/AP 표시가 사라졌습니다. 같은 블록의 else 갈래는
    /// <c>existing.isFullCombo || isFullCombo</c>로 누적하고 있었으므로, 누적이 의도였는데
    /// then 갈래에서만 빠뜨린 것이었습니다.</description></item>
    /// </list>
    ///
    /// <para>그래서 여기서는 셋을 각각 따로 정합니다.
    /// 최고 기록은 <b>점수</b>(동점이면 정확도)로만, FC/AP는 <b>한 번 달성하면 유지</b>,
    /// 최고 콤보는 <b>역대 최댓값</b>입니다.</para>
    /// </summary>
    public static class PlayRecordMerge
    {
        /// <summary>합치기 결과입니다. 어느 값을 파일에 쓸지는 호출부가 이걸 보고 정합니다.</summary>
        public readonly struct Outcome
        {
            /// <summary>이번 판이 새 최고 기록인지 여부입니다. 점수·정확도·판정수·저장시각의 출처를 가릅니다.</summary>
            public readonly bool IsNewHighScore;

            public readonly int Score;
            public readonly int MaxCombo;
            public readonly float Accuracy;
            public readonly bool IsFullCombo;
            public readonly bool IsAllPerfect;

            public Outcome(bool isNewHighScore, int score, int maxCombo, float accuracy, bool isFullCombo, bool isAllPerfect)
            {
                IsNewHighScore = isNewHighScore;
                Score = score;
                MaxCombo = maxCombo;
                Accuracy = accuracy;
                IsFullCombo = isFullCombo;
                IsAllPerfect = isAllPerfect;
            }
        }

        /// <summary>
        /// 이전 기록(<paramref name="hasExisting"/>가 false면 없음)과 이번 판을 합칩니다.
        /// </summary>
        public static Outcome Merge(
            bool hasExisting,
            int existingScore, int existingMaxCombo, float existingAccuracy, bool existingIsFullCombo, bool existingIsAllPerfect,
            int newScore, int newMaxCombo, float newAccuracy, bool newIsFullCombo, bool newIsAllPerfect)
        {
            // 최고 기록은 오직 점수로 정합니다. FC/AP 달성 여부는 여기 끼어들지 않습니다.
            // (끼워 넣었다가 위 <summary>의 1번 사고가 났습니다)
            bool isNewHighScore =
                !hasExisting
                || newScore > existingScore
                || (newScore == existingScore && newAccuracy > existingAccuracy);

            // 배지는 누적입니다. 이번 판이 FC가 아니어도 예전에 달성했으면 유지됩니다.
            bool isFullCombo = newIsFullCombo || (hasExisting && existingIsFullCombo);
            bool isAllPerfect = newIsAllPerfect || (hasExisting && existingIsAllPerfect);

            // 최고 콤보도 누적(역대 최댓값)입니다. 최고 기록 판과 묶어 두면 FC 배지는 켜져 있는데
            // maxCombo는 그보다 작은, 앞뒤가 안 맞는 기록이 나옵니다.
            int maxCombo = hasExisting ? Math.Max(existingMaxCombo, newMaxCombo) : newMaxCombo;

            return new Outcome(
                isNewHighScore,
                isNewHighScore || !hasExisting ? newScore : existingScore,
                maxCombo,
                isNewHighScore || !hasExisting ? newAccuracy : existingAccuracy,
                isFullCombo,
                isAllPerfect);
        }
    }
}
