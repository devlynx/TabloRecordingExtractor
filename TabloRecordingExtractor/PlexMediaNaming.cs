namespace TabloRecordingExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class PlexMediaNaming : IMediaNamingConvention
    {
        private const string S00EEpisodeNumberRegEx = @".* - S00E(?<EpisodeNumber>[\d]{2,5})";

        public string GetEpisodeDescription(Recording recording)
        {
            return GetEpisodeDescription(recording, recording.Metadata.recEpisode.jsonForClient.episodeNumber);
        }

        private string GetEpisodeDescription(Recording recording, int episodeNumber)
        {
            if (String.IsNullOrWhiteSpace(recording.Metadata.recEpisode.jsonForClient.title))
                return String.Format("{0} - S{1}E{2}",
                recording.Metadata.recSeries.jsonForClient.title,
                recording.Metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"),
                episodeNumber.ToString("00"));

            return String.Format("{0} - S{1}E{2} - {3}",
                recording.Metadata.recSeries.jsonForClient.title,
                recording.Metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"),
                episodeNumber.ToString("00"),
                recording.Metadata.recEpisode.jsonForClient.title);
        }

        private object SanitizeDescription(string title)
        {
            string result = title.Replace(":", String.Empty);
            result = result.Replace("?", String.Empty);
            result = result.Replace("*", String.Empty);

            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach (char c in invalidChars)
            {
                result = result.Replace(c.ToString(), String.Empty);
            }

            return result;
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
            return Sanitize(String.Format("{0}\\{1}.mp4",
                GetEpisodeOutputFolderName(outputDirectory, recording),
                SanitizeDescription(recording.Description)));
        }

        private string GetEpisodeOutputFolderName(string outputDirectory, Recording recording)
        {
            return Sanitize(String.Format("{0}\\TV Shows\\{1}\\Season {2}",
                        outputDirectory,
                        SanitizeDescription(recording.Metadata.recSeries.jsonForClient.title),
                        recording.Metadata.recEpisode.jsonForClient.seasonNumber.ToString("00")));
        }

        public string GetManualDescription(Recording recording)
        {
            return String.Format("{0}", SanitizeDescription(recording.Metadata.recManualProgram.jsonForClient.title));
        }

        public string GetManualOutputFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(String.Format("{0}\\{1}.mp4", outputDirectory, SanitizeDescription(recording.Description)));
        }

        public string GetMovieDescription(Recording recording)
        {
            return String.Format("{0} ({1})",
                SanitizeDescription(recording.Metadata.recMovie.jsonForClient.title),
                recording.Metadata.recMovie.jsonForClient.releaseYear);
        }

        public string GetMovieOutputFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(String.Format("{0}\\Movies\\{1}\\{1}.mp4",
                outputDirectory,
                SanitizeDescription(recording.Description)));
        }

        public string GetOtherOutputFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(GetManualOutputFileName(outputDirectory, recording));
        }

        public string GetSportsDescription(Recording recording)
        {
            return String.Format("{0} - {1}",
                SanitizeDescription(recording.Metadata.recSportOrganization.jsonForClient.title),
                SanitizeDescription(recording.Metadata.recSportEvent.jsonForClient.eventTitle));
        }

        public string GetSportsOutputFileName(string outputDirectory, Recording recording)
        {
            return Sanitize(String.Format("{0}\\Sports\\{1}.mp4",
                outputDirectory,
                SanitizeDescription(recording.Description)));
        }

        public string GetS00E00Description(string outputDirectory, Recording recording, ObservableCollection<Recording> recordings)
        {
            recording.Description = GetEpisodeDescription(recording);
            string outputFileName = GetEpisodeOutputFileName(outputDirectory, recording);

            int highestSeason00FileEpisode = GetHighestFileEpisode(outputDirectory, recording);

            Regex foundRecordingExpression = new Regex(S00EEpisodeNumberRegEx);

            IEnumerable<Recording> showRecordings = from r in recordings
                                                    where r.Type == RecordingType.Episode
                                                    && r.Metadata.recSeries.jsonForClient.title == recording.Metadata.recSeries.jsonForClient.title
                                                    select r;
            int highestFoundEpisode = 0;
            foreach (Recording forEachRecording in showRecordings)
            {
                Match match = foundRecordingExpression.Match(forEachRecording.Description);
                if (match.Success)
                {
                    int foundRecordingEpisodeNumber = (Convert.ToInt32(match.Groups["EpisodeNumber"].Value));
                    highestFoundEpisode = foundRecordingEpisodeNumber > highestFoundEpisode ? foundRecordingEpisodeNumber : highestFoundEpisode;
                }
            }
            int highestEpisode = Math.Max(highestSeason00FileEpisode, highestFoundEpisode);
            recording.Description = GetEpisodeDescription(recording, ++highestFoundEpisode);
            return recording.Description;
        }

        private int GetHighestFileEpisode(string outputDirectory, Recording recording)
        {
            string folderName = GetEpisodeOutputFolderName(outputDirectory, recording);
            if (!Directory.Exists(folderName))
                return 0;

            int highestEpisode = 0;
            IEnumerable<string> files = from fileInfo in Directory.GetFiles(folderName) select fileInfo;
            Regex fileNameExpression = new Regex(S00EEpisodeNumberRegEx);

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                Match match = fileNameExpression.Match(fileName);
                if (match.Success)
                {
                    int fileEpisodeNumber = (Convert.ToInt32(match.Groups["EpisodeNumber"].Value));
                    highestEpisode = fileEpisodeNumber > highestEpisode ? fileEpisodeNumber : highestEpisode;
                }
            }
            return highestEpisode;
        }
    }
}
