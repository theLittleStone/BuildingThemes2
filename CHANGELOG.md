# Changelog

All notable changes to Building Themes 2 are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/);
versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] — 2026-04-17

Complete modernisation of the original Building Themes mod by boformer.

### Added

- **Harmony 2.x migration** — replaced the old Detour/Redirection system with
  CitiesHarmony 2.x patches; no longer conflicts with mods that patch the same methods
- **Missing asset handling** — three configurable modes (Skip / Fill with vanilla /
  Fall back to vanilla), set globally in Mod Options or overridden per district
- **Empty level behavior** — Vanilla fallback / Cascade / Strict freeze when a theme has
  no buildings for a particular level
- **Blacklist mode** — invert a district theme so anything not explicitly excluded can
  spawn
- **Spawn Diagnostics modal** — shows accepted/rejected building counts and lists missing
  assets for the selected district
- **Workshop Dependencies modal** — lists all workshop assets in a theme by status
  (loaded / missing), with copy-to-clipboard and Steam overlay subscribe buttons
- **DLC / CCP filter** in the Theme Manager — filter buildings by installed expansion or
  Content Creator Pack (all 29 CCPs named)
- **Asset loading status filter** — Show loaded / Show missing / Show DLC-locked /
  Spawnable only checkboxes
- **Mod Compatibility Checker** — warns on startup if known incompatible mods
  (81 Tiles original, Building Simulation Overhaul, Runways and Taxiways) are active;
  detects Skyve and notes disabled assets in diagnostics output
- **Skyve integration** — Spawn Diagnostics identifies which missing assets are disabled
  in the active Skyve playset
- **Unified Harmony ID** — all patches registered under a single consistent ID
  (`com.github.roberto-naharro.BuildingThemes2`)
- **Per-district persistence** — missing-asset mode and empty-level behavior are now
  saved inside the save game and restored on load
- **Verbose debug logging** — all significant lifecycle, spawn, UI, and save/load events
  are logged when "Generate Debug Output" is enabled in Mod Options
- **Steam Workshop publishing** — `publish.sh` script and GitHub Actions workflow for
  automated deployment to the Workshop

### Fixed

- Double `MemoryStream` allocation on save-game deserialisation
- `debugCount` not reset between level loads (throttle fired immediately on second load)
- Null-reference exception on `<District>` elements with no `<themes>` child in save XML
- `OnDisabled` Harmony unpatch not guarded against shutdown-phase exceptions
- All stray `UnityEngine.Debug.LogException` calls unified to `Debugger.LogException`
- Workshop assets renamed by their creators now resolved by Steam package ID prefix

### Changed

- Minimum supported game version: Cities: Skylines 1.18.x
- Save data format bumped to version 2 (fully backwards-compatible with version 1 saves)
- Theme Manager filter bar reorganised: DLC filter on its own row; checkboxes row below it
