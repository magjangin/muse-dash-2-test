# 안드로이드 이식 안내 (Frida Gadget)

> **이 문서는 PC 모드가 완성되고 인게임 검증까지 끝난 뒤에 봅니다.** ([AGENTS.md](../AGENTS.md) "PC 모드 우선 원칙")
> 2편 안드로이드판은 아직 확인한 것이 없습니다. 출시 여부, 엔진, 백엔드 모두 모릅니다. 아래 방법은 **1편 안드로이드 6.8.0에서 실기로 확인한 것**이고, 2편에 맞는지는 그때 다시 확인합니다.

1편 안드로이드 모드는 PC 모드(C#·Harmony)를 **Frida Gadget + TypeScript(frida-il2cpp-bridge)** 로 다시 쓴 것입니다. 이 문서는 그 경험 중 2편에도 쓸 만한 것만 모았습니다. 1편 게임에 묶인 RVA·훅 표는 일부러 뺐습니다.

## 1. 자세한 문서 (1편 공개 저장소)

| 문서 | 내용 |
|---|---|
| [modder_guide_frida_gadget.md](https://github.com/magjangin/muse-dash-phone/blob/main/docs/modder_guide_frida_gadget.md) | 왜 Gadget인가, 복제 패키지, script 모드·핫리로드, bridge, Scoped Storage 대응 |
| [BUILD_CLONE.md](https://github.com/magjangin/muse-dash-phone/blob/main/docs/BUILD_CLONE.md) | 복제 APK를 손으로 만드는 순서(apktool → 매니페스트 → smali → Gadget → 서명 → OBB) |
| [modder_cookbook_step_by_step.md](https://github.com/magjangin/muse-dash-phone/blob/main/docs/modder_cookbook_step_by_step.md) | 기기 연결부터 배포까지 단계별, Harmony 패턴 5가지를 Frida로 옮기는 코드 |
| [modder_study_roadmap_cs_to_ts.md](https://github.com/magjangin/muse-dash-phone/blob/main/docs/modder_study_roadmap_cs_to_ts.md) | C# 모더용: 값/참조 타입, dump.cs 읽기, ARM64 호출 규약, Harmony↔Frida 1:1 표 |
| [ARCHITECTURE.md](https://github.com/magjangin/muse-dash-phone/blob/main/docs/ARCHITECTURE.md) | 1편 안드로이드 모드 소스 구조와 훅 위치 |
| [install/install.ps1](https://github.com/magjangin/muse-dash-phone/blob/main/install/install.ps1) | 기기의 정품 APK로 복제본을 자동으로 만드는 스크립트(2편이면 패키지명·OBB 이름을 바꿔 씀) |

1편 저장소의 개인용 원본(`H:\source\repos\com.prpr.musedash_v6.8.0-unity-android`)의 `AGENTS.md` ⑤ Dead Ends에 시행착오가 날짜별로 더 있습니다.

## 2. 출시 후 먼저 확인할 것

1. **안드로이드판이 있는가, 아키텍처는 무엇인가**: `adb shell pm path <패키지>`로 APK를 꺼내 `lib/arm64-v8a/`에 `libil2cpp.so`가 있으면 IL2CPP다. `libmono*.so`·`assets/bin/Data/Managed/*.dll`이 있으면 Mono이고, 이 문서의 bridge 방식 대신 다른 길을 찾는다.
2. **메타데이터 덤프**: `assets/bin/Data/Managed/Metadata/global-metadata.dat` + `libil2cpp.so` → Il2CppDumper → `dump.cs`·`script.json`. 메타데이터가 암호화돼 있으면 여기서 막힌다(1편은 평문이었다).
3. **PC판과 이름이 같은가**: 덤프의 타입·메서드 이름을 PC 2편 덤프(`Decompiled/`)와 대조한다. 1편은 PC와 안드로이드의 C# 이름이 거의 같아서 PC 훅을 이름으로 그대로 찾을 수 있었다. 단, **안드로이드 쪽이 스트리핑돼 없는 메서드**가 있었다(예: `AudioSettings.get_outputSampleRate`, `Skeleton.get_Skin`). 그럴 땐 필드를 직접 읽는다.
4. **값 타입 확인**: 1편 안드로이드는 `MusicData` 등이 struct였다. List 안의 struct는 배열에 값으로 박혀 있어 PC처럼 객체를 바꾸는 방식이 안 먹고, 배열을 새로 만들어 갈아 끼워야 했다.
5. **정품 앱 보호 장치**: `allowBackup`, 자기 서명 검사, 안티치트 라이브러리, 서버측 치터 플래그. 1편은 앱 자체 서명 검사가 없었고 실질적 위험은 서버 플래그였다. **실계정으로 변조한 점수를 올리지 않는다.**

## 3. 기본 구조 (1편에서 동작한 것)

- **복제 패키지**: 정품은 그대로 두고 패키지명을 `<원래>.mod`로 바꾼 사본을 따로 설치한다. 정품을 재서명해 덮으면 데이터가 지워진다. 매니페스트의 `package`, 모든 `android:authorities`, 앱 전용 permission, `taskAffinity`, 딥링크 host를 바꾸고 **클래스 이름은 그대로** 둔다.
- **Gadget 로드**: `UnityPlayerActivity.onCreate` 맨 앞에 `System.loadLibrary("frida-gadget")`. `lib/arm64-v8a/libfrida-gadget.so` + `libfrida-gadget.config.so`.
- **script 모드**: 설정의 `interaction.type = "script"`, `path`는 `files/mod.js`, `on_change: "reload"`. PC 없이 켜지고, `adb push`만 하면 바로 다시 로드된다. listen 모드는 PC 세션이 끝나면 훅도 풀려서 상시 모드에 맞지 않았다.
- **TS 프로젝트**: `frida-compile` + `frida-il2cpp-bridge`(1편은 0.14.0). PC 패치 클래스 하나를 TS 모듈 하나로 옮겼다.
- **배포용**: Android 11+에선 사용자가 `Android/data`에 파일을 넣기 어렵다. `mod.js`를 `lib/arm64-v8a/libfrida-gadget.script.so`로 APK에 넣고 설정 path를 그 상대 경로로(`on_change: "ignore"`) 바꾸면 설치만으로 동작한다.

## 4. PC(Harmony) → Frida 옮길 때 대응

| PC | 안드로이드 |
|---|---|
| Postfix로 반환값 바꾸기 | `Interceptor.attach` onLeave에서 `retval.replace` |
| Prefix `return false` | `Interceptor.replace`(원본을 안 부름) |
| 원본이 예외를 던질 수 있는 경로 | **attach 금지**. 원본 예외가 Frida 프레임을 못 넘어 프로세스가 abort된다. `replace`로 원본을 부르지 않는 쪽을 쓴다 |
| 매 프레임 훅(`Update`) | 게임의 실제 프레임 함수를 찾아야 한다. 1편은 `GameMusicScene.Run`이 프레임 함수인 줄 알았는데 한 번만 불렸다 |
| `new Color(...)` 같은 struct | `Class.alloc().unbox()` 후 필드 대입. 인자 없는 `.new()`는 예외 |
| enum 반환값 | bridge가 숫자가 아니라 ValueType으로 준다 → `.field("value__").value` |
| 리플렉션으로 private 필드 | 덤프의 필드 오프셋으로 직접 읽고 쓴다 |
| `File.*` (System.IO) | Frida `File`, 또는 libc `open/stat`를 `NativeFunction`으로 |

## 5. 다시 밟지 말 것 (1편 실측)

- `Process.attachModuleObserver` 금지: UnityMain에서 linker CHECK로 SIGABRT가 났다. `libil2cpp.so`가 보일 때까지 5ms `setInterval`로 폴링한다. bridge 0.14의 `Il2Cpp.initialize`도 모듈이 없으면 이 API를 쓰므로, 모듈이 보인 뒤에 `Il2Cpp.perform`을 부른다.
- `il2cpp_get_corlib() != NULL`을 init 완료로 보지 않는다: init 도중에 채워진다. 그때 붙으면 null 접근과 교착이 났다.
- `Il2Cpp.perform(..., "free")`: 훅 설치 뒤 스크립트 스레드를 IL2CPP에서 뗀다. 계속 붙어 있으면 GC·락과 엮여 로딩 화면에서 멈추는 것으로 의심됐다(확정은 못 함).
- 훅 onEnter/onLeave 안에서 부른 함수에는 다른 `Interceptor.replace`가 안 걸린다. 교체한 계산은 JS 함수로 직접 부른다.
- `il2cpp_runtime_invoke`에 bool·float 값 인자를 넘기면 값이 깨졌다(true가 false로, scale이 0으로). 값 인자 호출은 bridge로 직접 부른다.
- System.Decimal 플래그는 `>>> 0`으로 unsigned를 만든다. 안 그러면 음수에서 `writeU32`가 예외를 내고 훅이 조용히 실패한다.
- Frida 17.24.3 `File.seek(n, SEEK_CUR)`는 버퍼 위치 기준으로 움직인다 → `seek(tell()+n, SEEK_SET)`.
- 스크립트에서 libc `system()` 금지: fork 자식이 exec 전에 멈췄다.
- `/proc/self/cmdline`은 `readAllText`가 끝 NUL에서 예외 → `readAllBytes`.
- Gadget은 `Process.findModuleByName("libfrida-gadget.so")`에 안 잡힌다. 같은 폴더의 `libil2cpp.so` 경로로 lib 폴더를 찾는다.
- 앱이 Frida로 만든 파일은 0600이라 adb가 못 읽는다. 로그 파일은 adb로 먼저 만들어 두고(666) 앱이 이어 쓰게 한다. script 모드에선 `console.log`가 logcat에 안 나온다.
- Unity `UI.Text`는 font가 null이면 아무것도 안 그린다 → 씬에서 폰트를 찾아 `set_font`.
- `Resources.FindObjectsOfTypeAll` 결과(1만 개 이상)를 bridge로 하나씩 읽으면 수백 ms가 걸린다. 컴포넌트 타입으로 좁혀 찾는다.
- adb: 클라이언트 버전이 다른 adb 두 개를 섞으면 기기가 offline이 된다. Git Bash에선 `MSYS_NO_PATHCONV=1`. `adb push` 대상 폴더는 `mkdir -p`를 먼저 한다.
- Android 11+: `Android/obb/<남의 패키지>/`는 '출처를 알 수 없는 앱 설치'를 허용받은 앱만 만들 수 있다. `.apk` 열기는 시스템 설치 관리자로 패키지를 못박는다.

## 6. 공개 저장소 규칙

이 저장소는 공개다. 1편처럼 **유료 DLC·Muse Plus 해금 코드와 설명은 공개본에 넣지 않는다**(1편은 개인판에만 있고 공개본에서는 빼는 동기화 스크립트를 썼다). 게임 APK·OBB·라이브러리·덤프도 올리지 않는다.
