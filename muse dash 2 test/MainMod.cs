using MelonLoader;

[assembly: MelonInfo(typeof(muse_dash_2_test.MainMod), "muse-dash-2-custom-chart", muse_dash_2_test.ModInfo.Version, "화영왕")]
[assembly: MelonColor(255, 147, 112, 219)] // 1편과 같은 보라색(MediumPurple #9370DB)
// TODO(출시일): MelonLoader 로그의 "Game Developer" / "Game Name" 줄을 보고 채우십시오(1편: "PeroPeroGames", "MuseDash").
// 지정하지 않으면 어떤 게임에서도 로드되는 범용 모드로 취급됩니다.
// [assembly: MelonGame("PeroPeroGames", "MuseDash2")]
// 패치는 MelonLoader가 아니라 OnInitializeMelon 첫 줄의 PatchInstaller가 겁니다(깨진 패치를 걸지 않기 위해).
[assembly: HarmonyDontPatchAll]

namespace muse_dash_2_test
{
    /// <summary>
    /// 뮤즈대시 2 모드의 진입점입니다. 지금은 뼈대만 있습니다.
    /// 1편의 기능 이식 순서와 훅 목록은 docs/MD2_LAUNCH_CHECKLIST.md를 보십시오.
    /// </summary>
    public class MainMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            // 1편과 같은 자리입니다. 이 아래 초기화는 전부 패치가 걸린 뒤에 돌아야 합니다.
            PatchInstaller.ApplyAll(HarmonyInstance, MelonAssembly.Assembly);

            ModLogger.Msg($"뮤즈대시 2 모드 v{ModInfo.Version}이(가) 로드되었습니다.");

            // 게임 업데이트로 깨진 패치 대상이 있는지 시작 시 점검하여 요약 로그로 표시합니다.
            PatchHealthCheck.Run();
        }
    }
}
