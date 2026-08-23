$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$cainosRoot = 'C:\MMORPG-Project\mmorpg-mobile\Pixel Art Top Down - Basic\Assets\Cainos\Pixel Art Top Down - Basic\Texture'
$res = 'C:\MMORPG-Project\mmorpg-mobile\Project Zx\Assets\_Project\Resources'
$tiles = Join-Path $res 'Tiles'
$props = Join-Path $res 'Props\Cainos'

function Ensure-Dir($p) { if (-not (Test-Path $p)) { New-Item -ItemType Directory -Force -Path $p | Out-Null } }

function Get-SpriteRects([string]$metaPath) {
    $text = Get-Content $metaPath -Raw
    $rects = @()
    # Unity meta sprite blocks: name then rect x/y/width/height
    $matches = [regex]::Matches($text, 'name: (TX [^\r\n]+)\r?\n\s+rect:\r?\n\s+serializedVersion: 2\r?\n\s+x: (-?\d+)\r?\n\s+y: (-?\d+)\r?\n\s+width: (\d+)\r?\n\s+height: (\d+)')
    foreach ($m in $matches) {
        $rects += [pscustomobject]@{
            Name = $m.Groups[1].Value.Trim()
            X = [int]$m.Groups[2].Value
            Y = [int]$m.Groups[3].Value
            W = [int]$m.Groups[4].Value
            H = [int]$m.Groups[5].Value
        }
    }
    return $rects
}

function Save-Sprite([System.Drawing.Bitmap]$sheet, $rect, [string]$outPath, [scriptblock]$recolor = $null) {
    # Unity rect y is from bottom; System.Drawing y is from top.
    $unityYFromTop = $sheet.Height - $rect.Y - $rect.H
    $r = New-Object System.Drawing.Rectangle $rect.X, $unityYFromTop, $rect.W, $rect.H
    if ($r.X -lt 0 -or $r.Y -lt 0 -or $r.Right -gt $sheet.Width -or $r.Bottom -gt $sheet.Height) {
        Write-Host "SKIP OOB $($rect.Name) $r on $($sheet.Width)x$($sheet.Height)"
        return $false
    }
    $tile = $sheet.Clone($r, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        if ($recolor) { & $recolor $tile }
        # Skip nearly empty
        $opaque = 0
        for ($y = 0; $y -lt $tile.Height; $y += 2) {
            for ($x = 0; $x -lt $tile.Width; $x += 2) {
                if ($tile.GetPixel($x, $y).A -gt 16) { $opaque++ }
            }
        }
        if ($opaque -lt 4) { return $false }
        Ensure-Dir (Split-Path $outPath)
        $tile.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
        return $true
    } finally {
        $tile.Dispose()
    }
}

function Sanitize([string]$name) {
    ($name -replace '[^A-Za-z0-9]+', '_').Trim('_')
}

function Tint-Sand([System.Drawing.Bitmap]$bmp) {
    for ($y = 0; $y -lt $bmp.Height; $y++) {
        for ($x = 0; $x -lt $bmp.Width; $x++) {
            $p = $bmp.GetPixel($x, $y)
            if ($p.A -lt 8) { continue }
            # Warm sand: lift red/green, drop blue, slight brighten
            $r = [Math]::Min(255, [int]($p.R * 1.15 + 40))
            $g = [Math]::Min(255, [int]($p.G * 1.05 + 28))
            $b = [Math]::Min(255, [int]($p.B * 0.55 + 10))
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($p.A, $r, $g, $b))
        }
    }
}

function Tint-Wood([System.Drawing.Bitmap]$bmp) {
    for ($y = 0; $y -lt $bmp.Height; $y++) {
        for ($x = 0; $x -lt $bmp.Width; $x++) {
            $p = $bmp.GetPixel($x, $y)
            if ($p.A -lt 8) { continue }
            $r = [Math]::Min(255, [int]($p.R * 1.05 + 35))
            $g = [Math]::Min(255, [int]($p.G * 0.85 + 18))
            $b = [Math]::Min(255, [int]($p.B * 0.55 + 8))
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($p.A, $r, $g, $b))
        }
    }
}

# --- Clear previous mixed floors/props so style is Cainos-only ---
foreach ($biome in @('Outside', 'Sand', 'Dungeon', 'Inside')) {
    $dir = Join-Path $tiles $biome
    Ensure-Dir $dir
    Get-ChildItem $dir -Filter '*.png' -ErrorAction SilentlyContinue | Remove-Item -Force
    Get-ChildItem $dir -Filter '*.png.meta' -ErrorAction SilentlyContinue | Remove-Item -Force
}
Ensure-Dir $props
Get-ChildItem $props -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse
Ensure-Dir $props
# Keep Forest folder but empty Tiny RPG so ArtLibrary falls through to Cainos
$forest = Join-Path $res 'Props\Forest'
if (Test-Path $forest) {
    Get-ChildItem $forest -Filter '*.png' | Remove-Item -Force
    Get-ChildItem $forest -Filter '*.png.meta' | Remove-Item -Force
}

# --- Grass floors (Outside): prefer detailed / flower variants; keep a few solids ---
function Test-GrassUseful([System.Drawing.Bitmap]$bmp) {
    $opaque = 0
    $colors = @{}
    for ($y = 0; $y -lt $bmp.Height; $y++) {
        for ($x = 0; $x -lt $bmp.Width; $x++) {
            $c = $bmp.GetPixel($x, $y)
            if ($c.A -lt 128) { continue }
            $opaque++
            $key = "{0},{1},{2}" -f $c.R, $c.G, $c.B
            if (-not $colors.ContainsKey($key)) { $colors[$key] = 0 }
            $colors[$key]++
        }
    }
    if ($opaque -lt 40) { return $false }
    # Detailed tiles have several colors; solids have 1–2
    return ($colors.Count -ge 3)
}

$grassSheet = [System.Drawing.Bitmap]::FromFile((Join-Path $cainosRoot 'TX Tileset Grass.png'))
$grassRects = Get-SpriteRects ((Join-Path $cainosRoot 'TX Tileset Grass.png.meta'))
$grassOut = Join-Path $tiles 'Outside'
$n = 0
$solidKept = 0
# Flowers / named detail first, then remaining grass fills
$orderedGrass = @($grassRects | Where-Object {
    $_.Name -match 'TX Tileset Grass' -and $_.W -eq 32 -and $_.H -eq 32
} | Sort-Object @{ Expression = { if ($_.Name -match 'Flower') { 0 } else { 1 } } }, Name)
foreach ($rect in $orderedGrass) {
    if ($n -ge 64) { break }
    $unityYFromTop = $grassSheet.Height - $rect.Y - $rect.H
    $r = New-Object System.Drawing.Rectangle $rect.X, $unityYFromTop, $rect.W, $rect.H
    if ($r.X -lt 0 -or $r.Y -lt 0 -or $r.Right -gt $grassSheet.Width -or $r.Bottom -gt $grassSheet.Height) { continue }
    $tile = $grassSheet.Clone($r, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $useful = Test-GrassUseful $tile
        if (-not $useful) {
            if ($solidKept -ge 8) { continue }
            $solidKept++
        }
        Ensure-Dir $grassOut
        $path = Join-Path $grassOut ("cainos_grass_{0:D2}.png" -f $n)
        $tile.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $n++
    } finally {
        $tile.Dispose()
    }
}
Write-Host "Outside grass tiles: $n (solid fills kept: $solidKept)"
$grassSheet.Dispose()

# --- Stone floors (Dungeon + base for Sand/Inside tints) ---
$stoneSheet = [System.Drawing.Bitmap]::FromFile((Join-Path $cainosRoot 'TX Tileset Stone Ground.png'))
$stoneRects = Get-SpriteRects ((Join-Path $cainosRoot 'TX Tileset Stone Ground.png.meta'))
$stoneOut = Join-Path $tiles 'Dungeon'
$n = 0
foreach ($rect in $stoneRects) {
    if ($rect.Name -notmatch 'TX Tileset Stone') { continue }
    if ($rect.W -ne 32 -or $rect.H -ne 32) { continue }
    $path = Join-Path $stoneOut ("cainos_stone_{0:D2}.png" -f $n)
    if (Save-Sprite $stoneSheet $rect $path) { $n++ }
}
Write-Host "Dungeon stone tiles: $n"

# Sand = stone with sand tint (same style family, Unlimited biome)
$sandOut = Join-Path $tiles 'Sand'
$n = 0
foreach ($rect in $stoneRects) {
    if ($rect.Name -notmatch 'TX Tileset Stone') { continue }
    if ($rect.W -ne 32 -or $rect.H -ne 32) { continue }
    $path = Join-Path $sandOut ("cainos_sand_{0:D2}.png" -f $n)
    if (Save-Sprite $stoneSheet $rect $path { param($b) Tint-Sand $b }) { $n++ }
}
Write-Host "Sand (tinted stone) tiles: $n"

# Inside = stone with warm wood tint (same Cainos family)
$insideOut = Join-Path $tiles 'Inside'
$n = 0
foreach ($rect in $stoneRects) {
    if ($rect.Name -notmatch 'TX Tileset Stone') { continue }
    if ($rect.W -ne 32 -or $rect.H -ne 32) { continue }
    $path = Join-Path $insideOut ("cainos_wood_{0:D2}.png" -f $n)
    if (Save-Sprite $stoneSheet $rect $path { param($b) Tint-Wood $b }) { $n++ }
}
Write-Host "Inside (tinted stone) tiles: $n"
$stoneSheet.Dispose()

# --- Plants / trees / bushes ---
# Trees are split Lower (trunk) + Upper (canopy) in the sheet — composite into full trees.
function Save-CompositeTree([System.Drawing.Bitmap]$sheet, $lowerRect, $upperRect, [string]$outPath) {
    $uyL = $sheet.Height - $lowerRect.Y - $lowerRect.H
    $uyU = $sheet.Height - $upperRect.Y - $upperRect.H
    $lowerBmp = $sheet.Clone((New-Object System.Drawing.Rectangle $lowerRect.X, $uyL, $lowerRect.W, $lowerRect.H), [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $upperBmp = $sheet.Clone((New-Object System.Drawing.Rectangle $upperRect.X, $uyU, $upperRect.W, $upperRect.H), [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $w = [Math]::Max($lowerBmp.Width, $upperBmp.Width)
        $h = $lowerBmp.Height + $upperBmp.Height
        $canvas = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $g.Clear([System.Drawing.Color]::Transparent)
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
            # Upper (canopy) at top of canvas; Lower (trunk) directly below
            $g.DrawImage($upperBmp, [int](($w - $upperBmp.Width) / 2), 0)
            $g.DrawImage($lowerBmp, [int](($w - $lowerBmp.Width) / 2), $upperBmp.Height)
        } finally { $g.Dispose() }
        Ensure-Dir (Split-Path $outPath)
        $canvas.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $canvas.Dispose()
        return $true
    } finally {
        $lowerBmp.Dispose()
        $upperBmp.Dispose()
    }
}

$plantSheet = [System.Drawing.Bitmap]::FromFile((Join-Path $cainosRoot 'TX Plant.png'))
$plantRects = Get-SpriteRects ((Join-Path $cainosRoot 'TX Plant.png.meta'))
$tn = 0; $bn = 0; $gn = 0; $rnPlant = 0

$treeUppers = @($plantRects | Where-Object { $_.Name -match 'TX Tree .+ Upper$' })
foreach ($up in $treeUppers) {
    $base = ($up.Name -replace '\s+Upper$', '').Trim()
    $lo = $plantRects | Where-Object { $_.Name -eq "$base Lower" } | Select-Object -First 1
    $safe = Sanitize $base
    $path = Join-Path $props ("tree_{0:D2}_{1}.png" -f $tn, $safe)
    if ($lo) {
        if (Save-CompositeTree $plantSheet $lo $up $path) { $tn++ }
    } else {
        if (Save-Sprite $plantSheet $up $path) { $tn++ }
    }
}

foreach ($rect in $plantRects) {
    $safe = Sanitize $rect.Name
    if ($rect.Name -match 'Bush') {
        $path = Join-Path $props ("bush_{0:D2}_{1}.png" -f $bn, $safe)
        if (Save-Sprite $plantSheet $rect $path) { $bn++ }
    } elseif ($rect.Name -match 'Grass') {
        $path = Join-Path $props ("grassdet_{0:D2}_{1}.png" -f $gn, $safe)
        if (Save-Sprite $plantSheet $rect $path) { $gn++ }
    } elseif ($rect.Name -match 'Rock' -and $rect.W -ge 16) {
        $path = Join-Path $props ("rock_{0:D2}_{1}.png" -f $rnPlant, $safe)
        if (Save-Sprite $plantSheet $rect $path) { $rnPlant++ }
    }
}
Write-Host "Props trees=$tn bushes=$bn grassDet=$gn plantRocks=$rnPlant"
$plantSheet.Dispose()

# --- Props (rocks, crates, pots, pillars, etc.) ---
$propSheet = [System.Drawing.Bitmap]::FromFile((Join-Path $cainosRoot 'TX Props.png'))
$propRects = Get-SpriteRects ((Join-Path $cainosRoot 'TX Props.png.meta'))
$rn = $rnPlant; $on = 0
foreach ($rect in $propRects) {
    $safe = Sanitize $rect.Name
    if ($rect.Name -match 'Gravestone|Stone|Coffin|Rune|Pillar|Rock') {
        $path = Join-Path $props ("rock_{0:D2}_{1}.png" -f $rn, $safe)
        if (Save-Sprite $propSheet $rect $path) { $rn++ }
    } else {
        $path = Join-Path $props ("prop_{0:D2}_{1}.png" -f $on, $safe)
        if (Save-Sprite $propSheet $rect $path) { $on++ }
    }
}
Write-Host "Props rocks=$rn other=$on"
$propSheet.Dispose()

# Credits
@"
Survival map art — uniform Cainos pixel style (all biomes)

Source (textures only; not the full Unity package / project settings):
  Cainos — Pixel Art Top Down - Basic
  Sibling project: ..\Pixel Art Top Down - Basic\
  https://assetstore.unity.com/packages/2d/environments/pixel-art-top-down-basic-187605

Floors:
  Outside  = TX Tileset Grass
  Dungeon/Crypt = TX Tileset Stone Ground
  Unlimited (sand) = Stone Ground with sand tint (same style family)
  Inside = Stone Ground with warm wood tint (same style family)

Props:
  Trees = TX Plant Lower+Upper composited (full trunk+canopy)
  Bushes/rocks = TX Plant
  Crates/pots/pillars/gravestones = TX Props

Chromisu handpainted pack remains in Assets/Handpainted_Grass_and_Ground_Textures
but is NOT used for survival floors (style uniformity across all survival maps).
"@ | Set-Content (Join-Path $tiles 'CREDITS.txt') -Encoding UTF8

Write-Host "`nDone."
Get-ChildItem (Join-Path $tiles 'Outside') -Filter '*.png' | Measure-Object | ForEach-Object { "Outside $($_.Count)" }
Get-ChildItem (Join-Path $tiles 'Sand') -Filter '*.png' | Measure-Object | ForEach-Object { "Sand $($_.Count)" }
Get-ChildItem (Join-Path $tiles 'Dungeon') -Filter '*.png' | Measure-Object | ForEach-Object { "Dungeon $($_.Count)" }
Get-ChildItem (Join-Path $tiles 'Inside') -Filter '*.png' | Measure-Object | ForEach-Object { "Inside $($_.Count)" }
Get-ChildItem $props -Filter '*.png' | Measure-Object | ForEach-Object { "Cainos props $($_.Count)" }
