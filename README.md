# Building Themes 2

Building Themes 2 is a community-maintained fork of **Building Themes** by
[boformer (Sebastian Schöner)](https://github.com/boformer).
All original work and concept belong to him. Thank you, boformer.

Thanks also to **BloodyPenguin** for prior compatibility fixes.

Harmony 2.x migration maintained by [roberto-naharro](https://github.com/roberto-naharro).

---

Building Themes 2 is a Cities: Skylines 1 mod.
It lets you create building themes and apply them to cities and districts,
restricting which growable buildings can spawn in each area.

**Steam Workshop dependency:** [CitiesHarmony (Harmony 2.2)](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)
must be subscribed and enabled.

---

_Original detour system by Sebastian Schöner, MIT license:_
_<https://github.com/sschoener/cities-skylines-detour> (removed in v2.0.0)_

## Linux Build Setup (Windows game files on SMB share)

This project can be built on Linux by referencing the game assemblies from a Windows machine where Cities: Skylines is installed.

Current setup used in this repository:

- Windows host: DESKTOP-GHGB72P
- SMB share: //192.168.1.2/Cities_Skylines
- Linux mount point: /mnt/cities_skylines

The project file [BuildingThemes/BuildingThemes.csproj](BuildingThemes/BuildingThemes.csproj) is configured to reference managed game DLLs from /mnt/cities_skylines/Cities_Data/Managed.

## Prerequisites

- .NET SDK 8 installed
- Mono/xbuild installed
- cifs-utils installed
- Access to the Windows shared folder with game files

## One-time Linux setup

1. Install required tools:
 sudo apt-get update
 sudo apt-get install -y mono-complete mono-devel cifs-utils

2. Create mount point:
 sudo mkdir -p /mnt/cities_skylines

3. Mount the Windows share (recommended: authenticated account):
 sudo mount -t cifs //192.168.1.2/Cities_Skylines /mnt/cities_skylines -o username=YOUR_WINDOWS_USER,uid=$(id -u),gid=$(id -g),vers=3.0

4. Verify game DLLs are visible:
 ls /mnt/cities_skylines/Cities_Data/Managed/Assembly-CSharp.dll

## Restore dependencies

This project uses packages.config in [BuildingThemes/packages.config](BuildingThemes/packages.config).

If NuGet CLI is not available in apt, use nuget.exe:

1. Download NuGet CLI:
 mkdir -p ~/.local/tools
 wget -O ~/.local/tools/nuget.exe <https://dist.nuget.org/win-x86-commandline/latest/nuget.exe>

2. Restore packages:
 mono ~/.local/tools/nuget.exe restore BuildingThemes.sln

## Build

Run from repository root:

xbuild BuildingThemes.sln

Output DLL:

- BuildingThemes/bin/Debug/BuildingThemes.dll

## Notes about cross-platform adjustments

To make Linux builds work reliably, [BuildingThemes/BuildingThemes.csproj](BuildingThemes/BuildingThemes.csproj) includes:

- C# language version set to 7.2 (compatible with Mono compiler)
- Windows-only PostBuildEvent guarded with OS condition
- Game references pointing to /mnt/cities_skylines/Cities_Data/Managed

## Repository safety

- Do not commit game DLLs from Cities: Skylines.
- Build artifacts are local outputs; keep repository source-only.
