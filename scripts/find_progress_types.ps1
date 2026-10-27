param(
    [string]$DllPath = "H:\muse dash 2 hwa\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll",
    [string]$Net6Dir = "H:\muse dash 2 hwa\MelonLoader\net6",
    [string]$Il2cppDir = "H:\muse dash 2 hwa\MelonLoader\Il2CppAssemblies"
)

if (-not (Test-Path $DllPath)) {
    Write-Error "DLL not found: $DllPath"
    exit 1
}

# Register assembly resolution handler
$resolver = {
    param($sender, $args)
    $asmName = New-Object System.Reflection.AssemblyName($args.Name)
    $dllName = $asmName.Name + ".dll"
    
    # Try net6 folder
    $path = Join-Path $Net6Dir $dllName
    if (Test-Path $path) {
        return [System.Reflection.Assembly]::LoadFrom($path)
    }
    
    # Try Il2CppAssemblies folder
    $path = Join-Path $Il2cppDir $dllName
    if (Test-Path $path) {
        return [System.Reflection.Assembly]::LoadFrom($path)
    }
    
    return $null
}

[System.AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)

Write-Output "Loading assembly: $DllPath"
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($DllPath)
    Write-Output "Assembly loaded: $($asm.FullName)"
    $types = $asm.GetTypes()
    Write-Output "GetTypes() completed without exception."
} catch [System.Reflection.ReflectionTypeLoadException] {
    Write-Output "ReflectionTypeLoadException caught. Retrieving successfully loaded types..."
    $ex = $_.Exception
    $types = $ex.Types | Where-Object { $_ -ne $null }
} catch {
    Write-Output "General exception caught: $($_.Exception.GetType().FullName) - $($_.Exception.Message)"
    $types = @()
}

Write-Output "Total types retrieved: $($types.Count)"

if ($types.Count -gt 0) {
    # Broad search for progress bar or related battle stage indicators
    $keywords = @("Progress", "PlayBar", "StageBar", "Timeline", "PlayProgress", "StageProgress", "ProgressBar", "Slider", "Process", "Scroll", "Track", "Fever")
    $found = @()
    foreach ($type in $types) {
        $name = $type.Name
        if ($null -eq $name) { continue }
        
        $matches = $false
        foreach ($kw in $keywords) {
            if ($name.IndexOf($kw, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $matches = $true
                break;
            }
        }
        
        if ($matches) {
            $found += $type
        }
    }
    
    Write-Output "Found $($found.Count) matching types:"
    $found | Sort-Object FullName | Select-Object Name, FullName | Format-List
}
