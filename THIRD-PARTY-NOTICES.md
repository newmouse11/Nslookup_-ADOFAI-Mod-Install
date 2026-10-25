# Third-party notices

이 저장소는 아래 구성요소의 바이너리를 **포함하지 않습니다.** `tools/make-payload.ps1`이 빌드하는 사람의 게임 폴더에서 이 구성요소들을 읽어 `payload.zip`으로 묶습니다. 그렇게 만든 설치기(exe)를 다른 사람에게 배포하면, 배포하는 사람이 아래 라이선스 조건(저작권 고지 포함 등)을 지켜야 합니다.

| 구성요소 | 저작권자 | 라이선스 | 비고 |
|---|---|---|---|
| UnityModManager | newman55 | MIT | 고지 포함 필요 |
| UnityDoorstop (`winhttp.dll`) | NeighTools | LGPL-2.1 | 바이너리 배포 시 라이선스 사본과 대응 소스 제공 방법 필요 (소스: https://github.com/NeighTools/UnityDoorstop) |
| AdofaiTweaks | PizzaLovers007 | MIT | 고지 포함 필요 |
| JALib | Jongyeol | BSD-3-Clause | 고지 포함 필요 |
| JipperResourcePack | Jongyeol | BSD-3-Clause | 고지 포함 필요. 폰트 `MAPLESTORY_OTF_BOLD.OTF`가 함께 들어 있음(아래 참고) |
| AutoUpdate | HYONSU | 확인하지 않음 | 공개 저장소·라이선스 정보를 찾지 못함 |
| TogetherBootstrap | Sharky | 확인하지 않음 | 공개 저장소·라이선스 정보를 찾지 못함 |
| MapleStory 폰트 | Nexon (추정) | 확인하지 않음 | JipperResourcePack에 포함된 파일. 재배포 조건 확인 필요 |

라이선스 항목은 각 프로젝트의 LICENSE 파일(2026-10-25 확인)을 기준으로 했습니다. "확인하지 않음"으로 표시한 구성요소는 재배포 가능 여부를 알 수 없으므로, 그 구성요소가 들어간 설치기는 공개 배포하지 않는 것을 권장합니다.
