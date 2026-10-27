$il2cppDir = 'H:\muse dash 2 hwa\MelonLoader\Il2CppAssemblies'
$net6Dir = 'H:\muse dash 2 hwa\MelonLoader\net6'

$loading = New-Object System.Collections.Generic.HashSet[string]
[System.AppDomain]::CurrentDomain.add_AssemblyResolve({
    param($sender, $args)
    $name = (New-Object System.Reflection.AssemblyName($args.Name)).Name
    if ($loading.Contains($name)) { return $null }
    $loading.Add($name) | Out-Null
    try {
        $paths = @(
            (Join-Path $il2cppDir "$name.dll"),
            (Join-Path $net6Dir "$name.dll")
        )
        foreach ($p in $paths) {
            if (Test-Path $p) {
                return [System.Reflection.Assembly]::LoadFrom($p)
            }
        }
    } finally {
        $loading.Remove($name) | Out-Null
    }
    return $null
})

[System.Reflection.Assembly]::LoadFrom((Join-Path $net6Dir 'Il2CppInterop.Runtime.dll')) | Out-Null
$assembly2 = [System.Reflection.Assembly]::LoadFrom((Join-Path $il2cppDir 'Assembly-CSharp.dll'))

$types = @()
try {
    $types = $assembly2.GetTypes()
    Write-Host "GetTypes succeeded directly. Loaded $($types.Count) types."
} catch [System.Reflection.ReflectionTypeLoadException] {
    Write-Host "ReflectionTypeLoadException caught."
    Write-Host "LoaderExceptions:"
    foreach ($le in $_.Exception.LoaderExceptions) {
        Write-Host "  - $($le.Message)"
    }
    $types = $_.Exception.Types | Where-Object { $_ -ne $null }
    Write-Host "Loaded $($types.Count) successfully loaded types."
} catch {
    Write-Host "Other exception: $_"
}

# Print 10 types to check
Write-Host "Sample 10 types:"
$types | Select-Object -First 10 | ForEach-Object { Write-Host "  $($_.FullName)" }

# Let's search for Account and Save (case-insensitive)
$targets = $types | Where-Object { $_.FullName -and ($_.FullName.ToLower().Contains("account") -or $_.FullName.ToLower().Contains("save") -or $_.FullName.ToLower().Contains("game") -or $_.FullName.ToLower().Contains("data")) }
Write-Host "Found $($targets.Count) types matching Account/Save/Game/Data"
$targets | Select-Object -First 30 | ForEach-Object { Write-Host "  $($_.FullName)" }
