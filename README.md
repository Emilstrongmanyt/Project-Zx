# Project Zx

2D top-down endless horde survivor for iOS (landscape). Tap or drag to move, auto-melee combat, zombie and demon waves, XP/level-ups, and run talents.

## Project

| Item | Value |
|------|-------|
| Unity | 6000.5.2f1 |
| Bundle ID | `com.solodreams.projectzx` |
| Orientation | Landscape only |
| Repo | https://github.com/Emilstrongmanyt/Project-Zx |

## Local path

`C:\MMORPG-Project\mmorpg-mobile\Project Zx`

## Folder layout

Game content lives under `Assets/_Project/`:

- `Art/` — PNG sprites (100 PPU recommended)
- `Prefabs/` — Player, enemies, obstacles, pickups, VFX
- `Scenes/` — Boot, Game
- `Scripts/` — Core, Input, Player, Combat, Enemies, Waves, Progression, World, UI
- `Settings/` — ScriptableObject tuning data

## TestFlight CI

Pushes to `main` run `.github/workflows/ios-testflight.yml`:

1. Unity iOS build on Linux (game-ci)
2. Sign + archive on macOS
3. Upload to TestFlight via App Store Connect API

### GitHub secrets (repository settings)

Reuse from Dream Gate where the bundle ID matches:

| Secret | Notes |
|--------|-------|
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity password |
| `UNITY_SERIAL` | Personal license serial (F-prefix) |
| `APPLE_TEAM_ID` | Apple Developer team ID |
| `IOS_P12_BASE64` | Distribution certificate (.p12, base64) |
| `IOS_P12_PASSWORD` | Certificate password |
| `APPLE_CONNECT_KEY_ID` | App Store Connect API key ID |
| `APPLE_CONNECT_ISSUER_ID` | App Store Connect issuer ID |
| `APPLE_CONNECT_KEY` | API key .p8 contents (base64) |

**Project Zx specific:**

| Secret | Notes |
|--------|-------|
| `IOS_MOBILEPROVISION_BASE64` | App Store provisioning profile for **`com.solodreams.projectzx`** (not Dream Gate) |

### Apple setup checklist

1. Register App ID `com.solodreams.projectzx` in Apple Developer
2. Create App Store provisioning profile for that bundle ID
3. Create app in App Store Connect
4. Base64-encode the `.mobileprovision` → `IOS_MOBILEPROVISION_BASE64` on this repo
5. Replace `.github/ios/AppIcon-1024.png` with final 1024×1024 icon

## Development status

Phase 0 complete: Unity project, landscape iOS settings, repo CI scaffold, folder structure.

Next: movement prototype (tap/drag + center-locked camera + world scroll).