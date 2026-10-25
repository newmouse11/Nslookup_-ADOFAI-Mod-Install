# 바꾼 내용

이 저장소에서 원본과 달라진 것을 사소한 것까지 적었습니다.

## 1. JipperResourcePack 1.5.2.0 수정 빌드

배포 파일: `dist/jipper-key16b/JipperResourcePack-1.5.2.0-Key16B.dll` (151,552 B, SHA-256 `3c79b853e2aa5bcb9e9429b1b0ef99ee09ecda345bce2e146f2a1edfef6cd819`). 원본은 [Jongye0l/JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack) 태그 v1.5.2.0(BSD-3-Clause, © 2024 Jongyeol)이고, 공식 빌드가 아닙니다. 라이선스 고지는 `dist/jipper-key16b/NOTICE.txt`와 [NOTICE.md](NOTICE.md)에 있습니다.

### 1-1. 기능 변경 — `patches/jipper-key16b.patch`

키뷰어에 16키 레이아웃을 쓰는 두 번째 프리셋 `Key16B`를 추가했습니다. 키 배치를 따로 저장하고, 나머지 동작은 `Key16`과 같습니다.

| 파일 | 변경 |
|---|---|
| `KeyViewerContents/KeyviewerStyle.cs` | 열거형 끝에 `Key16B`(값 4) 추가. 기존 설정값 0~3은 그대로 |
| `KeyViewerContents/KeyViewerSetting.cs` | `key16B`, `key16BText`, `GhostKey16B` 필드 추가 (기본값은 `key16`과 같음) |
| `KeyViewerContents/KeyViewer.cs` | 스타일 선택 목록에 `Key16B` 추가. 키·텍스트·고스트·배치 순서 조회와 `키 공유` 전환에 `Key16B` 처리 추가. 16키 레이아웃 초기화와 `Total, Kps 표시` 토글을 `Key16`과 공유 |

### 1-2. 빌드를 위한 조정 — `patches/jipper-local-build-tweaks.patch`

동작을 바꾸지 않는 변경입니다. 얼불춤 r149 이전 게임 DLL에서도 컴파일되게 하려는 것입니다.

| 파일 | 변경 | 이유 |
|---|---|---|
| `JipperResourcePack.csproj` | `AdofaiTweaks`, `KeyboardChatterBlocker` 참조를 `$(RefDir)`의 스텁으로 | 두 모드가 설치되어 있지 않아도 빌드 |
| 같은 파일 | `Assembly-CSharp` 참조를 `$(RefDir)`의 파사드로 | r149+ 전용 API 보강 |
| 같은 파일 | `JipperResourcePack.VersionSafe` 프로젝트 참조를 사전 빌드 DLL 참조로 바꾸고 `BuildVersionSafeBuckets` 대상 제거 | 최신 버킷은 r149 이전 게임 DLL과 맞지 않음 |
| `OverlayContents/Status.cs` | `nameof(scrMisc.GetHitMarginInDeg)`, `nameof(scrMisc.GetHitMarginInSec)` 2곳을 같은 문자열 리터럴로 | 두 메서드가 r149에서 생김. `nameof`는 컴파일 때 문자열이 되므로 결과가 같음 |
| `OverlayContents/OverlayTextManagerNormal.cs`, `OverlayTextManagerCoop.cs` | `HitMargin.XPerfect.ToXScore()`를 `HitMarginHelper.ToXScore(HitMargin.XPerfect)`로 | 확장 메서드 호출을 정적 호출로 바꾼 것. 같은 IL |

### 1-3. 빌드 도구 — `tools/jipper-build/`

`Facade`(r149+ API를 보강한 `Assembly-CSharp` 참조), `stubs/`(`AdofaiTweaks`, `KeyboardChatterBlocker`의 빈 참조 어셈블리), 재현 절차 `README.md`. 게임 파일은 저장소에 없고, 결과물에는 게임 코드가 들어가지 않습니다.

### 1-4. 검증

- 수정 전 소스를 같은 방법으로 빌드해 공식 1.5.2.0 DLL과 IL을 비교했습니다. 차이는 `UnityModManager` 어셈블리 이름 문자열의 버전(0.32.4.0 → 0.32.5.0)과 데이터 영역 번지 이동뿐입니다.
- `Key16B`를 넣은 뒤 바뀐 메서드는 `KeyViewer`의 조회(`GetKeyCode`, `GetGhostKeyCode`, `GetKeyText`, `GetBackSequence`), `InitializeKeyViewer`, `ChangeKeyViewer`, `OnGUI`와 `KeyViewerSetting` 생성자였습니다.
- `tools/jipper-build/README.md`대로 새 폴더에서 다시 빌드한 DLL과 배포 DLL의 IL(28,721줄)이 같았습니다. 바이트는 컴파일마다 달라지므로 해시는 다릅니다.
- 확인하지 못한 것: 이 세션에서 게임을 실행해 `Key16B` 버튼을 눌러 보지는 못했습니다.

### 1-5. 주의

JALib의 자동 업데이트가 새 공식 버전으로 덮어쓰면 `Key16B` 값(4)을 모르는 버전이 됩니다. 이때는 `Settings.json`의 `KeyViewerStyle`을 `1`(Key16)로 돌리세요.

## 2. 설치기 (`src/`)

### 2-1. 원래 JipperResourcePack 설치기와 달라진 점

| 항목 | 원래 설치기 | 이 설치기 |
|---|---|---|
| 내려받는 파일 검증 | 없음 | 카탈로그의 SHA-256·크기와 일치해야 설치 |
| 버전 | `latest`(JALib은 프리릴리스 포함), 가변 Dropbox 링크 | 카탈로그에 버전 고정 |
| 모드 목록 | 실행 때 GitHub main 브랜치의 `mods.json`을 받음 | 실행 파일에 내장. 다른 목록은 `/catalog:`로 직접 지정 |
| 주소 | 목록이 주는 URL을 그대로 사용 | https만 허용 (loopback 테스트 제외), 리다이렉트도 검사 |
| 압축 해제 | 경로 검사 없음 | 게임 폴더와 모드 폴더 밖으로 나가는 경로, 드라이브·절대 경로·대체 스트림 거부 |
| 설치 순서 | 받으면서 바로 덮어씀, 실패하면 중간 상태로 남음 | 전부 받아 검증한 뒤 적용, 적용 중 실패하면 되돌림 |
| 백업·제거 | 모드 폴더 삭제 위주 | 덮어쓴 파일 백업, 설치 기록(영수증)에 있는 파일만 제거하고 백업 복원 |
| 게임 빌드 확인 | 게임 DLL을 불러와 상수 읽기 | 리플렉션 전용 로드로 상수만 읽음 (표시용) |
| 자동 업데이트 | 최신 릴리스 조회 | 없음 (카탈로그를 갱신해 새 버전으로 배포) |
| 언어 | 한국어/영어 | 한국어만 |

### 2-2. 구성요소 처리

- 구성요소는 체크로 고르고, 필요한 것(의존)은 자동으로 켜며 충돌하는 것은 함께 켜지 않습니다.
- 같은 경로를 여러 구성요소가 쓰면 카탈로그에서 뒤에 있는 것이 이깁니다. `Jipper Key16B 패치`가 공식 DLL을 대체하는 경우입니다. 영수증과 제거는 이긴 쪽에만 걸립니다.
- `umm` 종류는 UMM zip의 `UnityModManagerConfig.xml`에서 얼불춤 항목을 꺼내 `Config.xml`을 만듭니다. `doorstop` 종류는 게임 exe가 64비트인지 읽어 `x64`/`x86` `winhttp.dll`을 고르고 `doorstop_config.ini`를 씁니다.
- 설치된 `winhttp.dll`, `doorstop_config.ini`, `Config.xml`은 이전에 동작하던 환경의 파일과 바이트 단위로 같았습니다.

### 2-3. 화면 디자인

- 어두운 평면 테마: 바탕 `#0B0E11`, 패널 `#181A20`, 입력·버튼 `#1E2329`/`#2B3139`, 글자 `#EAECEF`/`#B7BDC6`/`#848E9C`.
- 노란색(`#FCD535`)은 `설치` 버튼과 키보드 포커스에만 씁니다. 성공·실패를 초록/빨강으로 칠하지 않고, 문제는 경고색(`#F0B90B`) 글자로만 표시합니다.
- 버튼 반경 4px, 그림자·그라데이션 없음, 표는 줄무늬 없이 호버·커서 행만 강조하고 글자 크기는 12px입니다.
- 진행 표시는 2px 선(채움은 노란색이 아닌 회색)입니다. 제목 표시줄도 어둡게 만듭니다(Windows 10 20H1 이상).
- 키보드로 표 이동(↑↓), 선택(Space), 버튼 포커스 표시를 지원합니다.

### 2-4. 그 밖의 사소한 것

- 로그 기본 위치 `%TEMP%\ADOFAI-Mod-Installer.log`, 다운로드 임시 폴더 `%TEMP%\AdofaiModInstaller\<무작위>`(설치 후 삭제), 백업 `게임폴더\ADOFAI-Mod-Installer\backup\<날짜-시각>\`, 영수증 `게임폴더\ADOFAI-Mod-Installer\receipt.json`.
- 관리자 권한을 요구하지 않고, Windows 10/11에 기본 포함된 .NET Framework 4.8에서 실행됩니다.
- 종료 코드: 0 성공, 1 오류, 2 잘못된 인자·게임 폴더 아님, 3 게임 실행 중, 4 다운로드·해시 검증 실패.
- 자체 검사(`/selftest`)가 모드 폴더 밖으로 나가는 zip 경로를 잡아내서, 게임 폴더 안이어도 그 모드의 폴더를 벗어나면 거부하도록 고쳤습니다.

## 3. 릴리스별

| 버전 | 변경 |
|---|---|
| v1.0.0 | 첫 공개: 해시 고정 다운로드, 롤백, 제거, 자체 검사 30개, CI |
| v1.1.0 | `Jipper Key16B 패치` 구성요소와 수정 DLL 배포, 같은 경로 처리(오버레이), 자체 검사 33개, `patches/`·`tools/jipper-build/`·`NOTICE` 보강 |
