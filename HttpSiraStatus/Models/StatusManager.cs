using HttpSiraStatus.Enums;
using HttpSiraStatus.Extentions;
using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Zenject;

namespace HttpSiraStatus.Models
{
    public class StatusManager : IStatusManager, IDisposable, ITickable
    {
        [Inject]
        internal StatusManager(GameStatus gameStatus, CutScoreInfoEntity.Pool cutScorePool)
        {
            this._gameStatus = gameStatus;
            this.JsonPool = new ObjectMemoryPool<JSONObject>(null, r => { r.Clear(); }, 20);
            this.UpdateAll();
            this._thread = new Thread(new ThreadStart(this.RaiseSendEvent));
            this._thread.Start();
            this._cutScorePool = cutScorePool;
        }
        public JSONObject StatusJSON { get; } = new JSONObject();
        public Queue<IBeatmapEventInformation> BeatmapEventJSON { get; } = new Queue<IBeatmapEventInformation>();
        public JSONObject OtherJSON { get; } = new JSONObject();
        public ObjectMemoryPool<JSONObject> JsonPool { get; }
        public ConcurrentQueue<JSONObject> JsonQueue { get; } = new ConcurrentQueue<JSONObject>();
        public Queue<(CutScoreInfoEntity entity, BeatSaberEvent e)> CutScoreInfoQueue { get; } = new Queue<(CutScoreInfoEntity entity, BeatSaberEvent e)>();

        public event SendEventHandler SendEvent;
        private readonly Thread _thread;
        private bool _disposedValue;
        private readonly GameStatus _gameStatus;
        private readonly CutScoreInfoEntity.Pool _cutScorePool;

        public void EmitStatusUpdate(ChangedProperty changedProps, BeatSaberEvent e)
        {
            this._gameStatus.updateCause = e.GetDescription();
            if ((changedProps & ChangedProperty.Game) == ChangedProperty.Game) {
                this.UpdateGameJSON();
            }

            if ((changedProps & ChangedProperty.Beatmap) == ChangedProperty.Beatmap) {
                this.UpdateBeatmapJSON();
            }

            if ((changedProps & ChangedProperty.Performance) == ChangedProperty.Performance) {
                this.UpdatePerformanceJSON();
            }

            if ((changedProps & ChangedProperty.Mod) == ChangedProperty.Mod) {
                this.UpdateModJSON();
                this.UpdatePlayerSettingsJSON();
            }

            this.EnqueueMessage(changedProps, e);
        }

        private void EnqueueMessage(ChangedProperty changedProps, BeatSaberEvent e)
        {
            var eventJSON = this.JsonPool.Spawn();
            eventJSON["event"] = e.GetDescription();

            if ((changedProps & ChangedProperty.AllButNoteCut) == ChangedProperty.AllButNoteCut) {
                eventJSON["status"] = this.StatusJSON;
            }
            else {
                var status = new JSONObject();

                if ((changedProps & ChangedProperty.Game) == ChangedProperty.Game) {
                    status["game"] = this.StatusJSON["game"];
                }

                if ((changedProps & ChangedProperty.Beatmap) == ChangedProperty.Beatmap) {
                    status["beatmap"] = this.StatusJSON["beatmap"];
                }

                if ((changedProps & ChangedProperty.Performance) == ChangedProperty.Performance) {
                    status["performance"] = this.StatusJSON["performance"];
                }

                if ((changedProps & ChangedProperty.Mod) == ChangedProperty.Mod) {
                    status["mod"] = this.StatusJSON["mod"];
                    status["playerSettings"] = this.StatusJSON["playerSettings"];
                }
                eventJSON["status"] = status;
            }

            if ((changedProps & ChangedProperty.Other) != 0) {
                eventJSON["other"] = this.OtherJSON;
            }
            this.JsonQueue.Enqueue(eventJSON);
        }

        private void RaiseSendEvent()
        {
            while (!this._disposedValue) {
                try {
                    while (this.JsonQueue.TryDequeue(out var json)) {
                        this.SendEvent?.Invoke(this, new SendEventArgs(json));
                        this.JsonPool.Despawn(json);
                    }
                }
                catch (Exception e) {
                    Plugin.Logger.Error(e);
                }
                Thread.Sleep(1);
            }
        }

        private void UpdateAll()
        {
            try {
                this.UpdateGameJSON();
                this.UpdateBeatmapJSON();
                this.UpdatePerformanceJSON();
                this.UpdateModJSON();
                this.UpdatePlayerSettingsJSON();
            }
            catch (Exception e) {
                Plugin.Logger.Error(e);
            }
        }

        private void UpdateGameJSON()
        {
            if (this.StatusJSON["game"] == null) {
                this.StatusJSON["game"] = new JSONObject();
            }

            var gameJSON = this.StatusJSON["game"].AsObject;

            gameJSON["pluginVersion"] = Plugin.PluginVersion;
            gameJSON["gameVersion"] = Plugin.GameVersion;
            gameJSON["scene"] = this.StringOrNull(this._gameStatus.scene);
            gameJSON["mode"] = $"{this._gameStatus.mode.GetDescription()}{this._gameStatus.characteristic}";
        }

        private void UpdateBeatmapJSON()
        {
            if (this._gameStatus.songName == null) {
                this.StatusJSON["beatmap"] = null;
                return;
            }

            if (this.StatusJSON["beatmap"] == null) {
                this.StatusJSON["beatmap"] = new JSONObject();
            }

            var beatmapJSON = this.StatusJSON["beatmap"].AsObject;

            beatmapJSON["songName"] = this.StringOrNull(this._gameStatus.songName);
            beatmapJSON["songSubName"] = this.StringOrNull(this._gameStatus.songSubName);
            beatmapJSON["songAuthorName"] = this.StringOrNull(this._gameStatus.songAuthorName);
            beatmapJSON["levelAuthorName"] = this.StringOrNull(this._gameStatus.levelAuthorName);
            beatmapJSON["songCover"] = string.IsNullOrEmpty(this._gameStatus.songCover) ? JSONNull.CreateOrGet() : new JSONString(this._gameStatus.songCover);
            beatmapJSON["songHash"] = this.StringOrNull(this._gameStatus.songHash);
            beatmapJSON["levelId"] = this.StringOrNull(this._gameStatus.levelId);
            beatmapJSON["songBPM"] = this._gameStatus.songBPM;
            beatmapJSON["noteJumpSpeed"] = this._gameStatus.noteJumpSpeed;
            beatmapJSON["noteJumpStartBeatOffset"] = this._gameStatus.noteJumpStartBeatOffset;
            beatmapJSON["songTimeOffset"] = new JSONNumber(this._gameStatus.songTimeOffset);
            beatmapJSON["start"] = this._gameStatus.start == 0 ? JSONNull.CreateOrGet() : new JSONNumber(this._gameStatus.start);
            beatmapJSON["paused"] = this._gameStatus.paused == 0 ? JSONNull.CreateOrGet() : new JSONNumber(this._gameStatus.paused);
            beatmapJSON["length"] = new JSONNumber(this._gameStatus.length);
            beatmapJSON["difficulty"] = this.StringOrNull(this._gameStatus.difficulty);
            beatmapJSON["difficultyEnum"] = this.StringOrNull(this._gameStatus.difficultyEnum);
            beatmapJSON["characteristic"] = this.StringOrNull(this._gameStatus.characteristic);
            beatmapJSON["notesCount"] = this._gameStatus.notesCount;
            beatmapJSON["bombsCount"] = this._gameStatus.bombsCount;
            beatmapJSON["obstaclesCount"] = this._gameStatus.obstaclesCount;
            beatmapJSON["maxScore"] = this._gameStatus.maxScore;
            beatmapJSON["maxRank"] = this._gameStatus.maxRank;
            beatmapJSON["environmentName"] = this._gameStatus.environmentName;

            if (beatmapJSON["color"] == null) {
                beatmapJSON["color"] = new JSONObject();
            }
            var colorJSON = beatmapJSON["color"].AsObject;

            this.UpdateColor(this._gameStatus.colorSaberA, colorJSON, "saberA");
            this.UpdateColor(this._gameStatus.colorSaberB, colorJSON, "saberB");
            this.UpdateColor(this._gameStatus.colorEnvironment0, colorJSON, "environment0");
            this.UpdateColor(this._gameStatus.colorEnvironment1, colorJSON, "environment1");
            this.UpdateColor(this._gameStatus.colorEnvironmentW, colorJSON, "environmentW");
            this.UpdateColor(this._gameStatus.colorEnvironmentBoost0, colorJSON, "environment0Boost");
            this.UpdateColor(this._gameStatus.colorEnvironmentBoost1, colorJSON, "environment1Boost");
            this.UpdateColor(this._gameStatus.colorEnvironmentBoostW, colorJSON, "environmentWBoost");
            this.UpdateColor(this._gameStatus.colorObstacle, colorJSON, "obstacle");
        }

        private void UpdateColor(Color? color, JSONObject parent, string key)
        {
            if (color == null) {
                parent[key] = JSONNull.CreateOrGet();
                return;
            }
            var color32 = (Color32?)color;

            var arr = parent[key] as JSONArray ?? new JSONArray();

            arr[0] = (int)color32.Value.r;
            arr[1] = (int)color32.Value.g;
            arr[2] = (int)color32.Value.b;

            parent[key] = arr;
        }

        private void UpdatePerformanceJSON()
        {
            if (this._gameStatus.start == 0) {
                this.StatusJSON["performance"] = null;
                return;
            }

            if (this.StatusJSON["performance"] == null) {
                this.StatusJSON["performance"] = new JSONObject();
            }

            var performanceJSON = this.StatusJSON["performance"].AsObject;

            performanceJSON["rawScore"].AsInt = this._gameStatus.rawScore;
            performanceJSON["score"].AsInt = this._gameStatus.score;
            performanceJSON["currentMaxScore"].AsInt = this._gameStatus.currentMaxScore;
            performanceJSON["relativeScore"].AsFloat = this._gameStatus.relativeScore;
            performanceJSON["rank"] = this._gameStatus.rank;
            performanceJSON["passedNotes"].AsInt = this._gameStatus.passedNotes;
            performanceJSON["hitNotes"].AsInt = this._gameStatus.hitNotes;
            performanceJSON["missedNotes"].AsInt = this._gameStatus.missedNotes;
            performanceJSON["lastNoteScore"].AsInt = this._gameStatus.lastNoteScore;
            performanceJSON["passedBombs"].AsInt = this._gameStatus.passedBombs;
            performanceJSON["hitBombs"].AsInt = this._gameStatus.hitBombs;
            performanceJSON["combo"].AsInt = this._gameStatus.combo;
            performanceJSON["maxCombo"].AsInt = this._gameStatus.maxCombo;
            performanceJSON["multiplier"].AsInt = this._gameStatus.multiplier;
            performanceJSON["multiplierProgress"].AsFloat = float.IsNaN(this._gameStatus.multiplierProgress) ? 0f : this._gameStatus.multiplierProgress;
            performanceJSON["batteryEnergy"] = this._gameStatus.modBatteryEnergy || this._gameStatus.modInstaFail ? new JSONNumber(this._gameStatus.batteryEnergy) : JSONNull.CreateOrGet();
            performanceJSON["energy"].AsFloat = this._gameStatus.energy;
            performanceJSON["softFailed"].AsBool = this._gameStatus.softFailed;
            performanceJSON["currentSongTime"].AsInt = this._gameStatus.currentSongTime;
        }

        private void UpdateModJSON()
        {
            if (this.StatusJSON["mod"] == null) {
                this.StatusJSON["mod"] = new JSONObject();
            }

            var modJSON = this.StatusJSON["mod"].AsObject;

            modJSON["multiplier"] = this._gameStatus.modifierMultiplier;
            modJSON["obstacles"] = this._gameStatus.modObstacles == null || this._gameStatus.modObstacles == "NoObstacles" ? new JSONBool(false) : new JSONString(this._gameStatus.modObstacles);
            modJSON["instaFail"] = this._gameStatus.modInstaFail;
            modJSON["noFail"] = this._gameStatus.modNoFail;
            modJSON["batteryEnergy"] = this._gameStatus.modBatteryEnergy;
            modJSON["batteryLives"] = this._gameStatus.modBatteryEnergy || this._gameStatus.modInstaFail ? new JSONNumber(this._gameStatus.batteryLives) : JSONNull.CreateOrGet();
            modJSON["disappearingArrows"] = this._gameStatus.modDisappearingArrows;
            modJSON["noBombs"] = this._gameStatus.modNoBombs;
            modJSON["songSpeed"] = this._gameStatus.modSongSpeed;
            modJSON["songSpeedMultiplier"] = this._gameStatus.songSpeedMultiplier;
            modJSON["noArrows"] = this._gameStatus.modNoArrows;
            modJSON["ghostNotes"] = this._gameStatus.modGhostNotes;
            modJSON["failOnSaberClash"] = this._gameStatus.modFailOnSaberClash;
            modJSON["strictAngles"] = this._gameStatus.modStrictAngles;
            modJSON["fastNotes"] = this._gameStatus.modFastNotes;
            modJSON["smallNotes"] = this._gameStatus.modSmallNotes;
            modJSON["proMode"] = this._gameStatus.modProMode;
            modJSON["zenMode"] = this._gameStatus.modZenMode;
        }

        private void UpdatePlayerSettingsJSON()
        {
            if (this.StatusJSON["playerSettings"] == null) {
                this.StatusJSON["playerSettings"] = new JSONObject();
            }

            var playerSettingsJSON = this.StatusJSON["playerSettings"].AsObject;

            playerSettingsJSON["staticLights"] = this._gameStatus.staticLights;
            playerSettingsJSON["leftHanded"] = this._gameStatus.leftHanded;
            playerSettingsJSON["playerHeight"] = this._gameStatus.playerHeight;
            playerSettingsJSON["sfxVolume"] = this._gameStatus.sfxVolume;
            playerSettingsJSON["reduceDebris"] = this._gameStatus.reduceDebris;
            playerSettingsJSON["noHUD"] = this._gameStatus.noHUD;
            playerSettingsJSON["advancedHUD"] = this._gameStatus.advancedHUD;
            playerSettingsJSON["autoRestart"] = this._gameStatus.autoRestart;
            playerSettingsJSON["saberTrailIntensity"] = this._gameStatus.saberTrailIntensity;
            playerSettingsJSON["environmentEffects"] = this._gameStatus.environmentEffects;
            playerSettingsJSON["hideNoteSpawningEffect"] = this._gameStatus.hideNoteSpawningEffect;
        }

        private JSONNode StringOrNull(string str)
        {
            return string.IsNullOrEmpty(str) ? JSONNull.CreateOrGet() : new JSONString(str);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue) {
                this._disposedValue = true;
            }
        }
        public void Dispose()
        {
            // ���̃R�[�h��ύX���Ȃ��ł��������B�N���[���A�b�v �R�[�h�� 'Dispose(bool disposing)' ���\�b�h�ɋL�q���܂�
            this.Dispose(disposing: true);
        }

        /// <summary>
        /// Update�݂����Ȃ��̃��C���X���b�h�ŌĂ΂��
        /// </summary>
        public void Tick()
        {
            this.EnqueueCutInfo();
            this.EnqueueBeatmapEvent();
        }

        private void EnqueueCutInfo()
        {
            while (this.CutScoreInfoQueue.TryDequeue(out var cutScoreInfo)) {
                var eventJSON = this.JsonPool.Spawn();
                eventJSON["event"] = cutScoreInfo.e.GetDescription();
                eventJSON["noteCut"] = cutScoreInfo.entity.ToJson();
                if (cutScoreInfo.e != BeatSaberEvent.NoteSpawned) {
                    var status = new JSONObject();
                    status["performance"] = this.StatusJSON["performance"];
                    eventJSON["status"] = status;
                }
                this.JsonQueue.Enqueue(eventJSON);
                this._cutScorePool.Despawn(cutScoreInfo.entity);
            }
        }

        private void EnqueueBeatmapEvent()
        {
            while (this.BeatmapEventJSON.TryDequeue(out var beatmapEventInformation)) {
                var eventJSON = this.JsonPool.Spawn();
                eventJSON["event"] = BeatSaberEvent.BeatmapEvent.GetDescription();
                eventJSON["beatmapEvent"] = beatmapEventInformation.SilializedJson;
                this.JsonQueue.Enqueue(eventJSON);
            }
        }
    }
}
