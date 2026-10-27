# 두 SignatureDumper 출력 폴더(Decompiled/)를 타입 파일 단위로 비교합니다.
# 2편 첫날 1편 덤프를 "이전 덤프"로 두고, 사라진 타입 / 새로 생긴 타입 / 둘 다 있는 타입을 나눠 봅니다.
# 둘 다 있는 타입은 -Detail을 주면 멤버 줄(시그니처) 차이까지 보여줍니다.
param(
    # 1편 6.7.0을 지금 덤퍼로 떠 둔 고정 스냅샷입니다(2026-10-27). 1편 저장소의 Decompiled/는 예전 덤퍼 출력이라
    # params 배열 표기(Int32[] / int[]) 같은 잡음 차이가 섞입니다.
    [string]$Old = (Join-Path $PSScriptRoot '..\reference\md1-6.7.0-signatures'),
    [string]$New = (Join-Path $PSScriptRoot '..\Decompiled'),
    [string]$Out = (Join-Path $PSScriptRoot '..\scratch\dump_diff.txt'),
    [switch]$Detail,
    # 2편에서 어셈블리 이름이 바뀌었을 때만 켭니다. 켜면 어셈블리 폴더를 빼고 "네임스페이스\타입.cs"로 대조하는데,
    # 여러 어셈블리에 같은 이름의 타입이 있으면 하나로 겹칩니다(1편 firstpass와 spine-unity가 그렇습니다).
    [switch]$IgnoreAssembly
)

foreach ($p in @($Old, $New)) {
    if (-not (Test-Path $p)) { Write-Output "폴더가 없습니다: $p"; exit 1 }
}

function Get-TypeFiles([string]$root) {
    $rootFull = (Resolve-Path $root).Path.TrimEnd('\')
    $map = @{}
    Get-ChildItem -Path $rootFull -Recurse -Filter *.cs -File | ForEach-Object {
        $rel = $_.FullName.Substring($rootFull.Length + 1)
        $key = if ($IgnoreAssembly) { ($rel -split '\\', 2)[1] } else { $rel }
        if ($key) { $map[$key] = $_.FullName }
    }
    return $map
}

$oldMap = Get-TypeFiles $Old
$newMap = Get-TypeFiles $New

$removed = $oldMap.Keys | Where-Object { -not $newMap.ContainsKey($_) } | Sort-Object
$added   = $newMap.Keys | Where-Object { -not $oldMap.ContainsKey($_) } | Sort-Object
$common  = $oldMap.Keys | Where-Object { $newMap.ContainsKey($_) } | Sort-Object

New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
$w = [System.IO.StreamWriter]::new($Out, $false, [System.Text.Encoding]::UTF8)
$w.WriteLine("Old: $Old")
$w.WriteLine("New: $New")
$w.WriteLine("사라진 타입 $($removed.Count)개 / 새 타입 $($added.Count)개 / 공통 $($common.Count)개")

$w.WriteLine(); $w.WriteLine("===== 사라진 타입 (Old에만 있음) =====")
$removed | ForEach-Object { $w.WriteLine("- $_") }
$w.WriteLine(); $w.WriteLine("===== 새 타입 (New에만 있음) =====")
$added | ForEach-Object { $w.WriteLine("+ $_") }

if ($Detail) {
    $w.WriteLine(); $w.WriteLine("===== 공통 타입의 시그니처 차이 =====")
    foreach ($k in $common) {
        $a = Get-Content $oldMap[$k] | ForEach-Object { $_.Trim() } | Where-Object { $_ }
        $b = Get-Content $newMap[$k] | ForEach-Object { $_.Trim() } | Where-Object { $_ }
        $diff = Compare-Object $a $b
        if ($diff) {
            $w.WriteLine(); $w.WriteLine("## $k")
            foreach ($d in $diff) {
                $mark = if ($d.SideIndicator -eq '<=') { '-' } else { '+' }
                $w.WriteLine("  $mark $($d.InputObject)")
            }
        }
    }
}
$w.Close()

Write-Output "사라진 타입 $($removed.Count)개 / 새 타입 $($added.Count)개 / 공통 $($common.Count)개"
Write-Output "결과: $Out"
