# catalog.json에 넣을 항목의 sha256과 size를 계산해 JSON 조각으로 출력한다.
#   .\tools\catalog-entry.ps1 -File .\JALib.zip -Id jalib -Name JALib -Version 1.0.0.45 -Kind mod -Dir JALib -Url https://...
# 파일은 직접 받아서 -File로 넘긴다. 받은 파일은 실행하지 않는다.
# GitHub 릴리스 파일은 API(assets[].digest)에도 sha256이 나오니 둘이 일치하는지 확인하면 더 안전하다.
param(
    [Parameter(Mandatory)][string]$File,
    [Parameter(Mandatory)][string]$Url,
    [Parameter(Mandatory)][string]$Id,
    [Parameter(Mandatory)][string]$Name,
    [Parameter(Mandatory)][string]$Version,
    [ValidateSet('umm', 'doorstop', 'mod', 'file')][string]$Kind = 'mod',
    [string]$Dir,
    [string]$Path,
    [string]$Author,
    [string]$License,
    [string]$Homepage
)
$ErrorActionPreference = 'Stop'
$item = [ordered]@{
    id = $Id; name = $Name; version = $Version; author = $Author; license = $License; homepage = $Homepage
    url = $Url
    sha256 = (Get-FileHash $File -Algorithm SHA256).Hash.ToLower()
    size = (Get-Item $File).Length
    kind = $Kind
}
if ($Dir) { $item.dir = $Dir }
if ($Path) { $item.path = $Path }
$item.GetEnumerator() | Where-Object { $null -ne $_.Value -and $_.Value -ne '' } | ForEach-Object -Begin { $o = [ordered]@{} } -Process { $o[$_.Key] = $_.Value } -End { $o | ConvertTo-Json }
