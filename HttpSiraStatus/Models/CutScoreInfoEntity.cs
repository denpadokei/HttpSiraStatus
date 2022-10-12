using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;
using UnityEngine;
using Zenject;

namespace HttpSiraStatus.Models
{
    public class CutScoreInfoEntity : ICutScoreInfoEntity
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
        public float beforeSwingRating { get; internal set; } = 0;
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
            this.beforeSwingRating = 0;
            this.afterSwingRating = 0;
            this.timeDeviation = 0;
            this.cutDirectionDeviation = 0;
            this.cutPoint = Vector3.zero;
            this.cutNormal = Vector3.zero;
            this.cutDistanceToCenter = 0;
            this.gameplayType = "";
        }

        public JSONObject ToJSON()
        {
            var notecut = new JSONObject();

            notecut["noteID"] = this.noteID;
            notecut["noteType"] = this.StringOrNull(this.noteType);
            notecut["noteCutDirection"] = this.StringOrNull(this.noteCutDirection);
            notecut["sliderHeadCutDirection"] = this.StringOrNull(this.sliderHeadCutDirection);
            notecut["sliderTailCutDirection"] = this.StringOrNull(this.sliderTailCutDirection);
            notecut["noteLine"] = this.noteLine;
            notecut["noteLayer"] = this.noteLayer;
            notecut["sliderHeadLine"] = this.sliderHeadLine;
            notecut["sliderHeadLayer"] = this.sliderHeadLayer;
            notecut["sliderTailLine"] = this.sliderTailLine;
            notecut["sliderTailLayer"] = this.sliderTailLayer;
            notecut["speedOK"] = this.speedOK;
            notecut["directionOK"] = this.noteType == "Bomb" ? JSONNull.CreateOrGet() : new JSONBool(this.directionOK);
            notecut["saberTypeOK"] = this.noteType == "Bomb" ? JSONNull.CreateOrGet() : new JSONBool(this.saberTypeOK);
            notecut["wasCutTooSoon"] = this.wasCutTooSoon;
            notecut["initialScore"] = this.initialScore == -1 ? JSONNull.CreateOrGet() : new JSONNumber(this.initialScore);
            notecut["finalScore"] = this.finalScore == -1 ? JSONNull.CreateOrGet() : new JSONNumber(this.finalScore);
            notecut["cutDistanceScore"] = this.cutDistanceScore == -1 ? JSONNull.CreateOrGet() : new JSONNumber(this.cutDistanceScore);
            notecut["swingRating"] = this.swingRating;
            notecut["beforeSwingRating"] = this.beforeSwingRating;
            notecut["afterSwingRating"] = this.afterSwingRating;
            notecut["multiplier"] = this.cutMultiplier;
            notecut["saberSpeed"] = this.saberSpeed;
            notecut["saberDir"] = new JSONArray();
            notecut["saberDir"][0].AsFloat = this.saberDir.x;
            notecut["saberDir"][1].AsFloat = this.saberDir.y;
            notecut["saberDir"][2].AsFloat = this.saberDir.z;
            notecut["saberType"] = this.StringOrNull(this.saberType);
            notecut["timeDeviation"] = this.timeDeviation;
            notecut["cutDirectionDeviation"] = this.cutDirectionDeviation;
            notecut["cutPoint"] = new JSONArray();
            notecut["cutPoint"][0].AsFloat = this.cutPoint.x;
            notecut["cutPoint"][1].AsFloat = this.cutPoint.y;
            notecut["cutPoint"][2].AsFloat = this.cutPoint.z;
            notecut["cutNormal"] = new JSONArray();
            notecut["cutNormal"][0].AsFloat = this.cutNormal.x;
            notecut["cutNormal"][1].AsFloat = this.cutNormal.y;
            notecut["cutNormal"][2].AsFloat = this.cutNormal.z;
            notecut["cutDistanceToCenter"] = this.cutDistanceToCenter;
            notecut["timeToNextBasicNote"] = this.timeToNextBasicNote;
            notecut["gameplayType"] = this.StringOrNull(this.gameplayType);

            return notecut;
        }

        private JSONNode StringOrNull(string str)
        {
            return string.IsNullOrEmpty(str) ? JSONNull.CreateOrGet() : new JSONString(str);
        }

        public class Pool : MemoryPool<CutScoreInfoEntity>
        {
            protected override void OnDespawned(CutScoreInfoEntity item)
            {
                item.ResetNoteCut();
            }
        }
    }
}
