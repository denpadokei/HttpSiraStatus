using UnityEngine;

namespace HttpSiraStatus.Models
{
    public class CustomGoodCutScoringElement : GoodCutScoringElement, ICutScoreBufferDidFinishReceiver
    {
        public NoteDataEntity NoteDataEntity { get; } = new NoteDataEntity();
        public Vector3 SaberDir { get; private set; }
        public Vector3 CutPoint { get; private set; }
        public Vector3 CutNormal { get; private set; }
        public int InitialScore { get; private set; }
        public void Init(in NoteCutInfo noteCutInfo, NoteController controller, bool noArrow)
        {

            this.NoteDataEntity.SetData(controller.noteData, noArrow);
            var noteTransform = controller.noteTransform;
            this.SaberDir = noteTransform.InverseTransformDirection(noteCutInfo.saberDir);
            this.CutPoint = noteTransform.InverseTransformPoint(noteCutInfo.cutPoint);
            this.CutNormal = noteTransform.InverseTransformDirection(noteCutInfo.cutNormal);
            base.Init(noteCutInfo);
            this.InitialScore = this.cutScore;
        }

        public override void Reinitialize()
        {
            base.Reinitialize();
            this.InitialScore = -1;
        }

        public new class Pool : ScoringElement.Pool<CustomGoodCutScoringElement>
        {
        }
    }
}
