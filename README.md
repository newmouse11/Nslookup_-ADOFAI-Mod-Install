# ADOFAI Mod Helper (얼불춤 모드 도우미)

얼불춤(A Dance of Fire and Ice) 모드를 처음 쓰는 사람을 위한 Windows 프로그램입니다.
Unity Mod Manager(UMM) 설치부터 모드 설치·업데이트·켜기/끄기까지 버튼으로 끝냅니다.

- **exe 하나**: 설치 없이 더블클릭으로 실행 (Windows 10 1903+ / 11, 기본 내장 .NET Framework 4.8 사용)
- **UMM 자동 설치**: 공식 배포처(Dropbox)에서 최신 UMM을 받아 공식 설치기와 같은 방식(DoorstopProxy)으로 설치
- **모드 원클릭 설치**: 모드 목록에서 고르면 GitHub 최신 릴리스를 받아 설치, 필요한 라이브러리(JALib 등)도 자동 설치
- **관리**: 켜기/끄기, 삭제(휴지통), 일괄 업데이트, UMM 제거, 게임 업데이트 감지
- **잘못된 설치 자동 정리**: `Mods/Mods/…` 중첩, 같은 모드 중복, 압축 안 푼 zip을 버튼 하나로 정리
- **진단 정보 복사**: 디스코드 문의에 붙여넣을 상태 요약 (경로·UMM·모드 목록·로그)
- 한국어 / English

## 사용법

1. [Releases](https://github.com/Newmouse11/adofai-mod-helper/releases)에서 `AdofaiModHelper.exe`를 받아 실행
   - "Windows의 PC 보호" 창이 뜨면 **추가 정보 → 실행** (서명되지 않은 프로그램이라 처음에 뜰 수 있음)
2. 마법사를 따라 **게임 찾기 → UMM 설치 → 모드 고르기**
3. 게임에서 `Ctrl + F10`을 누르면 UMM 모드 메뉴가 열립니다

게임이 켜져 있는 동안에는 모드 파일을 바꿀 수 없으니, 설치·켜기/끄기 전에 게임을 꺼 주세요.

## 모드 목록 관리 (`catalog/mods.json`)

프로그램은 실행할 때 이 저장소의 `catalog/mods.json`을 읽습니다. **이 파일만 고쳐서 push하면 도구를 다시 배포하지 않아도 목록이 바뀝니다.**
(인터넷이 안 되거나 파일을 못 받으면 exe에 내장된 사본을 씁니다.)

```jsonc
{
  "id": "TimingShow",                    // 모드 Info.json의 Id와 같아야 설치 여부·업데이트를 인식
  "name": "TimingShow",                  // 화면에 보일 이름
  "author": "SleepingCui",
  "repo": "SleepingCui/TimingShow",      // GitHub 소유자/저장소
  "asset": "^TimingShow\\.zip$",         // 릴리스 파일 중 UMM용 zip을 고르는 정규식
  "prerelease": false,                   // 정식 릴리스가 없는 저장소(JALib)만 true
  "library": false,                      // true면 마법사 목록에 숨기고 필요할 때만 자동 설치
  "requires": ["JALib"],                 // 함께 설치할 모드 Id
  "description": { "ko": "…", "en": "…" }
}
```

- UMM용이 아닌 MelonLoader 전용 모드는 넣지 마세요 (이 도구는 UMM만 지원).
- 항목을 추가했으면 `dotnet run --project tests/SelfTest -c Release -- --online`으로 **실제로 받아서 Id가 맞는지** 확인할 수 있습니다.

## 개발

필요: Windows + .NET SDK (10.0.402로 검증, 빌드 대상은 .NET Framework 4.8)

```bash
dotnet build src/AdofaiModHelper -c Release
```

결과물: `src/AdofaiModHelper/bin/Release/net48/AdofaiModHelper.exe` (이 파일 하나만 배포)

```bash
dotnet run --project tests/SelfTest -c Release
```

자체 검사: 가짜 게임 폴더에서 UMM 설치·진단·제거, 모드 설치·켜기/끄기·중첩 정리 등을 확인합니다. `-- --online`을 붙이면 실제 UMM과 목록의 모든 모드를 받아 설치까지 확인합니다.

| 위치 | 내용 |
|---|---|
| `src/AdofaiModHelper/Core/` | 화면과 무관한 로직 (게임 탐지, UMM 설치, 모드 관리, 목록, 진단) |
| `src/AdofaiModHelper/MainWindow.xaml(.cs)` | 마법사와 메인 화면 |
| `src/AdofaiModHelper/Theme.xaml` | 색·글꼴·컨트롤 스타일 |
| `src/AdofaiModHelper/Strings.*.xaml` | 한/영 문구 |
| `catalog/mods.json` | 원클릭 설치 모드 목록 |

## 동작 방식 (참고)

UMM 공식 설치기(`UnityModManagerApp/Form.cs`)의 DoorstopProxy 설치를 그대로 재현합니다.

- `게임\winhttp.dll` ← UMM zip의 `winhttp_x64.dll`(또는 x86) / `게임\doorstop_config.ini`
- `게임\A Dance of Fire and Ice_Data\Managed\UnityModManager\` ← `UnityModManager.dll`, `0Harmony.dll`, `dnlib.dll`, `UnityModManager.xml`, `Config.xml`
- 모드를 끄면 그 모드의 dll(Info.json의 `AssemblyName`)을 `이름.dll.disabled`로 바꾸고, UMM의 `Params.xml`도 꺼짐으로 맞춥니다. 켜면 원래 이름으로 되돌립니다. 꺼 둔 모드는 업데이트해도 꺼진 채로 유지됩니다.
- 지우는 것은 전부 휴지통으로 보냅니다. 세이브 폴더(`User`)는 건드리지 않습니다.

## 라이선스

- 이 도구에 포함된 글꼴: IBM Plex Sans, Noto Sans KR (SIL Open Font License 1.1) — [`licenses/`](licenses/)
- UMM은 포함하지 않고 실행 시 공식 배포처에서 받습니다 (UMM: MIT, newman55).
- 모드는 각 제작자의 저장소에서 직접 받으며, 각 모드의 라이선스를 따릅니다.
- 7th Beat Games와 무관한 비공식 도구입니다. 모드 사용으로 생긴 문제는 개발사가 책임지지 않으니, 게임이 안 켜지면 먼저 모드를 끄거나 UMM을 제거해 보세요.
