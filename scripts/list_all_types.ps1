$content = Get-Content 'HwaTypesDump.txt'
$types = $content | Where-Object { $_ -match '^\[Type\]' }
Write-Output "Total matched types in dump: $($types.Count)"
foreach ($t in $types) {
    Write-Output $t
}
