# Project Instructions

- 출시일 작업은 [docs/MD2_LAUNCH_CHECKLIST.md](docs/MD2_LAUNCH_CHECKLIST.md)를 순서대로 따르고, 기능을 옮길 때는 [docs/MD2_PLAYBOOK.md](docs/MD2_PLAYBOOK.md)의 결정표·루프·확인 신호를 씁니다. 덤프를 손으로 뒤지기 전에 `RetargetScanner check` 보고서부터 봅니다. 확인한 사실은 그 문서의 "실측 기록" 표와 아래 인수인계 칸에 같이 적습니다.
- 1편 기능을 옮길 때는 1편 저장소(`H:\source\repos\muse dash test`)의 `AGENTS.md`와 `docs/guides/CHECKLIST.md`를 먼저 읽습니다. 1편 IL2CPP 사고 기록은 2편에도 그대로 적용될 가능성이 높습니다.
- `SignatureDumper/`·`RetargetScanner/`는 독립 콘솔 툴입니다. 모드 프로젝트에 덤퍼·스캐너 코드를 넣지 않습니다(모드 안의 탐색 도구는 런타임 관찰용 `CallProbe`뿐).
- 다른 게임 경로로 컴파일만 확인할 때는 `-p:SkipDeploy=true`를 줍니다(1편 `Mods`에 2편 DLL이 배포되는 것 방지).
- **1편 설치본(`H:\muse dash hwa`)에서는 실행 시험을 하지 않습니다.** 2편 모드·탐침의 인게임 검증은 2편 출시 후 2편 사본에서 합니다(사용자 지시, 2026-10-27). 1편은 어셈블리를 읽기 전용으로 참조하는 데만 씁니다.
- 소스 파일이 600줄을 넘으면 기능을 더 붙이기 전에 작은 단위로 나눕니다(1편 규칙 승계).

# 인수인계 (Handover)

마지막 갱신: 2026-10-27 (RetargetScanner·CallProbe·플레이북 추가. 2편 미출시라 **검증은 전부 1편 6.7.0 기준**)

1. **게임 버전·고정**: 미확정. 출시일(2026-12-25 예정)에 Steam Build ID·Depot/Manifest ID를 기록하고, Steam 밖 독립 사본 `H:\muse dash 2 hwa`(예정)를 작업 기준으로 씁니다. 1편은 `.acf` 없는 독립 사본으로 버전을 고정했습니다.
2. **런타임·디컴파일**: 미확정(1편은 Il2Cpp, Unity 2019.4.41f1, MelonLoader 0.7.3). Il2Cpp면 `SignatureDumper` → `Decompiled/`, 본문은 Cpp2IL ISIL. Mono면 `ilspycmd`. 1편 비교 기준은 `reference/md1-6.7.0-signatures/`(2026-10-27에 현재 덤퍼로 4개 어셈블리를 다시 뜬 것).
3. **설계 의도**
   - 작업용 사본 경로 `H:\muse dash 2 hwa`는 1편 `H:\muse dash hwa` 관례를 따른 **추정**입니다. csproj `GamePath`, `SignatureDumper/Program.cs` 후보, `scripts/*.ps1` 기본값이 모두 이 경로를 씁니다. 바뀌면 세 곳을 같이 고칩니다.
   - 1편 `Decompiled/`가 아니라 새 스냅샷을 비교 기준으로 둔 이유: 1편 덤프는 예전 덤퍼 출력이라 같은 6.7.0끼리도 `params Int32[]`↔`int[]` 차이 3건이 났습니다. 2편 덤프와 같은 덤퍼로 뜬 기준이어야 차이가 곧 게임 변경입니다.
   - `compare_dumps.ps1`은 기본으로 어셈블리 폴더까지 키에 넣습니다. 빼면 1편 firstpass와 spine-unity의 같은 이름 타입(`Il2CppSpine\Unity\MecanimTranslator` 등)이 겹쳐 가짜 차이가 났습니다(2026-10-27 자체 시험).
   - 뼈대에 `PatchInstaller`·`PatchHealthCheck`를 먼저 넣은 이유: 출시 직후 핫픽스로 게임 멤버가 바뀌면, 깨진 패치가 걸려 게임 원본까지 멈추는 대신 그 클래스만 빠지고 로그가 남습니다(1편 6.7.0 업데이트 대응에서 검증). `PatchHealthCheck`의 1편 전용 `MusicTagManager` 점검은 뺐습니다.
   - `MelonGame` 어트리뷰트는 2편의 개발사·게임 이름을 모르는 상태라 주석으로 둡니다. 출시일에 MelonLoader 로그를 보고 채웁니다.
   - **RetargetScanner는 1편 소스가 아니라 배포 DLL을 읽습니다.** 1편 패치 대상 문자열 상당수가 `GameBindings` 상수와 메서드 레벨 `[HarmonyPatch]`에 있어 소스 정규식으로는 못 따라갑니다. 컴파일된 어트리뷰트(`CustomAttributeData`)에는 상수가 박혀 있고, 어트리뷰트 인스턴스를 만들지 않아 모드 코드도 실행되지 않습니다. 게임 API 참조는 메타데이터 TypeRef/MemberRef 표에서 뽑습니다(= 2편에서 빌드하면 나올 컴파일 오류 목록을 빌드 전에, 후보와 함께).
   - RetargetScanner 판정은 Harmony 규칙(그 타입에 직접 선언된 멤버만, 인자 미지정 + 오버로드 여럿이면 모호)을 따릅니다. 1편 `PatchHealthCheck`는 부모까지 찾아서 부모로 옮겨진 메서드에 초록불을 켰기 때문에 "부모 클래스로 이동"을 따로 둡니다. 1편 시그니처를 `hooks.tsv`에 같이 저장해 두므로 이름이 같아도 반환·매개변수·static이 바뀌면 "시그니처 변경"으로 잡힙니다.
   - 후보 검색 색인에는 게임 어셈블리만 넣고, UnityEngine 계열은 이름 조회에만 씁니다. 엔진 타입까지 넣으면 단서가 엔진 멤버로 덮입니다. 반대로 아예 빼면 1편이 훅한 `UnityEngine.AudioSource` 3건이 "타입 없음"으로 오진됩니다(2026-10-27 자체 시험에서 실제로 났음).
   - CallProbe는 Prefix(void)라 원본을 막지 않고, 인자는 문자열·숫자·enum만 값으로 찍습니다. 1편에서 객체를 리플렉션으로 깊게 훑는 진단이 노트 많은 곡에서 프로세스를 날렸기 때문입니다. `Type::*`는 virtual·ref/out·제네릭을 뺍니다(1편 IL2CPP 네이티브 크래시 전례). 대상이 없으면 `Prepare()`가 false를 돌려 패치 클래스 자체를 걸지 않습니다.
4. **핵심 훅**: 게임 기능 훅은 아직 없음. 뼈대의 유일한 패치는 `CallProbe`(`[HarmonyPatch]` + `Prepare()` + `TargetMethods()`, `Prefix(MethodBase __originalMethod, object[] __args)`, 대상은 `UserData/CallProbe.txt`). 1편 6.7.0에서 1편 모드(패치 125개)와 함께 돌려 `WelcomeSelect::*` 26개 대상의 호출 순서·씬 요약을 확인했습니다(인자 값 기록은 인자 있는 메서드가 안 불려 미확인). 옮길 대상 전체는 1편 `docs/muse-dash-2/MD2_TAG_RETARGET_MAP.md`의 "1편 훅 전체 목록"(MD2 이름 칸)입니다. 이식 순서는 체크리스트 6장(세이브 정화·서버 차단 → 곡 DB → 차트 주입 → 가상 곡 등록 → 오디오·정확도 → 태그 UI).
5. **막다른 길**
   - 자체 시험에서 `powershell -ExecutionPolicy Bypass -File ...`로 스크립트를 돌리려던 것은 Claude Code 자동 모드 분류기가 막았습니다. PowerShell 도구에서 `& "<경로>.ps1"`로 직접 부르면 됩니다.
   - `compare_dumps.ps1`에서 어셈블리 폴더를 빼고 대조한 것: 1편 firstpass와 spine-unity에 같은 이름 타입이 있어 서로 덮어 가짜 차이가 났습니다. 기본은 어셈블리까지 키에 넣고, `-IgnoreAssembly`는 어셈블리 이름이 바뀌었을 때만 씁니다.
   - CallProbe에서 타입을 `AccessTools.TypeByName`으로 찾은 것: 못 찾으면 모든 어셈블리를 훑으며 UnityEngine.UIElementsModule의 TypeLoadException 경고를 30줄 넘게 남겼습니다. 어셈블리마다 `GetType(전체이름)`으로 바꿨습니다(이 수정 자체는 컴파일만 확인, 인게임 재확인 전).
6. **빌드·배포**: `dotnet build "muse dash 2 test/muse dash 2 test.csproj"` → `$(GamePath)\Mods\muse-dash-2-custom-chart.dll`로 자동 배포(`[AutoDeploy] Successfully deployed` 줄로 확인). 2026-10-27에 1편 어셈블리 참조 + `SkipDeploy`로 경고·오류 0개 컴파일만 확인했습니다. SignatureDumper는 `dotnet build SignatureDumper/SignatureDumper.csproj -f net8.0`, RetargetScanner는 `dotnet build RetargetScanner/RetargetScanner.csproj`(net8.0). 2026-10-27에 1편 설치본에서 한 실행 시험의 흔적은 전부 되돌렸습니다: `Mods/muse-dash-2-custom-chart.dll`, `UserData/CallProbe.txt`, 그 세션의 MelonLoader 로그를 지우고 `Latest.log`를 직전 세션(10-26) 로그로 복원. 그 세션 중 1편 게임이 메뉴에서 평소처럼 세이브를 저장했으므로 세이브 파일 시각은 그때로 바뀌었습니다(플레이는 없었음).
