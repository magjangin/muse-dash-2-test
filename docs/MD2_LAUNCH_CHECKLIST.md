# 뮤즈대시 2 출시 대비 체크리스트

출시 예정: **2026-12-25** (전역 메모리 기준, 스토어 페이지에서 다시 확인할 것). 작성일 2026-10-27 = D-59.

이 문서는 "그날 무엇을, 어떤 순서로"만 다룹니다. 1편에서 **무엇을 옮길지**(훅 전체 목록, 태그 재타깃팅 지도)와 **왜 그렇게 했는지**는 1편 저장소 문서에 이미 있습니다.

- 1편 훅 전체 목록·MD2 이름 채울 칸: [MD2_TAG_RETARGET_MAP.md](../../muse%20dash%20test/docs/muse-dash-2/MD2_TAG_RETARGET_MAP.md) (맨 아래 "🗂️ 1편 훅 전체 목록")
- 분석 순서·3대 핵심 질문·Phase 0~9: [MUSE_DASH_2_SPECULATIVE_GUIDE.md](../../muse%20dash%20test/docs/muse-dash-2/MUSE_DASH_2_SPECULATIVE_GUIDE.md)
- 1편 사고 기반 체크리스트: [CHECKLIST.md](../../muse%20dash%20test/docs/guides/CHECKLIST.md)

확인한 사실은 맨 아래 **실측 기록** 표에 적고, 끝나면 [AGENTS.md](../AGENTS.md)의 "미확정" 칸을 채웁니다.

---

## 1. 출시 전 (지금 ~ D-1)

- [x] **1편 6.7.0 시그니처 스냅샷 고정** → `reference/md1-6.7.0-signatures/` (4개 어셈블리, 2026-10-27).
      1편 저장소의 `Decompiled/`는 예전 덤퍼 출력이라 그대로 비교하면 `params Int32[]`↔`int[]` 같은 잡음이 섞입니다.
      지금 덤퍼로 새로 떠 두었고, `compare_dumps.ps1`의 기본 비교 기준입니다.
- [x] 탐색 도구 이식·빌드 확인(SignatureDumper, `scripts/`), 모드 뼈대 컴파일 확인(1편 어셈블리로 `-p:SkipDeploy=true` 빌드).
- [ ] **스토어 페이지에서 App ID 확인.** 출시 후 `H:\steam\steamapps\appmanifest_<AppID>.acf`를 찾는 열쇠입니다.
- [ ] **1편 세이브·설정 백업.** 2편이 1편 세이브를 읽어 연동 보상을 주는 구조일 수 있습니다. 2편을 켜기 전에 1편 쪽을 먼저 떠 둡니다.
- [ ] **1편 보존 작업 마감 점검.** 2편 출시 뒤 1편 서비스·콜라보가 줄어들 위험이 있습니다(전역 메모리 "Abandonware 보존 프로젝트").
      콜라보 종료일은 추정하지 말고 1편 `CollabEndTimeDumpPatch`로 다시 실측합니다.
- [ ] **MelonLoader 최신 안정판 확인·내려받기.** 2편이 Unity 6 계열이면 0.7.3이 안 맞을 수 있습니다
      (In Falsus Demo에서 0.8.0 CI 베타가 Unity 6000.3의 `SceneManager.get_buildIndex()` 변경을 못 따라간 전례).
- [ ] DepotDownloader 준비(다운그레이드·특정 매니페스트 고정용).
- [ ] 디스크 여유 확인: 게임 원본 + 작업 사본 + Cpp2IL ISIL 출력(1편 약 380MB) + 덤프.
- [x] .NET SDK 6 / 8 / 10 설치됨(2026-10-27 확인).

---

## 2. 출시 당일 — 게임을 켜기 전 (10분)

- [ ] **Steam Build ID 기록.** `appmanifest_<AppID>.acf`의 `buildid`, `InstalledDepots`의 depot ID·`manifest` 값을 아래 표에 적습니다.
      출시 직후는 핫픽스가 잦아서, 이 숫자가 없으면 나중에 "어느 빌드에서 확인한 사실인지"를 잃습니다.
- [ ] **작업용 독립 사본 만들기.** Steam 설치 폴더를 `H:\muse dash 2 hwa`로 통째로 복사합니다(1편 `H:\muse dash hwa`와 같은 방식, Steam 밖이라 자동 업데이트를 안 받음).
      모드는 사본에만 깝니다. Steam 원본은 순정으로 두고 업데이트 비교용으로 씁니다.
      사본을 Steam 밖에서 켜려면 `steam_appid.txt`(App ID 한 줄)가 필요할 수 있습니다.
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
- [ ] 본문이 필요해지면 Cpp2IL ISIL(1편 방식, 약 30초):
  ```bash
  "H:\muse dash 2 hwa\MelonLoader\Dependencies\Il2CppAssemblyGenerator\Cpp2IL\Cpp2IL.exe" --game-path "H:/muse dash 2 hwa" --output-as isil --output-to "H:/source/repos/muse dash 2 test/scratch/isil"
  ```

## 5. 뼈대 첫 실행 (10분)

- [ ] `dotnet build "muse dash 2 test/muse dash 2 test.csproj"` → `[AutoDeploy] Successfully deployed` 줄 확인.
- [ ] `ls -la "H:/muse dash 2 hwa/Mods/"`에 `muse-dash-2-custom-chart.dll`이 있고 시각이 방금인지.
- [ ] 게임 실행 → 로그에 다음 세 줄:
  - `뮤즈대시 2 모드 v0.0.1이(가) 로드되었습니다.`
  - `[PatchInstaller] 패치 클래스 0개를 적용했습니다.`
  - `[PatchHealth] 패치 대상 0개 전부 정상 해석되었습니다.`
- [ ] 타이틀 → 곡 선택 → 한 곡 플레이 → 종료까지 크래시 없음(모드가 아무것도 안 하는 상태의 기준선).
- [ ] **여기까지 되면** AGENTS.md 1·2·6번을 실측값으로 채웁니다.

---

## 6. D+1 ~ : 1편 기능 이식 순서

원칙: 기능 하나를 옮길 때마다 1편 훅 목록 표의 **MD2 이름** 칸을 채우고, 게임에서 확인한 뒤 다음으로 갑니다.

1. [ ] **안전판 먼저 — 세이브 정화와 서버 전송 차단.** 커스텀 곡을 **한 번이라도 플레이하기 전에** 합니다.
       가짜 UID 기록이 순정 세이브에 들어가면 되돌리기 어렵고, 기록이 랭킹 서버로 올라가면 다른 플레이어에게 영향이 갑니다.
       1편 대응: `DataManager.Save()` Prefix/Postfix, `StandardNetworkRequest.SendRequest()`, `PnlRank.*`.
2. [ ] **곡 DB의 주인** 찾기(1편 `GlobalDataBase.dbMusicTag.m_AllMusicInfo`, 조회 `GetMusicInfoFromAll(uid)`).
3. [ ] **차트 주입 지점**(1편 `DBStageInfo.SetRuntimeMusicData(List)` Postfix). 1편 BMS 파서·로직 테스트는 게임과 무관해 그대로 옮길 후보입니다.
4. [ ] **가상 곡/앨범 등록 + 조회 우회**(임무 B·C, 데이터 계층 🟡). 여기까지 되면 커스텀 곡이 돕니다.
5. [ ] 오디오(BGM) 교체, 정확도·결과 화면, 강제 퍼펙트·오토 플레이.
6. [ ] **태그 탭·아이콘 UI(임무 A·D, 🔴)는 마지막.** 2편 UI가 재설계됐을 가능성이 가장 큰 곳이라 "이름 교체"가 아니라 "메커니즘 재분석"이 필요할 수 있습니다.

## 7. 이식할 때마다 다시 볼 IL2CPP 함정 (1편 사고 요약)

자세한 경위는 1편 [CHECKLIST.md](../../muse%20dash%20test/docs/guides/CHECKLIST.md).

- [ ] 값 타입 필드(1편 `MusicData`/`NoteConfigData`)는 `a.b.c = x`가 **조용히 버려집니다.** 지역 변수로 받아 고친 뒤 setter로 되돌려 씁니다. 리스트 원소도 `list[i] = note`로 되씁니다.
- [ ] `private` + `out` 조합, `virtual` 메서드 패치는 로그 없이 네이티브 크래시가 날 수 있습니다. 한 줄짜리 세터는 인라인돼 훅이 안 걸립니다.
- [ ] Harmony는 대상을 **그 클래스에 직접 선언된 멤버에서만** 찾습니다. `PatchHealthCheck`는 부모까지 찾으므로 초록불이어도 못 걸 수 있습니다.
- [ ] Prefix의 `catch`는 `return true`로 원본을 실행시킵니다. 예외를 삼키고 `false`면 게임이 멈춥니다. 게임 멤버 접근은 별도 헬퍼 메서드로 빼야 `PatchInstaller`가 못 보는 JIT 실패를 try가 잡습니다.
- [ ] 리플렉션으로 객체를 깊게 훑는 진단 탐침은 노트 많은 곡에서 프로세스를 날립니다. 진단은 개수·분포 요약까지만, 켠 채로 두지 않습니다.
- [ ] 문자열로 쓴 멤버 이름(`[HarmonyPatch(..., "M")]`, 리플렉션, 래퍼)은 컴파일러가 못 봅니다. 덤프와 대조합니다.
- [ ] GameObject·프리팹·애니메이션 이름, 씬 계층은 덤프에 없습니다. 로그로만 확인합니다.
- [ ] 게임 원본 필드를 덮기 전에 용도를 확인합니다(1편 `MusicInfo.music`은 제목이 아니라 음원 에셋 키였음).

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
| 세이브 위치 | | |
| 1편 대비 사라진 / 새 타입 수 | | |
