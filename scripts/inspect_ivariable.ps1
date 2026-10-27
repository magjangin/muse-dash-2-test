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

$dllFiles = Get-ChildItem -Path $il2cppDir -Filter "*.dll"
foreach ($file in $dllFiles) {
    try {
        $assembly = [System.Reflection.Assembly]::LoadFrom($file.FullName)
        $types = @()
        try {
            $types = $assembly.GetTypes()
        } catch [System.Reflection.ReflectionTypeLoadException] {
            $types = $_.Exception.Types
        }
        
        $match = $types | Where-Object { $_ -ne $null -and $_.FullName -like '*Nice*IData*' }
        if ($match -ne $null) {
            Write-Host "Found matching IData in assembly: $($file.Name)"
            foreach ($m in $match) {
                Write-Host "  Type: $($m.FullName)"
                Write-Host "  IsInterface: $($m.IsInterface)"
                Write-Host "  BaseType: $($m.BaseType)"
                
                $m.GetMethods() | ForEach-Object {
                    if ($_.Name -like '*get_Item*' -or $_.Name -like '*Get*' -or $_.Name -like '*Count*' -or $_.Name -like '*keys*') {
                        Write-Host "    Method: $_"
                    }
                }
            }
            
            $impls = $types | Where-Object { $_ -ne $null -and $_.IsClass -and ($_.FullName -like '*Nice*') }
            Write-Host "  Other Nice Classes in same assembly:"
            foreach ($imp in $impls) {
                Write-Host "    Class: $($imp.FullName) (Base: $($imp.BaseType))"
            }
        }
    } catch {}
}
