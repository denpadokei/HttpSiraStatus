using UnityEngine;

namespace HttpSiraStatus.Extentions
{
    public static class SaberMovementDataExtentions
    {
        public static float ComputeSwingRatingEx(this ISaberMovementData data)
        {
            return data.ComputeSwingRatingEx(false, 0);
        }

        /// <summary>
        /// BeatLeaderのパッチ除け
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static float ComputeSwingRatingEx(this ISaberMovementData data, bool overrideSegmenAngle, float overrideValue)
        {
            if (data is SaberMovementData saberMovementData) {
                if (saberMovementData._validCount < 2) {
                    return 0f;
                }
                var num = saberMovementData._data.Length;
                var num2 = saberMovementData._nextAddIndex - 1;
                if (num2 < 0) {
                    num2 += num;
                }
                var time = saberMovementData._data[num2].time;
                var num3 = time;
                var num4 = 0f;
                var segmentNormal = saberMovementData._data[num2].segmentNormal;
                var angleDiff = overrideSegmenAngle ? overrideValue : saberMovementData._data[num2].segmentAngle;
                var num5 = 2;
                num4 += SaberSwingRating.BeforeCutStepRating(angleDiff, 0f);
                while (time - num3 < 0.4f && num5 < saberMovementData._validCount) {
                    num2--;
                    if (num2 < 0) {
                        num2 += num;
                    }
                    var segmentNormal2 = saberMovementData._data[num2].segmentNormal;
                    angleDiff = saberMovementData._data[num2].segmentAngle;
                    var num6 = Vector3.Angle(segmentNormal2, segmentNormal);
                    if (num6 > 90f) {
                        break;
                    }
                    num4 += SaberSwingRating.BeforeCutStepRating(angleDiff, num6);
                    num3 = saberMovementData._data[num2].time;
                    num5++;
                }
                if (num4 > 1f) {
                    num4 = 1f;
                }
                return num4;
            }
            else {
                return 0f;
            }
        }
    }
}