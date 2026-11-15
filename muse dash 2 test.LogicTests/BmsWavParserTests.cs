namespace muse_dash_2_test.LogicTests
{
    // BmsWavParser: WAV 파일명 → UID(zzxxyy) 해석, NoteType 매핑, dt 추출, 보스 액션/전환 매핑 검증.
    // 파일명은 실제 차트(hwa2.bms)에서 그대로 가져왔습니다. 폴더 경로 + 한글 + 공백이 섞인 형태입니다.
    public static class BmsWavParserTests
    {
        public static void ParseWavName_ReturnsNullForBlankName()
        {
            Assert.Null(BmsWavParser.ParseWavName(null));
            Assert.Null(BmsWavParser.ParseWavName("   "));
        }

        public static void ParseWavName_StripsFolderPrefixFromWavValue()
        {
            // 실제 차트의 WAV 값은 "1번 씬 wav폴더\..." 처럼 한글 폴더 경로가 앞에 붙습니다.
            var info = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\011001_일반 노트1 지상 노멀_dt1.48.wav");

            Assert.NotNull(info);
            Assert.Equal("011001", info.Uid);
            Assert.Equal(1.48, info.Dt);
            Assert.Equal(1, info.NoteType);
            // 한글/공백이 섞인 이름은 프리팹명으로 못 쓰므로 UID로 대체됩니다.
            Assert.Equal("011001", info.PrefabName);
        }

        public static void ParseWavName_ReadsZeroDtFromTransitionNotes()
        {
            // 보스 전환/씬 전환 노트는 실제 차트에서 _dt0 으로 선언됩니다.
            var info = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010101_1번 씬 보스 등장_dt0.wav");

            Assert.Equal(0.0, info.Dt);
        }

        public static void ParseWavName_KeepsAsciiOnlyPrefabNameAndStripsDtSuffix()
        {
            // 한글이 없는 이름이면 파일명을 그대로 프리팹명으로 씁니다(_dt 접미사만 제거).
            var info = BmsWavParser.ParseWavName("011001_ground_normal_dt1.48.wav");

            Assert.Equal("011001_ground_normal", info.PrefabName);
            Assert.Equal(1.48, info.Dt);
        }

        public static void ParseWavName_MapsHeartAndScorePrefixes()
        {
            var heart = BmsWavParser.ParseWavName(@"10번 씬 wav폴더\000201_하트 지상_dt1.48.wav");
            Assert.Equal(6, heart.NoteType);
            Assert.Equal("sfx_hp", heart.KeyAudio);

            var score = BmsWavParser.ParseWavName(@"10번 씬 wav폴더\000301_음표 지상_dt1.48.wav");
            Assert.Equal(7, score.NoteType);
            Assert.Equal("sfx_score", score.KeyAudio);
        }

        public static void ParseWavName_ScenePrefixWinsOverSandbagXxRule()
        {
            // 0004xx는 UID 중간 두 자리가 "04"라 샌드백(8) 규칙과 충돌합니다.
            // prefix "0004"가 우선해서 SceneToggle(9)로 고정돼야 합니다.
            var info = BmsWavParser.ParseWavName(@"씬 전환 wav 폴더\000401_1번 씬 전환_dt0.wav");

            Assert.Equal(9, info.NoteType);
            Assert.Equal(0.0, info.Dt);
        }

        public static void ParseWavName_MapsXxToHoldGhostAndSandbag()
        {
            Assert.Equal(3, BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010201_홀드 지상 시작 노트_dt1.47.wav").NoteType);
            Assert.Equal(4, BmsWavParser.ParseWavName(@"1번 씬 wav폴더\011701_고스트 지상_dt1.48.wav").NoteType);
            Assert.Equal(8, BmsWavParser.ParseWavName("010401_샌드백.wav").NoteType);

            // xx=05(복선)는 매핑 테이블에 없어 일반 노트로 남습니다.
            Assert.Equal(1, BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010501_복선 지상_dt1.48.wav").NoteType);
        }

        public static void ParseWavName_DoesNotDistinguishHoldStartFromEndByName()
        {
            // 실제 차트는 같은 UID(010201)에 "시작 노트"/"끝 노트"만 다르게 붙여 씁니다.
            // 파서는 이 한글 라벨을 읽지 않으므로 둘의 파싱 결과가 완전히 같습니다.
            // → 시작/끝 구분은 전적으로 BmsNoteMatcher의 등장 순서에 달려 있습니다.
            var start = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010201_홀드 지상 시작 노트_dt1.47.wav");
            var end = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010201_홀드 지상 끝 노트_dt1.47.wav");

            Assert.Equal(start.Uid, end.Uid);
            Assert.Equal(start.NoteType, end.NoteType);
            Assert.Equal(start.PrefabName, end.PrefabName);
            Assert.Equal(start.Dt, end.Dt);
        }

        public static void ParseWavName_MapsBossEnterAndExitFromUid()
        {
            var enter = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010101_1번 씬 보스 등장_dt0.wav");
            Assert.Equal(0, enter.NoteType);
            Assert.Equal("empty_000", enter.PrefabName);
            Assert.Equal("in", enter.BossTransition);
            Assert.Equal("0101_boss", enter.BossName);
            Assert.Equal(1, enter.BossScene);

            var exit = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010102_1번 씬 보스 퇴장_dt0.wav");
            Assert.Equal("out", exit.BossTransition);
            Assert.Equal("0101_boss", exit.BossName);
        }

        public static void ParseWavName_DerivesBossTargetFromScenePrefixOfUid()
        {
            // UID는 zzxxyy 구조이고 zz가 씬 번호입니다. 8번 씬 보스 등장 노트.
            var info = BmsWavParser.ParseWavName(@"8번 씬 wav폴더\080101_8번 씬 보스 등장_dt0.wav");

            Assert.Equal("0801_boss", info.BossName);
            Assert.Equal(8, info.BossScene);
        }

        public static void ParseWavName_MapsFarAttackActionsFromUid()
        {
            Assert.Equal("boss_far_atk_1_start", BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010107_1번 씬 보스 원거리1 액션 시작_dt0.wav").BossAction);
            Assert.Equal("boss_far_atk_1_end", BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010108_1번 씬 보스 원거리1 액션 끝_dt0.wav").BossAction);
            Assert.Equal("boss_far_atk_2_start", BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010109_1번 씬 보스 원거리2 액션 시작_dt0.wav").BossAction);
            Assert.Equal("boss_far_atk_2_end", BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010110_1번 씬 보스 원거리2 액션 끝_dt0.wav").BossAction);

            // 원거리 액션 노트는 등장/퇴장이 아니므로 전환 상태를 갖지 않습니다.
            Assert.Null(BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010107_1번 씬 보스 원거리1 액션 시작_dt0.wav").BossTransition);
        }

        public static void ParseWavName_MapsSpeedTwoAndThreeProjectilesToTheirSpeedOneAction()
        {
            // 실제 차트(Star Trace)의 속도 2·3 보스 발사체. 매핑 표는 속도 1 번호만 갖고 있습니다.
            Assert.Equal("boss_far_atk_2", BmsWavParser.ParseWavName(@"2번 씬 스피드2 wav폴더\020702_보스 발사체2 보스 액션 사용 지상 스피드2_boss_dt0.7.wav").BossAction);
            Assert.Equal("boss_far_atk_2", BmsWavParser.ParseWavName(@"4번 씬 스피드3 wav폴더\040806_보스 발사체3 보스 액션 사용 공중 스피드3_boss_dt0.6.wav").BossAction);
            Assert.Equal("boss_far_atk_1_R", BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010603_보스 발사체1 보스 액션 사용 지상 스피드3_boss_dt0.6.wav").BossAction);
            Assert.Equal("boss_far_atk_1_L", BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010605_보스 발사체1 보스 액션 사용 공중 스피드2_boss_dt0.7.wav").BossAction);

            // _boss 표시가 없으면 속도와 관계없이 비워집니다.
            Assert.Equal("", BmsWavParser.ParseWavName(@"2번 씬 wav폴더\020702_보스 발사체2 보스 없이 지상 스피드2_dt0.7.wav").BossAction);
        }

        public static void ParseWavName_AssignsProjectileActionOnlyWhenNameMarksBoss()
        {
            var withBoss = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010601_보스 발사체1 보스 액션 사용 지상_boss_dt0.8.wav");
            Assert.Equal("boss_far_atk_1_R", withBoss.BossAction);

            // _boss / _atk 표시가 없으면 액션이 비워집니다.
            var withoutBoss = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010601_보스 발사체1 보스 없이 지상_dt0.8.wav");
            Assert.Equal("", withoutBoss.BossAction);
            Assert.Equal(0.8, withoutBoss.Dt);
        }

        public static void ParseWavName_KeepsDeclaredDtOnBossProjectile()
        {
            // 예전에는 ApplyBossProjectileAction이 파일명에 선언된 dt를 0.7로 덮어썼습니다.
            // 실제 차트의 _boss 항목 66개(발사체 1/2/3 × 지상/공중 × 11씬)가 전부 _dt0.8로 선언돼
            // 있어서, 그 노트를 마디에 배치하는 순간 선언값이 전부 0.7로 바뀔 상태였습니다.
            var withBoss = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010601_보스 발사체1 보스 액션 사용 지상_boss_dt0.8.wav");
            var withoutBoss = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010601_보스 발사체1 보스 없이 지상_dt0.8.wav");

            Assert.Equal(0.8, withBoss.Dt, "선언된 _dt0.8을 그대로 써야 합니다.");
            Assert.Equal("boss_far_atk_1_R", withBoss.BossAction);
            Assert.Equal(0.8, withoutBoss.Dt);
        }

        public static void ParseWavName_UsesDefaultDtWhenBossProjectileDeclaresNone()
        {
            // _dt 선언이 없으면 보스 전조 애니메이션 시간을 확보하는 기본값 0.7이 들어갑니다.
            var withBoss = BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010601_보스 발사체1 보스 액션 사용 지상_boss.wav");

            Assert.Equal(0.7, withBoss.Dt);
            Assert.Equal("boss_far_atk_1_R", withBoss.BossAction);

            // 0을 명시한 경우도 선언값이므로 기본값이 끼어들지 않아야 합니다.
            Assert.Equal(0.0, BmsWavParser.ParseWavName(@"1번 씬 wav폴더\010601_보스 발사체1 보스 액션 사용 지상_boss_dt0.wav").Dt);
        }

        public static void ParseWavName_UidTypeWinsOverNameKeywords()
        {
            // 영문 파일명은 프리팹명이 보존되는 1급 경로입니다(위 KeepsAsciiOnlyPrefabName 참고).
            // 예전에는 이름 기반 폴백이 UID 매핑 뒤에 무조건 실행돼 override로 동작했고,
            // else-if 순서상 "note"가 홀드(xx=02)보다 먼저 걸려 타입을 음표(7)로 덮어썼습니다.
            // 타입이 7이 되면 BmsNoteMatcher의 짝 매칭에서도 빠지는데 경고조차 뜨지 않았습니다.
            Assert.Equal(3, BmsWavParser.ParseWavName("010201_hold_note_start.wav").NoteType);
            Assert.Equal(8, BmsWavParser.ParseWavName("010401_sandbag_note.wav").NoteType);
            Assert.Equal(4, BmsWavParser.ParseWavName("011701_ghost_note.wav").NoteType);

            // 'graphpad' 안의 'hp'처럼 우연한 부분일치도 UID를 이기지 못해야 합니다.
            Assert.Equal(3, BmsWavParser.ParseWavName("010201_hold_graphpad.wav").NoteType);

            // 타격음도 함께 오염됐었습니다(sfx_score가 붙음).
            Assert.Null(BmsWavParser.ParseWavName("010201_hold_note_start.wav").KeyAudio);
        }

        public static void ParseWavName_StillUsesNameKeywordsWhenUidResolvesNothing()
        {
            // UID가 없거나 xx가 매핑 테이블에 없으면 이름 기반 추정이 그대로 살아 있어야 합니다.
            Assert.Equal(7, BmsWavParser.ParseWavName("score_pickup.wav").NoteType);
            Assert.Equal(6, BmsWavParser.ParseWavName("heart_pickup.wav").NoteType);

            // xx=05(복선)는 매핑에 없으므로 이름 규칙이 적용됩니다.
            Assert.Equal(3, BmsWavParser.ParseWavName("010501_hold_variant.wav").NoteType);
        }

        public static void ParseWavName_BossMarkersSurviveUidResolution()
        {
            // 보스 표식은 파일명에 명시적으로 적는 영문 키워드이므로 UID 해석 여부와 무관해야 합니다.
            var enter = BmsWavParser.ParseWavName("020301_boss_in_scene2.wav");
            Assert.Equal("in", enter.BossTransition);
            Assert.Equal("empty_000", enter.PrefabName);

            var exit = BmsWavParser.ParseWavName("020301_boss_out.wav");
            Assert.Equal("out", exit.BossTransition);
        }

        public static void ParseWavName_HandlesBgmEntryWithoutUid()
        {
            // 실제 차트 마지막 항목: #WAV0HS music.ogg (노트가 아닌 BGM, UID/dt 없음)
            var info = BmsWavParser.ParseWavName("music.ogg");

            Assert.NotNull(info);
            Assert.Null(info.Uid);
            Assert.Equal(1, info.NoteType);
            Assert.Equal(-1.0, info.Dt);
            Assert.Equal("music.ogg", info.RawWavName);
        }
    }
}
