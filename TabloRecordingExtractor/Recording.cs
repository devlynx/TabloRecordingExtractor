using System;

namespace TabloRecordingExtractor
{
    public class Recording
    {
        public RecordingType Type { get; set; }
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime RecordedOnDate { get; set; }
    }

    public enum RecordingType { Episode, Movie, Manual };
}
