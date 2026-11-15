namespace muse_dash_2_test
{
    public class HwaManifest
    {
        public string SourcePath;
        public string Uid;
        public string Title;
        public string CustomTitle;
        public string Artist;
        public string CustomArtist;
        public string LevelDesigner;
        public string Album;
        public int? Scene;
        public int? Difficulty1;
        public int? Difficulty2;
        public int? Difficulty3;
        public int? Difficulty4;
        public int? Difficulty5;

        /// <summary>
        /// 난이도별 채보 파일 이름입니다. 칸 번호가 난이도 번호(1~5)이고 0번 칸은 쓰지 않습니다.
        /// `hwa info.txt`의 '난이도1 bms: easy.bms' 같은 줄입니다. 비어 있는 난이도는 곡의 기본 채보를 씁니다.
        /// </summary>
        public readonly string[] DifficultyBmsFiles = new string[BmsChartSet.MaxDifficulty + 1];

        public double? Delay;
        public double? Offset;

        /// <summary>
        /// 이 커스텀 곡에서 고스트 노트(UID `xx=17`)를 판정선까지 보이게 할지.
        /// `hwa info.txt`의 '커스텀 곡 고스트 노트 보이기'입니다. 적지 않으면 null이고,
        /// 그때는 공식곡용 전역 설정(`config.txt`)을 그대로 따릅니다.
        /// </summary>
        public bool? ShowGhostNotes;

        /// <summary>
        /// WAV <c>_dt</c>를 무시하고 노트 계열·속도별 권장 dt(<see cref="BmsDtTable"/>)로 덮어쓸지.
        /// `hwa info.txt`의 'dt를 권장 표로 고치기'입니다. 적지 않거나 비활성화면 WAV <c>_dt</c>를 그대로 씁니다.
        /// </summary>
        public bool? FixDtToRecommended;

        /// <summary>
        /// 날짜와 상관없이 이 곡의 5번 씬(`scene_05`)을 크리스마스 판(`scene_05_christmas`)으로, 5번 보스를
        /// 크리스마스 보스로 바꿀지. `hwa info.txt`의 '크리스마스 배경'입니다. 적지 않거나 비활성화면 게임의
        /// 날짜 판정(12월 24·25일)을 그대로 따릅니다. 크리스마스 판은 5번 씬에만 있습니다.
        /// </summary>
        public bool? ChristmasScene;

        /// <summary>이 난이도의 레벨 숫자입니다(`난이도N` 줄). 적지 않았거나 없는 번호면 null입니다.</summary>
        public int? GetDifficultyLevel(int difficulty)
        {
            switch (difficulty)
            {
                case 1: return Difficulty1;
                case 2: return Difficulty2;
                case 3: return Difficulty3;
                case 4: return Difficulty4;
                case 5: return Difficulty5;
                default: return null;
            }
        }
    }
}
