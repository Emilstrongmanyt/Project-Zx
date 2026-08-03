$srcRoot = "C:\MMORPG-Project\mmorpg-mobile\Project Zx\Assets\Admurin's Pixel Items\PixelItems\Armory\Singles\Weapon Singles"
$dst = "C:\MMORPG-Project\mmorpg-mobile\Project Zx\Assets\_Project\Resources\Items\Admurin"
$metaTemplate = Get-Content (Join-Path $dst "weapon_bat_iron.png.meta") -Raw

$map = @{
    bat    = "Weapon2"
    spear  = "Weapon5"
    bow    = "Weapon15"
    staff  = "Weapon8"
    katana = "Weapon22"
}

# Iron/Steel already present; copy remaining materials (and re-copy iron/steel if missing).
$materials = @(
    "Iron", "Steel", "Copper", "Silver", "Gold", "Cobalt",
    "Platinum", "Adamantine", "Crimson", "Altair", "Angelic", "Fateful"
)

$count = 0
foreach ($mat in $materials) {
    $suffix = $mat.ToLowerInvariant()
    $folder = Join-Path $srcRoot $mat
    foreach ($key in $map.Keys) {
        $weaponId = $map[$key]
        $srcName = "${mat}_${weaponId}.png"
        $srcPath = Join-Path $folder $srcName
        $dstName = "weapon_${key}_${suffix}.png"
        $dstPath = Join-Path $dst $dstName

        if (-not (Test-Path $srcPath)) {
            Write-Host "MISSING $srcPath"
            continue
        }

        Copy-Item -Force $srcPath $dstPath
        $guid = [guid]::NewGuid().ToString("N")
        $spriteId = [guid]::NewGuid().ToString("N")
        $meta = $metaTemplate -replace "guid: [a-f0-9]+", "guid: $guid"
        $meta = $meta -replace "spriteID: [a-f0-9]+", "spriteID: $spriteId"
        Set-Content -Path ($dstPath + ".meta") -Value $meta -NoNewline
        $count++
        Write-Host "OK $dstName"
    }
}

Write-Host "Copied $count weapon tier files."
