$ErrorActionPreference = 'Stop'

function New-GuidHex {
    [guid]::NewGuid().ToString('N')
}

function Write-FolderMeta([string]$path) {
    $meta = "$path.meta"
    if (Test-Path $meta) { return }
    @"
fileFormatVersion: 2
guid: $(New-GuidHex)
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Set-Content -Path $meta -Encoding UTF8 -NoNewline
}

function Write-TextureMeta([string]$pngPath) {
    $meta = "$pngPath.meta"
    # Always rewrite so import settings are correct for Sprite.Create / Resources.
    $guid = if (Test-Path $meta) {
        $existing = Get-Content $meta -Raw
        if ($existing -match 'guid: ([a-f0-9]{32})') { $Matches[1] } else { New-GuidHex }
    } else { New-GuidHex }

    $spriteId = ([guid]::NewGuid().ToString('N').Substring(0,16)) + '0800000000000000'
@"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 64
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: iOS
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: $spriteId
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Set-Content -Path $meta -Encoding UTF8
}

$res = 'C:\MMORPG-Project\mmorpg-mobile\Project Zx\Assets\_Project\Resources'
Write-FolderMeta (Join-Path $res 'Tiles')
Write-FolderMeta (Join-Path $res 'Tiles\Outside')
Write-FolderMeta (Join-Path $res 'Tiles\Sand')
Write-FolderMeta (Join-Path $res 'Tiles\Dungeon')
Write-FolderMeta (Join-Path $res 'Tiles\Inside')
Write-FolderMeta (Join-Path $res 'Props')
Write-FolderMeta (Join-Path $res 'Props\Forest')
Write-FolderMeta (Join-Path $res 'Props\Cainos')

$pngs = @()
$pngs += Get-ChildItem (Join-Path $res 'Tiles') -Recurse -Filter '*.png'
$pngs += Get-ChildItem (Join-Path $res 'Props\Forest') -Filter '*.png' -ErrorAction SilentlyContinue
$pngs += Get-ChildItem (Join-Path $res 'Props\Cainos') -Filter '*.png' -ErrorAction SilentlyContinue
foreach ($p in $pngs) {
    Write-TextureMeta $p.FullName
}
Write-Host "Wrote metas for $($pngs.Count) textures"

