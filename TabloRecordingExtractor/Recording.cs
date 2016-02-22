using System;
using System.Diagnostics;

namespace TabloRecordingExtractor
{
    [DebuggerDisplay("{Type} - {Description}")]
    public class Recording
    {
        public RecordingType Type { get; set; }
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime RecordedOnDate { get; set; }
        public bool IsNotFinished { get; set; }
        public string Plot { get; set; }
        public RecordingMetadata Metadata { get; set; }

        public Recording()
        {
            IsNotFinished = false;
        }
    }


    public enum RecordingType { Episode, Movie, Sports, Manual };
}
