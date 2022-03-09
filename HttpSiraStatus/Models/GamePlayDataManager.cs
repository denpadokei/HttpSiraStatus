using BS_Utils.Gameplay;
using HttpSiraStatus.Harmonies;
using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;
using IPA.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace HttpSiraStatus.Models
{
    public class GamePlayDataManager : MonoBehaviour, IInitializable, IDisposable, ICutScoreBufferDidFinishReceiver
    {
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // パブリックメソッド
        public void HandleCutScoreBufferDidFinish(CutScoreBuffer cutScoreBuffer)
        {
            cutScoreBuffer.UnregisterDidFinishReceiver(this);
            if (cutScoreBuffer is CustomCutBuffer customCutBuffer) {
                var noteCutInfo = customCutBuffer.NoteCutInfo;
                this.SetNoteCutStatus(customCutBuffer.NoteController, noteCutInfo, false);
                this.gameStatus.swingRating = cutScoreBuffer.noteCutInfo.saberMovementData.ComputeSwingRating();
                this.gameStatus.afterSwingRating = cutScoreBuffer.afterCutSwingRating;
                this.gameStatus.beforSwingRating = cutScoreBuffer.beforeCutSwingRating;
                this.gameStatus.cutMultiplier = this._multiplier;

                this.statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteFullyCut);
                this.cutBufferPool.Despawn(customCutBuffer);
            }
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // プライベートメソッド
        private void OnMultiplayerLevelFinished(MultiplayerLevelCompletionResults obj)
        {
            switch (obj.playerLevelEndReason) {
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.Cleared:
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.GivenUp:
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.WasInactive:
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.HostEndedLevel:
                    this.OnLevelFinished();
                    break;
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.Failed:
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.Quit:
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.StartupFailed:
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.ConnectedAfterLevelEnded:
                    this.OnLevelFailed();
                    break;
                default:
                    this.OnLevelFinished();
                    break;
            }
        }

        private void RelativeScoreAndImmediateRankCounter_relativeScoreOrImmediateRankDidChangeEvent()
        {
            this.gameStatus.relativeScore = this.relativeScoreAndImmediateRankCounter.relativeScore;
            this.gameStatus.rank = RankModel.GetRankName(this.relativeScoreAndImmediateRankCounter.immediateRank);
            this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ScoreChanged);
        }
        private void OnEnergyChanged(float obj)
        {
            this.gameStatus.energy = obj;
            this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.EnergyChanged);
        }
        /// <summary>
        /// こいつ別スレで呼ぶと悲惨なことになるのでUpdateで呼ぶことにしました。
        /// </summary>
        private void OnObstacleInteraction()
        {
            if (this.playerHeadAndObstacleInteraction == null) {
                return;
            }
            // intersectingObstaclesのgetがフレーム単位で呼ばないといけない。
            // 別スレで呼ぶと関係ないとこで他のモジュール群が死ぬ。
            var currentHeadInObstacle = this.playerHeadAndObstacleInteraction.playerHeadIsInObstacle;

            if (!this.headInObstacle && currentHeadInObstacle) {
                this.headInObstacle = true;
                this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ObstacleEnter);
            }
            else if (this.headInObstacle && !currentHeadInObstacle) {
                this.headInObstacle = false;
                this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ObstacleExit);
            }
        }

        private void UpdateCurrentSongTime()
        {
            var songTime = Mathf.FloorToInt(this.audioTimeSource.songTime);
            if (this.gameStatus.currentSongTime != songTime) {
                this.gameStatus.currentSongTime = songTime;
                this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.BeatmapEvent);
            }
        }

        private void UpdateModMultiplier()
        {
            var energy = this.gameEnergyCounter.energy;

            this.gameStatus.modifierMultiplier = this.gameplayModifiersSO.GetTotalMultiplier(this.gameplayModifiersSO.CreateModifierParamsList(this.gameplayModifiers), energy);

            this.gameStatus.maxScore = this.gameplayModifiersSO.MaxModifiedScoreForMaxMultipliedScore(ScoreModel.ComputeMaxMultipliedScoreForBeatmap(this._beatmapData), this.gameplayModifiersSO.CreateModifierParamsList(this.gameplayModifiers), this.gameplayModifiersSO, energy);
            this.gameStatus.maxRank = RankModelHelper.MaxRankForGameplayModifiers(this.gameplayModifiers, this.gameplayModifiersSO, energy).ToString();
        }

        private void OnGamePause()
        {
            this.gameStatus.paused = Utility.GetCurrentTime();

            this.statusManager.EmitStatusUpdate(ChangedProperty.Beatmap, BeatSaberEvent.Pause);
        }

        private void OnGameResume()
        {
            this.gameStatus.start = Utility.GetCurrentTime() - (long)(this.audioTimeSource.songTime * 1000f / this.gameStatus.songSpeedMultiplier);
            this.gameStatus.paused = 0;

            this.statusManager.EmitStatusUpdate(ChangedProperty.Beatmap, BeatSaberEvent.Resume);
        }
        private void OnNoteWasSpawnedEvent(NoteController obj)
        {
            this.SetNoteCutStatus(obj);
            this.statusManager.EmitStatusUpdate(ChangedProperty.NoteCut, BeatSaberEvent.NoteSpawned);
        }
        private void OnNoteWasMissedEvent(NoteController obj)
        {
            // Event order: combo, multiplier, scoreController.noteWasMissed, (LateUpdate) scoreController.scoreDidChange
            var noteData = obj.noteData;
            this.gameStatus.batteryEnergy = this.gameEnergyCounter.batteryEnergy;
            this.gameStatus.energy = this.gameEnergyCounter.energy;

            this.SetNoteCutStatus(obj, default, false);

            if (noteData.colorType == ColorType.None) {
                this.gameStatus.passedBombs++;

                this.statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.BombMissed);
            }
            else {
                this.gameStatus.passedNotes++;
                this.gameStatus.missedNotes++;

                this.statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteMissed);
            }
        }

        private void OnNoteWasCutEvent(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            // Event order: combo, multiplier, scoreController.noteWasCut, (LateUpdate) scoreController.scoreDidChange, afterCut, (LateUpdate) scoreController.scoreDidChange
            var noteData = noteController.noteData;
            var multiplier = this._multiplier;
            this.SetNoteCutStatus(noteController, noteCutInfo);
            this.gameStatus.finalScore = -1;
            this.gameStatus.cutMultiplier = multiplier;
            if (noteData.colorType == ColorType.None) {
                this.gameStatus.passedBombs++;
                this.gameStatus.hitBombs++;

                this.statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.BombCut);
            }
            else {
                this.gameStatus.passedNotes++;

                if (noteCutInfo.allIsOK) {
                    this.gameStatus.hitNotes++;
                    this.statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteCut);
                }
                else {
                    this.gameStatus.missedNotes++;
                    this.statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteMissed);
                }
            }
            if (noteCutInfo.allIsOK) {
                this.cutBufferPool.Spawn(noteCutInfo, multiplier, noteController, this);
            }
        }

        private void ScoreController_scoringForNoteStartedEvent(ScoringElement obj)
        {
            if (obj is GoodCutScoringElement element) {
                this.gameStatus.cutDistanceScore = element.cutScoreBuffer.centerDistanceCutScore;
                this.gameStatus.initialScore = element.cutScore;
                this.statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteCut);
            }
        }

        private void ScoreController_scoringForNoteFinishedEvent(ScoringElement obj)
        {
            if (obj is GoodCutScoringElement element) {
                this.gameStatus.cutDistanceScore = element.cutScoreBuffer.centerDistanceCutScore;
                this.gameStatus.finalScore = element.cutScore;
                this.statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteCut);
            }
        }

        private void SetNoteCutStatus(NoteController noteController, NoteCutInfo noteCutInfo = default, bool initialCut = true)
        {
            var noteData = noteController.noteData;
            var entity = this.notePool.Spawn(noteData, this.gameplayModifiers.noArrows);
            this.gameStatus.ResetNoteCut();
            // Backwards compatibility for <1.12.1
            this.gameStatus.noteID = -1;
            // Check the near notes first for performance
            if (this._noteToIdMapping.TryGetValue(entity, out var noteID)) {
                this.gameStatus.noteID = noteID;
                if (this.lastNoteId < noteID) {
                    this.lastNoteId = noteID;
                }
            }
            else {
                this.gameStatus.noteID = this.lastNoteId;
            }
            // Backwards compatibility for <1.12.1
            var colorName = noteData.colorType.ToString();
            switch (noteData.colorType) {
                case ColorType.ColorA:
                    colorName = "NoteA";
                    break;
                case ColorType.ColorB:
                    colorName = "NoteB";
                    break;
                case ColorType.None:
                    colorName = "Bomb";
                    break;
                default:
                    break;
            }
            this.gameStatus.noteType = colorName;
            this.gameStatus.noteCutDirection = noteData.cutDirection.ToString();
            this.gameStatus.noteLine = noteData.lineIndex;
            this.gameStatus.noteLayer = (int)noteData.noteLineLayer;
            // If long notes are ever introduced, this name will make no sense
            this.gameStatus.timeToNextBasicNote = noteData.timeToNextColorNote;
            if (!EqualityComparer<NoteCutInfo>.Default.Equals(noteCutInfo, default)) {
                var noteScoreDefinition = ScoreModel.GetNoteScoreDefinition(noteCutInfo.noteData.scoringType);
                bool rateBeforeCut = noteScoreDefinition.maxBeforeCutScore > 0 && noteScoreDefinition.minBeforeCutScore != noteScoreDefinition.maxBeforeCutScore;
                bool rateAfterCut = noteScoreDefinition.maxAfterCutScore > 0 && noteScoreDefinition.minAfterCutScore != noteScoreDefinition.maxAfterCutScore;
                var noteTransform = noteController.noteTransform;
                this.gameStatus.speedOK = noteCutInfo.speedOK;
                this.gameStatus.directionOK = noteCutInfo.directionOK;
                this.gameStatus.saberTypeOK = noteCutInfo.saberTypeOK;
                this.gameStatus.wasCutTooSoon = noteCutInfo.wasCutTooSoon;
                this.gameStatus.saberSpeed = noteCutInfo.saberSpeed;
                var saberDir = noteTransform.InverseTransformDirection(noteCutInfo.saberDir);
                this.gameStatus.saberDirX = saberDir[0];
                this.gameStatus.saberDirY = saberDir[1];
                this.gameStatus.saberDirZ = saberDir[2];
                var rating = noteCutInfo.saberMovementData?.ComputeSwingRating();
                this.gameStatus.swingRating = noteCutInfo.saberMovementData == null ? -1 : rating.Value;
                this.gameStatus.afterSwingRating = rateAfterCut ? 0 : 1;
                this.gameStatus.beforSwingRating = rateAfterCut ? rating.Value : 1;
                this.gameStatus.saberType = noteCutInfo.saberType.ToString();
                this.gameStatus.timeDeviation = noteCutInfo.timeDeviation;
                this.gameStatus.cutDirectionDeviation = noteCutInfo.cutDirDeviation;
                var cutPoint = noteTransform.InverseTransformPoint(noteCutInfo.cutPoint);
                this.gameStatus.cutPointX = cutPoint[0];
                this.gameStatus.cutPointY = cutPoint[1];
                this.gameStatus.cutPointZ = cutPoint[2];
                var cutNormal = noteTransform.InverseTransformDirection(noteCutInfo.cutNormal);
                this.gameStatus.cutNormalX = cutNormal[0];
                this.gameStatus.cutNormalY = cutNormal[1];
                this.gameStatus.cutNormalZ = cutNormal[2];
                this.gameStatus.cutDistanceToCenter = noteCutInfo.cutDistanceToCenter;
            }
            this.notePool.Despawn(entity);
        }

        private void OnScoreDidChange(int scoreBeforeMultiplier, int scoreAfterMultiplier)
        {
            this.gameStatus.rawScore = scoreBeforeMultiplier;
            this.gameStatus.score = scoreAfterMultiplier;
            this.gameStatus.currentMaxScore = this.scoreController.immediateMaxPossibleModifiedScore;
            this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ScoreChanged);
        }
        private void OnComboDidChange(int combo)
        {
            this.gameStatus.combo = combo;
            // public int ScoreController#maxCombo
            this.gameStatus.maxCombo = (this._comboController as ComboController).maxCombo;
            this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ScoreChanged);
        }

        private void OnMultiplierDidChange(int multiplier, float multiplierProgress)
        {
            _multiplier = multiplier;
            this.gameStatus.multiplier = multiplier;
            this.gameStatus.multiplierProgress = multiplierProgress;
            this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ScoreChanged);
        }

        private void OnLevelFinished() => this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.Finished);

        private void OnLevelFailed() => this.statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.Failed);

        private void OnEnergyDidReach0Event()
        {
            if (this.gameStatus.modNoFail) {
                this.gameStatus.softFailed = true;
                this.UpdateModMultiplier();
                this.statusManager.EmitStatusUpdate(ChangedProperty.BeatmapAndPerformanceAndMod, BeatSaberEvent.SoftFailed);
            }
        }

        private void OnBeatmapEventDidTrigger(BeatmapEventData beatmapEventData)
        {
            this.gameStatus.beatmapEventType = (int)beatmapEventData.type;
            this.gameStatus.beatmapEventValue = beatmapEventData.executionOrder;

            this.statusManager.EmitStatusUpdate(ChangedProperty.BeatmapEvent, BeatSaberEvent.BeatmapEvent);
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // メンバ変数
        private IStatusManager statusManager;
        private GameStatus gameStatus;
        private CustomCutBuffer.Pool cutBufferPool;
        private NoteDataEntity.Pool notePool;
        private GameplayCoreSceneSetupData gameplayCoreSceneSetupData;
        private PauseController pauseController;
        private IScoreController scoreController;
        private IComboController _comboController;
        private GameplayModifiers gameplayModifiers;
        private IAudioTimeSource audioTimeSource;
        private BeatmapCallbacksController beatmapObjectCallbackController;
        private PlayerHeadAndObstacleInteraction playerHeadAndObstacleInteraction;
        private GameEnergyCounter gameEnergyCounter;
        private MultiplayerLocalActivePlayerFacade multiplayerLocalActivePlayerFacade;
        private RelativeScoreAndImmediateRankCounter relativeScoreAndImmediateRankCounter;
        private BeatmapObjectManager _beatmapObjectManager;
        private ILevelEndActions levelEndActions;
        private GameplayModifiersModelSO gameplayModifiersSO;
        private bool headInObstacle = false;
        private Thread _thread;
        private int lastNoteId = 0;
        private bool disposedValue;
        private int _multiplier = 1;
        private BeatmapDataCallbackWrapper _eventDataCallbackWrapper;
        private IReadonlyBeatmapData _beatmapData;
        /// <summary>
        /// Beat Saber 1.12.1 removes NoteData.id, forcing us to generate our own note IDs to allow users to easily link events about the same note.
        /// Before 1.12.1 the noteID matched the note order in the beatmap file, but this is impossible to replicate now without hooking into the level loading code.
        /// </summary>
        private readonly ConcurrentDictionary<NoteDataEntity, int> _noteToIdMapping = new ConcurrentDictionary<NoteDataEntity, int>();
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // 構築・破棄
        /// <summary>
        /// 引数やっば
        /// </summary>
        /// <param name="statusManager"></param>
        /// <param name="gameStatus"></param>
        /// <param name="customCutBufferPool"></param>
        /// <param name="noteEntityPool"></param>
        /// <param name="gameplayCoreSceneSetupData"></param>
        /// <param name="score"></param>
        /// <param name="gameplayModifiers"></param>
        /// <param name="audioTimeSource"></param>
        /// <param name="beatmapObjectCallbackController"></param>
        /// <param name="playerHeadAndObstacleInteraction"></param>
        /// <param name="gameEnergyCounter"></param>
        /// <param name="relative"></param>
        /// <param name="diContainer"></param>
        [Inject]
        private void Constractor(
            IStatusManager statusManager,
            GameStatus gameStatus,
            CustomCutBuffer.Pool customCutBufferPool,
            NoteDataEntity.Pool noteEntityPool,
            GameplayCoreSceneSetupData gameplayCoreSceneSetupData,
            IScoreController score,
            IComboController comboController,
            GameplayModifiers gameplayModifiers,
            IAudioTimeSource audioTimeSource,
            IReadonlyBeatmapData readonlyBeatmapData,
            BeatmapCallbacksController beatmapObjectCallbackController,
            PlayerHeadAndObstacleInteraction playerHeadAndObstacleInteraction,
            GameEnergyCounter gameEnergyCounter,
            RelativeScoreAndImmediateRankCounter relative,
            BeatmapObjectManager beatmapObjectManager,
            DiContainer diContainer)
        {
            this.statusManager = statusManager;
            this.gameStatus = gameStatus;
            this.cutBufferPool = customCutBufferPool;
            this.notePool = noteEntityPool;
            this.gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            this.scoreController = score;
            this.gameplayModifiers = gameplayModifiers;
            this.audioTimeSource = audioTimeSource;
            this.beatmapObjectCallbackController = beatmapObjectCallbackController;
            this.playerHeadAndObstacleInteraction = playerHeadAndObstacleInteraction;
            this.gameEnergyCounter = gameEnergyCounter;
            this.relativeScoreAndImmediateRankCounter = relative;
            this._beatmapObjectManager = beatmapObjectManager;
            this._comboController = comboController;
            this._beatmapData = readonlyBeatmapData;
            if (this.scoreController is ScoreController scoreController) {
                this.gameplayModifiersSO = scoreController.GetField<GameplayModifiersModelSO, ScoreController>("_gameplayModifiersModel");
            }
            this.pauseController = diContainer.TryResolve<PauseController>();
            this.levelEndActions = diContainer.TryResolve<ILevelEndActions>();
            this.multiplayerLocalActivePlayerFacade = diContainer.TryResolve<MultiplayerLocalActivePlayerFacade>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue) {
                if (disposing) {
                    Plugin.Logger.Debug("dispose call");
                    try {
                        this.gameStatus.scene = "Menu"; // XXX: impossible because multiplayerController is always cleaned up before this

                        this.gameStatus?.ResetMapInfo();

                        this.gameStatus?.ResetPerformance();

                        // Clear note id mappings.
                        this._noteToIdMapping?.Clear();

                        this.statusManager?.EmitStatusUpdate(ChangedProperty.AllButNoteCut, BeatSaberEvent.Menu);

                        this._thread?.Abort();

                        if (this.pauseController != null) {
                            this.pauseController.didPauseEvent -= this.OnGamePause;
                            this.pauseController.didResumeEvent -= this.OnGameResume;
                        }

                        if (this.scoreController != null) {
                            this.scoreController.scoreDidChangeEvent -= this.OnScoreDidChange;
                            this.scoreController.multiplierDidChangeEvent -= this.OnMultiplierDidChange;
                        }
                        if (this._comboController != null) {
                            this._comboController.comboDidChangeEvent -= this.OnComboDidChange;
                        }

                        if (this.multiplayerLocalActivePlayerFacade != null) {
                            this.multiplayerLocalActivePlayerFacade.playerDidFinishEvent -= this.OnMultiplayerLevelFinished;
                            this.multiplayerLocalActivePlayerFacade = null;
                        }

                        if (this.levelEndActions != null) {
                            this.levelEndActions.levelFinishedEvent -= this.OnLevelFinished;
                            this.levelEndActions.levelFailedEvent -= this.OnLevelFailed;
                        }

                        if (this.beatmapObjectCallbackController != null) {
                            beatmapObjectCallbackController.RemoveBeatmapCallback(_eventDataCallbackWrapper);
                            //this.beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= this.OnBeatmapEventDidTrigger;
                        }

                        if (this._beatmapObjectManager != null) {
                            this._beatmapObjectManager.noteWasSpawnedEvent -= this.OnNoteWasSpawnedEvent;
                            this._beatmapObjectManager.noteWasMissedEvent -= this.OnNoteWasMissedEvent;
                        }

                        if (this.gameEnergyCounter != null) {
                            this.gameEnergyCounter.gameEnergyDidChangeEvent -= this.OnEnergyChanged;
                            this.gameEnergyCounter.gameEnergyDidReach0Event -= this.OnEnergyDidReach0Event;
                        }
                        if (this.relativeScoreAndImmediateRankCounter) {
                            this.relativeScoreAndImmediateRankCounter.relativeScoreOrImmediateRankDidChangeEvent -= this.RelativeScoreAndImmediateRankCounter_relativeScoreOrImmediateRankDidChangeEvent;
                        }
                        ScoreControllerHandleWasCutPatch.NoteWasCut -= this.OnNoteWasCutEvent;
                    }
                    catch (Exception e) {
                        Plugin.Logger.Error(e);
                    }
                }
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public void Initialize()
        {
            _ = this.InitializeAsync();
        }
        public async Task InitializeAsync()
        {
            Plugin.Logger.Info("InitializeAsync()");
            // Check for multiplayer early to abort if needed: gameplay controllers don't exist in multiplayer until later
            this.gameStatus.scene = "Song";
            // Register event listeners
            // PauseController doesn't exist in multiplayer
            if (this.pauseController != null) {
                // public event Action PauseController#didPauseEvent;
                this.pauseController.didPauseEvent += this.OnGamePause;
                // public event Action PauseController#didResumeEvent;
                this.pauseController.didResumeEvent += this.OnGameResume;
            }
            // public ScoreController#scoreDidChangeEvent<int, int> // score
            this.scoreController.scoreDidChangeEvent += this.OnScoreDidChange;
            // public ScoreController#comboDidChangeEvent<int> // combo
            this._comboController.comboDidChangeEvent += this.OnComboDidChange;
            // public ScoreController#multiplierDidChangeEvent<int, float> // multiplier, progress [0..1]
            this.scoreController.multiplierDidChangeEvent += this.OnMultiplierDidChange;
            this.scoreController.scoringForNoteStartedEvent += this.ScoreController_scoringForNoteStartedEvent;
            this.scoreController.scoringForNoteFinishedEvent += this.ScoreController_scoringForNoteFinishedEvent;
            // public event Action<BeatmapEventData> BeatmapObjectCallbackController#beatmapEventDidTriggerEvent
            //this.beatmapObjectCallbackController.beatmapEventDidTriggerEvent += this.OnBeatmapEventDidTrigger;
            this._eventDataCallbackWrapper = this.beatmapObjectCallbackController.AddBeatmapCallback(0, new BeatmapDataCallback<BasicBeatmapEventData>(this.OnBeatmapEventDidTrigger));
            this._beatmapObjectManager.noteWasSpawnedEvent += this.OnNoteWasSpawnedEvent;
            ScoreControllerHandleWasCutPatch.NoteWasCut += this.OnNoteWasCutEvent;
            this._beatmapObjectManager.noteWasMissedEvent += this.OnNoteWasMissedEvent;
            this.relativeScoreAndImmediateRankCounter.relativeScoreOrImmediateRankDidChangeEvent += this.RelativeScoreAndImmediateRankCounter_relativeScoreOrImmediateRankDidChangeEvent;
            // public event Action GameEnergyCounter#gameEnergyDidReach0Event;
            this.gameEnergyCounter.gameEnergyDidReach0Event += this.OnEnergyDidReach0Event;
            this.gameEnergyCounter.gameEnergyDidChangeEvent += this.OnEnergyChanged;

            if (this.multiplayerLocalActivePlayerFacade != null) {
                this.multiplayerLocalActivePlayerFacade.playerDidFinishEvent += this.OnMultiplayerLevelFinished;
            }
            if (this.levelEndActions != null) {
                this.levelEndActions.levelFinishedEvent += this.OnLevelFinished;
                this.levelEndActions.levelFailedEvent += this.OnLevelFailed;
            }
            var diff = this.gameplayCoreSceneSetupData.difficultyBeatmap;
            var level = diff.level;

            this.gameStatus.partyMode = Gamemode.IsPartyActive;
            this.gameStatus.mode = this.gameplayCoreSceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            this.gameplayModifiers = this.gameplayCoreSceneSetupData.gameplayModifiers;
            var playerSettings = this.gameplayCoreSceneSetupData.playerSpecificSettings;
            var practiceSettings = this.gameplayCoreSceneSetupData.practiceSettings;

            var songSpeedMul = this.gameplayModifiers.songSpeedMul;
            if (practiceSettings != null) {
                songSpeedMul = practiceSettings.songSpeedMul;
            }
            // Generate NoteData to id mappings for backwards compatiblity with <1.12.1
            this._noteToIdMapping.Clear();

            this.lastNoteId = 0;
            var beatmapData = await diff.GetBeatmapDataBasicInfoAsync().ConfigureAwait(true);
            foreach (var note in _beatmapData.allBeatmapDataItems.OfType<NoteData>().OrderBy(x => x.time).ThenBy(x => x.lineIndex).ThenBy(x => x.noteLineLayer).ThenBy(x => x.cutDirection).Select((x, i) => new { note = x, index = i })) {
                this._noteToIdMapping.TryAdd(new NoteDataEntity(note.note, this.gameplayModifiers.noArrows), note.index);
            }
            this.gameStatus.songName = level.songName;
            this.gameStatus.songSubName = level.songSubName;
            this.gameStatus.songAuthorName = level.songAuthorName;
            this.gameStatus.levelAuthorName = level.levelAuthorName;
            this.gameStatus.songBPM = level.beatsPerMinute;
            this.gameStatus.noteJumpSpeed = diff.noteJumpMovementSpeed;
            this.gameStatus.noteJumpStartBeatOffset = diff.noteJumpStartBeatOffset;
            // 13 is "custom_level_" and 40 is the magic number for the length of the SHA-1 hash
            this.gameStatus.songHash = Regex.IsMatch(level.levelID, "^custom_level_[0-9A-F]{40}", RegexOptions.IgnoreCase) && !level.levelID.EndsWith(" WIP") ? level.levelID.Substring(13, 40) : null;
            this.gameStatus.levelId = level.levelID;
            this.gameStatus.songTimeOffset = (long)(level.songTimeOffset * 1000f / songSpeedMul);
            this.gameStatus.length = (long)(level.beatmapLevelData.audioClip.length * 1000f / songSpeedMul);
            this.gameStatus.start = Utility.GetCurrentTime() - (long)(this.audioTimeSource.songTime * 1000f / songSpeedMul);
            if (practiceSettings != null)
                this.gameStatus.start -= (long)(practiceSettings.startSongTime * 1000f / songSpeedMul);
            this.gameStatus.paused = 0;
            this.gameStatus.difficulty = diff.difficulty.Name();
            this.gameStatus.difficultyEnum = Enum.GetName(typeof(BeatmapDifficulty), diff.difficulty);
            this.gameStatus.notesCount = beatmapData.cuttableNotesCount;
            this.gameStatus.bombsCount = beatmapData.bombsCount;
            this.gameStatus.obstaclesCount = beatmapData.obstaclesCount;
            this.gameStatus.environmentName = level.environmentInfo.sceneInfo.sceneName;
            var colorScheme = this.gameplayCoreSceneSetupData.colorScheme ?? new ColorScheme(this.gameplayCoreSceneSetupData.environmentInfo.colorScheme);
            this.gameStatus.colorSaberA = colorScheme.saberAColor;
            this.gameStatus.colorSaberB = colorScheme.saberBColor;
            this.gameStatus.colorEnvironment0 = colorScheme.environmentColor0;
            this.gameStatus.colorEnvironment1 = colorScheme.environmentColor1;
            if (colorScheme.supportsEnvironmentColorBoost) {
                this.gameStatus.colorEnvironmentBoost0 = colorScheme.environmentColor0Boost;
                this.gameStatus.colorEnvironmentBoost1 = colorScheme.environmentColor1Boost;
            }
            this.gameStatus.colorObstacle = colorScheme.obstaclesColor;
            try {
                // From https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
                var sprite = await level.GetCoverImageAsync(CancellationToken.None).ConfigureAwait(true);
                var texture = sprite.texture;
                var active = RenderTexture.active;
                var temporary = RenderTexture.GetTemporary(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear
                );

                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;

                var cover = new Texture2D(texture.width, texture.height);
                cover.ReadPixels(new Rect(0, 0, temporary.width, temporary.height), 0, 0);
                cover.Apply();

                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(temporary);

                this.gameStatus.songCover = System.Convert.ToBase64String(
                    ImageConversion.EncodeToPNG(cover)
                );
            }
            catch (Exception e) {
                Plugin.Logger.Error(e);
                this.gameStatus.songCover = null;
            }

            this.UpdateModMultiplier();
            this.gameStatus.songSpeedMultiplier = songSpeedMul;
            this.gameStatus.batteryLives = this.gameEnergyCounter.batteryLives;

            this.gameStatus.modObstacles = this.gameplayModifiers.enabledObstacleType.ToString();
            this.gameStatus.modInstaFail = this.gameplayModifiers.instaFail;
            this.gameStatus.modNoFail = this.gameplayModifiers.noFailOn0Energy;
            this.gameStatus.modBatteryEnergy = this.gameplayModifiers.energyType == GameplayModifiers.EnergyType.Battery;
            this.gameStatus.modDisappearingArrows = this.gameplayModifiers.disappearingArrows;
            this.gameStatus.modNoBombs = this.gameplayModifiers.noBombs;
            this.gameStatus.modSongSpeed = this.gameplayModifiers.songSpeed.ToString();
            this.gameStatus.modNoArrows = this.gameplayModifiers.noArrows;
            this.gameStatus.modGhostNotes = this.gameplayModifiers.ghostNotes;
            this.gameStatus.modFailOnSaberClash = this.gameplayModifiers.failOnSaberClash;
            this.gameStatus.modStrictAngles = this.gameplayModifiers.strictAngles;
            this.gameStatus.modFastNotes = this.gameplayModifiers.fastNotes;
            this.gameStatus.modSmallNotes = this.gameplayModifiers.smallCubes;
            this.gameStatus.modProMode = this.gameplayModifiers.proMode;
            this.gameStatus.modZenMode = this.gameplayModifiers.zenMode;

            this.gameStatus.staticLights = (diff.difficulty == BeatmapDifficulty.ExpertPlus ? playerSettings.environmentEffectsFilterExpertPlusPreset : playerSettings.environmentEffectsFilterDefaultPreset) != EnvironmentEffectsFilterPreset.AllEffects;
            this.gameStatus.leftHanded = playerSettings.leftHanded;
            this.gameStatus.playerHeight = playerSettings.playerHeight;
            this.gameStatus.sfxVolume = playerSettings.sfxVolume;
            this.gameStatus.reduceDebris = playerSettings.reduceDebris;
            this.gameStatus.noHUD = playerSettings.noTextsAndHuds;
            this.gameStatus.advancedHUD = playerSettings.advancedHud;
            this.gameStatus.autoRestart = playerSettings.autoRestart;
            this.gameStatus.saberTrailIntensity = playerSettings.saberTrailIntensity;
            this.gameStatus.environmentEffects = (diff.difficulty == BeatmapDifficulty.ExpertPlus ? playerSettings.environmentEffectsFilterExpertPlusPreset : playerSettings.environmentEffectsFilterDefaultPreset).ToString();
            this.gameStatus.hideNoteSpawningEffect = playerSettings.hideNoteSpawnEffect;

            this._thread = new Thread(new ThreadStart(() =>
            {
                while (!this.disposedValue) {
                    try {
                        this.UpdateCurrentSongTime();
                    }
                    catch (Exception e) {
                        Plugin.Logger.Error(e);
                    }
                    finally {
                        Thread.Sleep(16);
                    }
                }
            }));
            this._thread.Start();
            this.statusManager.EmitStatusUpdate(ChangedProperty.AllButNoteCut, BeatSaberEvent.SongStart);
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // Unity Method
        private void Update()
        {
            try {
                this.OnObstacleInteraction();
            }
            catch (Exception e) {
                Plugin.Logger.Error(e);
            }
        }
        #endregion
    }
}
