namespace muse_dash_2_test.LogicTests
{
    // SongAudioFiles: 곡 폴더에서 BGM ogg 하나를 고르는 규칙(미리듣기와 배틀이 공유) 검증.
    public static class SongAudioFilesTests
    {
        public static void PickBgmOgg_PrefersOggNamedAfterSettingsTxt()
        {
            string picked = SongAudioFiles.PickBgmOgg(
                new[] { @"C:\hwa\곡\music.ogg", @"C:\hwa\곡\info.ogg" },
                new[] { @"C:\hwa\곡\info.txt" });

            Assert.Equal(@"C:\hwa\곡\info.ogg", picked);
        }

        public static void PickBgmOgg_FallsBackToKeywordThenFirstByName()
        {
            // txt와 이름이 같은 ogg가 없으면 키워드가 든 파일을, 그것도 없으면 이름순 첫 파일을 고릅니다.
            Assert.Equal(@"C:\hwa\곡\z_music.ogg", SongAudioFiles.PickBgmOgg(
                new[] { @"C:\hwa\곡\b.ogg", @"C:\hwa\곡\z_music.ogg", @"C:\hwa\곡\a.ogg" },
                new[] { @"C:\hwa\곡\hwa info.txt" }));

            Assert.Equal(@"C:\hwa\곡\a.ogg", SongAudioFiles.PickBgmOgg(
                new[] { @"C:\hwa\곡\b.ogg", @"C:\hwa\곡\a.ogg" },
                null));
        }

        public static void PickBgmOgg_TreatsDemoAsOrdinaryName()
        {
            // 메뉴 전용 규칙이 따로 있던 시절에는 미리듣기만 'demo'를 골랐고 배틀은 'music'을 골랐습니다.
            // 이제 둘은 같은 파일을 틀어야 하므로 'demo'는 키워드가 아닙니다.
            Assert.Equal(@"C:\hwa\곡\music.ogg", SongAudioFiles.PickBgmOgg(
                new[] { @"C:\hwa\곡\demo.ogg", @"C:\hwa\곡\music.ogg" },
                null));
        }

        public static void PickBgmOgg_ReturnsNullWithoutOggAndLeavesInputUntouched()
        {
            Assert.Null(SongAudioFiles.PickBgmOgg(new string[0], null));
            Assert.Null(SongAudioFiles.PickBgmOgg(null, null));

            var oggFiles = new[] { @"C:\b.ogg", @"C:\a.ogg" };
            SongAudioFiles.PickBgmOgg(oggFiles, null);
            Assert.Equal(@"C:\b.ogg", oggFiles[0]);
        }
    }
}
