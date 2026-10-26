# JipperResourcePack Key16B 빌드 방법

`dist/jipper-key16b/JipperResourcePack-1.5.2.0-Key16B.dll`을 만든 과정을 그대로 재현하는 도구입니다. 얼불춤이 설치된 PC와 .NET SDK 10 이상이 필요합니다.

## 왜 도구가 따로 필요한가

JipperResourcePack 소스는 얼불춤 r149 이상의 `Assembly-CSharp.dll`을 기준으로 합니다. 설치된 게임이 r149보다 오래되면(이 빌드는 r148에서 만들었습니다) 몇몇 게임 API가 없어서 그대로는 컴파일되지 않습니다. 그래서 컴파일에만 쓰는 참조 어셈블리를 보강했습니다. 게임 파일과 보강한 참조 DLL은 이 저장소에 없고, 결과물에는 게임 코드가 들어가지 않습니다.

| 도구 | 하는 일 |
|---|---|
| `Facade/` | 설치된 `Assembly-CSharp.dll` 사본에 r149+ 전용 API를 덧붙임: `HitMargin` 값(공식 DLL의 IL 상수에서 역산: TooEarly 0 … XPerfect 4 … Auto 12, OverPress 13, Midspin 14), `scrMarginTracker.xScore`, `HitMarginHelper.ToXScore` |
| `stubs/AdofaiTweaks`, `stubs/KeyboardChatterBlocker` | 공식 DLL이 참조하는 멤버만 가진 빈 어셈블리 (게임에 설치되어 있을 필요가 없게 함) |

## 순서

PowerShell 기준입니다. `$Game`은 게임 폴더, `$Work`는 작업 폴더입니다.

```powershell
$Game = "D:\SteamLibrary\steamapps\common\A Dance of Fire and Ice"
$Work = "C:\work\jb"; $Refs = "$Work\refs"; New-Item -ItemType Directory -Force $Refs | Out-Null
$Managed = "$Game\A Dance of Fire and Ice_Data\Managed"

# 1) 소스: v1.5.2.0 + 패치 2개
git clone https://github.com/Jongye0l/JipperResourcePack "$Work\src"
git -C "$Work\src" checkout v1.5.2.0
git -C "$Work\src" apply (Resolve-Path patches\jipper-key16b.patch)
git -C "$Work\src" apply (Resolve-Path patches\jipper-local-build-tweaks.patch)

# 2) 파사드 Assembly-CSharp
dotnet build tools\jipper-build\Facade -c Release -o "$Work\facade"
dotnet "$Work\facade\Facade.dll" "$Managed\Assembly-CSharp.dll" "$Refs\Assembly-CSharp.dll"

# 3) 스텁 어셈블리
foreach ($n in 'AdofaiTweaks','KeyboardChatterBlocker') {
  dotnet build "tools\jipper-build\stubs\$n" -c Release -o "$Work\stub-$n" -p:GameManagedPath="$Managed"
  Copy-Item "$Work\stub-$n\$n.dll" $Refs
}

# 4) VersionSafe: 공식 릴리스(JipperResourcePack.zip)의 VersionSafe\JipperResourcePack.VersionSafe.R149.dll을 복사
Copy-Item "$Game\Mods\JipperResourcePack\VersionSafe\JipperResourcePack.VersionSafe.R149.dll" "$Refs\JipperResourcePack.VersionSafe.dll"

# 5) 경로 설정 후 빌드
@"
<Project><PropertyGroup>
  <GameManagedPath>$($Managed -replace '\\','/')</GameManagedPath>
  <RefDir>$($Refs -replace '\\','/')</RefDir>
</PropertyGroup></Project>
"@ | Set-Content "$Work\src\Directory.Build.props.user"
dotnet build "$Work\src\JipperResourcePack\JipperResourcePack.csproj" -c Release
```

결과: `$Work\src\JipperResourcePack\bin\Release\net481\JipperResourcePack.dll`. 4단계의 VersionSafe 파일은 공식 릴리스 zip을 풀어 얻은 것을 쓰세요(게임에 이미 설치된 것도 같은 파일입니다).

## 검증 기록

수정 전 소스를 같은 방법으로 빌드해서 공식 1.5.2.0 DLL과 IL을 비교했습니다. 차이는 두 가지뿐이었습니다. 하나는 `UnityModManager` 어셈블리 이름 문자열의 버전(공식 0.32.4.0, 이 빌드 0.32.5.0, 컴파일할 때 참조한 UMM 버전)이고, 다른 하나는 데이터 영역 번지 이동입니다. Key16B를 넣은 뒤에는 의도한 메서드(`KeyViewer`의 조회·초기화·UI 메서드와 `KeyViewerSetting` 생성자)만 바뀌었습니다.
