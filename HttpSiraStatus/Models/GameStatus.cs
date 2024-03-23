using HttpSiraStatus.Enums;
using HttpSiraStatus.Interfaces;
using System;
using UnityEngine;

namespace HttpSiraStatus.Models
{
    internal class GameStatus : IGameStatus
    {
        public string updateCause { get; internal set; }
        public string scene { get; internal set; } = "Menu";
        public bool partyMode { get; internal set; } = false;
        public bool multiplayer { get; internal set; } = false;
        public GameModeHeadder mode { get; internal set; } = GameModeHeadder.Unknown;

        // Beatmap
        public string songName { get; internal set; } = null;
        public string songSubName { get; internal set; } = null;
        public string songAuthorName { get; internal set; } = null;
        public string levelAuthorName { get; internal set; } = null;
        public string[] levelAuthorNamesArray { get; internal set; } = Array.Empty<string>();
        public string[] lighterNamesArray { get; internal set; } = Array.Empty<string>();
        public string songCover { get; internal set; } = null;
        public string songHash { get; internal set; } = null;
        public string levelId { get; internal set; } = null;
        public float songBPM { get; internal set; }
        public float noteJumpSpeed { get; internal set; }
        public float noteJumpStartBeatOffset { get; internal set; }
        public long songTimeOffset { get; internal set; } = 0;
        public long length { get; internal set; } = 0;
        public long start { get; internal set; } = 0;
        public long paused { get; internal set; } = 0;
        public string difficulty { get; internal set; } = null;
        public string difficultyEnum { get; internal set; } = null;
        public string characteristic { get; internal set; } = null;
        public int notesCount { get; internal set; } = 0;
        public int bombsCount { get; internal set; } = 0;
        public int obstaclesCount { get; internal set; } = 0;
        public int maxScore { get; internal set; } = 0;
        public string maxRank { get; internal set; } = "E";
        public string environmentName { get; internal set; } = null;
        public Color? colorSaberA { get; internal set; } = null;
        public Color? colorSaberB { get; internal set; } = null;
        public Color? colorEnvironment0 { get; internal set; } = null;
        public Color? colorEnvironment1 { get; internal set; } = null;
        public Color? colorEnvironmentW { get; internal set; } = null;
        public Color? colorEnvironmentBoost0 { get; internal set; } = null;
        public Color? colorEnvironmentBoost1 { get; internal set; } = null;
        public Color? colorEnvironmentBoostW { get; internal set; } = null;
        public Color? colorObstacle { get; internal set; } = null;

        // Performance
        public int rawScore { get; internal set; } = 0;
        public int score { get; internal set; } = 0;
        public int currentMaxScore { get; internal set; } = 0;
        public string rank { get; internal set; } = "E";
        public int passedNotes { get; internal set; } = 0;
        public int hitNotes { get; internal set; } = 0;
        public int missedNotes { get; internal set; } = 0;
        public int lastNoteScore { get; internal set; } = 0;
        public int passedBombs { get; internal set; } = 0;
        public int hitBombs { get; internal set; } = 0;
        public int combo { get; internal set; } = 0;
        public int maxCombo { get; internal set; } = 0;
        public int multiplier { get; internal set; } = 0;
        public float multiplierProgress { get; internal set; } = 0;
        public int batteryEnergy { get; internal set; } = 1;
        public float energy { get; internal set; } = 0;
        public bool softFailed { get; internal set; } = false;
        public float relativeScore { get; internal set; } = 1;
        public int currentSongTime { get; internal set; } = 0;

        // Mods
        public float modifierMultiplier { get; internal set; } = 1f;
        public string modObstacles { get; internal set; } = "All";
        public bool modInstaFail { get; internal set; } = false;
        public bool modNoFail { get; internal set; } = false;
        public bool modBatteryEnergy { get; internal set; } = false;
        public int batteryLives { get; internal set; } = 1;
        public bool modDisappearingArrows { get; internal set; } = false;
        public bool modNoBombs { get; internal set; } = false;
        public string modSongSpeed { get; internal set; } = "Normal";
        public float songSpeedMultiplier { get; internal set; } = 1f;
        public bool modNoArrows { get; internal set; } = false;
        public bool modGhostNotes { get; internal set; } = false;
        public bool modFailOnSaberClash { get; internal set; } = false;
        public bool modStrictAngles { get; internal set; } = false;
        public bool modFastNotes { get; internal set; } = false;
        public bool modSmallNotes { get; internal set; } = false;
        public bool modProMode { get; internal set; } = false;
        public bool modZenMode { get; internal set; } = false;

        // Player settings
        public bool staticLights { get; internal set; } = false;
        public bool leftHanded { get; internal set; } = false;
        public float playerHeight { get; internal set; } = 1.7f;
        public float sfxVolume { get; internal set; } = 0.7f;
        public bool reduceDebris { get; internal set; } = false;
        public bool noHUD { get; internal set; } = false;
        public bool advancedHUD { get; internal set; } = false;
        public bool autoRestart { get; internal set; } = false;
        public float saberTrailIntensity { get; internal set; } = 0.5f;
        public string environmentEffects { get; internal set; } = EnvironmentEffectsFilterPreset.AllEffects.ToString();
        public bool hideNoteSpawningEffect { get; internal set; } = false;


        public void ResetMapInfo()
        {
            this.songName = null;
            this.songSubName = null;
            this.songAuthorName = null;
            this.levelAuthorName = null;
            this.levelAuthorNamesArray = Array.Empty<string>();
            this.lighterNamesArray = Array.Empty<string>();
            this.songCover = null;
            this.songHash = null;
            this.levelId = null;
            this.songBPM = 0f;
            this.noteJumpSpeed = 0f;
            this.songTimeOffset = 0;
            this.noteJumpStartBeatOffset = 0;
            this.length = 0;
            this.start = 0;
            this.paused = 0;
            this.difficulty = null;
            this.difficultyEnum = null;
            this.characteristic = null;
            this.notesCount = 0;
            this.obstaclesCount = 0;
            this.maxScore = 0;
            this.maxRank = "E";
            this.environmentName = null;
            this.colorSaberA = null;
            this.colorSaberB = null;
            this.colorEnvironment0 = null;
            this.colorEnvironment1 = null;
            this.colorEnvironmentW = null;
            this.colorEnvironmentBoost0 = null;
            this.colorEnvironmentBoost1 = null;
            this.colorEnvironmentBoostW = null;
            this.colorObstacle = null;
        }

        public void ResetPerformance()
        {
            this.rawScore = 0;
            this.score = 0;
            this.currentMaxScore = 0;
            this.rank = "E";
            this.passedNotes = 0;
            this.hitNotes = 0;
            this.missedNotes = 0;
            this.lastNoteScore = 0;
            this.passedBombs = 0;
            this.hitBombs = 0;
            this.combo = 0;
            this.maxCombo = 0;
            this.multiplier = 0;
            this.multiplierProgress = 0;
            this.batteryEnergy = 1;
            this.energy = 0;
            this.softFailed = false;
            this.relativeScore = 1;
            this.currentSongTime = 0;
        }
    }
}
