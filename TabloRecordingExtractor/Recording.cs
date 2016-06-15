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
        public bool HasFinishedRecording { get; set; }
        public string Plot { get; set; }
        public RecordingMetadata Metadata { get; set; }

        public bool IsS00E00
        {
            get
            {
                return Type == RecordingType.Episode
                    && IsS00
                    && Metadata.recEpisode.jsonForClient.episodeNumber == 0;
            }
        }

        public bool IsS00
        {
            get
            {
                return Type == RecordingType.Episode
                    && Metadata.recEpisode.jsonForClient.seasonNumber == 0;
            }
        }

        public Recording()
        {
            HasFinishedRecording = true;
        }

        public override string ToString()
        {
            return String.Format("Desc: {0} -- Type: {1} -- ID: {2}", Description, Type, Id);
        }
    }

    public enum RecordingType { Episode, Movie, Sports, Manual };
}
