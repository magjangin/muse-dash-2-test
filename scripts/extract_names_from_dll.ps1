param(
    [string]$File = 'H:\muse dash 2 hwa\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll',
    [int]$Max = 50
)

if (-not (Test-Path $File)) { Write-Output "파일이 없습니다: $File"; exit 1 }

try {
    $bytes = Get-Content -Path $File -Encoding Byte -ReadCount 0
    $text = [System.Text.Encoding]::ASCII.GetString($bytes)
} catch {
    Write-Output "파일 읽기 실패: $($_.Exception.Message)"; exit 1
}

$rx = '\b(?:Il2CppAssets|Il2Cpp|UnityEngine|System|Il2Cpp)\.[A-Za-z0-9_\.]+'
$matches = [regex]::Matches($text, $rx)

$set = New-Object System.Collections.Generic.HashSet[string]
foreach ($m in $matches) {
    $set.Add($m.Value) | Out-Null
}

if ($set.Count -eq 0) { Write-Output "일치하는 문자열 없음"; exit 0 }

$set | Sort-Object | Select-Object -First $Max | ForEach-Object { Write-Output $_ }

exit 0
