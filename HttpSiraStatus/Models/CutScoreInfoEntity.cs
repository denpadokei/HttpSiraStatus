using UnityEngine;

namespace HttpSiraStatus.Models
{
    public class CutScoreInfoEntity
    {
        public int noteID { get; internal set; } = -1;
        public string noteType { get; internal set; } = null;
        public string noteCutDirection { get; internal set; } = null;
        public string sliderHeadCutDirection { get; internal set; } = null;
        public string sliderTailCutDirection { get; internal set; } = null;
        public int noteLine { get; internal set; } = 0;
        public int noteLayer { get; internal set; } = 0;
        public int sliderHeadLine { get; internal set; } = 0;
        public int sliderHeadLayer { get; internal set; } = 0;
        public int sliderTailLine { get; internal set; } = 0;
        public int sliderTailLayer { get; internal set; } = 0;
        public bool speedOK { get; internal set; } = false;
        public bool directionOK { get; internal set; } = false;
        public bool saberTypeOK { get; internal set; } = false;
        public bool wasCutTooSoon { get; internal set; } = false;
        public int initialScore { get; internal set; } = -1;
        public int finalScore { get; internal set; } = -1;
        public int cutDistanceScore { get; internal set; } = -1;
        public int cutMultiplier { get; internal set; } = 0;
        public float saberSpeed { get; internal set; } = 0;
        public Vector3 saberDir { get; internal set; } = Vector3.zero;
        public string saberType { get; internal set; } = null;
        public float swingRating { get; internal set; } = 0;
        public float beforSwingRating { get; internal set; } = 0;
        public float afterSwingRating { get; internal set; } = 0;
        public float timeDeviation { get; internal set; } = 0;
        public float cutDirectionDeviation { get; internal set; } = 0;
        public Vector3 cutPoint { get; internal set; } = Vector3.zero;
        public Vector3 cutNormal { get; internal set; } = Vector3.zero;
        public float cutDistanceToCenter { get; internal set; } = 0;
        public float timeToNextBasicNote { get; internal set; } = 0;
        public string gameplayType { get; internal set; } = "";
        public void ResetNoteCut()
        {
            this.noteID = -1;
            this.noteType = null;
            this.noteCutDirection = null;
            this.sliderHeadCutDirection = null;
            this.sliderTailCutDirection = null;
            this.noteLine = 0;
            this.noteLayer = 0;
            this.sliderHeadLine = 0;
            this.sliderHeadLayer = 0;
            this.sliderTailLine = 0;
            this.sliderTailLayer = 0;
            this.speedOK = false;
            this.directionOK = false;
            this.saberTypeOK = false;
            this.wasCutTooSoon = false;
            this.initialScore = -1;
            this.finalScore = -1;
            this.cutDistanceScore = -1;
            this.cutMultiplier = 0;
            this.saberSpeed = 0;
            this.saberDir = Vector3.zero;
            this.saberType = null;
            this.swingRating = 0;
            this.beforSwingRating = 0;
            this.afterSwingRating = 0;
            this.timeDeviation = 0;
            this.cutDirectionDeviation = 0;
            this.cutPoint = Vector3.zero;
            this.cutNormal = Vector3.zero;
            this.cutDistanceToCenter = 0;
            this.gameplayType = "";
        }
    }
}
