# HttpSiraStatus

[![Build and Release](https://github.com/denpadokei/HttpSiraStatus/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/denpadokei/HttpSiraStatus/actions/workflows/build-and-release.yml)

現在のゲーム状態 (プレイ中の譜面情報・スコア・ノーツカットなど) を HTTP と WebSocket で外部へ配信する MOD。上流は opl-/beatsaber-http-status。現在はこのフォークで Beat Saber 1.41.1 向けにメンテナンスしている。単体では何も表示せず、配信された情報を読み取る外部ツールと組み合わせて使う。

## 機能

- `GET /status.json` で最新の状態をまるごと JSON で返す
- WebSocket `/socket` に接続すると `hello` イベントで全状態を送信し、以降は変化のあったイベントだけを逐次配信する
- ゲーム状態 (`game`)、譜面情報 (`beatmap`)、パフォーマンス (`performance`)、MOD 設定 (`mod`)、プレイヤー設定 (`playerSettings`) を JSON で提供する
- 譜面イベント (`beatmapEvent`) を V2 / V3 形式で配信する
- ノーツの出現・カット・取りこぼし・ボム、障害物の進入・退出、スコア変化、曲の開始・終了・失敗・再開・リスタートをイベントとして配信する
- 送信処理を専用スレッドで行い、ゲーム側のフレームへの影響を抑える

## 動作環境

| 項目 | 内容 |
| --- | --- |
| Beat Saber | 1.41.1 |
| 依存 MOD | BSIPA 4.3.5 以上 / SiraUtil 3.1.12 以上 |

## インストール

1. Beat Saber を 1.41.1 に合わせる (BSManager などのバージョン管理ツールを推奨)
2. BSIPA と上記の依存 MOD をインストールする
3. [Releases](https://github.com/denpadokei/HttpSiraStatus/releases) から `HttpSiraStatus-15.0.0-bs1.41.1.zip` をダウンロードし、`Plugins` フォルダをゲームフォルダへ上書きコピーする

## 使い方

この MOD は情報を配信するだけで、ゲーム内に UI は無い。ゲーム起動後、同じ PC 上のツールや配信オーバーレイから次のエンドポイントへ接続する。

| 種類 | エンドポイント | 内容 |
| --- | --- | --- |
| HTTP | `GET http://localhost:6557/status.json` | 現在の状態を丸ごと返す (`Content-Type: application/json`) |
| WebSocket | `ws://localhost:6557/socket` | 接続時に `hello` を送り、以降イベントを逐次送信する |

ポートは設定で変更できる。`/status.json` と `/socket` 以外のパスは 404 を返す。

### 送信される JSON

イベントは次の形で送られる。`event` はイベント名、`time` はイベント発生時刻の UNIX ミリ秒 (`status` は抜粋)。

```json
{
  "event": "songStart",
  "time": 1700000000000,
  "status": {
    "game": {
      "pluginVersion": "15.0.0",
      "gameVersion": "1.41.1",
      "scene": "Song",
      "mode": "SoloStandard"
    }
  }
}
```

- `status` はイベントの種類に応じて全体、または変更された部分 (`game` / `beatmap` / `performance` / `mod` / `playerSettings`) のみが入る
- ノーツ関連のイベントは `status.performance` に加えて `noteCut` を含む
- 譜面イベントは `beatmapEvent` を含む
- WebSocket 接続直後の `hello` は `status` に全体、`other` に追加情報 (現在は空オブジェクト) を含む

`status` の主なキーは次のとおり。

| オブジェクト | 主なキー |
| --- | --- |
| `game` | `pluginVersion`, `gameVersion`, `scene` (`Menu` / `Song` / `Spectator`), `mode` |
| `beatmap` | `songName`, `songSubName`, `songAuthorName`, `levelAuthorName`, `levelAuthorNamesArray`, `lighterNamesArray`, `songCover`, `songHash`, `levelId`, `songBPM`, `noteJumpSpeed`, `noteJumpStartBeatOffset`, `songTimeOffset`, `start`, `paused`, `length`, `difficulty`, `difficultyEnum`, `characteristic`, `notesCount`, `bombsCount`, `obstaclesCount`, `maxScore`, `maxRank`, `environmentName`, `color` |
| `beatmap.color` | `saberA`, `saberB`, `environment0`, `environment1`, `environmentW`, `environment0Boost`, `environment1Boost`, `environmentWBoost`, `obstacle` |
| `performance` | `rawScore`, `score`, `currentMaxScore`, `relativeScore`, `rank`, `passedNotes`, `hitNotes`, `missedNotes`, `lastNoteScore`, `passedBombs`, `hitBombs`, `combo`, `maxCombo`, `multiplier`, `multiplierProgress`, `batteryEnergy`, `energy`, `softFailed`, `currentSongTime` |
| `mod` | `multiplier`, `obstacles`, `instaFail`, `noFail`, `batteryEnergy`, `batteryLives`, `disappearingArrows`, `noBombs`, `songSpeed`, `songSpeedMultiplier`, `noArrows`, `ghostNotes`, `failOnSaberClash`, `strictAngles`, `fastNotes`, `smallNotes`, `proMode`, `zenMode` |
| `playerSettings` | `staticLights`, `leftHanded`, `playerHeight`, `sfxVolume`, `reduceDebris`, `noHUD`, `advancedHUD`, `autoRestart`, `saberTrailIntensity`, `environmentEffects`, `hideNoteSpawningEffect` |

### イベント一覧

`menu`, `songStart`, `pause`, `resume`, `restart`, `finished`, `failed`, `softFailed`, `scoreChanged`, `energyChanged`, `noteSpawned`, `noteCut`, `noteFullyCut`, `noteMissed`, `bombCut`, `bombMissed`, `obstacleEnter`, `obstacleExit`, `beatmapEvent`, `other`

より詳細な仕様は [protocol.md](protocol.md) を参照。

## 設定

`UserData/HttpSiraStatus.json` で変更する。ゲーム内に設定画面は無い。

| 項目 | 型 | 既定値 | 説明 |
| --- | --- | --- | --- |
| Port | int | 6557 | HTTP / WebSocket サーバーが待ち受けるポート番号 |
| SendBeatmapEvents | bool | true | 譜面イベント (`beatmapEvent`) を配信する |
| AsyncSetupMapInformation | bool | true | ノーツ ID と譜面イベントのマッピング構築をバックグラウンドで行い、`songStart` を先に送る (`SendBeatmapEvents` が true のときのみ有効) |

## ビルド

```powershell
git clone https://github.com/denpadokei/HttpSiraStatus.git
cd HttpSiraStatus
dotnet build .\HttpSiraStatus\HttpSiraStatus.csproj -c Release
```

`BeatSaberDir` に Beat Saber のインストール先を設定すると、ゲームの参照アセンブリを解決できます。

GitHub Actions の `build-and-release.yml` が push ごとにビルドし、`X.X.X` 形式のタグを push するとリリースを作成します。

## クレジット

- オリジナル作者: opl ([opl-/beatsaber-http-status](https://github.com/opl-/beatsaber-http-status))
- xyonico: [Beat Saber Discord Presence](https://github.com/xyonico/BeatSaberDiscordPresence) (このプラグインの元になった実装)
- sta: [websocket-sharp](https://github.com/sta/websocket-sharp)
- Maxaxik: テストと調査への協力

## ライセンス

MIT License (Copyright (c) 2018-2019 opl)。詳細は [LICENSE](LICENSE) を参照。
