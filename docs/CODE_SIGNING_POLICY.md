# Code signing policy

> 한국어 요약: 이 문서는 SignPath Foundation의 무료 오픈소스 코드 서명([조건](https://signpath.org/terms))에 맞춘 서명 정책 초안입니다. **2026-10-26에 신청했으나 이번에는 승인되지 않았고, 현재 릴리스는 서명되어 있지 않습니다.** 서명되지 않은 exe를 실행할 때의 파란 창은 [README](../README.md#파란-창windows-smartscreen이-뜰-때)를 보세요.

**Status: not signed.** The maintainer applied to the [SignPath Foundation](https://signpath.org/) free code signing program for open source projects on 2026-10-26. The application was declined for now because the project does not yet have enough community adoption and recognition (the Foundation's criterion, not a judgment of project quality). The maintainer plans to re-apply once the project is more widely used. Until a later application is accepted, release binaries are **not signed**.

<!-- 승인된 뒤에만 아래 문장을 README와 릴리스 본문, 이 문서 맨 위에 추가한다:
     Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/) -->

## What is signed

- Only `ADOFAI-Mod-Installer.exe`, built by the GitHub Actions workflow [`.github/workflows/build.yml`](../.github/workflows/build.yml) from the source in this repository (`src/`, including `src/catalog.json`).
- Third-party components are **not** signed by this project. The installer downloads them from their official sources and installs them only if their SHA-256 matches the catalog.
- `dist/jipper-key16b/JipperResourcePack-1.5.2.0-Key16B.dll` is a modified build of [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack) (BSD-3-Clause). It is shipped as data for the installer and is not submitted for signing.
- Every signing request is made from a tagged GitHub Actions build and needs manual approval.

## Team and roles

| Role | Who |
|---|---|
| Authors (can change the source) | [newmouse11](https://github.com/newmouse11) |
| Reviewers (review changes from non-committers) | [newmouse11](https://github.com/newmouse11) |
| Approvers (approve each signing request) | [newmouse11](https://github.com/newmouse11) |

This is currently a single-maintainer project. If contributors join, a second reviewer/approver will be added. Multi-factor authentication is required for every team member on the source repository and on SignPath before any signing request is made.

## Privacy policy

This program does not collect, store or send personal data and has no telemetry.

- **Network access:** it only downloads the components listed in `src/catalog.json` over HTTPS: release assets on `github.com` (and `objects.githubusercontent.com`) and the UnityModManager download on `dropbox.com`. As with any download, those services can see the IP address of the request. They are governed by the privacy policies of [GitHub](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement) and [Dropbox](https://www.dropbox.com/privacy).
- **Files written:** the game folder (`Mods\`, `winhttp.dll`, `doorstop_config.ini`, `A Dance of Fire and Ice_Data\Managed\UnityModManager\`), a state folder `ADOFAI-Mod-Installer\` inside the game folder (backups and the install receipt), and a temporary folder `%TEMP%\AdofaiModInstaller` that is deleted after installation.
- **No administrator rights** are requested.

## System changes and uninstall

The installer changes a game installation (it installs a mod loader and mods). Before it changes anything it downloads and verifies everything, backs up every file it overwrites, and rolls back on failure. The `제거 / Uninstall` option removes exactly the files recorded in the install receipt and restores the backups.
