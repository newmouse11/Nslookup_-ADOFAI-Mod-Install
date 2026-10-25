# ADOFAI one-click installer

얼불춤(A Dance of Fire and Ice) 모드 구성을 **내 게임 폴더 그대로** 패키징해서, 버튼 한 번으로 다른 PC에 재현하는 설치기입니다.

이 저장소에는 **설치기 소스와 패키징 도구만** 있습니다. 모드 바이너리(UnityModManager, Doorstop, 각 모드, 폰트)는 들어 있지 않습니다. 빌드하는 사람이 자기 게임 폴더에서 직접 패키징합니다. 이유는 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)를 보세요.

## 동작

- 스팀 레지스트리 → `libraryfolders.vdf` → 흔한 드라이브 경로 순으로 게임 폴더를 찾습니다.
- 페이로드의 파일을 게임 폴더에 풀고, 이미 있는 파일이 다르면 `ADOFAI-OneClick-Backup\날짜-시각\`에 보관한 뒤 덮어씁니다. 같은 파일은 건드리지 않고, 목록에 없는 다른 모드도 건드리지 않습니다.
- 압축 해제 경로가 게임 폴더 밖이면 거부합니다(Zip Slip 방지). 각 파일은 SHA-256으로 검사하고, 설치 후에도 전체를 다시 검증합니다.
- 게임이 실행 중이면 설치하지 않습니다. 설치기가 기준으로 삼은 게임 빌드(`Assembly-CSharp.dll` 해시)와 다르면 경고하고 한 번 더 눌러야 진행됩니다.
- 관리자 권한을 요구하지 않습니다. 네트워크 요청이 없습니다.

## 빌드

필요한 것: .NET SDK (C# 12 이상), 모드가 설치된 내 게임 폴더.

```powershell
# 1) 내 게임 폴더에서 payload.zip + manifest.txt 생성 (src 폴더에 만들어짐)
.\tools\make-payload.ps1 -Game "D:\SteamLibrary\steamapps\common\A Dance of Fire and Ice"

# 2) 설치기 빌드 -> src\bin\Release\net48\ADOFAI-OneClick-Installer.exe
dotnet build src\OneClick.csproj -c Release
```

`make-payload.ps1`이 패키지에 넣는 것: `Mods\<폴더>\Info.json`이 있는 활성 모드, UnityModManager 폴더, `winhttp.dll`, `doorstop_config.ini`. 제외하는 것: 플레이 기록(`Plays.dat`, `KeyCount.dat`), 캐시(`*.cache`, `assembly_cache`), 백업(`*.bak`), 로그.

## 사용

`ADOFAI-OneClick-Installer.exe`를 실행하고 `설치`를 누릅니다. 옵션:

| 옵션 | 설명 |
|---|---|
| `/silent` | 화면 없이 설치. 결과는 로그 파일에 기록 |
| `/target:"경로"` | 게임 폴더 지정 (생략하면 자동 탐색) |
| `/force` | 게임 빌드가 달라도 진행 (`/silent`일 때) |
| `/log:"경로"` | 로그 파일 위치 (기본: `%TEMP%\ADOFAI-OneClick.log`) |

종료 코드: 0 성공, 1 오류, 2 게임 폴더 아님 또는 빌드 불일치(`/force` 없음), 3 게임 실행 중.

## patches/jipper-key16b.patch

[JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack) v1.5.2.0의 키뷰어에 16키 레이아웃을 쓰는 두 번째 프리셋 `Key16B`(키 배치를 따로 저장)를 추가하는 패치입니다. 원본 프로젝트는 BSD-3-Clause이고, 이 패치도 같은 조건을 따릅니다.

```bash
git apply patches/jipper-key16b.patch
```

원본 소스는 r149 이상 게임 DLL을 기준으로 해서 구버전 게임에서는 그대로 컴파일되지 않습니다. 이 패치는 기능 변경만 담고 있고, 빌드 환경 조정은 포함하지 않았습니다.

## 알아둘 점

- 서명이 없는 exe라 SmartScreen/백신이 경고할 수 있습니다.
- 모드가 업데이트되면 설치기는 패키징 시점의 파일로 되돌립니다. 업데이트 후에는 패키지를 다시 만들어 빌드하세요.

## 라이선스

이 저장소의 코드는 [MIT](LICENSE)입니다. 패치는 원본(BSD-3-Clause)을 따릅니다.
