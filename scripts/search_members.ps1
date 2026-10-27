$lines = Get-Content 'HwaTypesDump.txt'

$currentType = ""
$matchedTypes = @{}

foreach ($line in $lines) {
    if ($line -match '^\[Type\] (.*)') {
        $currentType = $Matches[1]
    }
    elseif ($line -match '^\s+\[(Field|Property|Method)\] .* (.*Progress.*|.*Bar.*|.*Slider.*|.*Process.*|.*Timeline.*|.*Track.*)') {
        $member = $line.Trim()
        if (-not $matchedTypes.ContainsKey($currentType)) {
            $matchedTypes[$currentType] = @()
        }
        $matchedTypes[$currentType] += $member
    }
}

$outputFile = "search_members_output.txt"
$writer = [System.IO.StreamWriter]::new($outputFile, $false, [System.Text.Encoding]::UTF8)

$writer.WriteLine("Matched types count: $($matchedTypes.Keys.Count)")
foreach ($type in $matchedTypes.Keys) {
    $writer.WriteLine()
    $writer.WriteLine("========================================================")
    $writer.WriteLine("Type: $type")
    $writer.WriteLine("========================================================")
    foreach ($mem in $matchedTypes[$type]) {
        $writer.WriteLine("  $mem")
    }
}
$writer.Close()

Write-Output "Successfully wrote search results to search_members_output.txt"
