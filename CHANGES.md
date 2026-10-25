# 바꾼 내용

## JipperResourcePack 1.5.2.0 패치 (`patches/jipper-key16b.patch`)

키뷰어에 16키 레이아웃을 쓰는 두 번째 프리셋 `Key16B`를 추가했습니다. 키 배치를 따로 저장하고, 나머지 동작은 `Key16`과 같습니다. 원본 소스는 v1.5.2.0 태그입니다.

| 파일 | 변경 |
|---|---|
| `KeyViewerContents/KeyviewerStyle.cs` | 열거형 끝에 `Key16B`(값 4) 추가. 기존 설정값 0~3은 그대로 |
| `KeyViewerContents/KeyViewerSetting.cs` | `key16B`, `key16BText`, `GhostKey16B` 필드 추가 (기본값은 `key16`과 같음) |
| `KeyViewerContents/KeyViewer.cs` | 스타일 선택 목록에 `Key16B` 추가, 키·텍스트·고스트·배치 순서 조회와 `키 공유` 전환에 `Key16B` 처리 추가, 16키 레이아웃 초기화와 `Total, Kps 표시` 토글을 `Key16`과 공유 |

적용: `git apply patches/jipper-key16b.patch` (v1.5.2.0 태그에서 확인).

패치된 모드를 JALib의 자동 업데이트가 새 버전으로 덮어쓰면 `Key16B` 값(4)을 모르는 버전이 되므로, 이때는 `Settings.json`의 `KeyViewerStyle`을 `1`(Key16)로 돌려야 합니다.

## 원래 JipperResourcePack 설치기와 달라진 점

| 항목 | 원래 설치기 | 이 설치기 |
|---|---|---|
| 내려받는 파일 검증 | 없음 | 카탈로그의 SHA-256·크기와 일치해야 설치 |
| 버전 | `latest`(JALib은 프리릴리스 포함), 가변 Dropbox 링크 | 카탈로그에 버전 고정 |
| 모드 목록 | 실행 때 GitHub main 브랜치의 `mods.json`을 받음 | 실행 파일에 내장. 다른 목록은 `/catalog:`로 직접 지정 |
| 주소 | 목록이 주는 URL을 그대로 사용 | https만 허용 (loopback 테스트 제외), 리다이렉트도 검사 |
| 압축 해제 | 경로 검사 없음 | 게임 폴더와 모드 폴더 밖으로 나가는 경로 거부 |
| 설치 순서 | 받으면서 바로 덮어씀, 실패하면 중간 상태로 남음 | 전부 받아 검증한 뒤 적용, 적용 중 실패하면 되돌림 |
| 백업·제거 | 모드 폴더 삭제 위주 | 덮어쓴 파일 백업, 설치 기록(영수증)에 있는 파일만 제거하고 백업 복원 |
| 게임 빌드 확인 | 게임 DLL을 불러와 상수 읽기 | 리플렉션 전용 로드로 상수만 읽음 (표시용) |
| 자동 업데이트 | 최신 릴리스 조회 | 없음 (카탈로그를 갱신해 새 버전으로 배포) |
| 그 밖 | 한국어/영어 | 한국어만 |
