#pragma warning disable IDE1006 // 命名スタイル
using UnityEngine;

namespace HttpSiraStatus.Interfaces
{
    public interface ICutScoreInfoEntity
    {
        float afterSwingRating { get; }
        float beforeSwingRating { get; }
        float afterCutScore { get; }
        float beforeCutScore { get; }
        float cutDirectionDeviation { get; }
        int cutDistanceScore { get; }
        float cutDistanceToCenter { get; }
        int cutMultiplier { get; }
        Vector3 cutNormal { get; }
        Vector3 cutPoint { get; }
        bool directionOK { get; }
        int finalScore { get; }
        string gameplayType { get; }
        int initialScore { get; }
        string noteCutDirection { get; }
        int noteID { get; }
        int noteLayer { get; }
        int noteLine { get; }
        string noteType { get; }
        Vector3 saberDir { get; }
        float saberSpeed { get; }
        string saberType { get; }
        bool saberTypeOK { get; }
        string sliderHeadCutDirection { get; }
        int sliderHeadLayer { get; }
        int sliderHeadLine { get; }
        string sliderTailCutDirection { get; }
        int sliderTailLayer { get; }
        int sliderTailLine { get; }
        bool speedOK { get; }
        float swingRating { get; }
        float timeDeviation { get; }
        float timeToNextBasicNote { get; }
        bool wasCutTooSoon { get; }

        void ResetNoteCut();
    }
}