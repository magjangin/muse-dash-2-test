# 뮤즈대시 2 출시 대비 체크리스트

출시 예정: **2027년 여름 목표 (지스타 2026 현장 후기 제보, 미확정)**. 2026-05 게임 소식 계정들이 BitSummit 스폰서 페이지를 인용해 퍼뜨렸던 2026-12-25 설은 peropero 공식 발표가 없었고, 2026-11-15 확인 시 Steam 상점(app 4839390)은 날짜 없이 "출시 예정", GameFAQs는 TBA였습니다. 이어 2026-11-19 지스타 2026 현장 시연 후기(아이패드 시연, 한국어 음성 확인, 곡 선택 후 로딩 20~30초 등)에서 내년 여름 출시 목표라는 내용이 전해져 올겨울 출시는 사실상 무산된 것으로 봅니다. Steam 찜 알림과 peropero 공식 채널(X·Bilibili)을 보고, 확정되면 이 줄과 README·AGENTS.md를 같이 고칩니다. 아래 일정은 전부 "출시 당일·D+1" 같은 상대 기준이라 날짜가 바뀌어도 순서는 그대로입니다.

이 문서는 "그날 무엇을, 어떤 순서로"만 다룹니다. 1편에서 **무엇을 옮길지**(훅 전체 목록, 태그 재타깃팅 지도)와 **왜 그렇게 했는지**는 1편 저장소 문서에 이미 있습니다.

- 1편 훅 전체 목록·MD2 이름 채울 칸: [MD2_TAG_RETARGET_MAP.md](../../muse%20dash%20test/docs/muse-dash-2/MD2_TAG_RETARGET_MAP.md) (맨 아래 "🗂️ 1편 훅 전체 목록")
- 분석 순서·3대 핵심 질문·Phase 0~9: [MUSE_DASH_2_SPECULATIVE_GUIDE.md](../../muse%20dash%20test/docs/muse-dash-2/MUSE_DASH_2_SPECULATIVE_GUIDE.md)
- 1편 사고 기반 체크리스트: [CHECKLIST.md](../../muse%20dash%20test/docs/guides/CHECKLIST.md)
- **빠르게 결정·구현·확인하는 법**(보고서 읽기, 결정표, CallProbe 루프, 기능별 확인 신호): [MD2_PLAYBOOK.md](MD2_PLAYBOOK.md)

확인한 사실은 맨 아래 **실측 기록** 표에 적고, 끝나면 [AGENTS.md](../AGENTS.md)의 "미확정" 칸을 채웁니다.

---

## 1. 출시 전 (지금 ~ D-1)

- [x] **1편 6.7.0 시그니처 스냅샷 고정** → `reference/md1-6.7.0-signatures/` (4개 어셈블리, 2026-10-27).
      1편 저장소의 `Decompiled/`는 예전 덤퍼 출력이라 그대로 비교하면 `params Int32[]`↔`int[]` 같은 잡음이 섞입니다.
      지금 덤퍼로 새로 떠 두었고, `compare_dumps.ps1`의 기본 비교 기준입니다.
- [x] 탐색 도구 이식·빌드 확인(SignatureDumper, `scripts/`), 모드 뼈대 컴파일 확인(1편 어셈블리로 `-p:SkipDeploy=true` 빌드).
- [x] **1편 훅·API 목록 추출** → `docs/md1-retarget/`(훅 169건 [정적 167건 + 런타임 동적 2건], 게임 API 참조 428건, 패치 클래스→파일 126개, 1편 최신 v0.12.1+ 배포 DLL 기준).
      1편 6.7.0에 되돌려 대조해 정적 훅 167/167, API 428/428 전부 "그대로", 일부러 망가뜨린 목록으로 6가지 상태를 모두 잡는지 확인(2026-10-27 및 2026-11-09 v0.12.1+ 재추출 검증 완료).
      **1편 모드를 출시 전에 더 고치면** 플레이북 7장 명령으로 다시 뽑습니다.
- [x] CallProbe를 1편 6.7.0에서 실행 확인(와일드카드·호출 순서·씬 요약). 인자 값 기록은 미확인.
- [ ] **시연 행사 관찰(2026-12-05 무렵, 경기도).** 움직이는 앨범 커버가 Live2D인지 Spine인지 가리는 게 핵심입니다.
      볼 것과 기록 표: [MD2_DEMO_EVENT_CHECKLIST.md](MD2_DEMO_EVENT_CHECKLIST.md). 결과는 "시연판 기준"으로 아래 실측 기록에 적습니다.
- [ ] **스토어 페이지에서 App ID 확인.** 출시 후 `H:\steam\steamapps\appmanifest_<AppID>.acf`를 찾는 열쇠입니다.
- [ ] **1편 세이브·설정 백업.** 2편이 1편 세이브를 읽어 연동 보상을 주는 구조일 수 있습니다. 2편을 켜기 전에 1편 쪽을 먼저 떠 둡니다.
- [ ] **1편 보존 작업 마감 점검.** 2편 출시 뒤 1편 서비스·콜라보가 줄어들 위험이 있습니다(전역 메모리 "Abandonware 보존 프로젝트").
      콜라보 종료일은 추정하지 말고 1편 `CollabEndTimeDumpPatch`로 다시 실측합니다.
- [ ] **MelonLoader 최신 안정판 확인·내려받기.** 2편이 Unity 6 계열이면 0.7.3이 안 맞을 수 있습니다
      (In Falsus Demo에서 0.8.0 CI 베타가 Unity 6000.3의 `SceneManager.get_buildIndex()` 변경을 못 따라간 전례).
- [ ] DepotDownloader 준비(다운그레이드·특정 매니페스트 고정용).
- [ ] **오프라인 보존·DRM 우회 도구 준비.** Goldberg Steam Emulator(최신 `gbe_fork` 등 `steam_api64.dll` / `steam_api.dll` 배포본)와 Steamless(SteamStub 언패커, CLI 또는 GUI)를 미리 확보해 둡니다.
- [ ] 디스크 여유 확인: 게임 원본 + 작업 사본 + Cpp2IL ISIL 출력(1편 약 380MB) + 덤프.
- [x] .NET SDK 6 / 8 / 10 설치됨(2026-10-27 확인).

---

## 2. 출시 당일 — 게임을 켜기 전 (10분)

- [ ] **Steam Build ID 기록.** `appmanifest_<AppID>.acf`의 `buildid`, `InstalledDepots`의 depot ID·`manifest` 값을 아래 표에 적습니다.
      출시 직후는 핫픽스가 잦아서, 이 숫자가 없으면 나중에 "어느 빌드에서 확인한 사실인지"를 잃습니다.
- [ ] **작업용 독립 사본 만들기.** Steam 설치 폴더를 `H:\muse dash 2 hwa`로 통째로 복사합니다(1편 `H:\muse dash hwa`와 같은 방식, Steam 밖이라 자동 업데이트를 안 받음).
      모드와 오프라인 전환 처리는 사본에만 깝니다. Steam 원본은 순정으로 두고 업데이트 비교용으로 씁니다.
- [ ] **오프라인 독립 실행·보존 전환 (우선순위: Goldberg Emulator → Steamless → 로컬 Steam API Hooking).**
      사본을 Steam 클라이언트 없이 독립 실행·영구 보존할 수 있도록 아래 순서대로 확인합니다. 모든 원본 파일은 교체 전 `.bak`으로 백업합니다.
      1. **Goldberg Emulator 적용**: 사본의 `steam_api64.dll`(또는 `steam_api.dll`, 보통 루트 또는 `<이름>_Data/Plugins/x86_64/`)을 `.bak`으로 백업 후 Goldberg DLL로 교체합니다. 같은 위치에 `steam_settings/` 폴더를 만들고 `steam_appid.txt`(App ID 한 줄), 필요 시 `offline.txt`·`DLC.txt`를 배치합니다. Steam 클라이언트를 끈 상태에서 실행되는지 확인합니다.
      2. **Steamless 적용 (필요 시)**: Goldberg 적용 후에도 실행 파일(`.exe`)에 SteamStub DRM이 걸려 실행이 막히거나 Steam 클라이언트를 강제 호출할 때만 Steamless로 `.exe`를 언팩합니다(원본 `.exe`를 `.bak` 백업 후 `.unpacked.exe`로 교체).
      3. **로컬 Steam API Hooking (보완 단계)**: 위 두 단계 이후에도 인게임 DLC·소유권 검증이 남아 있는 경우에만 6장 1번 안전판(세이브 정화·서버 차단)과 함께 모드 측 로컬 훅(`SteamApps.BIsDlcInstalled`, `SteamManager.DLCVerify` 등 1편 `OfflineCustomSandbox` 대응)으로 보완합니다.
- [ ] (선택) Steam 원본도 고정하려면 `.acf` 자동 업데이트 차단. 핫픽스를 바로 받아 비교하고 싶다면 원본은 열어 두는 편이 낫습니다.
- [ ] **경로 이름 확정.** 실제 설치 폴더·exe·`_Data` 폴더 이름이 추정(`Muse Dash 2`, `H:\muse dash 2 hwa`)과 다르면 아래 세 곳을 고칩니다.
  - `muse dash 2 test/muse dash 2 test.csproj`의 `GamePath`
  - `SignatureDumper/Program.cs`의 `ResolveDefaultAssembliesDirectory` 후보
  - `scripts/*.ps1`의 기본 경로(`H:\muse dash 2 hwa`)

## 3. 정찰 (5분, 게임 파일만 보고)

- [ ] **백엔드 판별.** 사본 루트에 `GameAssembly.dll`과 `<이름>_Data/il2cpp_data/`가 있으면 **Il2Cpp**, `<이름>_Data/Managed/Assembly-CSharp.dll`이 크게 있으면 **Mono**.
      → Mono면 4장의 SignatureDumper 대신 `ilspycmd`로 본문까지 봅니다(전역 메모리 도구 선택 기준).
- [ ] **Unity 버전**: `UnityPlayer.dll` 파일 버전.
- [ ] **안티치트·서버 검증 범위.** EasyAntiCheat/BattlEye 폴더 유무, 랭킹·기록이 서버 검증형인지.
      진짜 경계선은 백엔드가 아니라 "게임 상태가 로컬에만 있는가"입니다.
- [ ] **Steam DRM·DLC 검증 구조.** `steam_api64.dll` 위치, `.exe`의 SteamStub 패킹 여부(Steamless 검사), 4장 덤프 후 인게임 `SteamAPI` / `BIsDlcInstalled` / `DLCVerify` 계열 검증 메서드 유무를 확인해 아래 실측 기록에 적습니다.
- [ ] **세이브 위치** 찾기(1편은 레지스트리 + 파일). 모드를 깔기 **전에** 순정 세이브를 백업합니다.

## 4. MelonLoader 설치와 덤프 (20~30분)

- [ ] 사본에 MelonLoader 설치 → 첫 실행(Il2Cpp면 `Il2CppAssemblies` 생성에 몇 분 걸립니다).
- [ ] `MelonLoader/Latest.log`에서 기록: `Game Name`, `Game Developer`, `Unity Version`, `Game Version`, `Game Type`.
      → `MainMod.cs`의 `[assembly: MelonGame(...)]` 주석을 풀고 채웁니다.
- [ ] `MelonLoader/` 아래 런타임 폴더 이름 확인(1편은 `net6`). 다르면 csproj의 `TargetFramework`와 참조 경로를 고칩니다.
- [ ] 실행이 안 되면: Mono + 스트리핑된 BCL이면 전역 메모리 "MelonLoader × 스트리핑된 Mono 게임 크래시"(같은 Unity LTS 줄 도너에서 BCL 이식). Il2Cpp면 MelonLoader 버전부터 의심.
- [ ] 어셈블리 목록 보기: `scripts/list_il2cpp_assemblies.ps1` → 4개 이름(`Assembly-CSharp`, `-firstpass`, `Il2CppPeroTools2`, `Il2Cppspine-unity`)이 그대로인지.
- [ ] **시그니처 덤프** (Il2Cpp):
  ```bash
  dotnet build SignatureDumper/SignatureDumper.csproj -f net8.0
  ```
  ```bash
  "SignatureDumper/bin/Debug/net8.0/SignatureDumper.exe" "H:\muse dash 2 hwa\MelonLoader\Il2CppAssemblies" "H:\source\repos\muse dash 2 test\Decompiled" "Assembly-CSharp.dll,Assembly-CSharp-firstpass.dll,Il2CppPeroTools2.dll,Il2Cppspine-unity.dll"
  ```
  어셈블리 이름이 다르면 세 번째 인자를 실제 이름으로 바꿉니다. 없는 대상은 실패로 셉니다(조용히 넘어가지 않음).
- [ ] **1편과 비교**: `scripts/compare_dumps.ps1 -Detail` → `scratch/dump_diff.txt`.
      어셈블리 이름이 바뀌었으면 `-IgnoreAssembly`(같은 이름의 타입이 여러 어셈블리에 있으면 겹친다는 한계가 있음).
      "사라진 타입"에 1편 훅 대상이 있으면 그 기능이 제일 먼저 재분석 대상입니다.
- [ ] **재타깃팅 보고서**(이게 첫날 할 일 목록이 됩니다):
  ```bash
  dotnet build RetargetScanner/RetargetScanner.csproj
  ```
  ```bash
  "RetargetScanner/bin/Debug/net8.0/RetargetScanner.exe" check docs/md1-retarget "H:\muse dash 2 hwa\MelonLoader\Il2CppAssemblies" scratch/retarget-report.md
  ```
  요약 표 숫자를 아래 실측 기록에 옮기고, 읽는 법은 [플레이북 2장](MD2_PLAYBOOK.md#2-재타깃팅-보고서-읽는-법).
- [ ] 본문이 필요해지면 Cpp2IL ISIL(1편 방식, 약 30초):
  ```bash
  "H:\muse dash 2 hwa\MelonLoader\Dependencies\Il2CppAssemblyGenerator\Cpp2IL\Cpp2IL.exe" --game-path "H:/muse dash 2 hwa" --output-as isil --output-to "H:/source/repos/muse dash 2 test/scratch/isil"
  ```

## 5. 뼈대 첫 실행 (10분)

- [ ] `dotnet build "muse dash 2 test/muse dash 2 test.csproj"` → `[AutoDeploy] Successfully deployed` 줄 확인.
- [ ] `ls -la "H:/muse dash 2 hwa/Mods/"`에 `muse-dash-2-custom-chart.dll`이 있고 시각이 방금인지.
- [ ] 게임 실행 → 로그에 다음 세 줄:
  - `뮤즈대시 2 모드 v0.0.1이(가) 로드되었습니다.`
  - `[PatchInstaller] 패치 클래스 0개를 적용했습니다.` (`CallProbe.txt`에 대상을 적었으면 1개)
  - `[PatchHealth] 패치 대상 0개 전부 정상 해석되었습니다.`
  - 첫 실행 때 `[CallProbe] 설정 파일 견본을 만들었습니다` → `UserData/CallProbe.txt` 생김
- [ ] 타이틀 → 곡 선택 → 한 곡 플레이 → 종료까지 크래시 없음(모드가 아무것도 안 하는 상태의 기준선).
- [ ] **여기까지 되면** AGENTS.md 1·2·6번을 실측값으로 채웁니다.

---

## 6. D+1 ~ : 1편 기능 이식 순서

> [!IMPORTANT]
> **PC 모드 완성 우선 원칙**: 안드로이드 프리다 가젯(Frida Gadget) 적용은 **PC 모드(MelonLoader 기반)가 먼저 완성되고 인게임 실기 검증까지 완전히 끝난 뒤에** 시도합니다. 개발 도중 모바일 포팅으로 주의가 분산되는 것을 막고 PC 모드의 기능 완성에만 집중합니다.
> 커밋·푸시·머지·브랜치 청소는 이 원칙에 묶이지 않고, 사용자가 요청하면 그때 진행합니다(2026-11-15 변경).

원칙: 기능 하나를 옮길 때마다 1편 훅 목록 표의 **MD2 이름** 칸을 채우고, 게임에서 확인한 뒤 다음으로 갑니다.
기능마다 도는 루프(찾기 → CallProbe 관찰 → 구현 → 확인)와 "됐다"의 기준은 [플레이북 4·6장](MD2_PLAYBOOK.md#4-기능-하나를-옮기는-루프). 기능당 상한 2시간, 넘기면 보류하고 다음 기능으로.

1. [ ] **안전판 먼저 — 세이브 정화와 서버 전송 차단.** 커스텀 곡을 **한 번이라도 플레이하기 전에** 합니다.
       가짜 UID 기록이 순정 세이브에 들어가면 되돌리기 어렵고, 기록이 랭킹 서버로 올라가면 다른 플레이어에게 영향이 갑니다.
       1편 대응: `DataManager.Save()` Prefix/Postfix, `StandardNetworkRequest.SendRequest()`, `PnlRank.*`.
       (오프라인 보존 보완) Goldberg·Steamless 적용 후에도 인게임 DLC/소유권 검증이 남아 있으면 1편 `OfflineCustomSandbox`(`SteamApps.BIsDlcInstalled`, `SteamManager.DLCVerify`) 패턴을 함께 확인·이식합니다. 반드시 온라인 랭킹·클라우드 차단 상태에서 테스트합니다.
2. [ ] **곡 DB의 주인** 찾기(1편 `GlobalDataBase.dbMusicTag.m_AllMusicInfo`, 조회 `GetMusicInfoFromAll(uid)`).
3. [ ] **차트 주입 지점**(1편 `DBStageInfo.SetRuntimeMusicData(List)` Postfix)과 **난이도별 차트 에셋 보정**(`StageBattleComponent.GetMusicDataFromStageInfo` Prefix). 1편 BMS 파서·info.txt 파서·`TextNormalizer` 등 게임 무관 로직과 로직 테스트 170개는 **2026-11-15에 이미 옮겼습니다**(`Bms/`, `Hwa/`, `Spine/`, `Core/` 일부, `muse dash 2 test.LogicTests/`). 여기서는 이 로직을 2편 훅에 잇는 일만 남습니다.
4. [ ] **가상 곡/앨범 등록 + 조회 우회**(임무 B·C, 데이터 계층 🟡). 여기까지 되면 커스텀 곡이 돕니다.
5. [ ] 오디오(BGM) 교체, 정확도·결과 화면, 강제 퍼펙트·오토 플레이.
6. [ ] **태그 탭·아이콘 UI(임무 A·D, 🔴)는 마지막.** 2편 UI가 재설계됐을 가능성이 가장 큰 곳이라 "이름 교체"가 아니라 "메커니즘 재분석"이 필요할 수 있습니다.

## 7. 이식할 때마다 다시 볼 IL2CPP 함정 (1편 사고 요약)

자세한 경위는 1편 [CHECKLIST.md](../../muse%20dash%20test/docs/guides/CHECKLIST.md).

- [ ] 값 타입 필드(1편 `MusicData`/`NoteConfigData`)는 `a.b.c = x`가 **조용히 버려집니다.** 지역 변수로 받아 고친 뒤 setter로 되돌려 씁니다. 리스트 원소도 `list[i] = note`로 되씁니다.
- [ ] `private` + `out` 조합, `virtual` 메서드 패치는 로그 없이 네이티브 크래시가 날 수 있습니다. 한 줄짜리 세터는 인라인돼 훅이 안 걸립니다.
- [ ] Harmony는 대상을 **그 클래스에 직접 선언된 멤버에서만** 찾습니다. `PatchHealthCheck`는 부모까지 찾으므로 초록불이어도 못 걸 수 있습니다.
- [ ] Prefix의 `catch`는 `return true`로 원본을 실행시킵니다. 예외를 삼키고 `false`면 게임이 멈춥니다. 게임 멤버 접근은 별도 헬퍼 메서드로 빼야 `PatchInstaller`가 못 보는 JIT 실패를 try가 잡습니다.
- [ ] **강제 퍼펙트에서 `isPerfectNode = true`를 켜지 않습니다.** 게임은 이 값을 "정확한 시각" 표시로 쓰고, 홀드 꼬리에서는 `TouchEndResult`가 `isPerfectNode`나 `isRight`가 켜진 칸에서 홀드를 끝내므로 홀드가 꼬리 첫 칸에서 일찍 끝납니다. 판정표(`m_TimeNodeOrders`)의 구조 플래그는 건드리지 않고 결과(`result`: `Great`/`Cool` → `Prefect`)만 바꿉니다.
- [ ] **난이도별 차트에서 `DBBattleStage.selectedDifficulty`는 배틀에서만 신뢰합니다.** 메뉴 곡 목록/기록 조회에서는 지난 배틀의 잔존 값이 남으므로, 메뉴는 반드시 `DBMusicTag.selectedDiffTglIndex`를 읽어야 합니다. 또한 호스트 곡에 없는 난이도(예: 0-4 Master)는 `GetMusicDataFromStageInfo`에서 맵 이름만 변경하여 NRE를 막습니다.
- [ ] **진행바 트윈 세터 등 컴파일러 생성 람다 메서드(`_MusicProgressInit_b__48_1` 등)는 빌드마다 번호가 바뀝니다.** 동적 대상 탐색(`TargetMethods`)과 정적 대상 폴백을 두고, 매 프레임 리플렉션 풀스캔이 일어나지 않도록 캐싱/스로틀링합니다.
- [ ] **BMS 메타데이터 파싱 시 영문 축약형의 공백 분리(`you'  re`, `don ' t` 등)를 자동 교정(`TextNormalizer`)합니다.** 외부 차트 인코딩 변환이나 작성자 오타가 UI 전반(곡명·결과창·RPC)에 깨져 나오는 것을 방지합니다.
- [ ] 리플렉션으로 객체를 깊게 훑는 진단 탐침은 노트 많은 곡에서 프로세스를 날립니다. 진단은 개수·분포 요약까지만, 켠 채로 두지 않습니다.
- [ ] 문자열로 쓴 멤버 이름(`[HarmonyPatch(..., "M")]`, 리플렉션, 래퍼)은 컴파일러가 못 봅니다. 덤프와 대조합니다.
- [ ] GameObject·프리팹·애니메이션 이름, 씬 계층은 덤프에 없습니다. 로그로만 확인합니다.
- [ ] 게임 원본 필드를 덮기 전에 용도를 확인합니다(1편 `MusicInfo.music`은 제목이 아니라 음원 에셋 키였음).

## 8. 신유형 징후 확인 (1편 지식이 안 통할 때)

2편 문제의 대부분은 1편 유형의 변형이겠지만(주관적 추정 70~80%), 출시 첫 1~2주에 처음 보는 유형을 최소 한 건 만날 가능성도 높게 봅니다(80~90%, 근거 없는 감). "1편과 같을 것"은 가정이므로 아래 순서로 먼저 확인합니다.

**A. 1편 체계가 그대로 통하는지 (3장·4장 직후)**

- [ ] 백엔드·Unity 버전이 1편(Il2Cpp, 2019.4.41f1)과 같은가? 다르면 `SignatureDumper`·디컴파일 경로·훅 방식이 통째로 달라질 수 있으니 6장 이식을 시작하지 않고 AGENTS.md 2번부터 다시 씁니다.
- [ ] MelonLoader가 뜨는가? 안 뜨면 MelonLoader 버전·런타임 폴더(4장)부터 의심합니다.
- [ ] 어셈블리 4개 이름이 그대로인가? 바뀌었으면 `compare_dumps.ps1 -IgnoreAssembly`와 덤퍼 세 번째 인자를 고칩니다.
- [ ] 재타깃팅 보고서에서 "타입 없음" 비율이 큰가? 1편 훅 169건 중 대응률이 낮으면 이름 변경이 아니라 구조 재설계로 봅니다.

**B. 1편에 대응 항목이 없는 신유형 후보 (3장 정찰 때 같이 봅니다)**

- [ ] 무결성 검사·안티치트(EasyAntiCheat/BattlEye 폴더, 파일 해시 검증, 실행 파일 서명).
- [ ] 새 에셋 로딩 방식(Addressables, 암호화 번들)과 `StreamingAssets` 구조 변화.
- [ ] 새 세이브·서버 구조(서버 검증형 기록, 로컬 세이브 암호화). 세이브 정화(6장 1번)의 전제가 깨질 수 있습니다.
- [ ] 코드 생성 구조 변화(DOTS/ECS, 대량 제네릭, 스트리핑 강화로 훅 대상이 사라짐).

**C. 신유형을 만나면 (추측 금지)**

- [ ] 훅을 추측으로 짜지 않고 `CallProbe`로 호출 순서·씬 요약부터 관찰합니다(플레이북 4장 루프).
- [ ] 기능당 상한 2시간(6장)을 지키고, 넘기면 보류하고 다음 기능으로 갑니다.
- [ ] 확인한 사실은 아래 실측 기록에, 시도했다가 실패한 것은 AGENTS.md 5번(막다른 길)에 즉시 적습니다.
- [ ] 1편 저장소 `AGENTS.md`·`docs/guides/CHECKLIST.md`에 비슷한 사고가 없는지 먼저 찾습니다.

**D. 지스타 2026 시연 후기에서 나온 제보 (2026-11-19, 디시 전언, 미검증)**

- 8월 시연판 대비 채보 패턴이 일부 바뀌었습니다(깜짝 등장 파트가 일반 패턴으로 바뀜). 시연판 덤프·채보 관찰값은 출시판과 다를 수 있으니 출시 후 다시 잽니다.
- 곡 선택 후 로딩이 20~30초로 길었다는 제보. 출시판에서도 길면 차트 주입 시점(`GetMusicDataFromStageInfo` 등) 지연 측정 때 원본 로딩 시간과 모드 지연을 구분해 둡니다.
- 한국어 음성 지원. 사전 예약 특전은 오프라인 엽서뿐(인게임 아이템 아님)이라 세이브·DLC 구조와 무관합니다. 겨울 출시설은 무산.

**E. 지스타 2026 개발자 대화·아케이드 시연 관찰 (2026-11-21~22, 디시 제보 + 시연 영상, 미검증)**

개발자 발언은 대부분 "계획"이라고 했고 실행 여부는 불확실합니다. 출시 후 실제와 대조하기 전에는 가정으로만 둡니다.

- **보스 전면 교체, 채보 일부 변경, 펫·스킨·악곡·미니게임 증가.** 1편 기준 값(`BmsHostMaps` 맵 이름 규칙, `BmsDtTable`)과 판정·노트 로직이 그대로 통한다고 가정하지 않습니다. 1편 보스는 탄막을 노트로 보내는 역할이었는데, 2편은 보스 상호작용이 다를 수 있습니다(아케이드 시연 영상의 CLEAR 표시가 보스 타격 결과인지는 영상 한 장면으로는 불명). 출시 후 `CallProbe`로 전투 씬 호출 순서부터 관찰합니다.
- **멀티플레이(최대 2:2, 4인) 가능성.** 1편에 없는 통신 구조라서 세이브 정화·서버 차단(6장 1번) 단계에서 멀티플레이 통신이 막히거나 깨지지 않는지 따로 봅니다. 일반 악곡 외에 미니게임도 넣을 수 있다는 말(확실치 않음)이 있어, 미니게임 씬에서 곡 DB·차트 주입 훅이 잘못 걸리지 않는지 씬 구분도 확인합니다.
- **모바일 기준 개발(1편과 동일).** PC판은 스팀 출시. 아케이드는 일단 중국 내 설치만이라 모드 대상이 아닙니다. 안드로이드 이식은 PC 완성 뒤라는 기존 방침 유지.
- **아케이드 시연 영상**(YouTube `rYmY98vl1PU`): 4버튼 탁상형 기체, 화면에 삼각형·링 등 도형 노트가 보임. 아케이드 전용 빌드일 수 있어 PC판 노트 형태로 단정하지 않습니다.
- **스토리·대사는 미완성**(번역투 대사는 시연용이고 출시 전 어투 조정 예정). 아트워크·스티커 기능과 굿즈, 아트북 통합(vol.3)은 모드와 무관합니다.

**F. 아케이드 기체 스펙·인터뷰 원문 확인 (2026-11-23, 원문 3건을 직접 읽어 대조)**

AI 요약표를 그대로 믿지 않고 원문과 맞춰 본 결과입니다. 아케이드는 PC판과 별도 빌드일 수 있어 모드 대상은 아닙니다. 다만 같은 팀이 같은 게임으로 만든 것이라 **PC판 시스템 변경의 단서**로는 씁니다.

| 항목 | 내용 | 출처(원문 확인) |
| --- | --- | --- |
| 기체 크기 | 40×40×52cm. "아케이드 기체치고는 콤팩트"라고 평가 | [Game*Spark 2026-11-21](https://www.gamespark.jp/article/2026/11/21/175170.html) (어뮤즈먼트 엑스포 2026, 11/6~7) |
| 디스플레이 | 16인치 Full HD, 165Hz 대응 | 같은 기사 |
| 버튼 | 4개: 지상 공격 2 + 공중 공격 2. 누름감이 매우 가벼움. 최종 배치는 미확정(시제품 최적화 중) | 같은 기사, [4Gamer 2026-05 (URL 날짜 기준)](https://www.4gamer.net/games/004/G100415/20260525026/) |
| 스위치 | 클릭감 없는 리니어 방식 | [패미통 2026-07-08](https://www.famitsu.com/article/202607/76096) (BitSummit PUNCH 시연 기사) |
| 외형 | 블랙 버전이 중국 선전 G-Fusion Game Fest(2026-06-28~29)에 전시됨. 대형 기체는 필요한 장소용으로 예정(상세 없음) | 패미통, 4Gamer |
| 설치·결제 | 식당 입구·쇼핑몰 광장·영화관 로비 등. 중국은 WeChat Pay·Alipay 선결제, 가격 미정. 해외는 현지 파트너 모색, 일본 전개 미정 | 4Gamer, Game*Spark |
| 수록 | `Muse Dash 2` + 리듬 천국풍 미니게임 모음 | Game*Spark |
| PC판 | Game*Spark는 "PC(Steam/macOS) 근일 배신 예정"이라고만 적음. 출시일은 어느 기사에도 없음 | Game*Spark |
| 레인 매핑 | 왼쪽 2버튼 = 위 레인(공중), 오른쪽 2버튼 = 아래 레인(지상). 필자는 직관적이라고 평가. 일부 오브젝트는 닿으면 피해를 주므로 레인 이동으로 피해야 함(1편과 같은 구조) | [Game*Spark 2026-05-23](https://www.gamespark.jp/article/2026/05/23/166813.html) (BitSummit PUNCH, CiGA 부스) |
| 메뉴 매핑 | 맨 왼쪽 = 곡 정렬, 가운데 2개 = 좌우 이동, 맨 오른쪽 = 결정 | 같은 기사 |
| 토큰 방식 | 토큰을 써서 `MUSEDASH 2` 또는 리듬 모드(기사 표기 "Rythm Party")를 플레이. 시연곡 "BRAIN POWER"·"Pancake is Love"를 최고 난이도로 플레이했고 **자켓 아트가 애니메이션**으로 바뀜 | 같은 기사 |
| 가격·시기(5월 당시) | 가격 미정, 1편처럼 1회 구매 + 후속 DLC 가능성이 높음. 출시는 2026년 말 목표이나 2027년 1~2월로 밀릴 수 있음. 주력은 모바일·PC, 아케이드는 중국에서 먼저. 기체는 큰 변경이 없으면 현 디자인 유지 | 같은 기사(개발사 인터뷰) |
| PC 최소 사양 | Windows 10/11 64비트, Core i5-9400, RAM 8GB, GTX 1050 Ti, 저장 3GB 이상. macOS도 최소 사양 존재(10.14, 8GB). 권장 사양 기재 없음. 플랫폼은 iOS·Android·Windows·macOS·아케이드, 유통은 App Store·Google Play·Steam | [나무위키](https://namu.wiki/w/Muse%20Dash%202) (위키는 누구나 고칠 수 있어 상점 페이지로 재확인) |
| 행사 일정 | 2026-05 BitSummit PUNCH, 08 부산 인디 커넥트 페스티벌, 11 어뮤즈먼트 엑스포·지스타, **12월 AGF 2026(예정)**. 다음 정보가 나올 기회 | 나무위키 |

**기체 크기 체감 비교 (2026-11-23, AI 요약 수치를 직접 검산)**

기체 값(40×40×52cm)은 Game*Spark 원문이고, 비교 대상인 일반 ATX 미들타워(폭 약 21~23, 깊이 약 42~47, 높이 약 45~49cm)는 통념 범위라 출처가 없습니다. 아래 수치는 이 두 전제로 계산한 것입니다.

| 항목 | PeroPero PUNCH! | 미들타워(약 22×45×47cm 가정) | 비교 |
| --- | --- | --- | --- |
| 폭 | 40cm | 약 22cm | 미들타워 약 2대 폭(2대를 붙이면 약 44cm) |
| 깊이 | 40cm | 약 45cm | 오히려 약 5cm 짧음 |
| 높이 | 52cm | 약 47cm | 약 5cm 더 높음 |
| 부피(박스 기준) | 약 83L(40×40×52 = 83,200cm³) | 약 46~47L | 약 1.8배. 위쪽이 좁아지는 형태면 실제 체감 부피는 더 작음 |

- 정리하면 "미들타워 2대를 나란히 붙인 폭에, 깊이는 비슷하고, 키만 조금 더 큰 상자"입니다. 책상 위에 올려도 되는 크기이고, 오락실 대형 기체와는 전혀 다른 물건입니다.
- 원 AI 요약의 "16인치 노트북 폭", "27인치 모니터 스탠드 높이", "듀얼 챔버 어항 케이스보다 10cm 넓음" 같은 비유는 근거 수치가 없어 적지 않았습니다. 같은 요약에 있던 SVG 도식도 문서에는 넣지 않았습니다(치수 값은 위 표와 같음).

**모드에 직접 영향이 있는 것: 4Gamer 인터뷰의 채보 변경 3가지**

- **모양이 변하는(곡선) 롱노트**: 누르고 있으면 완료되는 방식이고 주로 어려운 난이도에 나옴. 1편 홀드 변환·판정표(`m_TimeNodeOrders`, `isPerfectNode`) 가정이 통하는지 출시 후 확인합니다. 4장 덤프에서 홀드 노트 데이터 필드에 경로·곡선 정보가 늘었는지 먼저 봅니다.
- **텔레포터 + 분기 채보**: 채보 중간 텔레포터를 맞추면 숨겨진 다른 채보 경로로 갈라짐. **난이도별 BMS 주입과 호스트 곡 맵 치환 설계가 가장 크게 흔들릴 후보**입니다. 곡 하나가 채보를 여러 갈래로 가지는 구조면 `BmsChartSet`(난이도당 BMS 1개)과 `GetMusicDataFromStageInfo` 치환 가정이 깨집니다.
- **새 점수 체계**: "3대 변경 중 하나"로만 언급되고 상세 없음. 정확도 계산·기록 병합(`PlayRecordMerge`)·결과창을 옮길 때(6장 5번) 1편 수식을 쓰지 않고 실측합니다.
- **조작은 여전히 탭과 홀드**: 프로듀서가 "복잡해진 건 조작이 아니라 비주얼"이라고 함. 입력 방식 변경 가능성은 낮게 봅니다(이 문장도 아케이드 기준일 수 있어 가정으로만 둡니다).
- 미니게임은 곡 DB와 별개 씬일 가능성이 큽니다(E항목과 같은 대응). 멀티플레이는 폰·PC 예정이고 아이템 던지기 모드가 있다고 하며 아케이드 배틀은 검토 중이라고 했습니다. 콘솔판 가능성(전용 컨트롤러)도 언급됐지만 수익화는 미정입니다.

**AI 요약표에서 원문으로 확인되지 않은 것 (쓰지 않음)**

- "가정용으로는 외장 전용 컨트롤러 출시가 더 유력"이라는 해석: 패미통은 아케이드 기체를 "전용 물리 버튼 컨트롤러로 플레이한다"고만 적었고, 가정용 판매 전망은 원문에 없었습니다. 4Gamer는 콘솔판 가능성과 전용 컨트롤러만 언급하고 확정하지 않았습니다.
- 요약표의 "2026년 11월 엑스포 리포트와 개발자 인터뷰"라는 묶음 표기: 실제로는 11월(Game*Spark), 7월(패미통), 5월(4Gamer)로 서로 다른 시기의 기사입니다.
- 미니게임 이름: 11월 Game*Spark는 "리듬 천국풍 미니게임", 5월 Game*Spark는 "Rythm Party"(기사 표기 그대로, 오타 가능), 나무위키는 "리듬 파티"입니다. 공식 표기는 미확정이라 이름으로 검색·식별하지 않습니다.
- 2차 AI 요약표의 "대형 원형 버튼 4개 일렬 배치, 버튼별 LED", "고양이 귀 헤더·LED 눈·스피커 그릴", "Steam판 기반 빌드가 구동되는 미니 아케이드(Arcade Heroes)": 읽은 원문에서 확인하지 못했습니다. 11월 Game*Spark는 버튼 배치 상세가 없고, 5월 Game*Spark는 LED·스위치 종류 언급이 없으며, Arcade Heroes는 "미니 아케이드 캐비닛"이라고만 쓰고 사양·버튼·노트 변경은 언급이 없습니다. 외관 문구(고양이 귀 등)는 패미통과 11월 Game*Spark 사진 설명일 수 있으나 이번에 대조하지 않았습니다.
- 5월 Game*Spark의 "연말 출시, 밀리면 2027년 1~2월"은 5월 시점 정보이고, 지스타 현장 제보(2027년 여름 목표)가 더 최근입니다. 둘이 어긋나므로 출시일은 Steam 상점을 기준으로 봅니다.
- 이 기체 정보 자체는 모드 구현에 직접 쓰이지 않습니다. 의미 있는 건 2줄 레인 구조 유지(1편 지식 적용 가능성)와 PC 사양, 모바일·macOS 병행 출시 가능성(안드로이드 이식은 기존 방침대로 PC 완성 뒤)입니다.

---

## 실측 기록 (출시 후 채울 칸)

| 항목 | 값 | 확인 날짜 |
|---|---|---|
| Steam App ID | | |
| Steam Build ID (`.acf` `buildid`) | | |
| Depot ID / Manifest ID | | |
| 게임 버전 (MelonLoader 로그) | | |
| Unity 버전 | | |
| 백엔드 (Mono / Il2Cpp) | | |
| Steam 설치 폴더 이름 | | |
| 작업용 사본 경로 | `H:\muse dash 2 hwa` (예정) | |
| exe / `_Data` 폴더 이름 | | |
| MelonLoader 버전 | | |
| MelonLoader 런타임 폴더 (`net6` 등) | | |
| `Game Developer` / `Game Name` | | |
| 게임 어셈블리 목록 | | |
| 안티치트 / 서버 검증 범위 | | |
| Steam DRM / 오프라인 전환 (Goldberg / Steamless / 훅) | | |
| 세이브 위치 | | |
| 1편 대비 사라진 / 새 타입 수 | | |
| 재타깃팅: 훅 그대로 / 전체, API 문제 수 | | |
