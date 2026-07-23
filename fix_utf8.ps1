$dir = "C:\Users\Administrator\My project\Assets\Scripts\Simulation\Abilities"
$files = @("Batch1Abilities.cs", "Batch2Abilities.cs", "Batch3Abilities.cs")

foreach ($f in $files) {
    $path = Join-Path $dir $f
    $lines = [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)
    $newLines = @()
    foreach ($line in $lines) {
        # Pattern: // comment followed by 8+ spaces then code
        if ($line -match '^(\s*// .*?)\s{8,}(\S.*)$') {
            $newLines += $Matches[1]
            $newLines += $Matches[2]
        } else {
            $newLines += $line
        }
    }
    [System.IO.File]::WriteAllLines($path, $newLines, (New-Object System.Text.UTF8Encoding $false))
    Write-Output ("Fixed: " + $f)
}