# Beat Saber HTTP Status protocol

This document describes the protocol implemented by HttpSiraStatus, the fork of
[opl-/beatsaber-http-status](https://github.com/opl-/beatsaber-http-status) maintained for Beat Saber 1.41.1.

Everything below was checked against the code in this repository. If a consumer relies on the upstream document, note the
fork-specific fields marked **fork**.

## Index

1. [Connecting](#connecting)
1. [Endpoints](#endpoints)
1. [Standard objects](#standard-objects)
1. [Events](#events)
1. [Configuration](#configuration)

## Connecting

The server listens on port `6557` by default (**B**eat **S**aber **ST**atus). Change it with `Port` in
`UserData/HttpSiraStatus.json`. Secure connections are not supported.

## Endpoints

### `GET /status.json`

Returns the [Status object](#status-object) as `application/json` (UTF-8, status code `200`). The response has no
`event`, `time` or `other` wrapper.

### `/socket`

Connect here for the WebSocket. On connect the server sends the [`hello` event](#hello-event), then sends
[events](#events) as they happen. Any other HTTP path returns `404`.

## Standard objects

### Event object

Every WebSocket message has this shape.

```js
{
	"event": String, // See [Events](#events)
	"time": Integer, // UNIX timestamp in milliseconds of the moment the message was sent
	"status": Object, // See [Status object](#status-object). May be partial or absent, depending on the event type.
	"beatmapEvent": Object, // See [Beatmap event object](#beatmap-event-object). Only on beatmap event messages.
	"noteCut": Object, // See [Note cut object](#note-cut-object). Only on note-related messages.
	"other": Object, // Additional data. Currently always an empty object, and only sent with `hello`.
}
```

`time` is added by the WebSocket layer, so it is not present in `/status.json`.

### Status object

```js
{
	"game": {
		"pluginVersion": String, // Currently running version of the plugin
		"gameVersion": String, // Version of the game the current plugin version targets
		"scene": "Menu" | "Song", // Current activity. This build does not report a spectator scene.
		"mode": String, // Game mode header followed by the characteristic serialized name, e.g. "SoloStandard", "PartyNoArrows", "MultiplayerStandard". The header is one of "Solo", "Party", "Multiplayer" or "Unknown".
	},
	"beatmap": null | {
		"songName": String,
		"songSubName": String,
		"songAuthorName": String,
		"levelAuthorName": String, // All mappers joined with ","
		"levelAuthorNamesArray": String[], // **fork** Mapper names as an array
		"lighterNamesArray": String[], // **fork** Lighter names as an array
		"songCover": null | String, // Base64 encoded PNG of the song cover
		"songHash": null | String, // SHA-1 hash of a custom level. null for OST and WIP levels.
		"levelId": String, // Raw levelId
		"songBPM": Number,
		"noteJumpSpeed": Number,
		"noteJumpStartBeatOffset": Number,
		"songTimeOffset": Integer, // Milliseconds into the audio where the beatmap starts, adjusted for the song speed multiplier
		"start": null | Integer, // UNIX timestamp in milliseconds of when the map started. Changes on resume and is shifted by the practice start time. null before the map starts.
		"paused": null | Integer, // UNIX timestamp in milliseconds of the pause, null while playing
		"length": Integer, // Map length in milliseconds, adjusted for the song speed multiplier
		"difficulty": String, // Translated difficulty name
		"difficultyEnum": "Easy" | "Normal" | "Hard" | "Expert" | "ExpertPlus",
		"characteristic": String, // Characteristic serialized name, e.g. "Standard", "NoArrows", "OneSaber", "360Degree", "90Degree", "Lightshow", "Lawless"
		"notesCount": Integer, // Cube count, even with No Arrows or a modded map
		"bombsCount": Integer, // Bomb count, even with No Bombs
		"obstaclesCount": Integer, // Obstacle count, even with No Obstacles
		"maxScore": Integer, // Max score achievable with the current modifier multiplier
		"maxRank": "SSS" | "SS" | "S" | "A" | "B" | "C" | "D" | "E", // Max rank achievable with the current modifiers
		"environmentName": String,
		"color": { // Every entry is [red, green, blue] in [0..255], or null when the environment does not provide it
			"saberA": [Integer, Integer, Integer], // Left saber and its notes
			"saberB": [Integer, Integer, Integer], // Right saber and its notes
			"environment0": [Integer, Integer, Integer],
			"environment1": [Integer, Integer, Integer],
			"environmentW": null | [Integer, Integer, Integer], // **fork** Environment white color
			"environment0Boost": null | [Integer, Integer, Integer], // null when the scheme has no boost colors; use environment0 instead
			"environment1Boost": null | [Integer, Integer, Integer], // null when the scheme has no boost colors; use environment1 instead
			"environmentWBoost": null | [Integer, Integer, Integer], // **fork** Boost white color
			"obstacle": [Integer, Integer, Integer],
		},
	},
	"performance": null | {
		"rawScore": Integer, // Score without the modifier multiplier
		"score": Integer, // Score with the modifier multiplier
		"currentMaxScore": Integer, // Max score with the modifier multiplier achievable at the current note count
		"relativeScore": Number, // Accuracy in [0..1]
		"rank": "SSS" | "SS" | "S" | "A" | "B" | "C" | "D" | "E",
		"passedNotes": Integer, // Hit or missed cubes
		"hitNotes": Integer,
		"missedNotes": Integer,
		"lastNoteScore": Integer, // **fork** Score of the most recent fully cut note, updated right before `noteFullyCut`
		"passedBombs": Integer, // Hit or missed bombs
		"hitBombs": Integer,
		"combo": Integer,
		"maxCombo": Integer,
		"multiplier": Integer, // Current combo multiplier, one of 1, 2, 4, 8
		"multiplierProgress": Number, // Progress towards the next multiplier in [0..1)
		"batteryEnergy": null | Integer, // Remaining battery lives. null unless Battery Energy or Insta Fail is enabled.
		"energy": Number, // **fork** Current energy in [0..1]
		"softFailed": Boolean, // true after the energy reached 0 with No Fail enabled
		"currentSongTime": Integer, // Seconds since the map started. Refreshed about once per second.
	},
	"mod": {
		"multiplier": Number, // Score multiplier from the gameplay modifiers
		"obstacles": false | "FullHeightOnly" | "All", // No Walls. false when obstacles are disabled.
		"instaFail": Boolean, // 1 Life
		"noFail": Boolean,
		"batteryEnergy": Boolean, // 4 Lives
		"batteryLives": null | Integer, // 4 with Battery Energy, 1 with Insta Fail, null with neither
		"disappearingArrows": Boolean,
		"noBombs": Boolean,
		"songSpeed": String, // "Normal", "Slower", "Faster" or "SuperFast"
		"songSpeedMultiplier": Number, // May be overridden by practice settings
		"noArrows": Boolean,
		"ghostNotes": Boolean,
		"failOnSaberClash": Boolean, // Hidden modifier
		"strictAngles": Boolean,
		"fastNotes": Boolean, // Hidden modifier
		"smallNotes": Boolean,
		"proMode": Boolean,
		"zenMode": Boolean,
	},
	"playerSettings": {
		"staticLights": Boolean, // true when environmentEffects is not "AllEffects"
		"leftHanded": Boolean,
		"playerHeight": Number,
		"sfxVolume": Number, // [0..1]
		"reduceDebris": Boolean,
		"noHUD": Boolean,
		"advancedHUD": Boolean,
		"autoRestart": Boolean,
		"saberTrailIntensity": Number, // [0..1]
		"environmentEffects": "AllEffects" | "StrobeFilter" | "NoEffects",
		"hideNoteSpawningEffect": Boolean,
	},
}
```

`beatmap` and `performance` are `null` while the menu is active or before a map starts.

### Note cut object

Each note cut has a **cut plane** defined by three points of the cutting saber:

- its top on the frame the collision happened,
- its bottom on the frame the collision happened,
- its center on the frame previous to the collision frame.

In **note space**, positive X, positive Y and positive Z point right, up and forward. The arrow of a directional note sits at
positive Y and points towards negative Y. The origin is the center of the note.

```js
{
	"noteID": Integer, // ID assigned by the plugin while walking the map
	"noteType": "NoteA" | "NoteB" | "Bomb",
	"noteCutDirection": String, // "Up", "Down", "Left", "Right", "UpLeft", "UpRight", "DownLeft", "DownRight", "Any", "None"
	"sliderHeadCutDirection": null | String, // **fork** Cut direction of a slider head
	"sliderTailCutDirection": null | String, // **fork** Cut direction of a slider tail
	"noteLine": Integer, // Horizontal position, left to right [0..3]
	"noteLayer": Integer, // Vertical position, bottom to top [0..2]
	"sliderHeadLine": Integer, // **fork**
	"sliderHeadLayer": Integer, // **fork**
	"sliderTailLine": Integer, // **fork**
	"sliderTailLayer": Integer, // **fork**
	"speedOK": Boolean, // Saber speed was enough
	"directionOK": null | Boolean, // Cut direction was correct. null for bombs.
	"saberTypeOK": null | Boolean, // Correct saber was used. null for bombs.
	"wasCutTooSoon": Boolean,
	"initialScore": null | Integer, // Score for the cut so far without multipliers. null when not applicable or for bombs.
	"finalScore": null | Integer, // Full cut score without multipliers, set on `noteFullyCut`
	"cutDistanceScore": null | Integer, // Score for the distance between the cut plane and the note center [0..15]
	"swingRating": Number, // Before-cut rating on `noteCut`, after-cut rating on `noteFullyCut`. -1 when no swing data is available, such as on missed notes and bombs.
	"beforeSwingRating": Number, // **fork** Raw before-cut swing rating
	"afterSwingRating": Number, // **fork** Raw after-cut swing rating
	"beforeCutScore": Number, // **fork** Score from the before-cut swing
	"afterCutScore": Number, // **fork** Score from the after-cut swing
	"multiplier": Integer, // Combo multiplier at the time of the cut
	"saberSpeed": Number,
	"saberDir": [Number, Number, Number], // Saber movement in note space on the collision frame (current - previous tip position)
	"saberType": null | "SaberA" | "SaberB",
	"timeDeviation": Number, // Seconds from the perfect cut time
	"cutDirectionDeviation": Number, // Degrees from the perfect cut angle
	"cutPoint": [Number, Number, Number], // Point on the cut plane closest to the note center, in note space
	"cutNormal": [Number, Number, Number], // Normal of the cut plane in note space
	"cutDistanceToCenter": Number, // Distance from the note center to the cut plane
	"timeToNextBasicNote": Number, // Seconds until the next colored note
	"gameplayType": String, // **fork** NoteData gameplay type
}
```

`noteSpawned` fills only the note data fields; cut and swing fields keep their defaults. For bombs `directionOK`,
`saberTypeOK`, `initialScore`, `finalScore` and `cutDistanceScore` are `null`. The default values for the numeric fields are
`-1` for the score values and `0` for the rest.

### Beatmap event object

Background on the beatmap event system: <https://bsmg.wiki/mapping/basic-lighting.html>

V2 events (version `2.6.0`):

```js
{
	"version": "2.6.0",
	"time": Number, // Beat time
	"executionOrder": Integer,
	"type": Integer,
	"value": Integer,
	"floatValue": Number,
	"previousSameTypeEventData": null | Object, // Same shape without previous/next
	"nextSameTypeEventData": null | Object,
}
```

V3 events (version `3.0.0`):

```js
{
	"version": "3.0.0",
	"type": Integer, // See V3BeatmapEventType below
	"time": Number, // Beat time
	"executionOrder": Integer,
	// Type-specific fields, see the table below
	"previousSameTypeEventData": null | Object, // Same shape without previous/next
	"nextSameTypeEventData": null | Object,
}
```

Type-specific fields by `type`:

| Type | Name | Fields |
| --- | --- | --- |
| 1 | BPM | `bpm` |
| 2 | ColorBoost | `boostColorsAreOn` |
| 3 | FloatFx | `value`, `easeType` |
| 4 | LightColor | `groupId`, `elementId`, `transitionType`, `colorType`, `brightness`, `strobeBeatFrequency` |
| 5 | LightRotation | `groupId`, `elementId`, `usePreviousEventValue`, `easeType`, `axis`, `loopCount`, `rotationDirection`, `rotation` |
| 6 | LightTranslation | `groupId`, `elementId`, `usePreviousEventValue`, `easeType`, `axis`, `translation`, `distribution` |
| 7 | SpawnRotation | `rotation` |
| 8 | NJS | `easeType`, `usePreviousValue`, `relativeNoteJumpSpeed` |

Enum values are sent as strings (`"Instant"`, `"X"`, `"Color0"`, and so on), and `type` is sent as an integer. For
`LightTranslation`, `translation` and `distribution` are also sent as strings.

`V3BeatmapEventType`:

```csharp
public enum V3BeatmapEventType
{
    Unknown = 0,
    BPM = 1,
    ColorBoost = 2,
    FloatFx = 3,
    LightColor = 4,
    LightRotation = 5,
    LightTranslation = 6,
    SpawnRotation = 7,
    NJS = 8, // **fork**
}
```

## Events

Events are sent over the WebSocket. See [Event object](#event-object) for the envelope.

### `hello` event

Sent when a client connects. Contains the full Status object and the `other` object (currently `{}`).

### `songStart` event

Fired when the map starts, after the map metadata has been read. Contains the full Status object.

With `AsyncSetupMapInformation` enabled the event is sent before the plugin finishes indexing note IDs and beatmap events, so
early `noteSpawned` messages may arrive before the mapping is ready.

### `menu` event

Fired when the gameplay scene is torn down, that is when the player returns to the menu. The map and performance values
have already been reset, so `beatmap` and `performance` are `null`. Contains the full Status object.

### `pause` / `resume` events

Fired when the map is paused or resumed. Both contain only the `beatmap` property. `beatmap.paused` holds the pause
timestamp, and `beatmap.start` is recalculated on resume.

### `restart` event

Fired when the player restarts the map. Contains the `beatmap` and `performance` properties.

### `finished` / `failed` events

Fired when the map is finished or failed. Both contain only the `performance` property.

### `softFailed` event

Fired when energy reaches 0 while No Fail is enabled. Contains the `beatmap`, `performance` and `mod` properties, because
the modifier multiplier and the affected scores change at that moment. `performance.softFailed` stays `true` until the end
of the map.

### `energyChanged` event

Fired whenever the energy value changes. Contains only the `performance` property.

### `scoreChanged` event

Fired when the score, combo or multiplier changes. Contains only the `performance` property.

### `obstacleEnter` / `obstacleExit` events

Fired when the player's head enters or leaves an obstacle. Both contain only the `performance` property.

### `noteSpawned` event

Fired when a note or slider is spawned. Contains only the `noteCut` object, with the note data fields filled and the cut
fields at their defaults. No `status` is included.

### `noteCut` event

Fired when a note is cut. Contains the `performance` property and the `noteCut` object.

### `noteFullyCut` event

Fired when the after-cut score buffer finishes, which is when the full cut score is known. `performance.lastNoteScore` is
updated right before this event. Contains the `performance` property and the `noteCut` object. Not fired for bombs.

### `noteMissed` event

Fired when a note is missed or cut incorrectly. Contains the `performance` property and the `noteCut` object with the note
data only.

### `bombCut` event

Fired when a bomb is hit. Contains the `performance` property and the `noteCut` object.

### `bombMissed` event

Declared in the event enum for compatibility but not emitted by this build.

### `beatmapEvent` event

Real beatmap events are sent with only the `beatmapEvent` property (no `status`), one message per triggered event.

The plugin also reuses the `beatmapEvent` name once per second to publish the song timer: those messages have
`status.performance` with an updated `currentSongTime` and no `beatmapEvent` property. A consumer that only cares about
light and ring events should ignore messages without a `beatmapEvent` field.

### `other` event

Reserved for plugin-specific data. The `other` object is currently always empty and no event of this name is emitted.

## Configuration

`UserData/HttpSiraStatus.json` (written by BSIPA on first run):

| Key | Type | Default | Description |
| --- | --- | --- | --- |
| `Port` | int | 6557 | Port used by the HTTP and WebSocket server |
| `SendBeatmapEvents` | bool | true | Send `beatmapEvent` messages |
| `AsyncSetupMapInformation` | bool | true | Build the note ID and beatmap event mapping in the background and send `songStart` first. Only meaningful while `SendBeatmapEvents` is enabled. |
