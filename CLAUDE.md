# ADOFAI Mod Helper — 프로젝트 규칙

## Git / GitHub
Why: 사용자가 "업데이트할 때마다 GitHub에 올려"라고 지시함 (전역 CLAUDE.md §12 "Never push"의 이 프로젝트 한정 예외).
- 검증된 변경마다: secret-scan → 커밋 → `git push origin main`. force-push와 히스토리 재작성은 여전히 금지.
- 커밋 작성자는 GitHub noreply 주소만 쓴다: `newmouse11 <77029057+newmouse11@users.noreply.github.com>`
  (저장소 로컬 git config에 설정됨). 개인 이메일이 커밋에 들어가면 공개 저장소에 노출된다.
- 원격: `https://github.com/Newmouse11/adofai-mod-helper` (기본 브랜치 `main`). 도구가 `catalog/mods.json`을
  이 저장소 main 브랜치에서 읽으므로 경로·브랜치 이름을 바꾸면 `Core/Catalog.cs`의 `RemoteUrl`도 같이 바꾼다.

## 검증
- `dotnet build src/AdofaiModHelper -c Release` 경고 0, `tests/SelfTest` 전부 통과 후에만 커밋.
- `catalog/mods.json`을 바꿨으면 `dotnet run --project tests/SelfTest -c Release -- --online`으로 실제 설치·Id 일치까지 확인.
- 사용자의 실제 게임 폴더는 사용자가 요청할 때만 수정한다. 테스트는 임시 폴더에 만든 복사본으로.
