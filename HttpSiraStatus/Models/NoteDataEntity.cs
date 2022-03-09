using System;
using System.Collections.Generic;
using Zenject;

namespace HttpSiraStatus.Models
{
    /// <summary>
    /// <see cref="Dictionary{TKey, TValue}"/>用にハッシュ値を固定で返すラッパークラス
    /// </summary>
    public record NoteDataEntity
    {
        // C#の命名規則に違反するけど元々のNoteDataをラップするのでここは目をつむる
        public ColorType colorType { get; private set; }
        public NoteCutDirection cutDirection { get; private set; }
        public float timeToNextColorNote { get; private set; }
        public float timeToPrevColorNote { get; private set; }
        public NoteLineLayer noteLineLayer { get; private set; }
        public NoteLineLayer beforeJumpNoteLineLayer { get; private set; }
        public int flipLineIndex { get; private set; }
        public float flipYSide { get; private set; }
        public int executionOrder
        {
            get;
            private set;
        }
        public NoteData.GameplayType gameplayType
        {
            get;
            private set;
        }

        public NoteData.ScoringType scoringType
        {
            get;
            private set;
        }
        public float time { get; private set; }
        public int lineIndex { get; private set; }
        public NoteDataEntity()
        {
        }
        public NoteDataEntity(NoteData note, bool noArrow)
        {
            this.SetData(note, noArrow);
        }

        public void SetData(NoteData note, bool noArrow)
        {
            this.gameplayType = note.gameplayType;
            this.scoringType = note.scoringType;
            this.time = note.time;
            this.executionOrder = note.executionOrder;
            this.lineIndex = note.lineIndex;
            this.colorType = note.colorType;
            this.cutDirection = noArrow ? NoteCutDirection.Any : note.cutDirection;
            this.timeToNextColorNote = note.timeToNextColorNote;
            this.timeToPrevColorNote = note.timeToPrevColorNote;
            this.noteLineLayer = note.noteLineLayer;
            this.beforeJumpNoteLineLayer = note.beforeJumpNoteLineLayer;
            this.flipLineIndex = note.flipLineIndex;
            this.flipYSide = note.flipYSide;
        }

        public class Pool : MemoryPool<NoteData, bool, NoteDataEntity>
        {
            protected override void Reinitialize(NoteData p1, bool p2, NoteDataEntity item)
            {
                item.SetData(p1, p2);
            }
        }
    }
}
