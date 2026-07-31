# Utility Master — Domain Glossary

## Core Concepts

| Term | Definition |
|------|------------|
| **Target** | A specific location on a map where a grenade (smoke/flash/HE/molotov) should land, or a wallbang/jump spot. Stored as `TargetEntity`. |
| **Lineup** | A specific standing position and aiming instruction used to throw a grenade at a Target. Also used for the firing/positioning spot for wallbang/jump tricks. Stored as `LineupEntity`. |
| **Trick** | A spot on the map that is not a grenade throw, such as a boost or camp position. Boost and Camp are standalone `TrickEntity`. |
| **AimPoint** | A visual reference image attached to a Lineup that shows where to aim. Stored as `AimPointEntity`. |
| **Profile** | A user profile that groups all Targets, Lineups, and Tricks for a user. Default profiles provide built-in spots. Stored as `ProfileEntity`. |
| **Map** | A CS2 map with a minimap image, coordinates, and floor definitions. Defined statically in `HomePage.Maps`. |

## Type Taxonomy

| Category | Entity Type | Storage Model |
|----------|-------------|---------------|
| Smoke grenade | `smoke` | TargetEntity → LineupEntity |
| Flash grenade | `flash` | TargetEntity → LineupEntity |
| HE grenade | `he` | TargetEntity → LineupEntity |
| Molotov/Incendiary | `molotov` | TargetEntity → LineupEntity |
| Wallbang spot | `wallbang` | TargetEntity → LineupEntity |
| Jump/boost spot | `jump` | TargetEntity → LineupEntity |
| Boost position | `boost` | TrickEntity (standalone) |
| Camp position | `camp` | TrickEntity (standalone) |

## State Concepts

| Term | Definition |
|------|------------|
| **IsDefault** | A spot that comes with the built-in config (`default_config.json`). Read-only unless explicitly allowed by user settings. |
| **IsPro** | A lineup marked as professional-level technique. |
| **AsNoTracking** | EF Core query mode used for read-only display data to avoid memory buildup in the change tracker. |
| **ActiveProfile** | The currently selected user profile. Only one profile is active at a time. |

## Key Architecture Decisions

See `docs/adr/` for detailed rationale:
- **ADR-0001**: Service Layer with `IDataService` / `DataService` pattern
