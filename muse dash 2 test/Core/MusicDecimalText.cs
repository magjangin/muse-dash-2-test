using System;
using System.Globalization;

namespace muse_dash_2_test
{
    /// <summary>
    /// 게임이 내놓은 <c>Decimal.ToString()</c> 문자열을 double로 되돌리는 순수 판정 규칙입니다.
    /// 게임에 의존하지 않으므로 로직 테스트(<c>MusicDecimalTextTests</c>)로 검증합니다.
    ///
    /// <para><b>왜 두 번 시도하는가</b>: <c>Il2CppSystem.Decimal.ToString()</c>은 인자가 없어
    /// 문화권을 따릅니다. 소수점 구분자가 쉼표인 로케일(de/fr/ru 등)에서는 "15,5"가 나오는데,
    /// 이를 InvariantCulture로만 파싱하면 실패해서 0.0이 됩니다. 이 값은 정렬 비교뿐 아니라
    /// <c>ApplyBmsDoubleState</c>에서 dt/showTick으로 <b>되써지기 때문에</b>, 0.0 폴백은
    /// 더블 노트의 등장 타이밍을 통째로 망가뜨립니다.</para>
    ///
    /// <para>게다가 문자열을 만드는 쪽과 읽는 쪽이 <b>서로 다른 런타임</b>입니다.
    /// <c>ToString()</c>은 IL2CPP 런타임의 문화권을, 파싱은 관리 런타임의 문화권을 따르므로
    /// 둘이 어긋날 수 있습니다(게임이 InvariantGlobalization으로 빌드된 경우 등).
    /// 그래서 현재 문화권으로 먼저 시도하고, 실패하면 InvariantCulture로 재시도합니다.</para>
    ///
    /// <para>둘 다 실패하면 예외를 던지지 않고 0.0으로 degrade하되, <b>조용히 넘기지 않고</b>
    /// 한 번은 알립니다. 이 변환은 노트마다 도는 자리라 매번 로그를 남기면 그 자체가 렉이 됩니다.</para>
    /// </summary>
    public static class MusicDecimalText
    {
        // 두 로케일 모두에서 실패했을 때 로그가 노트 수만큼 쏟아지는 것을 막습니다.
        private static bool warnedParseFailure;

        /// <summary>현재 문화권을 1순위로 삼아 파싱합니다.</summary>
        public static double Parse(string raw)
        {
            return Parse(raw, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// 1순위 문화권을 지정해 파싱합니다. 실패하면 InvariantCulture로 재시도하고,
        /// 그래도 실패하면 0.0을 돌려주며 1회만 경고합니다.
        /// (테스트가 실제 로케일에 기대지 않도록 1순위를 주입받습니다.)
        /// </summary>
        internal static double Parse(string raw, IFormatProvider primaryCulture)
        {
            if (string.IsNullOrEmpty(raw)) return 0.0;

            // ToString()이 쓴 문화권으로 먼저 되돌립니다.
            if (double.TryParse(raw, NumberStyles.Float, primaryCulture, out double parsed))
            {
                return parsed;
            }

            // 문자열을 만든 런타임과 읽는 런타임의 문화권이 어긋난 경우를 위한 2차 시도.
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            // 여기까지 왔으면 차트 타이밍이 손상됩니다. 조용히 0을 넘기지 않고 반드시 알립니다.
            if (!warnedParseFailure)
            {
                warnedParseFailure = true;
                ModLogger.Error(
                    $"[MusicDecimal] Decimal 파싱 실패: raw='{raw}', culture={CultureInfo.CurrentCulture.Name}. "
                    + "이 상태에서는 더블 노트의 dt/showTick이 0으로 덮어써져 채보 타이밍이 깨집니다. (이 경고는 1회만 출력)");
            }

            return 0.0;
        }
    }
}
