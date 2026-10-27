# 뮤즈대시 2 탐색·구현 플레이북

[체크리스트](MD2_LAUNCH_CHECKLIST.md)가 "무엇을 빠뜨리지 않을까"라면, 이 문서는 "**어떻게 빨리 결정하고, 빨리 만들고, 빨리 확인할까**"입니다.
핵심은 세 가지입니다.

1. **보고서로 일을 나눕니다.** 덤프를 손으로 뒤지기 전에 `RetargetScanner check`로 1편 훅 166건·게임 API 참조 425건의 상태를 한 번에 봅니다.
2. **재빌드 없이 관찰합니다.** 후보 메서드는 `UserData/CallProbe.txt`에 적고 게임만 다시 켜서 호출 순서와 인자를 봅니다.
3. **기능마다 "확인 신호"를 미리 정해 둡니다.** 무엇이 보이면 끝인지 정해 두지 않으면 같은 기능을 몇 번이고 다시 봅니다.

---

## 1. 첫날 흐름과 시간 상한

| 단계 | 상한 | 끝의 기준 | 넘기면 |
|---|---|---|---|
| 사본·버전 기록·정찰 | 20분 | 체크리스트 2·3장 칸이 다 참 | — |
| MelonLoader 설치·첫 실행 | 30분 | `Il2CppAssemblies` 생성, 로그에 `Game Name` | 3장 결정표 "MelonLoader" |
| 덤프 + 재타깃팅 보고서 | 15분 | `scratch/retarget-report.md` 생성 | — |
| 뼈대 첫 실행 | 15분 | 로그 세 줄(체크리스트 5장) | csproj 런타임 폴더·`MelonGame` 확인 |
| 보고서 분류 | 30분 | 기능별로 "그대로 / 이름만 / 재분석" 나눔(2장) | — |
| 안전판(세이브·서버 차단) | 2시간 | 6장 확인 신호 | **커스텀 곡 플레이 금지 유지** |
| 곡 DB → 차트 주입 → 가상 곡 | 기능당 2시간 | 6장 확인 신호 | 그 기능은 보류, 다음 기능으로 |

기능 하나가 상한을 넘으면 붙잡지 말고 보류 목록에 적고 넘어갑니다. 1편의 막다른 길 대부분은 "조금만 더" 하다가 원인보다 늦은 자리를 고친 경우였습니다(1편 AGENTS.md 5장).

---

## 2. 재타깃팅 보고서 읽는 법

```bash
"RetargetScanner/bin/Debug/net8.0/RetargetScanner.exe" check docs/md1-retarget "H:\muse dash 2 hwa\MelonLoader\Il2CppAssemblies" scratch/retarget-report.md
```

`docs/md1-retarget/`에는 1편 v0.11.1 배포 DLL에서 뽑은 목록이 들어 있습니다(2026-10-27, 1편 6.7.0으로 자체 시험해 훅 166/166·API 425/425 "그대로").

| 보고서 상태 | 뜻 | 할 일 | 예상 시간 |
|---|---|---|---|
| 그대로 | 같은 타입에 같은 메서드, 시그니처도 같음 | 1편 패치 파일을 그대로 옮김. 그래도 **동작은 바뀌었을 수 있으니** 6장 확인 신호로 봄 | 분 단위 |
| 시그니처 변경 | 메서드는 있는데 반환값·매개변수·static 여부가 바뀜 | 패치의 매개변수(`__0`, 이름 주입, `__instance`, `__result`)를 새 시그니처에 맞춤 | 10~30분 |
| 오버로드 불일치 | 인자 타입을 지정했는데 그 오버로드가 없음 | 단서 칸의 실제 오버로드로 `new Type[]`를 고침 | 10분 |
| 모호 | 인자 미지정인데 같은 이름이 여럿 | 단서 칸에서 1편 시그니처와 같은 것을 골라 인자를 명시 | 10분 |
| 부모 클래스로 이동 | 부모 타입에만 있음. **Harmony는 자식 타입으로는 못 겁니다** | 부모 타입에 걸고 `__instance` 타입으로 걸러냄(다른 자식도 다 걸리므로) | 30분 |
| 메서드 없음 + 비슷한 이름 | 이름만 바뀌었을 가능성 | 후보를 `CallProbe.txt`에 넣어 1편과 같은 시점에 불리는지 확인 | 30분 |
| 타입 없음 + 같은 이름 타입 | 네임스페이스만 이동 | `using`만 고침. 멤버 줄은 다시 돌려 봄 | 분 단위 |
| 단서 없음 | 구조가 바뀜 | **재분석 대상.** 4장 루프로 처음부터 | 기능당 상한 2시간 |
| 런타임 대상 패치 | `TargetMethods`라 정적 점검 불가(1편 `PnlVictoryLoggingPatch`) | 옮긴 뒤 로그로 확인 | — |

**보고서가 못 보는 것**(그래서 그대로여도 안심하면 안 되는 것):
- 1편 `ModReflection`·래퍼에 **문자열로 쓴 멤버 이름**(1편 6.7.0 대조 때 약 50개). 컴파일 참조가 아니라 메타데이터에 안 남습니다.
- GameObject·프리팹·애니메이션 이름, 씬 계층(`"BGM"`, `"TxtArtist"`, `"SldHp"` 등). 로그나 UnityExplorer로만 보입니다.
- 이름이 같고 **의미가 바뀐** 멤버. 1편 `musicIndex` vs `listIndex`처럼 값이 우연히 맞다가 갈라지는 경우.

**규모 판단**: 보고서 요약에서 🔴 UI 계층(1편 훅 목록 표의 위험도) 훅 대부분이 "타입 없음 / 단서 없음"이면 2편 UI는 재설계된 것입니다. 그 주에는 UI 기능(태그 아이콘, 곡 선택 화면 텍스트, 결과 배너)을 통째로 미루고 데이터 계층(곡 DB, 차트, 정확도)만 옮깁니다.

1편 패치 클래스가 어느 파일에 있는지는 `docs/md1-retarget/patch-files.tsv`(1편 커밋 `7453ef0` 기준)에 있습니다.

---

## 3. 결정표

| 상황 | 결정 | 이유 |
|---|---|---|
| `GameAssembly.dll` 있음 | Il2Cpp 경로: SignatureDumper → RetargetScanner → 본문은 Cpp2IL ISIL | 1편과 같음 |
| `Managed/Assembly-CSharp.dll`만 있음 | Mono 경로: `ilspycmd -p -o Decompiled "<…>/Managed/Assembly-CSharp.dll"`로 본문까지. RetargetScanner는 Il2Cpp 이름 체계(`Il2Cpp…` 접두사)라 그대로는 못 씀 → 덤프 `rg`로 대조 | 본문이 보이면 로그 역추적보다 빠름 |
| MelonLoader가 안 켜짐(Il2Cpp) | 최신 안정판 → 최신 nightly 순서. Unity 6 계열이면 0.7.3은 처음부터 건너뜀 | In Falsus Demo 전례(0.8.0 CI가 Unity 6000.3 API 변경을 못 따라감) |
| MelonLoader가 안 켜짐(Mono, `TypeLoadException`/`MissingMethodException`) | 스트리핑된 BCL → 같은 Unity LTS 줄 도너에서 이식 | 전역 메모리 "스트리핑된 Mono 게임 크래시" |
| 안티치트 있음 / 기록이 서버 검증형 | 오프라인 전용 사본에서만 작업, 서버 전송 차단을 기능 1순위로 | 다른 플레이어·계정에 영향이 가는 유일한 경로 |
| 패치 하나가 게임을 멈춤 | 그 클래스만 빼고 다시 켬. `PatchInstaller` 로그에 남은 이름부터 | 1편: JIT 실패 Prefix는 원본까지 막음 |
| 로그 없이 게임이 꺼짐 | 마지막으로 추가한 패치가 virtual / private+out / 한 줄 세터인지 먼저 의심 | 1편 CHECKLIST의 IL2CPP 함정 |
| 후보 메서드가 여럿 | 고르지 말고 전부 `CallProbe.txt`에 넣고 한 번에 관찰 | 덤프 읽기보다 한 번 실행이 빠름 |
| 제목·텍스트가 화면에 안 맞음 | 표시 컴포넌트가 아니라 **조회 함수**부터 찾음 | 1편: `LongSongNameController.Refresh`에서 고친 건 원인보다 늦은 자리였음 |

---

## 4. 기능 하나를 옮기는 루프

**찾기 → 관찰 → 구현 → 확인**을 기능마다 한 바퀴씩 돕니다. 바퀴가 끝나면 커밋합니다.

### 찾기 (덤프)

```bash
rg -l "SetRuntimeMusicData" Decompiled
```
```bash
rg -n "class .*Stage.*Info" Decompiled
```

1편 가이드의 키워드 계열(등록 `Add/Register/Init`, 조회 `Get…By…/Find`, 정화 `Save/Write/Serialize`)로 좁힙니다. 이름이 그대로면 보고서가 이미 찾아 줬을 것이므로, 여기 오는 것은 이름이 바뀐 경우입니다. **같은 일을 하는 1편 메서드의 매개변수 타입**으로 찾는 편이 이름으로 찾는 것보다 잘 맞습니다(예: `List<MusicData>`를 받는 메서드).

### 관찰 (CallProbe, 재빌드 없음)

`H:\muse dash 2 hwa\UserData\CallProbe.txt`(첫 실행 때 견본이 생김)에 후보를 적고 게임만 다시 켭니다.

```text
limit=5
Il2CppAssets.Scripts.Database.DBStageInfo::SetRuntimeMusicData
Il2CppAssets.Scripts.UI.Panels.PnlStage::*
```

```bash
grep -E "\[CallProbe\]|\[Scene\]" "H:/muse dash 2 hwa/MelonLoader/Latest.log"
```

- `#번호`는 전역 호출 순서, `f번호`는 Unity 프레임입니다. "곡을 고른 뒤 배틀 씬 로드 전" 같은 시점은 `[Scene] 로드` 줄과 번호로 가릅니다.
- 씬이 바뀔 때마다 `[CallProbe] 요약(…)` 줄이 그 사이 불린 대상과 횟수를 남깁니다. 와일드카드로 건 뒤 이 줄만 보면 "이 화면에서 실제로 쓰는 메서드"가 추려집니다.
- 문자열·숫자·enum 인자는 값으로, 게임 객체는 타입 이름으로만 찍힙니다. 객체 안을 봐야 하면 그 기능 전용 Postfix를 따로 쓰되, 개수·요약까지만 찍습니다.
- `Type::*`는 virtual·ref/out·제네릭을 뺍니다. 그런 메서드가 필요하면 이름을 직접 적습니다(경고가 남음).
- 1편 6.7.0에서 1편 모드(패치 125개)와 함께 돌려 확인했습니다(2026-10-27): `WelcomeSelect::*`로 26개 대상이 걸렸고, 호출 순서 `AdjustWelcomeUI → OnEnable → Start → OnDisable`과 `Loading` 씬 전 요약 줄이 찍혔으며, 메뉴(`UISystem_PC`)까지 크래시가 없었습니다. **인자 값 기록은 그 시험에서 인자 있는 메서드가 불리지 않아 아직 확인하지 못했습니다.** 첫 사용 때 인자가 있는 대상 하나로 먼저 보십시오.

### 구현

1. `patch-files.tsv`에서 1편 파일을 찾아 `muse dash 2 test/Patches/…`로 복사하고 namespace를 `muse_dash_2_test`로 바꿉니다.
2. 보고서 단서대로 대상·매개변수를 고칩니다.
3. 게임 멤버를 만지는 코드는 별도 헬퍼 메서드로 빼고 패치 본문에서 `try/catch`(Prefix의 catch는 `return true`)로 감쌉니다. `PatchInstaller`는 패치 메서드 자체의 JIT만 미리 봅니다.
4. 문자열로 쓴 멤버 이름이 있으면 덤프와 대조합니다.

### 확인

```bash
dotnet build "muse dash 2 test/muse dash 2 test.csproj"
```

1. `[AutoDeploy] Successfully deployed` 줄
2. 게임 로그 `[PatchInstaller] 패치 클래스 N개를 적용했습니다.` — N이 늘었는지, "제외" 목록이 비었는지
3. `[PatchHealth] … 전부 정상`
4. 아래 6장의 그 기능 확인 신호
5. **폴백 경로**: BMS·앨범이 없는 곡도 배틀까지 들어가 봄(1편 CHECKLIST 맨 위 항목)

---

## 5. 로그 빨리 읽기

```bash
grep -E "PatchInstaller|PatchHealth|\[Scene\]|trampoline|Exception|Error" "H:/muse dash 2 hwa/MelonLoader/Latest.log"
```

| 로그에 보이는 것 | 뜻 |
|---|---|
| `During invoking native->managed trampoline` 반복 | 패치 메서드가 예외를 던지는 중. 그 패치의 Prefix는 원본도 막고 있음 |
| `[PatchInstaller] … 제외: X` | X의 패치 메서드가 현재 빌드로 컴파일 안 됨(참조 멤버가 사라짐) |
| `[PatchHealth] … 해석되지 않았습니다` | 대상 메서드가 없음. 보고서와 같은 결론이어야 함 |
| 마지막 줄이 갑자기 끊김 | 네이티브 크래시. 마지막에 추가한 패치·탐침부터 |
| `[CallProbe] 타입을 찾지 못했습니다` | 타입 전체 이름 오타이거나 `Il2Cpp` 접두사 누락(덤프 폴더 경로를 그대로 쓰면 맞음) |

---

## 6. 기능별 확인 신호

1편에서 실제로 쓴 판정 기준입니다. "됐다"를 이 표로만 판단합니다.

| 기능 (1편 대상) | 확인 신호 | 흔한 실패 → 1편에서 찾은 원인 |
|---|---|---|
| 세이브 정화 (`DataManager.Save`) | 커스텀 곡 플레이 후 세이브 파일에 가짜 UID(1편 `1999-`)가 **없음**. 플레이 전후 세이브를 파일 비교 | 저장 경로가 둘(파일 + 레지스트리)이라 한쪽만 막음 |
| 서버 전송 차단 (`StandardNetworkRequest.SendRequest`, `PnlRank.*`) | 커스텀 곡 결과 화면에서 랭킹 요청 로그가 차단 메시지로 끝남 | — |
| 곡 DB 주입 (`dbMusicTag.m_AllMusicInfo`) | `GetMusicInfoFromAll("<가상 UID>")`가 null이 아님(1편 `[대성공]` 로그) | 조회 함수가 UID가 아니라 인덱스로 물음(`…ByIndex`) |
| 차트 주입 (`DBStageInfo.SetRuntimeMusicData` Postfix) | 주입한 노트 수 = BMS 노트 수, 첫 노트가 BMS 시각에 맞음 | 값 타입 필드 쓰기 유실(`a.b.c = x`), 리스트 원소 되쓰기 누락 |
| 노트 속도·모양 | 속도 2·3 노트가 보이고 판정선에 맞게 도착 | 프리팹 끝자리(1편 `_2`/`_3`), dt만 바꾸면 렌더가 깨짐 |
| 곡 제목 | 목록·하단 제목이 커스텀 제목, **곡을 넘기는 순간에도** 맞음 | 세션의 선택 곡이 조회보다 늦게 갱신(1편 `DrawingUid`) |
| BGM 교체 | 배틀 중 커스텀 음원, 진행바 길이가 커스텀 곡 기준 | 업데이트로 새 오디오 소스 필드가 생김(1편 6.7.0 `m_BgmOneShotSource`) |
| 정확도·결과 화면 | 올퍼펙트 플레이가 100%로 표시 | 하트·음표 집계가 분모·분자에서 빠짐 |
| 강제 퍼펙트 | 판정이 전부 퍼펙트이고 **홀드가 꼬리까지 유지** | `isPerfectNode`를 켜면 홀드가 꼬리 첫 칸에서 끝남 |
| 태그 탭·아이콘 | 커스텀 탭이 보이고 눌렀을 때 곡 목록이 뜸 | 2편 UI 재설계 가능성이 가장 큰 곳. 아이콘이 안 떠도 곡은 돌아가므로 마지막 |

---

## 7. 빠른 명령 모음

| 하려는 것 | 명령 |
|---|---|
| 덤퍼 빌드 | `dotnet build SignatureDumper/SignatureDumper.csproj -f net8.0` |
| 덤프 | 체크리스트 4장 |
| 1편과 타입 비교 | PowerShell에서 `& .\scripts\compare_dumps.ps1 -Detail` |
| 재타깃팅 보고서 | 2장 맨 위 |
| 1편 목록 다시 뽑기(1편 모드를 고쳤을 때) | `RetargetScanner extract "H:\muse dash hwa\Mods\muse-dash-custom-chart.dll" "H:\muse dash hwa\MelonLoader\Il2CppAssemblies" docs/md1-retarget` |
| 모드 빌드·배포 | `dotnet build "muse dash 2 test/muse dash 2 test.csproj"` |
| 2편 없이 컴파일만 | `-p:GamePath="H:\muse dash hwa" -p:SkipDeploy=true` 추가 |
| 본문(ISIL) | 체크리스트 4장 Cpp2IL |
| 함수 하나 의사코드 | `H:\source\repos\rea-hwa\tools\pe_slice.py --dll "<GameAssembly.dll>" --rva <RVA> --out <조각.dll>` → REA `open_binary`(ghidra) → `procedure_pseudo_code`. RVA는 `ilspycmd -t <타입>`으로 `cpp2il_out`에서 |
