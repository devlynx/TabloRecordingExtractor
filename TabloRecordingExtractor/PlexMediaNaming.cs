using System;
using System.IO;

namespace TabloRecordingExtractor
{
    public class PlexMediaNaming : IMediaNamingConvention
    {
        public string GetEpisodeDescription(Recording recording)
        {
            return String.Format("{0} - S{1}E{2} - {3}",
                recording.Metadata.recSeries.jsonForClient.title,
                recording.Metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"),
                recording.Metadata.recEpisode.jsonForClient.episodeNumber.ToString("00"),
                recording.Metadata.recEpisode.jsonForClient.title);
        }

        private string Sanitize(string outputFileName)
        {
            string path = Path.GetDirectoryName(outputFileName);
            string fileName = Path.GetFileName(outputFileName);

            char[] invalids = Path.GetInvalidFileNameChars();
            string newFileName = String.Join("_", fileName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return Path.Combine(path, newFileName);
        }

        public string GetEpisodeOutputFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(String.Format("{0}\\TV Shows\\{1}\\Season {2}\\{3}.mp4",
                        outputDirectory,
                        recording.Metadata.recSeries.jsonForClient.title,
                        recording.Metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"),
                        recording.Description));
        }

        public string GetManualDescription(Recording recording)
        {
            return String.Format("{0}", recording.Metadata.recManualProgram.jsonForClient.title);
        }

        public string GetManualOutputFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(String.Format("{0}\\{1}.mp4", outputDirectory, recording.Description));
        }

        public string GetMovieDescription(Recording recording)
        {
            return String.Format("{0} ({1})",
                recording.Metadata.recMovie.jsonForClient.title,
                recording.Metadata.recMovie.jsonForClient.releaseYear);
        }

        public string GetMovieOutputFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(String.Format("{0}\\Movies\\{1}\\{1}.mp4",
                outputDirectory,
                recording.Description));
        }

        public string GetOtherOutupuFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(GetManualOutputFileName(outputDirectory, recording));
        }

        public string GetSportsDescription(Recording recording)
        {
            return String.Format("{0} - {1}",
                recording.Metadata.recSportOrganization.jsonForClient.title,
                recording.Metadata.recSportEvent.jsonForClient.eventTitle);
        }

        public string GetSportsOutputFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(String.Format("{0}\\Sports\\{1}.mp4",
                outputDirectory,
                recording.Description));
        }
    }
}
