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
$assembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $il2cppDir 'Assembly-CSharp.dll'))

$types = @()
try {
    $types = $assembly.GetTypes()
} catch [System.Reflection.ReflectionTypeLoadException] {
    $types = $_.Exception.Types | Where-Object { $_ -ne $null }
}

$targets = @(
    "Il2Cpp.SavedSongResult",
    "Il2CppAssets.Scripts.Helpers.AccountGameDatas",
    "Il2CppAssets.Scripts.UI.AccountSaveUtils"
)

foreach ($targetName in $targets) {
    $t = $types | Where-Object { $_.FullName -eq $targetName }
    if ($null -ne $t) {
        Write-Host "=============================================="
        Write-Host "TYPE: $($t.FullName)"
        Write-Host "=============================================="
        
        Write-Host "--- Fields ---"
        $t.GetFields([System.Reflection.BindingFlags]::Public -or [System.Reflection.BindingFlags]::NonPublic -or [System.Reflection.BindingFlags]::Instance -or [System.Reflection.BindingFlags]::Static) | ForEach-Object {
            Write-Host "  $($_.FieldType.Name) $($_.Name)"
        }
        
        Write-Host "--- Properties ---"
        $t.GetProperties() | ForEach-Object {
            Write-Host "  $($_.PropertyType.Name) $($_.Name)"
        }
        
        Write-Host "--- Methods ---"
        $t.GetMethods() | ForEach-Object {
            Write-Host "  $($_.ReturnType.Name) $($_.Name)($([string]::Join(', ', ($_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }))))"
        }
    } else {
        Write-Host "Could not find type: $targetName"
    }
}
