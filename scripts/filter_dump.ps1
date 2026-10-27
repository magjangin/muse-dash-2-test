$content = Get-Content 'HwaTypesDump.txt'
$types = $content | Where-Object { $_ -match '^\[Type\]' }
Write-Output "Total matched types in dump: $($types.Count)"

Write-Output "`n--- FILTERED TYPES (Stage|Battle|Play|Bar|Progress|Timeline|Slider|Track) ---"
$filtered = $types | Where-Object { $_ -match 'Stage|Battle|Play|Bar|Progress|Timeline|Slider|Track' }
foreach ($t in $filtered) {
    Write-Output $t
}
