using HttpSiraStatus.Enums;
using UnityEngine;

namespace HttpSiraStatus.Interfaces
{
#pragma warning disable IDE1006 // 命名スタイル
    public interface IGameStatus
    {
        bool advancedHUD { get; }
        bool autoRestart { get; }
        int batteryEnergy { get; }
        int batteryLives { get; }
        int bombsCount { get; }
        string characteristic { get; }
        Color? colorEnvironment0 { get; }
        Color? colorEnvironment1 { get; }
        Color? colorEnvironmentBoost0 { get; }
        Color? colorEnvironmentBoost1 { get; }
        Color? colorObstacle { get; }
        Color? colorSaberA { get; }
        Color? colorSaberB { get; }
        int combo { get; }
        int currentMaxScore { get; }
        int currentSongTime { get; }
        string difficulty { get; }
        string difficultyEnum { get; }
        float energy { get; }
        string environmentEffects { get; }
        string environmentName { get; }
        bool hideNoteSpawningEffect { get; }
        int hitBombs { get; }
        int hitNotes { get; }
        int lastNoteScore { get; }
        bool leftHanded { get; }
        long length { get; }
        string levelAuthorName { get; }
        string levelId { get; }
        int maxCombo { get; }
        string maxRank { get; }
        int maxScore { get; }
        int missedNotes { get; }
        bool modBatteryEnergy { get; }
        bool modDisappearingArrows { get; }
        GameModeHeadder mode { get; }
        bool modFailOnSaberClash { get; }
        bool modFastNotes { get; }
        bool modGhostNotes { get; }
        float modifierMultiplier { get; }
        bool modInstaFail { get; }
        bool modNoArrows { get; }
        bool modNoBombs { get; }
        bool modNoFail { get; }
        string modObstacles { get; }
        bool modProMode { get; }
        bool modSmallNotes { get; }
        string modSongSpeed { get; }
        bool modStrictAngles { get; }
        bool modZenMode { get; }
        bool multiplayer { get; }
        int multiplier { get; }
        float multiplierProgress { get; }
        bool noHUD { get; }
        float noteJumpSpeed { get; }
        float noteJumpStartBeatOffset { get; }
        int notesCount { get; }
        int obstaclesCount { get; }
        bool partyMode { get; }
        int passedBombs { get; }
        int passedNotes { get; }
        long paused { get; }
        float playerHeight { get; }
        string rank { get; }
        int rawScore { get; }
        bool reduceDebris { get; }
        float relativeScore { get; }
        float saberTrailIntensity { get; }
        string scene { get; }
        int score { get; }
        float sfxVolume { get; }
        bool softFailed { get; }
        string songAuthorName { get; }
        float songBPM { get; }
        string songCover { get; }
        string songHash { get; }
        string songName { get; }
        float songSpeedMultiplier { get; }
        string songSubName { get; }
        long songTimeOffset { get; }
        long start { get; }
        bool staticLights { get; }
        string updateCause { get; }
    }
}