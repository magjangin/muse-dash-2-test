# muse dash 2 test

뮤즈대시 2(2026-12-25 출시 예정) 커스텀 차트 모드를 위한 준비 저장소입니다.
지금은 **탐색 도구 + 모드 뼈대 + 출시일 체크리스트**만 있습니다. 게임 기능 코드는 출시 후 1편([`H:\source\repos\muse dash test`](../muse%20dash%20test))에서 하나씩 옮깁니다.

**출시일에 할 일은 [docs/MD2_LAUNCH_CHECKLIST.md](docs/MD2_LAUNCH_CHECKLIST.md)를 위에서부터 따라가면 됩니다.**

## 구성

| 경로 | 내용 |
|---|---|
| `SignatureDumper/` | 1편에서 옮긴 Il2Cpp 시그니처 덤퍼(독립 콘솔, System.Reflection, net472;net8.0). 기본 입력 경로만 2편용으로 바꿈 |
| `scripts/` | 1편에서 옮긴 PowerShell 탐색 스크립트 + 새로 만든 `compare_dumps.ps1` |
| `muse dash 2 test/` | MelonLoader 모드 뼈대. `MainMod`, `ModLogger`, 1편의 `PatchInstaller`·`PatchHealthCheck`(범용 부분만) |
| `docs/` | 출시 대비 체크리스트 |
| `reference/md1-6.7.0-signatures/` | 1편 6.7.0 시그니처 고정 스냅샷(로컬 전용, git 제외). 2편 덤프와 비교할 기준 |
| `Decompiled/`, `scratch/` | 2편 덤프·임시 출력(로컬 전용, git 제외) |

## 탐색 스크립트

기본 경로는 전부 `H:\muse dash 2 hwa`(출시 후 확정할 작업용 사본)입니다. 경로가 다르면 `param` 기본값이나 맨 위 변수만 고치면 됩니다.

| 스크립트 | 하는 일 | 2편에서 주의 |
|---|---|---|
| `compare_dumps.ps1` | 두 덤프 폴더를 타입 파일 단위로 비교(사라진/새/공통, `-Detail`이면 시그니처 줄 차이) | 기본 비교 기준은 `reference/md1-6.7.0-signatures`. 어셈블리 이름이 바뀌면 `-IgnoreAssembly` |
| `list_il2cpp_assemblies.ps1` | `Il2CppAssemblies` 폴더의 DLL 목록 | 그대로 사용 |
| `test_types.ps1` | `Assembly-CSharp.dll`의 `GetTypes()`가 되는지, 실패하면 LoaderExceptions 출력 | 그대로 사용 |
| `extract_names_from_dll.ps1` | DLL 바이트에서 `Il2Cpp*`/`UnityEngine.*` 이름 문자열을 뽑음 | 그대로 사용 |
| `find_account_save_types.ps1`, `find_record_types.ps1`, `find_progress_types.ps1`, `inspect_datamanager.ps1`, `inspect_record_saving.ps1`, `inspect_ivariable.ps1` | 세이브·기록·진행도 관련 타입을 리플렉션으로 찾아 멤버 출력 | **1편 타입 이름이 박혀 있는 견본**입니다(`AccountGameDatas`, `SavedSongResult` 등). 2편 덤프를 보고 검색어를 바꿔 쓰십시오 |
| `filter_dump.ps1`, `list_all_types.ps1`, `search_members.ps1`, `list_matched_types.ps1` | 현재 폴더의 `HwaTypesDump.txt`(`[Type]`/`[Field]` 줄 형식 텍스트)를 걸러 봄 | 그 텍스트를 만들던 1편 런타임 덤퍼는 이제 없습니다. 같은 형식의 텍스트가 있을 때만 쓸 수 있습니다 |

## 옮기지 않은 것

- 게임 기능 코드(`Bms/`, `Patches/` 등)와 로직 테스트: 2편 타입을 확인한 뒤 기능 단위로 옮깁니다.
- `reset_progress.bat`: 1편 세이브·레지스트리 전용이라 2편 세이브 위치를 알기 전엔 위험합니다.
- `_complexity_analyze.py`: 게임 탐색이 아니라 모드 코드 품질 도구입니다(1편 경로 고정).
- 1편 `docs/`: 링크로만 참조합니다(문서 안 링크가 1편 소스 파일을 가리킴).

## 빌드

```bash
dotnet build "muse dash 2 test/muse dash 2 test.csproj"
```

2편이 없을 때 컴파일만 확인하려면 1편 어셈블리를 참조하고 배포를 끕니다(1편 `Mods`에 2편 DLL이 들어가지 않게):

```bash
dotnet build "muse dash 2 test/muse dash 2 test.csproj" -p:GamePath="H:\muse dash hwa" -p:SkipDeploy=true
```
