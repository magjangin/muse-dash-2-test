using MelonLoader;

namespace muse_dash_2_test
{
    /// <summary>
    /// 이 모드의 로그는 전부 이 클래스를 거칩니다. <c>MelonLogger</c>를 직접 부르지 마십시오.
    ///
    /// <para>1편에서 <c>MelonLogger.Msg/Warning/Error</c>를 Harmony Prefix로 전역 가로채 음소거하려다,
    /// MelonLoader 0.7.3의 HarmonyX가 순수 관리 메서드를 디투어하는 경로에서 NRE로 터져 <c>PatchAll</c>이 통째로
    /// 중단됐습니다. 로그 수준은 가로채기가 아니라 호출부(여기)에서 판정합니다. 1편의 UMPC 로그 억제 같은 수준
    /// 조절은 필요해지면 여기에 붙입니다.</para>
    /// </summary>
    public static class ModLogger
    {
        public static bool VerboseEnabled { get; set; }

        public static void Msg(string msg) => MelonLogger.Msg(msg);

        public static void Warning(string msg) => MelonLogger.Warning(msg);

        public static void Error(string msg) => MelonLogger.Error(msg);

        public static void Verbose(string msg)
        {
            if (VerboseEnabled) MelonLogger.Msg(msg);
        }
    }
}
