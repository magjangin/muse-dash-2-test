param(
    [string]$Path = 'H:\muse dash 2 hwa\MelonLoader\Il2CppAssemblies'
)

if (-not (Test-Path $Path)) {
    Write-Output "경로를 찾을 수 없습니다: $Path"
    exit 1
}

Write-Output "Scanning DLLs in: $Path"
$dlls = Get-ChildItem -Path $Path -Filter *.dll -File -ErrorAction SilentlyContinue | Sort-Object Name
if ($dlls -eq $null -or $dlls.Count -eq 0) {
    Write-Output "DLL 파일이 없습니다."
    exit 0
}

Write-Output "Found $($dlls.Count) assemblies:`n"
foreach ($d in $dlls) {
    Write-Output $d.FullName
}

exit 0
