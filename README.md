# HttpSiraStatus

[![Build and Release](https://github.com/denpadokei/HttpSiraStatus/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/denpadokei/HttpSiraStatus/actions/workflows/build-and-release.yml)

A Beat Saber mod that publishes the current game state (beatmap details, score, note cuts and so on) over HTTP and
WebSocket. Upstream is [opl-/beatsaber-http-status](https://github.com/opl-/beatsaber-http-status); this fork is currently
maintained for Beat Saber 1.40.8 and 1.44.1. The mod shows nothing in game — it is meant to be read by external tools and
stream overlays.

## Features

- `GET /status.json` returns the whole current state as JSON
- Connecting to the WebSocket on `/socket` sends a `hello` event with the full state, then only the events that change
- Publishes `game`, `beatmap`, `performance`, `mod` and `playerSettings` objects
- Publishes beatmap events in both V2 (`2.6.0`) and V3 (`3.0.0`) form
- Publishes events for note spawn, cut, miss and bombs, obstacle enter and exit, score changes, and song start, finish, fail, restart and pause
- Sends from a dedicated thread so gameplay frames are not blocked

## Requirements

| Item | Details |
| --- | --- |
| Beat Saber | 1.40.8 or 1.44.1 |
| Dependencies (1.40.8) | BSIPA 4.3.5 or later / SiraUtil 3.1.12 or later |
| Dependencies (1.44.1) | BSIPA 4.3.6 or later / SiraUtil 3.2.0 or later |

## Installation

1. Set Beat Saber to 1.40.8 or 1.44.1 (a version manager such as BSManager is recommended)
2. Install BSIPA and the dependencies for that game version
3. Download the matching zip from [Releases](https://github.com/denpadokei/HttpSiraStatus/releases) and copy its
   `Plugins` folder over the game folder:

| Beat Saber | Release |
| --- | --- |
| 1.40.8 | `HttpSiraStatus-14.2.0-bs1.40.8.zip` |
| 1.44.1 | `HttpSiraStatus-16.0.0-bs1.44.1.zip` |

## Usage

The mod only publishes data; there is no in-game UI. After starting the game, connect a tool or overlay on the same PC to
one of these endpoints.

| Type | Endpoint | Description |
| --- | --- | --- |
| HTTP | `GET http://localhost:6557/status.json` | Returns the full current status as `application/json` |
| WebSocket | `ws://localhost:6557/socket` | Sends `hello` on connect, then events as they happen |

The port is configurable. Paths other than `/status.json` and `/socket` return `404`.

### Messages

Every WebSocket message has an `event` name, a `time` in UNIX milliseconds, and then the payload for that event.

```json
{
  "event": "songStart",
  "time": 1700000000000,
  "status": {
    "game": {
      "pluginVersion": "16.0.0",
      "gameVersion": "1.44.1",
      "scene": "Song",
      "mode": "SoloStandard"
    }
  }
}
```

- `status` holds the whole state, or only the objects that changed (`game` / `beatmap` / `performance` / `mod` / `playerSettings`)
- Note events add a `noteCut` object
- Beatmap events add a `beatmapEvent` object
- `hello` carries the whole state plus an `other` object, which is currently empty

The main keys of `status`:

| Object | Keys |
| --- | --- |
| `game` | `pluginVersion`, `gameVersion`, `scene` (`Menu` / `Song`), `mode` |
| `beatmap` | `songName`, `songSubName`, `songAuthorName`, `levelAuthorName`, `levelAuthorNamesArray`, `lighterNamesArray`, `songCover`, `songHash`, `levelId`, `songBPM`, `noteJumpSpeed`, `noteJumpStartBeatOffset`, `songTimeOffset`, `start`, `paused`, `length`, `difficulty`, `difficultyEnum`, `characteristic`, `notesCount`, `bombsCount`, `obstaclesCount`, `maxScore`, `maxRank`, `environmentName`, `color` |
| `beatmap.color` | `saberA`, `saberB`, `environment0`, `environment1`, `environmentW`, `environment0Boost`, `environment1Boost`, `environmentWBoost`, `obstacle` |
| `performance` | `rawScore`, `score`, `currentMaxScore`, `relativeScore`, `rank`, `passedNotes`, `hitNotes`, `missedNotes`, `lastNoteScore`, `passedBombs`, `hitBombs`, `combo`, `maxCombo`, `multiplier`, `multiplierProgress`, `batteryEnergy`, `energy`, `softFailed`, `currentSongTime` |
| `mod` | `multiplier`, `obstacles`, `instaFail`, `noFail`, `batteryEnergy`, `batteryLives`, `disappearingArrows`, `noBombs`, `songSpeed`, `songSpeedMultiplier`, `noArrows`, `ghostNotes`, `failOnSaberClash`, `strictAngles`, `fastNotes`, `smallNotes`, `proMode`, `zenMode` |
| `playerSettings` | `staticLights`, `leftHanded`, `playerHeight`, `sfxVolume`, `reduceDebris`, `noHUD`, `advancedHUD`, `autoRestart`, `saberTrailIntensity`, `environmentEffects`, `hideNoteSpawningEffect` |

### Events

`hello`, `menu`, `songStart`, `pause`, `resume`, `restart`, `finished`, `failed`, `softFailed`, `scoreChanged`, `energyChanged`, `noteSpawned`, `noteCut`, `noteFullyCut`, `noteMissed`, `bombCut`, `obstacleEnter`, `obstacleExit`, `beatmapEvent`

Note that `beatmapEvent` is also sent about once per second with an updated `performance.currentSongTime` and no
`beatmapEvent` field, so consumers should check for that field before treating a message as a light event.

See [protocol.md](protocol.md) for the full field-by-field specification.

## Configuration

Edit `UserData/HttpSiraStatus.json`. There is no in-game settings screen.

| Key | Type | Default | Description |
| --- | --- | --- | --- |
| `Port` | int | 6557 | Port the HTTP and WebSocket server listens on |
| `SendBeatmapEvents` | bool | true | Publish `beatmapEvent` messages. Event JSON is built when an event fires, so enabling this does not add load time. |

## Build

`versions.json` is the single source of truth for the supported game versions. It maps each game version to the plugin
version and its BSIPA/SiraUtil dependencies:

| Beat Saber | Plugin version | Release tag |
| --- | --- | --- |
| 1.40.8 | 14.2.0 | `14.2.0` |
| 1.44.1 | 16.0.0 | `16.0.0` |

Use the helper to prepare the selected manifest and build against the matching game installation:

```powershell
git clone https://github.com/denpadokei/HttpSiraStatus.git
cd HttpSiraStatus

.\build.ps1 -GameVersion 1.40.8 -BeatSaberDir "C:\Path\To\1.40.8"
.\build.ps1 -GameVersion 1.44.1 -BeatSaberDir "C:\Path\To\1.44.1"
```

The helper temporarily writes the selected manifest, builds with the matching `Version`, and restores the manifest
afterwards. `scripts\prepare-manifest.ps1` can be used on its own when you need a manifest-only update.

The GitHub Actions workflow `build-and-release.yml` builds both versions on every push and pull request. A release is
created only by pushing a tag that matches a `mod_version` in `versions.json`; the tag for 1.40.8 is `14.2.0` and the
tag for 1.44.1 is `16.0.0`. The release name is `HttpSiraStatus-<mod_version>-bs<game_version>`.

## Tests

The unit tests run without a Beat Saber installation. The test project links only the game-independent sources, so
`BeatSaberDir` does not need to be set.

```powershell
dotnet test .\tests\HttpSiraStatus.Tests\HttpSiraStatus.Tests.csproj -c Release
```

The GitHub Actions workflow `test.yml` runs the tests on pull requests and on pushes to `master`.

## Credits

- Original author: opl ([opl-/beatsaber-http-status](https://github.com/opl-/beatsaber-http-status))
- xyonico: [Beat Saber Discord Presence](https://github.com/xyonico/BeatSaberDiscordPresence), the base of this plugin
- sta: [websocket-sharp](https://github.com/sta/websocket-sharp)
- Maxaxik: testing and investigation

## License

MIT License (Copyright (c) 2018-2019 opl). See [LICENSE](LICENSE) for details.
