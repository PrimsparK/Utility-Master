<p align="center">
  <sub><a href="README.md">English</a> &middot; <a href="README.zh.md">中文</a></sub>
</p>

# Utility Master

A tool to check/upload utilities target and lineup for CS2. Fully Vibe-Coding.

## About

CS2 Nades & Tricks Tool

### Links

- [GitHub Issues](https://github.com/primspark/UtilityMaster/issues)
- [Bilibili](https://space.bilibili.com/558194430)
- [primspark@outlook.com](mailto:primspark@outlook.com)

### License

MIT License - Open Source

### Credits

- Map icons & minimap images from [MurkyYT/cs2-map-icons](https://github.com/MurkyYT/cs2-map-icons)
- Nade icons from [Liquipedia](https://liquipedia.net/)

### Disclaimers

1. Nade icons, map images, and related assets are property of Valve Corporation, used for non-commercial community purposes. Contact primspark@outlook.com for removal requests.
2. This is a community-made unofficial tool. No affiliation, association, sponsorship, or endorsement by Valve Corporation.
3. CS2 related trademarks are property of Valve Corporation.
4. For educational and exchange purposes only.

## Source Build

This repository only contains original source code and assets, with no compiled artifacts. Build it yourself after downloading:

```powershell
dotnet restore
dotnet build -c Release
```

Run `bin\Release\net8.0-windows\UtilityMaster.exe` after the build, or use:

```powershell
dotnet run --project UtilityMaster.csproj
```

## Release Builds

Packaged application binaries are not committed to the source repository. They are distributed separately in Releases. A release archive only contains the runnable application, its runtime dependencies, and required assets, without source code, build intermediates, or developer tooling files.
Release archives keep only English, Simplified Chinese, and Traditional Chinese language resources. Other language resources are not packaged.

## Future Updates
1. Update the default nades/tricks for most of maps. 
2. More functions?

---

v1.0.0
