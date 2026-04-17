# Building Themes 2

Apply building themes to districts — only buildings in the selected themes will grow in a
zone. Subscribe to theme mods from the Workshop, or create and manage your own themes
entirely in-game.

**Building Themes 2** is a community-maintained fork of the original
[Building Themes](https://github.com/boformer/BuildingThemes) by
[boformer (Sebastian Schöner)](https://github.com/boformer), updated to run on modern
Cities: Skylines with Harmony 2.x.

[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3708105182) ·
[Report a bug](https://github.com/roberto-naharro/BuildingThemes2/issues) ·
[GitHub](https://github.com/roberto-naharro/BuildingThemes2)

> **Maintenance notice:** This mod is maintained by a single volunteer in their spare time.
> Bug reports and feature requests are read and appreciated, but fixes may take weeks or
> months depending on availability. If you hit a problem, please
> [open an issue](https://github.com/roberto-naharro/BuildingThemes2/issues) so it is
> tracked — nothing gets forgotten, just worked on when time allows.

---

## Requirements

- **[Harmony (Mod Dependency)](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)** —
  subscribe and enable this first. The mod silently does nothing if Harmony is not installed.

---

## Quick Start

1. Open a **district policy panel** → go to the **Themes** tab.
2. Click **Theme Manager** → select a theme and include buildings (or import a style).
3. Back in the **Themes** tab, check **Enable Theme Management for this district**.
4. Enable one or more themes using the checkboxes.
5. Zone the area — only buildings in the active themes will grow.

If no theme is active for a district, any growable building can spawn (vanilla behavior).

---

## Features

- **Per-district themes** — assign different building sets to each district.
- **Theme Manager** — full UI to browse, filter, include/exclude individual buildings.
- **DLC & CCP filter** — filter the building list by installed DLC or Content Creator Pack.
- **Workshop Dependencies modal** — see exactly which assets are missing, copy their IDs,
  or subscribe to them directly via the Steam overlay.
- **Spawn Diagnostics** — see accepted/rejected building counts, missing-asset lists,
  and a live count of non-theme buildings currently placed in the district.
- **Auto-bulldoze** — gradually demolish growable buildings that don't belong to the
  district's active themes, replacing them with themed buildings over time.
- **Asset Cloning** — duplicate a building and assign a different wealth level to it.
- **Missing asset handling** — three modes for how to respond when a subscribed building
  isn't loaded.
- **Level behavior** — control what happens when a building tries to level up but your
  theme has no building for that level.
- **Blacklist mode** — invert the theme: everything spawns except what you explicitly
  exclude.
- **Spawn weight** — control how often a building appears relative to others in the same
  zone, level, and footprint slot.

---

## Theme Manager

Open from the **Themes** tab in the district policy panel.

### Filters

| Filter | Description |
| --- | --- |
| Zone toggles | Show/hide buildings by zone type |
| Display (Origin) | All / Default (vanilla/DLC) / Custom (Workshop) / Cloned |
| Display (Status) | All / Included / Excluded |
| Level | Filter by building level (1–5) |
| Size | Filter by footprint (width × depth) |
| DLC / CCP | Filter by installed expansion or Content Creator Pack |
| Name | Filter by name or Steam Workshop ID |
| Show loaded / missing / DLC-locked | Toggle visibility of asset groups |
| Spawnable only | Show only fully spawnable buildings |

### Bulk Actions

- **Include All / None** — include or exclude everything in the current filtered view.
- **Include Valid** — include all loaded, in-bounds-dimension buildings in the view.
- **Exclude Missing** — remove all unloaded assets from the current filtered view.
- **Workshop Dependencies** — lists all workshop assets in the selected theme (loaded /
  missing), with copy-to-clipboard and one-click Steam subscribe buttons.

### Per-Building Options

- **Spawn weight** (1–100, default 10) — see [Spawn Weight](#spawn-weight) below.
- **Upgrade building** — forces a specific building when this one levels up.
- **Clone Building** — creates a copy with a different wealth level.
- **Asset name** — the internal prefab name (read-only, click to select and copy). For Workshop assets the Steam ID is shown in the label.
- **Plop** — immediately places one instance of the building anywhere in the city (useful for testing).
- **Bulldoze All** — removes every placed instance of this building from the entire city at once.

### Building List Indicators

Each row in the building list shows a small colored badge on the right edge when attention is needed:

| Badge color | Meaning |
| --- | --- |
| Red | Workshop asset is not loaded (unsubscribed, disabled, or failed to load) |
| Grey | Asset is unavailable — DLC not owned or wrong map environment |

### Spawn Weight

Spawn weight controls how often a building is chosen relative to the other buildings
competing for the same **slot** — same zone type, level, and footprint size.

**How the selection works:**

The game picks a random building from a weighted pool. Each building is entered into
that pool once per unit of weight, so a building at weight 20 has twice as many entries
as one at weight 10 and is picked roughly twice as often.

| Weight | Effect |
| --- | --- |
| 1 | Appears very rarely relative to buildings with higher weight |
| 10 *(default)* | Baseline — all buildings at the default weight spawn with equal frequency |
| 20 | Appears roughly twice as often as a weight-10 building |
| 100 | Maximum — appears ten times as often as the default |

**Important:** weight is only meaningful when two or more buildings share the same slot.
A building that is alone in its slot always spawns regardless of its weight value.

**Example:** you have three 2×2 Level 1 Residential Low buildings in your theme:

| Building | Weight | Share of spawns |
| --- | --- | --- |
| House A | 10 | 25 % |
| House B | 10 | 25 % |
| House C | 20 | 50 % |

To make a building appear rarely, lower its weight toward 1. To effectively remove it
from a theme without deleting it, uncheck **Include** instead of setting weight to 1.

### How Spawning Works

A new zone always spawns a Level 1 building first — include one in your theme. It upgrades
when the zone's requirements are met. The Level 2 building must have the same width and
equal or smaller length than the Level 1.

Example: a 2×3 L1 Low Residential can upgrade to a 2×3, 2×2, or 2×1 L2.

If a building is shorter than the plot, the mod shrinks the plot to the building's actual
footprint (unless props or trees occupy the extra space). This enables tight layouts like
the UK Terraced Housing theme.

---

## District Options

Open via the **District Options** button in the Themes tab.

| Option | Description |
| --- | --- |
| Allow buildings not in any theme | Blacklist mode — anything not explicitly excluded can spawn |
| Auto-bulldoze non-theme buildings | Gradually demolishes growable buildings in the district that are not valid for the active themes. Replacements follow the normal themed spawn rules. |
| Level behavior | What happens when a building levels up but the theme has no building for that level: **Vanilla fallback** (default) or **Strict** (freeze upgrades) |
| Missing asset handling | Per-district override of the global missing-asset mode |
| Spawn Diagnostics | Accepted/rejected counts, missing-asset list, and a live list of non-theme buildings currently placed in the district |

---

## Auto-Bulldoze

Enable **Auto-bulldoze non-theme buildings** in District Options to have the mod
gradually demolish any growable zone building in that district that is not part of the
active themes. Replacements spawn according to the normal themed-spawn rules.

- The scan runs in the background — buildings are removed a batch at a time so performance
  impact is minimal. Expect full turnover within a few seconds of game time.
- Only active in districts where **Theme Management** is enabled and **Blacklist mode** is
  off (blacklist mode has no concept of "non-theme").
- After changing which buildings are included in a theme, the scan resets so newly-invalid
  buildings are caught quickly rather than waiting for the cursor to cycle through the
  whole city.
- Use **Spawn Diagnostics** (District Options) to see which non-theme buildings are
  currently present in the district before and after enabling auto-bulldoze.

---

## Missing Assets

**Missing asset handling** controls what happens when a workshop building in your theme is
not loaded (unsubscribed, disabled in Skyve, or failed to load). This setting applies to
**initial spawning only** — it has no effect on how existing buildings level up.

Set the default in **Mod Options → Building Themes**, or override per district in
**District Options**.

| Mode | Behaviour |
| --- | --- |
| **Skip** | The missing building is quietly dropped. Theme still applies using only loaded assets. Areas may be sparse if many assets are missing. |
| **Fill with vanilla** *(default)* | For each missing asset, vanilla buildings of the same zone type and size supplement the pool. Your loaded theme buildings still appear; vanilla fills the gaps. |
| **Fall back to vanilla** | If any building is missing in a size/level slot, that entire slot falls back to vanilla buildings only. No sparse areas, but theme coverage is reduced. |

---

## Level Behavior

**Level behavior** controls what happens when a building tries to level up but your theme
has **no buildings at all** for the target level. This setting applies to **upgrades only**
— it does not affect which buildings spawn on new zones.

Set the default in **Mod Options → Building Themes**, or override per district in
**District Options**.

| Mode | Behaviour |
| --- | --- |
| **Vanilla fallback** *(default)* | The game picks a vanilla building of the upgraded level. The building changes appearance on upgrade. |
| **Strict** | Upgrades are blocked entirely. The building stays at its current level indefinitely. |

> **Note:** These two settings are independent. "Fill with vanilla" (missing asset mode)
> fills gaps at spawn time; it does not affect upgrades. "Vanilla fallback" (level behavior)
> applies at upgrade time; it does not affect which buildings spawn on new zones.
>
> To control which specific building a level-up produces, use the **Upgrade building** field
> in the Theme Manager options panel for that building. That explicit mapping takes priority
> over the Level behavior setting.

---

## Asset Cloning

Clone a building and assign a different wealth level — useful when you don't have enough
assets for every level, or to add variety.

To clone: select a building in the Theme Manager → **Clone Building** → enter a name and
choose the wealth level. The clone appears in the building list.

> **Important:** Clones are generated at level-load time. Return to the main menu and
> reload after creating clones.
>
> **Important:** If you delete a theme that contains clones, or disable the mod, those
> buildings disappear on next load. The save file itself is not broken.

Cloning can be disabled in mod options.

---

## Compatibility

**Compatible with Cities: Skylines 1.21.1-f9** (and later 1.x releases).

**Not compatible with:**

- **81 Tiles (original)** — use [81 Tiles 2](https://steamcommunity.com/sharedfiles/filedetails/?id=2862121823) instead (fully supported)
- **Building Themes (original)** — unsubscribe the original before enabling this fork
- **Building Simulation Overhaul**
- **Runways and Taxiways** — use [Airport Roads](http://steamcommunity.com/sharedfiles/filedetails/?id=465127441) instead

Save games made with the original Building Themes are **fully compatible** — district
assignments load normally.

For broader mod-compatibility diagnostics (conflicts, disabled assets, missing
dependencies) use [Skyve](https://steamcommunity.com/sharedfiles/filedetails/?id=2979559117).
If Skyve is installed, Spawn Diagnostics will note which missing assets may be disabled in
your playset and give you IDs to re-enable them.

---

## Troubleshooting — Buildings Not Spawning

Work through this list in order:

**1. Theme is empty or has no Level 1 buildings**
Open Theme Manager → select your theme → set the Status filter to **Included**. If the
list is empty, no buildings are in your theme. Every theme must include at least one
Level 1 building for the target zone type.

**2. Workshop assets are not loaded**
Missing (red) buildings in the Theme Manager indicate assets that are not active in your
playset. Use **Workshop Dependencies** to copy their IDs for resubscription or click
**Subscribe Missing** to subscribe from within the game.

**3. Wrong zone type**
The building's zone type must match the zone you placed. A commercial building will never
spawn in a residential zone.

**4. Theme not enabled for the district**
Open the district policy panel → Themes tab → confirm **Enable Theme Management** is
checked and your theme has a checkmark.

**5. Run Spawn Diagnostics**
District policy panel → Themes tab → **District Options** → **Spawn Diagnostics**. Shows
accepted/rejected counts and lists missing assets by name.

---

## Theme Mods

These Workshop mods provide ready-made themes:

| Theme | Links |
| --- | --- |
| **UK Terraced Housing** | [Buildings](https://steamcommunity.com/sharedfiles/filedetails/?id=452704398) · [Theme mod](https://steamcommunity.com/sharedfiles/filedetails/?id=470539837) |
| **Neo-eclectic Homes** | [Buildings](https://steamcommunity.com/sharedfiles/filedetails/?id=464133310) · [Theme mod](https://steamcommunity.com/sharedfiles/filedetails/?id=471015559) |
| **American Trailer Homes** | [Buildings](https://steamcommunity.com/sharedfiles/filedetails/?id=437051479) · [Theme mod](https://steamcommunity.com/sharedfiles/filedetails/?id=471183698) |

---

## How Data Is Saved

- **Custom themes and options** are stored in `BuildingThemes.xml` in your Cities: Skylines
  install folder.
- **District theme assignments** are stored inside each save game
  (key: `BuildingThemes-SaveData`).

You do not need to edit these files — everything is configurable in-game.

---

## Bug Reports

Please open an issue on GitHub with:

- Your game version
- A list of other active mods
- The relevant section of `output_log.txt` (search for `Building Themes`)
- Output from **Spawn Diagnostics** if buildings are not spawning

[► Open a bug report on GitHub](https://github.com/roberto-naharro/BuildingThemes2/issues)

---

## Credits

**boformer (Sebastian Schöner)** — original mod concept, architecture, and all game logic.
Building Themes 2 would not exist without his work.

**BloodyPenguin** — prior compatibility fixes and contributions to the original mod.

**roberto-naharro** — Harmony 2.x migration, new features, and community maintenance of
this fork.

---

## Development

### Linux Build Setup (Windows game files on SMB share)

This project is developed on Linux, referencing the game assemblies from a Windows machine
where Cities: Skylines is installed via an SMB share.

Example setup (replace with your own values):

- Windows host: `YOUR-WINDOWS-PC`
- SMB share: `//192.168.x.x/Cities_Skylines`
- Linux mount point: `/mnt/cities_skylines`

The project file references managed DLLs from `/mnt/cities_skylines/Cities_Data/Managed`.

### Prerequisites

- Mono / xbuild
- `cifs-utils` (for SMB mounting)

### One-time setup

```bash
sudo apt-get install -y mono-complete mono-devel cifs-utils
sudo mkdir -p /mnt/cities_skylines

# Mount the Windows share
sudo mount -t cifs //192.168.x.x/Cities_Skylines /mnt/cities_skylines \
  -o username=YOUR_WINDOWS_USER,uid=$(id -u),gid=$(id -g),vers=3.0

# Verify
ls /mnt/cities_skylines/Cities_Data/Managed/Assembly-CSharp.dll
```

Copy `.env.example` to `.env` and fill in your values.

### Restore packages

```bash
mono ~/.local/tools/nuget.exe restore BuildingThemes.sln
```

### Build

```bash
xbuild BuildingThemes.sln                      # Debug (default)
xbuild BuildingThemes.sln /p:Configuration=Release
```

Output: `BuildingThemes/bin/Debug/BuildingThemes.dll`

### Deploy to game

```bash
./mount-cities.sh        # mount Windows SMB shares (first time / after reboot)
./deploy.sh              # Debug build → stage to dist/ → copy to game Mods folder
./deploy.sh --release    # Release build
```

`deploy.sh` also tails the last 60 lines of `output_log.txt` after deploying.

### Releasing a new version

Releases are managed by [Release Please](https://github.com/googleapis/release-please).
Write commits using [Conventional Commits](https://www.conventionalcommits.org/):

| Prefix | Semver bump | Example |
| --- | --- | --- |
| `fix:` | patch (2.0.0 → 2.0.1) | `fix: DLC filter overlap with Display row` |
| `feat:` | minor (2.0.0 → 2.1.0) | `feat: add Subscribe Missing button` |
| `feat!:` or `BREAKING CHANGE:` | major | `feat!: change save format` |
| `chore:`, `docs:`, `refactor:` | no bump | housekeeping |

**Release flow:**

1. Commit your changes using the prefixes above — Release Please opens a Release PR
   automatically and keeps it up to date.
2. Run `./deploy.sh --release` locally and commit `dist/` to `master`.
3. Merge the Release PR → tag `vX.Y.Z` is created → Workshop deploy fires automatically.

### Publish to Steam Workshop

```bash
./deploy.sh --release    # build & stage
./publish.sh             # upload dist/ to Steam Workshop via SteamCMD
./publish.sh "Fix: DLC filter overlap"   # with a change note
```

`publish.sh` requires `steamcmd` to be installed and reads `STEAM_USERNAME` from `.env`.
SteamCMD will prompt for your password and Steam Guard code interactively — no secrets
are stored.

For automated publishing via GitHub Actions, see
[`.github/workflows/workshop-deploy.yml`](.github/workflows/workshop-deploy.yml).

### Notes on cross-platform compatibility

- C# language version set to 7.2 (Mono compiler compatible).
- Windows-only `PostBuildEvent` is guarded with an OS condition in the `.csproj`.
- Game DLL references point to `/mnt/cities_skylines/Cities_Data/Managed` — do not commit
  these files (copyright).
