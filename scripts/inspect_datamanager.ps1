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
$assembly1 = [System.Reflection.Assembly]::LoadFrom((Join-Path $il2cppDir 'Assembly-CSharp-firstpass.dll'))
$assembly2 = [System.Reflection.Assembly]::LoadFrom((Join-Path $il2cppDir 'Assembly-CSharp.dll'))

$types = @()
try {
    $types += $assembly1.GetTypes()
} catch [System.Reflection.ReflectionTypeLoadException] {
    $types += $_.Exception.Types
}
try {
    $types += $assembly2.GetTypes()
} catch [System.Reflection.ReflectionTypeLoadException] {
    $types += $_.Exception.Types
}

$target = $types | Where-Object { $_ -ne $null -and ($_.FullName -like '*Nice*DataManager*' -or $_.FullName -like '*Nice*DataObject*') }

foreach ($t in $target) {
    Write-Host "=============================================="
    Write-Host "Type: $($t.FullName)"
    Write-Host "=============================================="
    $t.GetMethods() | ForEach-Object {
        if ($_.Name -like '*Count*' -or $_.Name -like '*Key*' -or $_.Name -like '*Item*' -or $_.Name -like '*Value*' -or $_.Name -like '*Get*' -or $_.Name -like '*Contain*') {
            Write-Host "  $($_.ToString())"
        }
    }
}
