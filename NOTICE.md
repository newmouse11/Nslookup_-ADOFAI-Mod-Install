# 출처와 저작권

이 설치기는 아래 구성요소를 **저장소에 포함하지 않고**, 설치할 때 각 배포처에서 직접 내려받습니다. 내려받는 파일은 `src/catalog.json`에 적힌 SHA-256과 크기가 일치할 때만 설치됩니다.

| 구성요소 | 만든 사람 | 라이선스 | 출처 | 설치 때 받는 파일 |
|---|---|---|---|---|
| UnityModManager | newman55 (© 2018 newman55) | MIT | https://github.com/newman55/unity-mod-manager | 공식 안내(README)의 Dropbox 링크. 현재 0.33.2 |
| UnityDoorstop | NeighTools | LGPL-2.1 | https://github.com/NeighTools/UnityDoorstop | GitHub 릴리스 v4.5.0 `doorstop_win_release_4.5.0.zip` |
| JALib | Jongyeol | BSD-3-Clause | https://github.com/Jongye0l/JALib | GitHub 릴리스 v1.0.0.45 `JALib.zip` |
| JipperResourcePack | Jongyeol (© 2024 Jongyeol) | BSD-3-Clause | https://github.com/Jongye0l/JipperResourcePack | GitHub 릴리스 v1.5.2.0 `JipperResourcePack.zip` |
| AdofaiTweaks | PizzaLovers007 (© 2021 PizzaLovers007) | MIT | https://github.com/PizzaLovers007/AdofaiTweaks | GitHub 릴리스 v2.9.3, v2.8.1 |
| TogetherBootstrap | fangshenghan | 저장소에 라이선스 파일 없음 | https://github.com/fangshenghan/TogetherBootstrap-Mod | GitHub 릴리스 v1.5.5 |
| KeyboardChatterBlocker | fangshenghan | 저장소에 라이선스 파일 없음 | https://github.com/fangshenghan/KeyboardChatterBlocker | GitHub 릴리스 0.1.2 |

라이선스 항목은 각 저장소의 LICENSE(또는 GitHub가 인식한 라이선스)를 2026-10-25에 확인한 결과입니다. 라이선스 파일이 없는 두 모드는 이 설치기가 제작자의 배포처에서 내려받기만 하며, 저장소에 사본을 두지 않습니다.

게임 `A Dance of Fire and Ice`와 그 파일은 제작사의 소유이며 이 저장소에 포함되어 있지 않습니다.

## 이 저장소에 들어 있는 것

| 파일 | 저작권 | 라이선스 |
|---|---|---|
| `src/`, `tools/`, 문서 | © 2026 newmouse11 | MIT (`LICENSE`) |
| `patches/jipper-key16b.patch` | JipperResourcePack의 © 2024 Jongyeol 코드에 대한 변경분 | BSD-3-Clause (아래) |

## 참고한 프로젝트

설치 순서(UnityModManager, Doorstop, JALib, 모드)와 `doorstop_config.ini` 내용은 [JipperResourcePack 설치기](https://github.com/Jongye0l/JipperResourcePack/tree/main/Installer)(BSD-3-Clause, © 2024 Jongyeol)의 방식을 참고했습니다. 코드는 새로 작성했고, 달라진 점은 [CHANGES.md](CHANGES.md)에 적었습니다. 얼불춤용 `Config.xml`은 UnityModManager zip에 들어 있는 `UnityModManagerConfig.xml`의 항목에서 만들어집니다.

## BSD 3-Clause License (JipperResourcePack)

Copyright (c) 2024, Jongyeol
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
