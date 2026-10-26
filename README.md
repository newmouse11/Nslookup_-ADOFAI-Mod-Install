# ADOFAI Mod Installer

한국어 · [English](README.en.md)

얼불춤(A Dance of Fire and Ice) 모드 환경(UnityModManager, Doorstop, JALib, 모드)을 한 번에 설치하는 오픈소스 설치기입니다. 파일은 설치기에 넣어 두지 않고 **공식 배포처에서 직접 받아**, 카탈로그에 적힌 SHA-256과 일치할 때만 설치합니다.

원래 JipperResourcePack 설치기처럼 **시작 → 폴더 선택 → 설치 요소 선택 → 설치 → 완료** 5단계 마법사입니다. (아래 화면은 미리보기입니다.)

| 시작 | 설치 요소 선택 |
|---|---|
| ![시작](docs/wizard-1-welcome.png) | ![설치 요소 선택](docs/wizard-3-select.png) |
| **설치** | **완료** |
| ![설치](docs/wizard-4-install.png) | ![완료](docs/wizard-5-finish.png) |

## 기능

- 스팀에서 게임 폴더를 자동으로 찾고, 게임 빌드(r번호)를 보여줍니다. 폴더 선택 화면에서 경로 분석 결과(게임 발견, UnityModManager·Doorstop 상태)를 보여줍니다.
- 이미 설치된 구성요소는 설치된 버전과 함께 표시하고, 필수 요소는 없을 때만 자동으로 선택합니다. 구성요소 위에 마우스를 올리면 만든 사람, 라이선스, 출처 링크가 나옵니다.
- 구성요소를 체크해서 고릅니다. 필요한 것(의존)은 자동으로 켜지고, 서로 충돌하는 것(예: AdofaiTweaks 2.9.3 / 2.8.1)은 함께 켜지지 않습니다.
- 전부 내려받아 검증한 **뒤에** 게임 폴더를 바꿉니다. 적용 중 실패하면 되돌립니다.
- 덮어쓰는 파일은 백업하고, 설치 기록(영수증)을 남겨서 **제거**하면 기록에 있는 파일만 지우고 백업을 복원합니다. 전체 제거와 구성요소별 제거를 고를 수 있습니다.
- 설치·제거 중에는 창을 닫을 수 없고(중간에 끊기는 것 방지), 게임이 켜져 있으면 시작하지 않습니다. 완료 화면에서 `얼불춤 실행`을 고를 수 있습니다.
- 관리자 권한이 필요 없습니다.

## 설치되는 것

| 구성요소 | 버전 | 라이선스 | 기본 |
|---|---|---|---|
| UnityModManager | 0.33.2 | MIT | 필수 |
| UnityDoorstop | 4.5.0 | LGPL-2.1 | 필수 |
| JALib | 1.0.0.45 | BSD-3-Clause | 선택됨 |
| JipperResourcePack | 1.5.2.0 | BSD-3-Clause | 선택됨 (JALib 필요) |
| Jipper Key16B 패치 | 1.5.2.0 | BSD-3-Clause | (Jipper 필요) 공식 DLL을 수정 빌드로 교체 |
| AdofaiTweaks | 2.9.3 / 2.8.1 | MIT | |
| TogetherBootstrap | 1.5.5 | 미지정 | |
| KeyboardChatterBlocker | 0.1.2 | 미지정 | |

출처와 저작권은 [NOTICE.md](NOTICE.md), 바꾼 내용은 [CHANGES.md](CHANGES.md)를 보세요.

## 설치 방법

**준비:** Windows 10/11, 인터넷 연결, 스팀판 얼불춤. 게임은 꺼 두세요. 설치기는 .NET Framework 4.8로 동작하며 대부분의 Windows 10/11에 이미 들어 있습니다. (관리자 권한은 필요 없습니다.)

1. [릴리스 페이지](https://github.com/newmouse11/Nslookup_-ADOFAI-Mod-Install/releases/latest)에서 `ADOFAI-Mod-Installer.exe`를 받습니다. 같은 곳의 `SHA256SUMS.txt`와 해시를 비교해 두면 더 안전합니다. ([방법](#파란-창windows-smartscreen이-뜰-때))
2. exe를 실행합니다. 파란 창이 뜨면 아래 [파란 창](#파란-창windows-smartscreen이-뜰-때) 절의 방법으로 넘어가세요.
3. **시작:** 안내를 읽고 `다음`을 누릅니다. 이때 인터넷 연결을 확인합니다.
4. **폴더 선택:** 스팀에서 얼불춤 폴더를 자동으로 찾습니다. 못 찾았거나 다른 곳에 설치했다면 `찾아보기`로 `A Dance of Fire and Ice.exe`가 있는 폴더를 고릅니다. 오른쪽 `경로 분석` 상자에서 게임을 찾았는지, UnityModManager·Doorstop이 이미 있는지 확인하고 `다음`을 누릅니다.
5. **설치 요소 선택:** 작업은 `설치`를 고릅니다.
   - `필수 요소 설치`: UnityModManager와 UnityDoorstop입니다. 이미 있으면 `(설치됨 …)`으로 표시되고, 없을 때만 자동으로 선택됩니다.
   - `추가 모드 설치`: 원하는 모드를 체크합니다. JALib과 JipperResourcePack은 기본으로 선택돼 있고, 필요한 모드(의존)는 자동으로 같이 켜지며 서로 충돌하는 것(AdofaiTweaks 2.9.3 / 2.8.1)은 함께 켤 수 없습니다. 키뷰어에 두 번째 16키 프리셋(`Key16B`)이 필요하면 `Jipper Key16B 패치`를 체크하세요.
   - 항목에 마우스를 올리면 아래 `구성요소 정보`에 만든 사람, 라이선스, 출처 링크가 나옵니다. `다음`을 누릅니다.
6. **설치:** 내려받기 → SHA-256 확인 → 적용 순서로 진행됩니다. 끝날 때까지 기다리세요. 진행 중에는 창이 닫히지 않습니다. 도중에 실패하면 변경을 되돌립니다.
7. **완료:** `얼불춤 실행`을 체크한 채 `종료`를 누르면 게임이 시작됩니다.
8. 게임 안에서 `Ctrl + F10`(UnityModManager 기본 단축키)을 눌러 모드 창을 열고 설치한 모드가 보이는지 확인합니다.

**제거:** 설치기를 다시 실행해 `설치 요소 선택`에서 `제거`를 고릅니다. 이 설치기로 설치한 기록이 있을 때만 켜집니다. `전체 제거` 또는 `선택한 것만 제거`를 고를 수 있고, 덮어썼던 파일은 백업에서 되돌립니다. 설치한 뒤 직접 수정한 파일은 지우지 않고 남겨 둡니다.

## 명령줄 옵션

| 옵션 | 설명 |
|---|---|
| `/silent` | 화면 없이 설치 (결과는 로그 파일에 기록) |
| `/uninstall` | 설치 기록에 있는 파일만 제거하고 백업을 복원 |
| `/target:"경로"` | 게임 폴더 (생략하면 자동 탐색) |
| `/components:a,b` | 설치할 id (생략하면 기본 선택). 필수·의존 항목은 자동 추가 |
| `/catalog:"파일"` | 내장 카탈로그 대신 쓸 카탈로그 |
| `/list` | 카탈로그의 id 목록을 로그로 출력 |
| `/log:"파일"` | 로그 파일 (기본 `%TEMP%\ADOFAI-Mod-Installer.log`) |
| `/selftest` | 보안·롤백 자체 검사 (네트워크 불필요) |
| `/shot:"png" /page:0~4` | 개발용 화면 미리보기를 PNG로 저장 (실제 다운로드·설치는 하지 않음) |

종료 코드: 0 성공, 1 오류, 2 잘못된 인자 또는 게임 폴더 아님, 3 게임 실행 중, 4 다운로드·해시 검증 실패.

## 빌드

.NET SDK 8 이상이 필요합니다. 게임 파일은 필요 없습니다.

```powershell
dotnet build src\AdofaiModInstaller.csproj -c Release
```

결과: `src\bin\Release\net48\ADOFAI-Mod-Installer.exe`. 태그(`v*`)를 푸시하면 GitHub Actions가 빌드·자체 검사를 거쳐 실행 파일과 SHA-256 체크섬을 릴리스에 올립니다.

## 파란 창(Windows SmartScreen)이 뜰 때

브라우저로 받은 exe를 처음 실행하면 "Windows에서 PC를 보호했습니다" 같은 **파란 창**이 뜰 수 있습니다. 이 설치기에 코드 서명이 아직 없고 이용자가 적어서 SmartScreen이 모르는 파일로 취급하기 때문이며, 악성으로 판정했다는 뜻은 아닙니다.

1. 받은 파일이 릴리스의 `SHA256SUMS.txt`와 같은 파일인지 확인합니다.

   ```powershell
   Get-FileHash .\ADOFAI-Mod-Installer.exe -Algorithm SHA256
   ```

2. 해시가 같으면 파란 창에서 `추가 정보` → `실행`을 누릅니다.
3. 창이 뜨기 전에 없애려면 exe를 우클릭 → `속성` → 아래쪽 `차단 해제`에 체크 → `확인`을 누릅니다. (인터넷에서 받았다는 표식이 지워집니다.)

해시가 다르거나 이 저장소의 릴리스가 아닌 곳에서 받았다면 실행하지 마세요. 경고를 없애려고 SmartScreen이나 백신을 끄지 마세요. 백신이 따로 경고하면 같은 방법으로 해시를 확인한 뒤 본인이 판단해 예외를 추가하세요.

**코드 서명:** 아직 없습니다. 2026-10-26에 SignPath Foundation의 무료 오픈소스 코드 서명을 신청했지만, 프로젝트가 새롭고 이용자·외부 언급이 아직 부족하다는 이유로 이번에는 승인되지 않았습니다. 이용자가 늘면 다시 신청할 계획이며, 정책 초안은 [docs/CODE_SIGNING_POLICY.md](docs/CODE_SIGNING_POLICY.md)에 있습니다. 그때까지는 위의 해시 확인 방법을 쓰세요.

## 자주 묻는 질문

<details>
<summary>파란 창("Windows에서 PC를 보호했습니다")이 떠요</summary>

SmartScreen 경고입니다. 위의 [파란 창](#파란-창windows-smartscreen이-뜰-때) 절의 방법대로 해시를 확인한 뒤 `추가 정보` → `실행`을 누르세요.
</details>

<details>
<summary>백신이 설치기나 <code>winhttp.dll</code>을 막거나 지워요</summary>

설치기는 서명이 없어서 이용자가 적은 동안 일부 백신이 의심할 수 있습니다. 또 UnityDoorstop의 `winhttp.dll`은 게임 시작 때 모드 로더를 불러오는 방식이라 동작이 일부 악성코드와 비슷해 보여서 오탐이 나기도 합니다. 먼저 받은 파일의 SHA-256이 릴리스의 `SHA256SUMS.txt`와 같은지 확인하세요. 같고 이 저장소의 릴리스에서 받았다면, 게임 폴더에 한해 예외를 추가할지는 본인이 판단해서 결정하세요. 백신이나 SmartScreen을 통째로 끄지는 마세요.
</details>

<details>
<summary>얼불춤 폴더를 못 찾아요 / "선택한 폴더는 얼불춤 폴더가 아닙니다"</summary>

자동 탐색은 스팀 설치 경로와 스팀 라이브러리 폴더만 찾습니다. 스팀에서 `얼불춤` 우클릭 → `관리` → `로컬 파일 탐색`을 누르면 게임 폴더가 열리니, 그 주소창의 경로를 `찾아보기`로 선택하세요. 폴더 안에 `A Dance of Fire and Ice.exe`와 `A Dance of Fire and Ice_Data` 폴더가 모두 있어야 게임 폴더로 인정됩니다. 스팀이 아닌 설치는 같은 방법으로 폴더만 고르면 되지만 확인하지는 못했습니다.
</details>

<details>
<summary>"게임이 실행 중입니다"라고 나와요</summary>

게임이 켜져 있으면 파일이 잠겨서 설치할 수 없습니다. 게임을 끈 뒤 다시 `다음`을 누르세요. 창이 안 보여도 프로세스가 남아 있을 수 있으니 작업 관리자에서 `A Dance of Fire and Ice`를 확인하세요.
</details>

<details>
<summary>다운로드 또는 해시 오류가 나요</summary>

- 인터넷 연결과 GitHub·Dropbox 접속이 되는지 확인하세요. 학교·회사 망에서는 막혀 있을 수 있습니다.
- "해시 불일치"는 받은 파일이 카탈로그에 적힌 것과 다르다는 뜻입니다. 이때는 **아무것도 설치되지 않는 것이 정상**입니다. UnityModManager는 배포처가 Dropbox 링크뿐이라 파일이 바뀌면 이렇게 될 수 있으니, [이슈](https://github.com/newmouse11/Nslookup_-ADOFAI-Mod-Install/issues)로 알려 주세요.
</details>

<details>
<summary>설치는 끝났는데 게임에서 모드 창이 안 떠요</summary>

1. 게임을 완전히 끈 뒤 다시 실행하세요.
2. 게임 안에서 `Ctrl + F10`을 누르세요(단축키를 바꿨다면 바꾼 키).
3. 게임 폴더에 `winhttp.dll`과 `doorstop_config.ini`가 있는지 보세요. 없으면 백신이 지웠을 수 있습니다.
4. 설치기를 다시 실행하면 빠진 필수 요소가 자동으로 다시 선택됩니다.

그래도 안 되면 [이슈](https://github.com/newmouse11/Nslookup_-ADOFAI-Mod-Install/issues)에 로그와 게임 빌드(r번호)를 올려 주세요. 참고로 이 설치기로 설치한 모드가 실제 게임에서 로드되는지는 아직 확인하지 못했습니다([검증 상태](#검증-상태)).
</details>

<details>
<summary>게임이 업데이트됐더니 모드가 사라졌어요 / 경로 분석에 노란 경고가 떠요</summary>

설치기를 다시 실행하세요. `경로 분석`의 "UnityModManager가 Assembly 방식으로 설치되어 있습니다"는 게임을 업데이트할 때마다 다시 설치해야 하는 방식이라는 뜻이고, "Doorstop이 구버전입니다"는 `설치 요소 선택`에서 UnityDoorstop을 다시 설치하라는 뜻입니다.
</details>

<details>
<summary>이미 다른 방법(공식 Jipper 설치기, 수동 설치)으로 설치했어요</summary>

설치된 구성요소는 설치된 버전과 함께 `(설치됨 …)`으로 표시되고, 필수 요소는 없을 때만 자동으로 선택됩니다. 덮어쓰는 파일은 `게임폴더\ADOFAI-Mod-Installer\backup\`에 백업하고, `제거`를 하면 이 설치기가 설치한 파일만 지우고 백업을 되돌립니다. 그래도 큰 변경 전에는 `Mods` 폴더를 복사해 두면 안심입니다.
</details>

<details>
<summary>중간에 실패했어요. 로그는 어디 있나요?</summary>

적용 중 오류가 나면 변경을 자동으로 되돌립니다. 완료 화면의 `로그 보기`를 누르거나 `%TEMP%\ADOFAI-Mod-Installer.log`를 여세요. 로그에는 게임 폴더 경로가 들어 있어 Windows 사용자 이름이 보일 수 있으니, 이슈에 올릴 때는 지워서 올리세요.
</details>

<details>
<summary>"액세스가 거부되었습니다" 같은 오류가 나요</summary>

흔한 원인은 게임이 켜져 있는 경우, 백신의 폴더 보호(랜섬웨어 방지, Windows의 `제어된 폴더 액세스` 등)가 게임 폴더 쓰기를 막는 경우입니다. 보호 기능에 설치기를 허용하거나 잠시 해제한 뒤 다시 시도하세요. 설치기는 관리자 권한을 요구하지 않습니다.
</details>

<details>
<summary>Key16B 패치가 뭐예요? 키뷰어가 이상해졌어요</summary>

JipperResourcePack 키뷰어의 스타일 줄에 `Key16B`(두 번째 16키 프리셋)를 추가한 수정 빌드로 `Jipper Key16B 패치`를 체크하면 설치됩니다([자세히](#key16b-패치)). JALib의 자동 업데이트가 공식 Jipper로 덮어쓴 뒤 키뷰어가 이상하면, `Mods\JipperResourcePack\Settings.json`의 `KeyViewerStyle` 값을 `1`(Key16)로 바꾸거나 설치기에서 이 패치를 다시 설치하세요.
</details>

<details>
<summary>모드를 더 추가하고 싶어요</summary>

[카탈로그 수정](#카탈로그-수정-구성요소-추가)을 보세요. 설치한 모드는 게임 안에서 제한 없이 실행되는 코드이니 출처를 믿을 수 있는 것만 추가하세요.
</details>

<details>
<summary>공식 프로그램인가요? Mac·Linux에서도 되나요?</summary>

7th Beat Games의 공식 프로그램이 아닌 커뮤니티 도구입니다. Windows 전용이며 다른 운영체제는 지원하지 않습니다.
</details>

## 카탈로그 수정 (구성요소 추가)

`src/catalog.json`에 항목을 추가하고 다시 빌드하거나, `/catalog:`로 다른 파일을 지정합니다.

| 필드 | 설명 |
|---|---|
| `id`, `name`, `version`, `author`, `license`, `homepage`, `note` | 표시와 출처 정보 |
| `url`, `sha256`, `size` | 받을 주소(https)와 정확한 해시·바이트 수. 하나라도 다르면 설치 중단 |
| `kind` | `umm` · `doorstop` · `mod`(`Mods\<dir>`에 설치) · `file`(`path`에 파일 하나) |
| `dir`, `path` | `mod`의 폴더 이름, `file`의 게임 폴더 기준 상대 경로 |
| `required`, `defaultOn` | 필수 / 기본 선택 |
| `requires`, `conflicts` | 의존 / 충돌하는 id |

해시와 크기는 `tools\catalog-entry.ps1 -File 받은파일.zip -Url ... -Id ... -Name ... -Version ...`으로 계산할 수 있습니다. GitHub 릴리스는 API(`assets[].digest`)의 값과도 대조하세요.

`mod`의 zip은 `Info.json`이 루트에 있거나, 최상위 폴더 하나 안에 있어야 합니다.

## 검증 상태

- 자체 검사(`/selftest`) 40개 통과: 카탈로그 검증, 경로 이탈 거부, zip 구조 처리, 롤백·제거(부분 제거 포함), 해시·크기 검증, 같은 경로 처리, 설치된 구성요소 판단.
- 마법사 화면을 UI 자동화로 실제 클릭해서 시작 → 폴더 → 요소 선택 → 설치 → 완료 → 종료(설치), 제거 → 확인 → 완료 → 종료(제거) 전 과정이 가짜 게임 폴더에서 끝까지 동작하는 것을 확인했습니다.
- Key16B 패치 구성요소: 새 설치와 공식 Jipper 위에 교체 설치 모두 DLL이 정확히 들어가고, 제거하면 파일이 남지 않는 것을 확인했습니다.
- 실행 파일 자리표시자만 있는 가짜 게임 폴더에 공식 배포처에서 실제로 받아 설치, 재설치, 제거, 기존 파일 백업·복원, 해시 불일치 거부를 확인했습니다. 설치된 `winhttp.dll`, `doorstop_config.ini`, `Config.xml`은 실제로 동작 중인 환경의 파일과 바이트 단위로 같았습니다.
- GitHub Actions의 태그 빌드(v1.2.0, v1.2.1)는 빌드, 자체 검사, 릴리스 업로드까지 성공했고, 릴리스에서 받은 exe의 해시와 자체 검사도 확인했습니다.
- 확인하지 못한 것: 설치된 모드가 **실제 게임에서 로드되는지**.
- UnityModManager는 배포처의 파일(현재 0.33.2)을 그대로 설치합니다. 이전 버전(0.32.x)에서 확인된 조합과 다를 수 있습니다.

## Key16B 패치

JipperResourcePack 키뷰어의 스타일 줄에 `Key16B` 버튼(두 번째 16키 프리셋, 키 배치를 따로 저장)을 추가한 수정 빌드입니다. 설치기에서 `Jipper Key16B 패치`를 체크하면 공식 DLL을 이 빌드로 교체하고, 제거하면 되돌립니다.

| 위치 | 내용 |
|---|---|
| `dist/jipper-key16b/` | 수정 DLL(`JipperResourcePack-1.5.2.0-Key16B.dll`)과 라이선스 고지 |
| `patches/` | 기능 변경(`jipper-key16b.patch`)과 빌드 조정(`jipper-local-build-tweaks.patch`) |
| `tools/jipper-build/` | 직접 다시 빌드하는 도구와 절차 |

공식 빌드가 아니며 1.5.2.0 전용입니다. 바뀐 내용 전부와 검증 결과는 [CHANGES.md](CHANGES.md)에 있습니다.

## 저장소 구성

| 경로 | 내용 |
|---|---|
| `src/` | 설치기 소스와 내장 카탈로그(`catalog.json`) |
| `dist/` | Key16B 수정 DLL |
| `patches/`, `tools/` | 패치, 카탈로그 항목 계산 스크립트, DLL 재빌드 도구 |
| `README.md`, `README.en.md` | 한국어 / 영어 설명서 |
| `NOTICE.md`, `CHANGES.md`, `SECURITY.md`, `LICENSE` | 출처·저작권, 바꾼 내용, 보안, 라이선스 |
| `docs/` | 화면 미리보기, 코드 서명 정책 |

## 라이선스

[MIT](LICENSE) © 2026 newmouse11. 패치는 원본(BSD-3-Clause)을 따릅니다.
