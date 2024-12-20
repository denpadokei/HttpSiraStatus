using System;
using System.ComponentModel;

namespace HttpSiraStatus.Enums
{
    /// <summary>
    /// 拡張メソッド用クラス
    /// </summary>
    public static class EnumExtention
    {
        /// <summary>
        /// <see cref="Enum"/>の<see cref="DescriptionAttribute"/>属性に指定された文字列を取得する拡張メソッドです。
        /// </summary>
        /// <param name="value">文字列を取得したい<see cref="Enum"/></param>
        /// <returns></returns>
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            return Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute
                ? attribute.Description
                : value.ToString();
        }
    }

    /// <summary>
    /// イベントの名称と値を管理します。
    /// </summary>
    public enum BeatSaberEvent
    {
        [Description("other")]
        Other = int.MaxValue,
        [Description("menu")]
        Menu = 0,
        [Description("songStart")]
        SongStart,
        [Description("obstacleEnter")]
        ObstacleEnter,
        [Description("obstacleExit")]
        ObstacleExit,
        [Description("pause")]
        Pause,
        [Description("resume")]
        Resume,
        [Description("bombCut")]
        BombCut,
        [Description("noteSpawned")]
        NoteSpawned,
        [Description("noteCut")]
        NoteCut,
        [Description("noteFullyCut")]
        NoteFullyCut,
        [Description("bombMissed")]
        BombMissed,
        [Description("noteMissed")]
        NoteMissed,
        [Description("scoreChanged")]
        ScoreChanged,
        [Description("finished")]
        Finished,
        [Description("failed")]
        Failed,
        [Description("beatmapEvent")]
        BeatmapEvent,
        [Description("energyChanged")]
        EnergyChanged,
        [Description("softFailed")]
        SoftFailed
    }

    /// <summary>
    /// 変更されたプロパティを管理する列挙型
    /// クラスじゃないのでパフォーマンス面でHTTPStatusより良いはず。
    /// </summary>
    [Flags]
    public enum ChangedProperty
    {
        Game = 1,
        Beatmap = 1 << 1,
        Performance = 1 << 2,
        NoteCut = 1 << 3,
        Mod = 1 << 4,
        BeatmapEvent = 1 << 5,
        Other = 1 << 31,
        AllButNoteCut = Game | Beatmap | Performance | Mod,
        PerformanceAndNoteCut = Performance | NoteCut,
        BeatmapAndPerformanceAndMod = Beatmap | Performance | Mod
    }

    public enum GameModeHeadder
    {
        Unknown,
        [Description("Solo")]
        Solo,
        [Description("Party")]
        Party,
        [Description("Multiplayer")]
        Multiplayer,
    }

    public enum V3BeatmapEventType
    {
        Unknown,
        BPM,
        ColorBoost,
        FloatFx,
        LightColor,
        LightRotation,
        LightTranslation,
        SpawnRotation,
        NJS
    }
}
