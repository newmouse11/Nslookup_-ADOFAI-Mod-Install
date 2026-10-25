# 현재 게임 폴더의 "활성 모드 상태"를 payload.zip + manifest.txt로 만든다.
# 개인 통계(Plays.dat, KeyCount.dat), 캐시(.cache, assembly_cache), 백업(.bak), 로그는 제외한다.
param(
    [Parameter(Mandatory)][string]$Game,
    [string]$Out = (Join-Path $PSScriptRoot '..\src')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$umm = 'A Dance of Fire and Ice_Data\Managed\UnityModManager'
$skipName = '^(Plays\.dat|KeyCount\.dat|Log\.txt)$'
$skipPath = '\.cache$|\.bak$|\\assembly_cache\\'

# (항목 이름, 버전, 파일 목록) 수집
$groups = New-Object System.Collections.Generic.List[object]
function Add-Group([string]$name, [string]$version, [object[]]$files) {
    $groups.Add([pscustomobject]@{ Name = $name; Version = $version; Files = $files })
}

$ummFiles = Get-ChildItem "$Game\$umm" -File | Where-Object { $_.Name -notmatch $skipName }
Add-Group 'UnityModManager' (Get-Item "$Game\$umm\UnityModManager.dll").VersionInfo.FileVersion $ummFiles
Add-Group 'Doorstop' (Get-Item "$Game\winhttp.dll").VersionInfo.FileVersion @(Get-Item "$Game\winhttp.dll", "$Game\doorstop_config.ini")

# Mods\<폴더>\Info.json이 있는 폴더만 활성 모드로 본다 (중첩된 Mods\Mods 같은 폴더는 제외)
foreach ($d in Get-ChildItem "$Game\Mods" -Directory | Sort-Object Name) {
    $info = Join-Path $d.FullName 'Info.json'
    if (-not (Test-Path $info)) { continue }
    $j = Get-Content $info -Raw | ConvertFrom-Json
    $files = Get-ChildItem $d.FullName -Recurse -File | Where-Object {
        $_.Name -notmatch $skipName -and $_.FullName -notmatch $skipPath
    }
    Add-Group $j.Id "$($j.Version)" $files
}

$zipPath = Join-Path $Out 'payload.zip'
if (Test-Path $zipPath) { [IO.File]::Delete($zipPath) }
$lines = New-Object System.Collections.Generic.List[string]
$sha = [Security.Cryptography.SHA256]::Create()
$gameDll = "$Game\A Dance of Fire and Ice_Data\Managed\Assembly-CSharp.dll"
$lines.Add("meta`tgameHash`t" + (Get-FileHash $gameDll -Algorithm SHA256).Hash)
$lines.Add("meta`tbuiltAt`t" + (Get-Date -Format 'yyyy-MM-dd'))

$zip = [IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
    foreach ($g in $groups) {
        $lines.Add("item`t$($g.Name)`t$($g.Version)`t$(@($g.Files).Count)")
        foreach ($f in $g.Files) {
            $rel = $f.FullName.Substring($Game.Length + 1).Replace('\', '/')
            $bytes = [IO.File]::ReadAllBytes($f.FullName)
            $hash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '')
            $entry = $zip.CreateEntry($rel, [IO.Compression.CompressionLevel]::Optimal)
            $s = $entry.Open(); $s.Write($bytes, 0, $bytes.Length); $s.Dispose()
            $lines.Add("file`t$($g.Name)`t$hash`t$($bytes.Length)`t$rel")
        }
    }
} finally { $zip.Dispose() }
[IO.File]::WriteAllLines((Join-Path $Out 'manifest.txt'), $lines, (New-Object Text.UTF8Encoding($false)))
"payload: {0:N0} bytes, files: {1}" -f (Get-Item $zipPath).Length, @($lines | Where-Object { $_ -like 'file*' }).Count
$lines | Where-Object { $_ -like 'item*' -or $_ -like 'meta*' }
