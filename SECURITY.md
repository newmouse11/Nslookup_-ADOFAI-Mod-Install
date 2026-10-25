# 보안

## 설치기가 하는 일

- 카탈로그(`src/catalog.json`)에 적힌 https 주소에서만 파일을 받고, SHA-256과 크기가 다르면 아무것도 설치하지 않습니다.
- 관리자 권한을 요구하지 않습니다. 쓰는 곳은 게임 폴더와 임시 폴더(`%TEMP%\AdofaiModInstaller`)뿐입니다.
- 설치하는 모드는 게임 안에서 제한 없이 실행되는 코드입니다. 카탈로그에 넣을 모드의 출처를 확인하세요.

## 취약점 신고

GitHub 저장소의 Security 탭에서 `Report a vulnerability`로 비공개 신고를 보내주세요. (저장소 설정에서 Private vulnerability reporting이 켜져 있어야 합니다.) 공개 이슈에는 재현 방법을 올리지 마세요.

## 알려진 한계

- UnityModManager는 배포처가 Dropbox 링크뿐입니다. 파일이 바뀌면 해시가 달라져 설치가 중단되며, 그때는 `src/catalog.json`의 해시를 새로 확인해 갱신해야 합니다.
- 실행 파일에 코드 서명이 없어 SmartScreen이나 백신이 경고할 수 있습니다. 릴리스에는 SHA-256 체크섬을 함께 올립니다.
