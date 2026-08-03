$srcDir = "C:\MMORPG-Project\mmorpg-mobile\Project Zx\Assets\Admurin's Pixel Items\PixelItems\Miscellaneous\Singles"
$dst = "C:\MMORPG-Project\mmorpg-mobile\Project Zx\Assets\_Project\Resources\Items\Admurin"
$metaTemplate = Get-Content (Join-Path $dst "weapon_bat_iron.png.meta") -Raw

$map = @{
    "01_Skull_Human.png"           = "skull_human.png"
    "02_Skull_Horned.png"          = "skull_horned.png"
    "03_Skull_Demon.png"           = "skull_demon.png"
    "04_Skull_Cyclops.png"         = "skull_cyclops.png"
    "05_Skull_Canine.png"          = "skull_canine.png"
    "06_Skull_Aquatic.png"         = "skull_aquatic.png"
    "21_Skull_Titan_Orc.png"       = "skull_titan_orc.png"
    "22_Skull_Orc_Horned.png"      = "skull_orc_horned.png"
    "23_Skull_Orc_Horned_B.png"    = "skull_orc_horned_b.png"
    "24_Skull_Titan_Cyclops.png"   = "skull_titan_cyclops.png"
    "25_Skull_Titan_FourEyed.png"  = "skull_titan_foureyed.png"
    "26_Skull_Titan_Aquatic.png"   = "skull_titan_aquatic.png"
}

$count = 0
foreach ($srcName in $map.Keys) {
    $srcPath = Join-Path $srcDir $srcName
    $dstName = $map[$srcName]
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

Write-Host "Copied $count skull projectile files."
