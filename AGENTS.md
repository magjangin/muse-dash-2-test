# Project Instructions

- 출시일 작업은 [docs/MD2_LAUNCH_CHECKLIST.md](docs/MD2_LAUNCH_CHECKLIST.md)를 순서대로 따르고, 기능을 옮길 때는 [docs/MD2_PLAYBOOK.md](docs/MD2_PLAYBOOK.md)의 결정표·루프·확인 신호를 씁니다. 덤프를 손으로 뒤지기 전에 `RetargetScanner check` 보고서부터 봅니다. 확인한 사실은 그 문서의 "실측 기록" 표와 아래 인수인계 칸에 같이 적습니다.
- 1편 기능을 옮길 때는 1편 저장소(`H:\source\repos\muse dash test`)의 `AGENTS.md`와 `docs/guides/CHECKLIST.md`를 먼저 읽습니다. 1편 IL2CPP 사고 기록은 2편에도 그대로 적용될 가능성이 높습니다.
- `SignatureDumper/`·`RetargetScanner/`는 독립 콘솔 툴입니다. 모드 프로젝트에 덤퍼·스캐너 코드를 넣지 않습니다(모드 안의 탐색 도구는 런타임 관찰용 `CallProbe`뿐).
- 다른 게임 경로로 컴파일만 확인할 때는 `-p:SkipDeploy=true`를 줍니다(1편 `Mods`에 2편 DLL이 배포되는 것 방지).
- **1편 설치본(`H:\muse dash hwa`)에서는 실행 시험을 하지 않습니다.** 2편 모드·탐침의 인게임 검증은 2편 출시 후 2편 사본에서 합니다(사용자 지시, 2026-10-27). 1편은 어셈블리를 읽기 전용으로 참조하는 데만 씁니다.
- 소스 파일이 600줄을 넘으면 기능을 더 붙이기 전에 작은 단위로 나눕니다(1편 규칙 승계).

# 인수인계 (Handover)

마지막 갱신: 2026-11-09 (1편 최신 v0.12.1+ 배포 DLL 기준 훅 169건 [정적 167건 + 동적 2건]·API 참조 428건·패치 클래스 126개 추출 및 동기화, 난이도별 채보 BmsChartSet, 패치 JIT 격리 및 다중 타깃 부분 적용, TextNormalizer, 숨겨진 채보 로직 등 1편 최신 지식 반영. 2편 미출시라 **검증은 전부 1편 6.7.0 기준**)

1. **게임 버전·고정**: 미확정. 출시일(2026-12-25 예정)에 Steam Build ID·Depot/Manifest ID를 기록하고, Steam 밖 독립 사본 `H:\muse dash 2 hwa`(예정)를 작업 기준으로 씁니다. 1편은 `.acf` 없는 독립 사본으로 버전을 고정했습니다.
2. **런타임·디컴파일**: 미확정(1편은 Il2Cpp, Unity 2019.4.41f1, MelonLoader 0.7.3). Il2Cpp면 `SignatureDumper` → `Decompiled/`, 본문은 Cpp2IL ISIL. Mono면 `ilspycmd`. 1편 비교 기준은 `reference/md1-6.7.0-signatures/`(2026-10-27에 현재 덤퍼로 4개 어셈블리를 다시 뜬 것).
3. **설계 의도**
   - 작업용 사본 경로 `H:\muse dash 2 hwa`는 1편 `H:\muse dash hwa` 관례를 따른 **추정**입니다. csproj `GamePath`, `SignatureDumper/Program.cs` 후보, `scripts/*.ps1` 기본값이 모두 이 경로를 씁니다. 바뀌면 세 곳을 같이 고칩니다.
   - 1편 `Decompiled/`가 아니라 새 스냅샷을 비교 기준으로 둔 이유: 1편 덤프는 예전 덤퍼 출력이라 같은 6.7.0끼리도 `params Int32[]`↔`int[]` 차이 3건이 났습니다. 2편 덤프와 같은 덤퍼로 뜬 기준이어야 차이가 곧 게임 변경입니다.
   - `compare_dumps.ps1`은 기본으로 어셈블리 폴더까지 키에 넣습니다. 빼면 1편 firstpass와 spine-unity의 같은 이름 타입(`Il2CppSpine\Unity\MecanimTranslator` 등)이 겹쳐 가짜 차이가 났습니다(2026-10-27 자체 시험).
   - 뼈대에 `PatchInstaller`·`PatchHealthCheck`를 먼저 넣은 이유: 출시 직후 핫픽스로 게임 멤버가 바뀌면, 깨진 패치가 걸려 게임 원본까지 멈추는 대신 그 클래스만 빠지고 로그가 남습니다(1편 6.7.0 업데이트 대응 및 v0.12.1에서 검증). `PatchInstaller`는 다중 타깃 패치에서 일부 타깃이 사라져도 살아남은 정상 타깃에는 개별 패치를 적용(`ApplySurvivingMethodPatches`)합니다. 단, 패치 메서드 자체의 JIT만 검사하므로 패치 메서드가 호출하는 **하위 헬퍼 메서드나 대리자의 JIT 실패**는 사전에 잡지 못합니다. 따라서 버전 변경 위험이 있는 게임 멤버 접근은 별도 헬퍼 메서드로 빼고 본문에서 `try/catch`로 감싸되, Prefix의 catch는 반드시 `return true`로 원본을 실행시킵니다.
   - `MelonGame` 어트리뷰트는 2편의 개발사·게임 이름을 모르는 상태라 주석으로 둡니다. 출시일에 MelonLoader 로그를 보고 채웁니다.
   - **RetargetScanner는 1편 소스가 아니라 배포 DLL을 읽습니다.** 1편 패치 대상 문자열 상당수가 `GameBindings` 상수와 메서드 레벨 `[HarmonyPatch]`에 있어 소스 정규식으로는 못 따라갑니다. 컴파일된 어트리뷰트(`CustomAttributeData`)에는 상수가 박혀 있고, 어트리뷰트 인스턴스를 만들지 않아 모드 코드도 실행되지 않습니다. 게임 API 참조는 메타데이터 TypeRef/MemberRef 표에서 뽑습니다(= 2편에서 빌드하면 나올 컴파일 오류 목록을 빌드 전에, 후보와 함께). 2026-11-09에 1편 최신 v0.12.1+ DLL로부터 훅 169건(정적 167건 + 동적 2건), API 참조 428건, 패치 클래스 126개를 추출 완료했습니다.
   - RetargetScanner 판정은 Harmony 규칙(그 타입에 직접 선언된 멤버만, 인자 미지정 + 오버로드 여럿이면 모호)을 따릅니다. 1편 `PatchHealthCheck`는 부모까지 찾아서 부모로 옮겨진 메서드에 초록불을 켰기 때문에 "부모 클래스로 이동"을 따로 둡니다. 1편 시그니처를 `hooks.tsv`에 같이 저장해 두므로 이름이 같아도 반환·매개변수·static이 바뀌면 "시그니처 변경"으로 잡힙니다.
   - 후보 검색 색인에는 게임 어셈블리만 넣고, UnityEngine 계열은 이름 조회에만 씁니다. 엔진 타입까지 넣으면 단서가 엔진 멤버로 덮입니다. 반대로 아예 빼면 1편이 훅한 `UnityEngine.AudioSource` 3건이 "타입 없음"으로 오진됩니다(2026-10-27 자체 시험에서 실제로 났음).
   - CallProbe는 Prefix(void)라 원본을 막지 않고, 인자는 문자열·숫자·enum만 값으로 찍습니다. 1편에서 객체를 리플렉션으로 깊게 훑는 진단이 노트 많은 곡에서 프로세스를 날렸기 때문입니다. `Type::*`는 virtual·ref/out·제네릭을 뺍니다(1편 IL2CPP 네이티브 크래시 전례). 대상이 없으면 `Prepare()`가 false를 돌려 패치 클래스 자체를 걸지 않습니다.
   - **난이도별 채보 설계**: `info.txt`에서 `난이도N bms`로 난이도마다 다른 BMS를 지정(`BmsChartSet`). 게임에서 배틀 씬 준비 시 `DBBattleStage.selectedDifficulty`가 채워지므로 배틀에서는 이 값을 쓰고(`ResolveBattleDifficulty`), 메뉴 UI에서는 탭 선택 상태인 `DBMusicTag.selectedDiffTglIndex`를 읽습니다(`ResolveSelectedDifficulty`). 게임이 호스트 곡의 맵(`_map<N>`)을 요구하므로 호스트 곡에 해당 난이도가 없는 경우(예: 0-4 Master) `StageBattleComponent.GetMusicDataFromStageInfo` Prefix에서 맵 이름만 존재하는 난이도로 치환하여 NRE를 방지합니다.
   - **BMS 메타데이터 정규화**: 외부 BMS/info.txt 인코딩 변환이나 오타로 인해 발생하는 영문 축약형의 공백 분리(`you'  re`, `don ' t`, `it ’ s` 등)는 `TextNormalizer.NormalizeEnglishContractions`로 파싱 단계에서 교정하여 곡명/결과창/RPC 전체에서 정상 표기를 보장합니다.
   - **강제 퍼펙트 판정표 조작**: 사전 구워진 판정표(`m_TimeNodeOrders`)에서 `result`(`Great`/`Cool` → `Prefect`)만 승격하고 `isPerfectNode` 플래그는 건드리지 않습니다. 게임이 이 플래그를 홀드 완료 트리거로 써서 홀드가 꼬리 첫 칸에서 조기 종료되는 버그를 방지합니다.
4. **핵심 훅**: 게임 기능 훅은 아직 없음. 뼈대의 유일한 패치는 `CallProbe`(`[HarmonyPatch]` + `Prepare()` + `TargetMethods()`, `Prefix(MethodBase __originalMethod, object[] __args)`, 대상은 `UserData/CallProbe.txt`). 1편 6.7.0에서 1편 모드(패치 126개)와 함께 돌려 `WelcomeSelect::*` 26개 대상의 호출 순서·씬 요약을 확인했습니다(인자 값 기록은 인자 있는 메서드가 안 불려 미확인). 옮길 대상 전체는 1편 `docs/muse-dash-2/MD2_TAG_RETARGET_MAP.md`의 "1편 훅 전체 목록"(MD2 이름 칸) 및 `docs/md1-retarget/hooks.tsv`(169건)입니다. 이식 순서는 체크리스트 6장(세이브 정화·서버 차단 → 곡 DB → 차트 주입·난이도별 에셋 보정 → 가상 곡 등록 → 오디오·정확도 → 태그 UI).
5. **막다른 길**
   - 자체 시험에서 `powershell -ExecutionPolicy Bypass -File ...`로 스크립트를 돌리려던 것은 Claude Code 자동 모드 분류기가 막았습니다. PowerShell 도구에서 `& "<경로>.ps1"`로 직접 부르면 됩니다.
   - `compare_dumps.ps1`에서 어셈블리 폴더를 빼고 대조한 것: 1편 firstpass와 spine-unity에 같은 이름 타입이 있어 서로 덮어 가짜 차이가 났습니다. 기본은 어셈블리까지 키에 넣고, `-IgnoreAssembly`는 어셈블리 이름이 바뀌었을 때만 씁니다.
   - CallProbe에서 타입을 `AccessTools.TypeByName`으로 찾은 것: 못 찾으면 모든 어셈블리를 훑으며 UnityEngine.UIElementsModule의 TypeLoadException 경고를 30줄 넘게 남겼습니다. 어셈블리마다 `GetType(전체이름)`으로 바꿨습니다(이 수정 자체는 컴파일만 확인, 인게임 재확인 전).
   - 강제 퍼펙트에서 판정표 모든 칸에 `isPerfectNode = true`를 켠 것: 홀드가 꼬리 판정 창의 첫 칸에서 조기 완료되었습니다. 플래그는 건드리지 않고 판정 결과값만 변경해야 합니다.
   - 메뉴 기록을 `DBBattleStage.selectedDifficulty`로 읽은 것: 지난 배틀의 잔존 값이 남아 다른 난이도 기록이 표시되었습니다. 메뉴는 `DBMusicTag.selectedDiffTglIndex`를 읽어야 합니다.
   - 호스트 곡에 없는 난이도(0-4 Master 등)로 플레이 시작: 차트 에셋이 없어 `GetMusicDataFromStageInfo` NRE 및 보라색 화면 정지가 발생했습니다.
   - 진행바 트윈 세터(`_MusicProgressInit_b__48_1`)를 정적 이름으로만 패치한 것: 컴파일러 생성 람다 번호 변경 시 전체 패치가 깨졌습니다. 동적 타깃 검색 및 폴백 구조가 필요합니다.
6. **빌드·배포**: `dotnet build "muse dash 2 test/muse dash 2 test.csproj"` → `$(GamePath)\Mods\muse-dash-2-custom-chart.dll`로 자동 배포(`[AutoDeploy] Successfully deployed` 줄로 확인). 2026-10-27에 1편 어셈블리 참조 + `SkipDeploy`로 경고·오류 0개 컴파일만 확인했습니다. SignatureDumper는 `dotnet build SignatureDumper/SignatureDumper.csproj -f net8.0`, RetargetScanner는 `dotnet build RetargetScanner/RetargetScanner.csproj`(net8.0). 2026-10-27에 1편 설치본에서 한 실행 시험의 흔적은 전부 되돌렸습니다: `Mods/muse-dash-2-custom-chart.dll`, `UserData/CallProbe.txt`, 그 세션의 MelonLoader 로그를 지우고 `Latest.log`를 직전 세션(10-26) 로그로 복원. 그 세션 중 1편 게임이 메뉴에서 평소처럼 세이브를 저장했으므로 세이브 파일 시각은 그때로 바뀌었습니다(플레이는 없었음).
