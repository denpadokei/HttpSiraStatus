using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;
using IPA.Utilities;
using SiraUtil.Affinity;
using SiraUtil.Zenject;
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
    public class GamePlayDataManager : MonoBehaviour, IAsyncInitializable, IDisposable, IAffinity
    {
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // パブリックメソッド
        [AffinityPatch(typeof(ScoreController), nameof(ScoreController.HandleNoteWasCut))]
        [AffinityPostfix]
        public void NoteWasCutPostfix(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            if (noteController.noteData.colorType == ColorType.None) {
                this._gameStatus.passedBombs++;
                this._gameStatus.hitBombs++;
            }
            else {
                this._gameStatus.passedNotes++;
                if (noteCutInfo.allIsOK) {
                    this._gameStatus.hitNotes++;
                }
                else {
                    this._gameStatus.missedNotes++;
                }
            }
            if (noteCutInfo.allIsOK) {
                var goodCutScoringElement = this._customGoodCutScoringElementPool.Spawn();
                goodCutScoringElement.Init(noteCutInfo, noteController, this._gameplayModifiers.noArrows);
                this._sortedScoringElementsWithoutMultiplier.InsertIntoSortedListFromEnd(goodCutScoringElement);
                this.ScoreController_scoringForNoteStartedEvent(goodCutScoringElement, noteController.noteData.colorType);
                this._sortedNoteTimesWithoutScoringElements.Remove(noteCutInfo.noteData.time);
            }
            else {
                var badCutScoringElement = this._badCutScoringElementPool.Spawn();
                badCutScoringElement.Init(noteCutInfo, noteController, this._gameplayModifiers.noArrows);
                this._sortedScoringElementsWithoutMultiplier.InsertIntoSortedListFromEnd(badCutScoringElement);
                this.ScoreController_scoringForNoteStartedEvent(badCutScoringElement, noteController.noteData.colorType);
                this._sortedNoteTimesWithoutScoringElements.Remove(noteCutInfo.noteData.time);
            }
        }

        [AffinityPatch(typeof(ScoreController), nameof(ScoreController.HandleNoteWasMissed))]
        [AffinityPostfix]
        public void NoteWasMissedPostfix(NoteController noteController)
        {
            if (noteController.noteData.colorType == ColorType.None) {
                this._gameStatus.passedBombs++;
            }
            else {
                this._gameStatus.passedNotes++;
                this._gameStatus.missedNotes++;
            }
            var noteData = noteController.noteData;
            if (noteData.scoringType == NoteData.ScoringType.Ignore) {
                return;
            }
            var missScoringElement = this._missScoringElementPool.Spawn();
            missScoringElement.Init(noteData);
            this._sortedScoringElementsWithoutMultiplier.InsertIntoSortedListFromEnd(missScoringElement);
            this.ScoreController_scoringForNoteStartedEvent(missScoringElement, noteController.noteData.colorType);
            this._sortedNoteTimesWithoutScoringElements.Remove(noteData.time);
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
            this._gameStatus.relativeScore = this._relativeScoreAndImmediateRankCounter.relativeScore;
            this._gameStatus.rank = RankModel.GetRankName(this._relativeScoreAndImmediateRankCounter.immediateRank);
            this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ScoreChanged);
        }
        private void OnEnergyChanged(float obj)
        {
            this._gameStatus.energy = obj;
            this._gameStatus.batteryEnergy = this._gameplayModifiers.energyType == GameplayModifiers.EnergyType.Bar ? 0 : Mathf.RoundToInt(1f / obj);
            this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.EnergyChanged);
        }
        /// <summary>
        /// こいつ別スレで呼ぶと悲惨なことになるのでUpdateで呼ぶことにしました。
        /// </summary>
        private void OnObstacleInteraction()
        {
            if (this._playerHeadAndObstacleInteraction == null) {
                return;
            }
            // intersectingObstaclesのgetがフレーム単位で呼ばないといけない。
            // 別スレで呼ぶと関係ないとこで他のモジュール群が死ぬ。
            var currentHeadInObstacle = this._playerHeadAndObstacleInteraction.playerHeadIsInObstacle;

            if (!this._headInObstacle && currentHeadInObstacle) {
                this._headInObstacle = true;
                this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ObstacleEnter);
            }
            else if (this._headInObstacle && !currentHeadInObstacle) {
                this._headInObstacle = false;
                this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ObstacleExit);
            }
        }

        private void UpdateCurrentSongTime()
        {
            var songTime = Mathf.FloorToInt(this._audioTimeSource.songTime);
            if (this._gameStatus.currentSongTime != songTime) {
                this._gameStatus.currentSongTime = songTime;
                this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.BeatmapEvent);
            }
        }

        private void UpdateModMultiplier()
        {
            var energy = this._gameEnergyCounter.energy;

            this._gameStatus.modifierMultiplier = this._gameplayModifiersSO.GetTotalMultiplier(this._gameplayModifiersSO.CreateModifierParamsList(this._gameplayModifiers), energy);

            this._gameStatus.maxScore = this._gameplayModifiersSO.MaxModifiedScoreForMaxMultipliedScore(ScoreModel.ComputeMaxMultipliedScoreForBeatmap(this._beatmapData), this._gameplayModifiersSO.CreateModifierParamsList(this._gameplayModifiers), this._gameplayModifiersSO, energy);
            this._gameStatus.maxRank = RankModelHelper.MaxRankForGameplayModifiers(this._gameplayModifiers, this._gameplayModifiersSO, energy).ToString();
        }

        private void OnGamePause()
        {
            this._gameStatus.paused = Utility.GetCurrentTime();

            this._statusManager.EmitStatusUpdate(ChangedProperty.Beatmap, BeatSaberEvent.Pause);
        }

        private void OnGameResume()
        {
            this._gameStatus.start = Utility.GetCurrentTime() - (long)(this._audioTimeSource.songTime * 1000f / this._gameStatus.songSpeedMultiplier);
            this._gameStatus.paused = 0;

            this._statusManager.EmitStatusUpdate(ChangedProperty.Beatmap, BeatSaberEvent.Resume);
        }
        private void OnNoteWasSpawnedEvent(NoteController obj)
        {
            if (obj.noteData.scoringType != NoteData.ScoringType.Ignore) {
                this._sortedNoteTimesWithoutScoringElements.InsertIntoSortedListFromEnd(obj.noteData.time);
            }
            this.SetNoteCutStatus(obj.noteData);
            this._statusManager.EmitStatusUpdate(ChangedProperty.NoteCut, BeatSaberEvent.NoteSpawned);
        }

        private void OnBeatmapObjectManager_sliderWasSpawnedEvent(SliderController obj)
        {
            this.SetNoteCutStatus(obj.sliderData);
            this._statusManager.EmitStatusUpdate(ChangedProperty.NoteCut, BeatSaberEvent.NoteSpawned);
        }
        private void ScoreController_scoringForNoteStartedEvent(ScoringElement obj, ColorType colorType)
        {
            if (obj is CustomGoodCutScoringElement element) {
                var cutScoreBuffer = element.cutScoreBuffer;
                var noteCutInfo = cutScoreBuffer.noteCutInfo;
                var notecut = this.SetNoteCutStatus(element.NoteDataEntity, element.SaberDir, element.CutPoint, element.CutNormal, noteCutInfo);
                notecut.cutDistanceScore = element.cutScoreBuffer.centerDistanceCutScore;
                notecut.initialScore = element.cutScore;
                this._statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteCut);
            }
            else if (obj is MissScoringElement && colorType != ColorType.None) {
                this.SetNoteCutStatus(obj.noteData);
                this._statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteMissed);
            }
            else if (obj is CustomBadCutScoringElement badElement) {
                var notecut = this.SetNoteCutStatus(badElement.NoteDataEntity, badElement.SaberDir, badElement.CutPoint, badElement.CutNormal, badElement.NoteCutInfo);
                notecut.initialScore = badElement.cutScore;
                if (colorType == ColorType.None) {
                    this._statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.BombCut);
                }
                else {
                    this._statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteCut);
                }
            }
        }

        private void ScoreController_scoringForNoteFinishedEvent(ScoringElement obj)
        {
            if (obj is CustomGoodCutScoringElement element) {
                var cutScoreBuffer = element.cutScoreBuffer;
                var noteCutInfo = cutScoreBuffer.noteCutInfo;
                var notecut = this.SetNoteCutStatus(element.NoteDataEntity, element.SaberDir, element.CutPoint, element.CutNormal, noteCutInfo);
                notecut.cutMultiplier = element.multiplier;
                notecut.cutDistanceScore = element.cutScoreBuffer.centerDistanceCutScore;
                notecut.finalScore = element.cutScore;
                this._statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteFullyCut);
            }
            else if (obj is CustomBadCutScoringElement badElement && obj.noteData.colorType != ColorType.None) {
                var notecut = this.SetNoteCutStatus(badElement.NoteDataEntity, badElement.SaberDir, badElement.CutPoint, badElement.CutNormal, badElement.NoteCutInfo);
                notecut.cutMultiplier = badElement.multiplier;
                notecut.finalScore = badElement.cutScore;
                this._statusManager.EmitStatusUpdate(ChangedProperty.PerformanceAndNoteCut, BeatSaberEvent.NoteFullyCut);
            }
        }

        private CutScoreInfoEntity SetNoteCutStatus(BeatmapObjectData obj)
        {
            CutScoreInfoEntity cutScoreInfoEntity = null;
            if (obj is NoteData noteData) {
                var noteDataEntity = this._notePool.Spawn(noteData, this._gameplayModifiers.noArrows);
                cutScoreInfoEntity = this.SetNoteCutStatus(noteDataEntity);
                this._notePool.Despawn(noteDataEntity);
            }
            else if (obj is SliderData sliderData) {
                var noteDataEntity = this._sliderPool.Spawn(sliderData);
                cutScoreInfoEntity = this.SetNoteCutStatus(noteDataEntity);
                this._sliderPool.Despawn(noteDataEntity);
            }
            return cutScoreInfoEntity;
        }

        private CutScoreInfoEntity SetNoteCutStatus(IBeatmapObjectEntity entity, in Vector3 saberDir = default, in Vector3 cutPoint = default, in Vector3 cutNormal = default, in NoteCutInfo noteCutInfo = default)
        {
            var notecut = new CutScoreInfoEntity
            {
                // Backwards compatibility for <1.12.1
                noteID = -1
            };
            // Check the near notes first for performance
            if (this._noteToIdMapping.TryGetValue(entity, out var noteID)) {
                notecut.noteID = noteID;
                if (this._lastNoteId < noteID) {
                    this._lastNoteId = noteID;
                }
            }
            else {
                notecut.noteID = this._lastNoteId;
            }
            // Backwards compatibility for <1.12.1
            string colorName;
            switch (entity.colorType) {
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
                    colorName = entity.colorType.ToString();
                    break;
            }
            notecut.noteType = colorName;
            if (entity is NoteDataEntity noteDataEntity) {
                notecut.noteCutDirection = noteDataEntity.cutDirection.ToString();
                notecut.noteLine = noteDataEntity.lineIndex;
                notecut.noteLayer = (int)noteDataEntity.noteLineLayer;
                // If long notes are ever introduced, this name will make no sense
                notecut.timeToNextBasicNote = noteDataEntity.timeToNextColorNote;
                notecut.gameplayType = noteDataEntity.gameplayType.ToString();
            }
            else if (entity is SliderDataEntity sliderDataEntity) {
                notecut.sliderHeadCutDirection = sliderDataEntity.headCutDirection.ToString();
                notecut.sliderTailCutDirection = sliderDataEntity.tailCutDirection.ToString();
                notecut.sliderHeadLine = sliderDataEntity.headLineIndex;
                notecut.sliderHeadLayer = (int)sliderDataEntity.headLineLayer;
                notecut.sliderTailLine = sliderDataEntity.tailLineIndex;
                notecut.sliderTailLayer = (int)sliderDataEntity.tailLineLayer;
            }
            if (!EqualityComparer<NoteCutInfo>.Default.Equals(noteCutInfo, default)) {
                var noteScoreDefinition = ScoreModel.GetNoteScoreDefinition(noteCutInfo.noteData.scoringType);
                var rateBeforeCut = noteScoreDefinition.maxBeforeCutScore > 0 && noteScoreDefinition.minBeforeCutScore != noteScoreDefinition.maxBeforeCutScore;
                var rateAfterCut = noteScoreDefinition.maxAfterCutScore > 0 && noteScoreDefinition.minAfterCutScore != noteScoreDefinition.maxAfterCutScore;
                notecut.speedOK = noteCutInfo.speedOK;
                notecut.directionOK = noteCutInfo.directionOK;
                notecut.saberTypeOK = noteCutInfo.saberTypeOK;
                notecut.wasCutTooSoon = noteCutInfo.wasCutTooSoon;
                notecut.saberSpeed = noteCutInfo.saberSpeed;
                notecut.saberDir = saberDir;
                var rating = noteCutInfo.saberMovementData?.ComputeSwingRating();
                notecut.swingRating = noteCutInfo.saberMovementData == null ? -1 : rating.Value;
                notecut.afterSwingRating = rateAfterCut ? 0 : 1;
                notecut.beforSwingRating = rateAfterCut ? rating.Value : 1;
                notecut.saberType = noteCutInfo.saberType.ToString();
                notecut.timeDeviation = noteCutInfo.timeDeviation;
                notecut.cutDirectionDeviation = noteCutInfo.cutDirDeviation;
                notecut.cutPoint = cutPoint;
                notecut.cutNormal = cutNormal;
                notecut.cutDistanceToCenter = noteCutInfo.cutDistanceToCenter;
            }
            this._statusManager.CutScoreInfoQueue.Enqueue(notecut);
            return notecut;
        }

        private void OnScoreDidChange(int scoreBeforeMultiplier, int scoreAfterMultiplier)
        {
            this._gameStatus.rawScore = scoreBeforeMultiplier;
            this._gameStatus.score = scoreAfterMultiplier;
            this._gameStatus.currentMaxScore = this._scoreController.immediateMaxPossibleModifiedScore;
            this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ScoreChanged);
        }
        private void OnComboDidChange(int combo)
        {
            this._gameStatus.combo = combo;
            // public int ScoreController#maxCombo
            this._gameStatus.maxCombo = (this._comboController as ComboController).maxCombo;
            this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ScoreChanged);
        }

        private void OnMultiplierDidChange(int multiplier, float multiplierProgress)
        {
            this._gameStatus.multiplier = multiplier;
            this._gameStatus.multiplierProgress = multiplierProgress;
            this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.ScoreChanged);
        }

        private void OnLevelFinished()
        {
            this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.Finished);
        }

        private void OnLevelFailed()
        {
            this._statusManager.EmitStatusUpdate(ChangedProperty.Performance, BeatSaberEvent.Failed);
        }

        private void OnEnergyDidReach0Event()
        {
            if (this._gameStatus.modNoFail) {
                this._gameStatus.softFailed = true;
                this.UpdateModMultiplier();
                this._statusManager.EmitStatusUpdate(ChangedProperty.BeatmapAndPerformanceAndMod, BeatSaberEvent.SoftFailed);
            }
        }

        private void OnBeatmapEventDidTrigger(BeatmapEventData beatmapEventData)
        {
            IBeatmapEventInformation info;
            switch (beatmapEventData) {
                case BasicBeatmapEventData basic:
                    // V2 map
                    info = this._v2Pool.Spawn();
                    info.Init(basic);
                    this._statusManager.BeatmapEventJSON.Enqueue(info);
                    break;
                case BPMChangeBeatmapEventData bpm:
                case ColorBoostBeatmapEventData color:
                case LightColorBeatmapEventData lightColor:
                case LightRotationBeatmapEventData lightRotation:
                case SpawnRotationBeatmapEventData spawn:
                default:
                    info = this._v3Pool.Spawn();
                    info.Init(beatmapEventData);
                    this._statusManager.BeatmapEventJSON.Enqueue(info);
                    break;
            }
            this._statusManager.EmitStatusUpdate(ChangedProperty.BeatmapEvent, BeatSaberEvent.BeatmapEvent);
        }

        public void DespawnScoringElement(ScoringElement scoringElement)
        {
            switch (scoringElement) {
                case CustomGoodCutScoringElement good:
                    this._customGoodCutScoringElementPool.Despawn(good);
                    break;
                case CustomBadCutScoringElement bad:
                    this._badCutScoringElementPool.Despawn(bad);
                    break;
                case MissScoringElement miss:
                    this._missScoringElementPool.Despawn(miss);
                    break;
                default:
                    break;
            }
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // メンバ変数
        private IStatusManager _statusManager;
        private GameStatus _gameStatus;
        private MemoryPoolContainer<CustomGoodCutScoringElement> _customGoodCutScoringElementPool;
        private MemoryPoolContainer<CustomBadCutScoringElement> _badCutScoringElementPool;
        private MemoryPoolContainer<MissScoringElement> _missScoringElementPool;
        private NoteDataEntity.Pool _notePool;
        private SliderDataEntity.Pool _sliderPool;
        private V2BeatmapEventInfomation.Pool _v2Pool;
        private V3BeatmapEventInfomation.Pool _v3Pool;
        private GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private PauseController _pauseController;
        private IScoreController _scoreController;
        private IComboController _comboController;
        private GameplayModifiers _gameplayModifiers;
        private IAudioTimeSource _audioTimeSource;
        private BeatmapCallbacksController _beatmapObjectCallbackController;
        private PlayerHeadAndObstacleInteraction _playerHeadAndObstacleInteraction;
        private GameEnergyCounter _gameEnergyCounter;
        private MultiplayerLocalActivePlayerFacade _multiplayerLocalActivePlayerFacade;
        private RelativeScoreAndImmediateRankCounter _relativeScoreAndImmediateRankCounter;
        private BeatmapObjectManager _beatmapObjectManager;
        private ILevelEndActions _levelEndActions;
        private GameplayModifiersModelSO _gameplayModifiersSO;
        private bool _headInObstacle = false;
        private Thread _thread;
        private int _lastNoteId = 0;
        private bool _disposedValue;
        private BeatmapDataCallbackWrapper _eventDataCallbackWrapper;
        private IReadonlyBeatmapData _beatmapData;
        private List<GameplayModifierParamsSO> _gameplayModifierParams;
        private readonly List<ScoringElement> _sortedScoringElementsWithoutMultiplier = new List<ScoringElement>(50);
        private readonly List<ScoringElement> _scoringElementsWithMultiplier = new List<ScoringElement>(50);
        private readonly Queue<ScoringElement> _scoringElementsToRemove = new Queue<ScoringElement>(50);
        private readonly List<float> _sortedNoteTimesWithoutScoringElements = new List<float>(50);
        private readonly ScoreMultiplierCounter _scoreMultiplierCounter = new ScoreMultiplierCounter();
        private readonly ScoreMultiplierCounter _maxScoreMultiplierCounter = new ScoreMultiplierCounter();
        /// <summary>
        /// Beat Saber 1.12.1 removes NoteData.id, forcing us to generate our own note IDs to allow users to easily link events about the same note.
        /// Before 1.12.1 the noteID matched the note order in the beatmap file, but this is impossible to replicate now without hooking into the level loading code.
        /// </summary>
        private readonly ConcurrentDictionary<IBeatmapObjectEntity, int> _noteToIdMapping = new ConcurrentDictionary<IBeatmapObjectEntity, int>();
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // 構築・破棄
        /// <summary>
        /// 引数やっば
        /// </summary>
        /// <param name="statusManager"></param>
        /// <param name="gameStatus"></param>
        /// <param name="customCutBufferPool"></param>
        /// <param name="badCutScoringElementPool"></param>
        /// <param name="missScoringElementPool"></param>
        /// <param name="noteDataEntityPool"></param>
        /// <param name="sliderDataEntityPool"></param>
        /// <param name="gameplayCoreSceneSetupData"></param>
        /// <param name="score"></param>
        /// <param name="comboController"></param>
        /// <param name="gameplayModifiers"></param>
        /// <param name="audioTimeSource"></param>
        /// <param name="readonlyBeatmapData"></param>
        /// <param name="beatmapObjectCallbackController"></param>
        /// <param name="playerHeadAndObstacleInteraction"></param>
        /// <param name="gameEnergyCounter"></param>
        /// <param name="relative"></param>
        /// <param name="beatmapObjectManager"></param>
        /// <param name="diContainer"></param>
        [Inject]
        private void Constractor(
            IStatusManager statusManager,
            GameStatus gameStatus,
            CustomGoodCutScoringElement.Pool customCutBufferPool,
            CustomBadCutScoringElement.Pool badCutScoringElementPool,
            MissScoringElement.Pool missScoringElementPool,
            NoteDataEntity.Pool noteDataEntityPool,
            SliderDataEntity.Pool sliderDataEntityPool,
            V2BeatmapEventInfomation.Pool v2BeatmapEventInfomationPool,
            V3BeatmapEventInfomation.Pool v3BeatmapEventInfomationPool,
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
            this._statusManager = statusManager;
            this._gameStatus = gameStatus;
            this._customGoodCutScoringElementPool = new MemoryPoolContainer<CustomGoodCutScoringElement>(customCutBufferPool);
            this._badCutScoringElementPool = new MemoryPoolContainer<CustomBadCutScoringElement>(badCutScoringElementPool);
            this._missScoringElementPool = new MemoryPoolContainer<MissScoringElement>(missScoringElementPool);
            this._notePool = noteDataEntityPool;
            this._sliderPool = sliderDataEntityPool;
            this._v2Pool = v2BeatmapEventInfomationPool;
            this._v3Pool = v3BeatmapEventInfomationPool;
            this._gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            this._scoreController = score;
            this._gameplayModifiers = gameplayModifiers;
            this._audioTimeSource = audioTimeSource;
            this._beatmapObjectCallbackController = beatmapObjectCallbackController;
            this._playerHeadAndObstacleInteraction = playerHeadAndObstacleInteraction;
            this._gameEnergyCounter = gameEnergyCounter;
            this._relativeScoreAndImmediateRankCounter = relative;
            this._beatmapObjectManager = beatmapObjectManager;
            this._comboController = comboController;
            this._beatmapData = readonlyBeatmapData;
            if (this._scoreController is ScoreController scoreController) {
                this._gameplayModifiersSO = scoreController.GetField<GameplayModifiersModelSO, ScoreController>("_gameplayModifiersModel");
            }
            this._pauseController = diContainer.TryResolve<PauseController>();
            this._levelEndActions = diContainer.TryResolve<ILevelEndActions>();
            this._multiplayerLocalActivePlayerFacade = diContainer.TryResolve<MultiplayerLocalActivePlayerFacade>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue) {
                if (disposing) {
                    Plugin.Logger.Debug("dispose call");
                    try {
                        this._gameStatus.scene = "Menu"; // XXX: impossible because multiplayerController is always cleaned up before this

                        this._gameStatus?.ResetMapInfo();

                        this._gameStatus?.ResetPerformance();

                        // Clear note id mappings.
                        this._noteToIdMapping?.Clear();

                        this._statusManager?.EmitStatusUpdate(ChangedProperty.AllButNoteCut, BeatSaberEvent.Menu);

                        if (this._pauseController != null) {
                            this._pauseController.didPauseEvent -= this.OnGamePause;
                            this._pauseController.didResumeEvent -= this.OnGameResume;
                        }

                        if (this._scoreController != null) {
                            this._scoreController.scoreDidChangeEvent -= this.OnScoreDidChange;
                            this._scoreController.multiplierDidChangeEvent -= this.OnMultiplierDidChange;
                        }
                        if (this._comboController != null) {
                            this._comboController.comboDidChangeEvent -= this.OnComboDidChange;
                        }

                        if (this._multiplayerLocalActivePlayerFacade != null) {
                            this._multiplayerLocalActivePlayerFacade.playerDidFinishEvent -= this.OnMultiplayerLevelFinished;
                            this._multiplayerLocalActivePlayerFacade = null;
                        }

                        if (this._levelEndActions != null) {
                            this._levelEndActions.levelFinishedEvent -= this.OnLevelFinished;
                            this._levelEndActions.levelFailedEvent -= this.OnLevelFailed;
                        }

                        if (this._beatmapObjectCallbackController != null) {
                            this._beatmapObjectCallbackController.RemoveBeatmapCallback(this._eventDataCallbackWrapper);
                            //this.beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= this.OnBeatmapEventDidTrigger;
                        }

                        if (this._beatmapObjectManager != null) {
                            this._beatmapObjectManager.noteWasSpawnedEvent -= this.OnNoteWasSpawnedEvent;
                            this._beatmapObjectManager.sliderWasSpawnedEvent -= this.OnBeatmapObjectManager_sliderWasSpawnedEvent;
                        }

                        if (this._gameEnergyCounter != null) {
                            this._gameEnergyCounter.gameEnergyDidChangeEvent -= this.OnEnergyChanged;
                            this._gameEnergyCounter.gameEnergyDidReach0Event -= this.OnEnergyDidReach0Event;
                        }
                        if (this._relativeScoreAndImmediateRankCounter) {
                            this._relativeScoreAndImmediateRankCounter.relativeScoreOrImmediateRankDidChangeEvent -= this.RelativeScoreAndImmediateRankCounter_relativeScoreOrImmediateRankDidChangeEvent;
                        }
                    }
                    catch (Exception e) {
                        Plugin.Logger.Error(e);
                    }
                }
                this._disposedValue = true;
            }
        }

        ~GamePlayDataManager()
        {
            this.Dispose(disposing: false);
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            Plugin.Logger.Info("InitializeAsync()");
            // Check for multiplayer early to abort if needed: gameplay controllers don't exist in multiplayer until later
            this._gameStatus.scene = "Song";
            // Register event listeners
            // PauseController doesn't exist in multiplayer
            if (this._pauseController != null) {
                // public event Action PauseController#didPauseEvent;
                this._pauseController.didPauseEvent += this.OnGamePause;
                // public event Action PauseController#didResumeEvent;
                this._pauseController.didResumeEvent += this.OnGameResume;
            }
            // public ScoreController#scoreDidChangeEvent<int, int> // score
            this._scoreController.scoreDidChangeEvent += this.OnScoreDidChange;
            // public ScoreController#comboDidChangeEvent<int> // combo
            this._comboController.comboDidChangeEvent += this.OnComboDidChange;
            // public ScoreController#multiplierDidChangeEvent<int, float> // multiplier, progress [0..1]
            this._scoreController.multiplierDidChangeEvent += this.OnMultiplierDidChange;
            // public event Action<BeatmapEventData> BeatmapObjectCallbackController#beatmapEventDidTriggerEvent
            //this.beatmapObjectCallbackController.beatmapEventDidTriggerEvent += this.OnBeatmapEventDidTrigger;
            this._eventDataCallbackWrapper = this._beatmapObjectCallbackController.AddBeatmapCallback(new BeatmapDataCallback<BeatmapEventData>(this.OnBeatmapEventDidTrigger));
            this._eventDataCallbackWrapper = this._beatmapObjectCallbackController.AddBeatmapCallback(new BeatmapDataCallback<BasicBeatmapEventData>(this.OnBeatmapEventDidTrigger));
            this._beatmapObjectManager.noteWasSpawnedEvent += this.OnNoteWasSpawnedEvent;
            this._beatmapObjectManager.sliderWasSpawnedEvent += this.OnBeatmapObjectManager_sliderWasSpawnedEvent;

            this._relativeScoreAndImmediateRankCounter.relativeScoreOrImmediateRankDidChangeEvent += this.RelativeScoreAndImmediateRankCounter_relativeScoreOrImmediateRankDidChangeEvent;
            // public event Action GameEnergyCounter#gameEnergyDidReach0Event;
            this._gameEnergyCounter.gameEnergyDidReach0Event += this.OnEnergyDidReach0Event;
            this._gameEnergyCounter.gameEnergyDidChangeEvent += this.OnEnergyChanged;

            if (this._multiplayerLocalActivePlayerFacade != null) {
                this._multiplayerLocalActivePlayerFacade.playerDidFinishEvent += this.OnMultiplayerLevelFinished;
            }
            if (this._levelEndActions != null) {
                this._levelEndActions.levelFinishedEvent += this.OnLevelFinished;
                this._levelEndActions.levelFailedEvent += this.OnLevelFailed;
            }
            var diff = this._gameplayCoreSceneSetupData.difficultyBeatmap;
            var level = diff.level;

            this._gameplayModifiers = this._gameplayCoreSceneSetupData.gameplayModifiers;
            var playerSettings = this._gameplayCoreSceneSetupData.playerSpecificSettings;
            var practiceSettings = this._gameplayCoreSceneSetupData.practiceSettings;

            var songSpeedMul = this._gameplayModifiers.songSpeedMul;
            if (practiceSettings != null) {
                songSpeedMul = practiceSettings.songSpeedMul;
            }
            // Generate NoteData to id mappings for backwards compatiblity with <1.12.1
            this._noteToIdMapping.Clear();

            this._lastNoteId = 0;
            var beatmapData = await diff.GetBeatmapDataBasicInfoAsync().ConfigureAwait(true);
            foreach (var note in this._beatmapData.allBeatmapDataItems.Where(x => x is NoteData || x is SliderData).OrderBy(x => x.time).Select((x, i) => (x, i))) {
                if (note.x is NoteData noteData) {
                    if (!this._noteToIdMapping.TryAdd(new NoteDataEntity(noteData, this._gameplayModifiers.noArrows), note.i)) {
                        Plugin.Logger.Warn($"Dupulicate NoteData. Can't create NoteDataEntity. noteID{note.i}");
                        Plugin.Logger.Warn($"{note.x}");
                    }
                }
                else if (note.x is SliderData sliderData) {
                    if (!this._noteToIdMapping.TryAdd(new SliderDataEntity(sliderData), note.i)) {
                        Plugin.Logger.Warn($"Dupulicate SliderData. Can't create SliderDataEntity. noteID{note.i}");
                        Plugin.Logger.Warn($"{note.x}");
                    }
                }
            }
            this._gameStatus.songName = level.songName;
            this._gameStatus.songSubName = level.songSubName;
            this._gameStatus.songAuthorName = level.songAuthorName;
            this._gameStatus.levelAuthorName = level.levelAuthorName;
            this._gameStatus.songBPM = level.beatsPerMinute;
            this._gameStatus.noteJumpSpeed = diff.noteJumpMovementSpeed;
            this._gameStatus.noteJumpStartBeatOffset = diff.noteJumpStartBeatOffset;
            // 13 is "custom_level_" and 40 is the magic number for the length of the SHA-1 hash
            this._gameStatus.songHash = Regex.IsMatch(level.levelID, "^custom_level_[0-9A-F]{40}", RegexOptions.IgnoreCase) && !level.levelID.EndsWith(" WIP") ? level.levelID.Substring(13, 40) : null;
            this._gameStatus.levelId = level.levelID;
            this._gameStatus.songTimeOffset = (long)(level.songTimeOffset * 1000f / songSpeedMul);
            this._gameStatus.length = (long)(level.beatmapLevelData.audioClip.length * 1000f / songSpeedMul);
            this._gameStatus.start = Utility.GetCurrentTime() - (long)(this._audioTimeSource.songTime * 1000f / songSpeedMul);
            if (practiceSettings != null) {
                this._gameStatus.start -= (long)(practiceSettings.startSongTime * 1000f / songSpeedMul);
            }

            this._gameStatus.paused = 0;
            this._gameStatus.difficulty = diff.difficulty.Name();
            this._gameStatus.difficultyEnum = Enum.GetName(typeof(BeatmapDifficulty), diff.difficulty);
            this._gameStatus.characteristic = diff.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            this._gameStatus.notesCount = beatmapData.cuttableNotesCount;
            this._gameStatus.bombsCount = beatmapData.bombsCount;
            this._gameStatus.obstaclesCount = beatmapData.obstaclesCount;
            this._gameStatus.environmentName = level.environmentInfo.sceneInfo.sceneName;
            var colorScheme = this._gameplayCoreSceneSetupData.colorScheme ?? new ColorScheme(this._gameplayCoreSceneSetupData.environmentInfo.colorScheme);
            this._gameStatus.colorSaberA = colorScheme.saberAColor;
            this._gameStatus.colorSaberB = colorScheme.saberBColor;
            this._gameStatus.colorEnvironment0 = colorScheme.environmentColor0;
            this._gameStatus.colorEnvironment1 = colorScheme.environmentColor1;
            if (colorScheme.supportsEnvironmentColorBoost) {
                this._gameStatus.colorEnvironmentBoost0 = colorScheme.environmentColor0Boost;
                this._gameStatus.colorEnvironmentBoost1 = colorScheme.environmentColor1Boost;
            }
            this._gameStatus.colorObstacle = colorScheme.obstaclesColor;
            this._gameplayModifierParams = this._gameplayModifiersSO.CreateModifierParamsList(this._gameplayModifiers);
            try {
                // From https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
                // Modified to correctly handle texture atlases. Fixes #82.
                var active = RenderTexture.active;

                var sprite = await level.GetCoverImageAsync(CancellationToken.None);
                var texture = sprite.texture;
                var temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;

                var spriteRect = sprite.rect;
                var uv = sprite.uv[0];

                var cover = new Texture2D((int)spriteRect.width, (int)spriteRect.height);
                // Unity sucks. The coordinates of the sprite on its texture atlas are only accessible through the Sprite.uv property since rect always returns `x=0,y=0`, so we need to convert them back into texture space.
                cover.ReadPixels(new Rect(
                    uv.x * texture.width,
                    texture.height - uv.y * texture.height,
                    spriteRect.width,
                    spriteRect.height
                ), 0, 0);
                cover.Apply();

                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(temporary);

                this._gameStatus.songCover = Convert.ToBase64String(ImageConversion.EncodeToPNG(cover));
            }
            catch (Exception e) {
                Plugin.Logger.Error(e);
                this._gameStatus.songCover = null;
            }

            this.UpdateModMultiplier();
            this._gameStatus.songSpeedMultiplier = songSpeedMul;
            this._gameStatus.batteryLives = this._gameEnergyCounter.batteryLives;

            this._gameStatus.modObstacles = this._gameplayModifiers.enabledObstacleType.ToString();
            this._gameStatus.modInstaFail = this._gameplayModifiers.instaFail;
            this._gameStatus.modNoFail = this._gameplayModifiers.noFailOn0Energy;
            this._gameStatus.modBatteryEnergy = this._gameplayModifiers.energyType == GameplayModifiers.EnergyType.Battery;
            this._gameStatus.modDisappearingArrows = this._gameplayModifiers.disappearingArrows;
            this._gameStatus.modNoBombs = this._gameplayModifiers.noBombs;
            this._gameStatus.modSongSpeed = this._gameplayModifiers.songSpeed.ToString();
            this._gameStatus.modNoArrows = this._gameplayModifiers.noArrows;
            this._gameStatus.modGhostNotes = this._gameplayModifiers.ghostNotes;
            this._gameStatus.modFailOnSaberClash = this._gameplayModifiers.failOnSaberClash;
            this._gameStatus.modStrictAngles = this._gameplayModifiers.strictAngles;
            this._gameStatus.modFastNotes = this._gameplayModifiers.fastNotes;
            this._gameStatus.modSmallNotes = this._gameplayModifiers.smallCubes;
            this._gameStatus.modProMode = this._gameplayModifiers.proMode;
            this._gameStatus.modZenMode = this._gameplayModifiers.zenMode;

            this._gameStatus.staticLights = (diff.difficulty == BeatmapDifficulty.ExpertPlus ? playerSettings.environmentEffectsFilterExpertPlusPreset : playerSettings.environmentEffectsFilterDefaultPreset) != EnvironmentEffectsFilterPreset.AllEffects;
            this._gameStatus.leftHanded = playerSettings.leftHanded;
            this._gameStatus.playerHeight = playerSettings.playerHeight;
            this._gameStatus.sfxVolume = playerSettings.sfxVolume;
            this._gameStatus.reduceDebris = playerSettings.reduceDebris;
            this._gameStatus.noHUD = playerSettings.noTextsAndHuds;
            this._gameStatus.advancedHUD = playerSettings.advancedHud;
            this._gameStatus.autoRestart = playerSettings.autoRestart;
            this._gameStatus.saberTrailIntensity = playerSettings.saberTrailIntensity;
            this._gameStatus.environmentEffects = (diff.difficulty == BeatmapDifficulty.ExpertPlus ? playerSettings.environmentEffectsFilterExpertPlusPreset : playerSettings.environmentEffectsFilterDefaultPreset).ToString();
            this._gameStatus.hideNoteSpawningEffect = playerSettings.hideNoteSpawnEffect;

            this._thread = new Thread(new ThreadStart(() =>
            {
                while (!this._disposedValue) {
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
            this._statusManager.EmitStatusUpdate(ChangedProperty.AllButNoteCut, BeatSaberEvent.SongStart);
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

        private void LateUpdate()
        {
            var lastProcessedElementTime = this._sortedNoteTimesWithoutScoringElements.Any() ? this._sortedNoteTimesWithoutScoringElements[0] : float.MaxValue;
            var limitSongTime = this._audioTimeSource.songTime + 0.15f;
            var removeElementCount = 0;

            foreach (var scoringElement in this._sortedScoringElementsWithoutMultiplier) {
                if (limitSongTime <= scoringElement.time && scoringElement.time <= lastProcessedElementTime) {
                    break;
                }
                this._scoreMultiplierCounter.ProcessMultiplierEvent(scoringElement.multiplierEventType);
                if (scoringElement.wouldBeCorrectCutBestPossibleMultiplierEventType == ScoreMultiplierCounter.MultiplierEventType.Positive) {
                    this._maxScoreMultiplierCounter.ProcessMultiplierEvent(ScoreMultiplierCounter.MultiplierEventType.Positive);
                }
                scoringElement.SetMultipliers(this._scoreMultiplierCounter.multiplier, this._maxScoreMultiplierCounter.multiplier);
                this._scoringElementsWithMultiplier.Add(scoringElement);
                removeElementCount++;
            }
            this._sortedScoringElementsWithoutMultiplier.RemoveRange(0, removeElementCount);
            foreach (var scoringElement2 in this._scoringElementsWithMultiplier) {
                if (scoringElement2.isFinished) {
                    this._scoringElementsToRemove.Enqueue(scoringElement2);
                    this.ScoreController_scoringForNoteFinishedEvent(scoringElement2);
                }
            }
            while (this._scoringElementsToRemove.Any()) {
                var scoringElement3 = this._scoringElementsToRemove.Dequeue();
                this._scoringElementsWithMultiplier.Remove(scoringElement3);
                this.DespawnScoringElement(scoringElement3);
            }
        }
        #endregion
    }
}
