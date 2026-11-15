using System;
using System.Globalization;

namespace muse_dash_2_test
{
    /// <summary>노트 dt 권장표가 묶는 노트 계열입니다.</summary>
    public enum DtFamily
    {
        /// <summary>권장표에 없는 계열(샌드백·보스 톱니·속도 자리가 없는 uid 등). WAV <c>_dt</c>를 그대로 둡니다.</summary>
        None,
        /// <summary>일반 / 트랩 / 음표 / Up·Down (하트·고스트 포함).</summary>
        Normal,
        /// <summary>홀드(롱노트). 막대 길이를 속도별 고정 시간으로 그리기 때문에 일반과 값이 다릅니다.</summary>
        Hold,
        /// <summary>보스 발사체(xx=06/07/08, 기본 레인).</summary>
        BossProjectile,
        /// <summary>씬 전환(0004yy)·보스 액션처럼 속도가 없는 빈 트리거. dt=0.</summary>
        FixedEvent
    }

    /// <summary>
    /// 노트 계열·속도별 dt 권장표(2026-10-25 실기 확정)와 차터 허용 범위입니다.
    /// <para>모드는 기본적으로 WAV <c>_dt</c>를 그대로 씁니다. <c>hwa info.txt</c>의
    /// 'dt를 권장 표로 고치기 = 활성화'일 때만 이 표로 덮어씁니다(docs/guides/DT_RECOMMENDED_TABLE.md).</para>
    /// </summary>
    public static class BmsDtTable
    {
        // [속도 1, 속도 2, 속도 3]
        private static readonly double[] NormalDt = { 1.48, 1.28, 1.18 };
        private static readonly double[] HoldDt = { 1.47, 1.26, 1.10 };
        private static readonly double[] BossProjectileDt = { 0.8, 0.76, 0.6 };

        // 5번 씬 발사체2·3(xx=07/08)만 비행 애니메이션이 7~8프레임 깁니다(2026-11-11, 게임 번들 프리팹 전수 조사):
        //   다른 씬 in_boss_2_23 / 20 / 17  →  5번 씬 in_boss_2_31 / 28 / 24 (속도 1 / 2 / 3).
        // 위 표에 그 프레임 차이(1프레임 = 1/30초)를 더한 값입니다. 속도 1의 1.07은 summertime(5번 씬)에서 실기 확인,
        // 속도 2·3은 계산값입니다. 0.8을 그대로 쓰면 판정이 노트보다 약 0.27초 먼저 옵니다.
        // 발사체1(xx=06)은 5번 씬도 21 / 18 / 15로 다른 씬(20 / 18 / 15)과 거의 같아 따로 두지 않습니다.
        private static readonly double[] Scene05ProjectileDt = { 1.07, 1.03, 0.83 };

        // 일반 계열에서 차터가 권장표를 벗어나 고를 수 있는 범위(사용자 지정, 2026-10-25).
        // 속도 1은 범위 없이 1.48 고정입니다.
        private static readonly double[] NormalMin = { 1.48, 1.18, 0.98 };
        private static readonly double[] NormalMax = { 1.48, 1.38, 1.28 };

        // WAV 이름의 dt는 소수 3자리로 반올림돼 들어옵니다.
        private const double Epsilon = 0.0005;

        public static DtFamily Classify(string uid, int noteType, string bossAction)
        {
            if (uid != null && uid.StartsWith("0004", StringComparison.Ordinal)) return DtFamily.FixedEvent;
            if (noteType == 9) return DtFamily.FixedEvent;

            string xx = uid != null && uid.Length >= 4 ? uid.Substring(2, 2) : null;
            int motionGroup = MotionGroup(uid);
            bool projectile = (xx == "06" || xx == "07" || xx == "08") && (motionGroup == 0 || motionGroup == 1);
            if (projectile) return DtFamily.BossProjectile;

            // 보스 등장·퇴장·원거리 시작/끝(xx=01, boss_in/out/swap)은 dt=0인 빈 트리거입니다.
            if (noteType == 0 && !string.IsNullOrWhiteSpace(bossAction)) return DtFamily.FixedEvent;
            if (xx == "01") return DtFamily.FixedEvent;

            if (Speed(uid) == 0) return DtFamily.None;

            switch (noteType)
            {
                case 3: return DtFamily.Hold;
                case 8: return DtFamily.None; // 샌드백: 실측값 없음
            }

            // 보스 톱니(xx=09)는 공식 곡 dt가 발사체와 같아 보이지만(0.667, 속도 2) 실기 확정값이 없습니다.
            if (xx == "09") return DtFamily.None;

            return DtFamily.Normal;
        }

        /// <summary>uid <c>yy</c>의 속도 1·2·3. 속도 자리가 없는 uid(0004yy, xx=01, 범위 밖)는 0.</summary>
        public static int Speed(string uid)
            => TryParseYy(uid, out int yy) ? (yy - 1) % 3 + 1 : 0;

        private static int MotionGroup(string uid)
            => TryParseYy(uid, out int yy) ? (yy - 1) / 3 : -1;

        private static bool TryParseYy(string uid, out int yy)
        {
            yy = 0;
            if (uid == null || uid.Length < 6) return false;
            if (uid.StartsWith("0004", StringComparison.Ordinal) || uid.Substring(2, 2) == "01") return false;
            return int.TryParse(uid.Substring(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out yy) && yy >= 1 && yy <= 18;
        }

        /// <summary>
        /// 권장 dt. 권장표에 없는 계열이면 false.
        /// <paramref name="uid"/>를 주면 씬마다 다른 값(5번 씬 발사체2·3)을 고릅니다. 없으면 기본 표입니다.
        /// </summary>
        public static bool TryGetRecommended(DtFamily family, int speed, out double dt, string uid = null)
        {
            dt = -1.0;
            if (family == DtFamily.FixedEvent)
            {
                dt = 0.0;
                return true;
            }

            double[] table = TableFor(family, uid);
            if (table == null || speed < 1 || speed > 3) return false;

            dt = table[speed - 1];
            return true;
        }

        /// <summary>
        /// 차터가 적은 dt가 허용되는지 봅니다. 일반 계열은 범위(속도 1 = 1.48, 속도 2 = 1.18~1.38,
        /// 속도 3 = 0.98~1.28), 홀드·보스 발사체·고정 이벤트는 권장값 그대로만 허용합니다.
        /// 권장표에 없는 계열이나 dt를 적지 않은 노트(음수)는 판정하지 않고 true입니다.
        /// </summary>
        public static bool IsAllowed(DtFamily family, int speed, double dt, out string expected, string uid = null)
        {
            expected = null;
            if (dt < 0.0) return true;
            if (!TryGetRecommended(family, speed, out double recommended, uid)) return true;

            if (family == DtFamily.Normal)
            {
                double min = NormalMin[speed - 1];
                double max = NormalMax[speed - 1];
                expected = min == max ? Format(min) : Format(min) + "~" + Format(max);
                return dt >= min - Epsilon && dt <= max + Epsilon;
            }

            expected = Format(recommended);
            return Math.Abs(dt - recommended) <= Epsilon;
        }

        public static string Describe(DtFamily family)
        {
            switch (family)
            {
                case DtFamily.Normal: return "일반";
                case DtFamily.Hold: return "홀드";
                case DtFamily.BossProjectile: return "보스 발사체";
                case DtFamily.FixedEvent: return "고정 이벤트";
                default: return "권장표 밖";
            }
        }

        private static double[] TableFor(DtFamily family, string uid)
        {
            switch (family)
            {
                case DtFamily.Normal: return NormalDt;
                case DtFamily.Hold: return HoldDt;
                case DtFamily.BossProjectile: return IsScene05LongProjectile(uid) ? Scene05ProjectileDt : BossProjectileDt;
                default: return null;
            }
        }

        /// <summary>5번 씬 발사체2·3(<c>0507yy</c>, <c>0508yy</c>)인지. 비행 애니메이션이 다른 씬보다 깁니다.</summary>
        private static bool IsScene05LongProjectile(string uid)
            => uid != null && uid.Length >= 4
               && (uid.StartsWith("0507", StringComparison.Ordinal) || uid.StartsWith("0508", StringComparison.Ordinal));

        private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
