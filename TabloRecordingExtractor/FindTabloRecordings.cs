//#define LimitRecordingsFound
//#define TargetSingleRecording

namespace TabloRecordingExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Practices.ServiceLocation;
    using Newtonsoft.Json;

    public class FindTabloRecordings
    {
        public IPAddress TabloIPAddress { get; set; }

        public string OutputDirectory { get; set; }

        public async Task<ObservableCollection<Recording>> Find(IProgress<ProgressBarInfo> progressBar, IProgress<string> progressFileCountLabel)
        {
            progressFileCountLabel.Report("Initalizing finding recordings...");
            await Task.Delay(10);
            ObservableCollection<Recording> recordings = new ObservableCollection<Recording>();

            IMediaNamingConvention mediaNamingConvention = ServiceLocator.Current.GetInstance<IMediaNamingConvention>();

            progressFileCountLabel.Report(String.Format("Finding recordings on Tablo at IP {0}...", TabloIPAddress));
            List<int> foundRecordings = await GetRecordingList(TabloIPAddress);
            progressFileCountLabel.Report(String.Format("Found {0} files - getting recording metadata...", foundRecordings.Count()));

            int foundCount = foundRecordings.Count();
            int i = 0;
            progressBar.Report(new ProgressBarInfo() { Maximum = foundCount, Value = i });
            progressFileCountLabel.Report("Loading metadata for recordings found...");

            foreach (var foundRecording in foundRecordings)
            {
                #region IF DEFs
#if TargetSingleRecording
                if (foundRecording != 611517)
                    continue;
#endif // TargetSingleRecording
#if LimitRecordingsFound
                if (recordings.Count == 30)
                    break;
#endif // LimitRecordingsFound
                #endregion
                RecordingMetadata metadata = GetRecordingMetadata(TabloIPAddress, foundRecording);
                if (metadata != null)
                {
                    if (i % 10 == 0)
                        progressFileCountLabel.Report(String.Format("Loading metadata for recordings found: {0} of {1}", i, foundCount));

                    Recording recording = new Recording() { Id = foundRecording, Metadata = metadata };
                    if (recording.Metadata.recEpisode != null)
                    {
                        recording.Type = RecordingType.Episode;
                        if (recording.IsS00E00)
                            recording.Description = mediaNamingConvention.GetS00E00Description(OutputDirectory, recording, recordings);
                        else
                            recording.Description = mediaNamingConvention.GetEpisodeDescription(recording);
                        recording.Plot = recording.Metadata.recEpisode.jsonForClient.description;
                        recording.RecordedOnDate = DateTime.Parse(recording.Metadata.recEpisode.jsonForClient.airDate);
                        if (recording.Metadata.recEpisode.jsonForClient.video.state.ToLower() != "finished")
                            recording.HasFinishedRecording = false;
                    }
                    else if (recording.Metadata.recMovie != null)
                    {
                        recording.Type = RecordingType.Movie;
                        recording.Description = mediaNamingConvention.GetMovieDescription(recording);
                        recording.RecordedOnDate = DateTime.Parse(recording.Metadata.recMovieAiring.jsonForClient.airDate);
                        recording.Plot = recording.Metadata.recMovie.jsonForClient.plot;

                        if (recording.Metadata.recMovieAiring.jsonForClient.video.state.ToLower() != "finished")
                            recording.HasFinishedRecording = false;
                    }
                    else if (recording.Metadata.recSportEvent != null)
                    {
                        recording.Type = RecordingType.Sports;
                        recording.Description = mediaNamingConvention.GetSportsDescription(recording);
                        recording.RecordedOnDate = DateTime.Parse(recording.Metadata.recSportEvent.jsonForClient.airDate);
                        recording.Plot = recording.Metadata.recSportEvent.jsonForClient.description;

                        if (recording.Metadata.recSportEvent.jsonForClient.video.state.ToLower() != "finished")
                            recording.HasFinishedRecording = false;
                    }
                    else if (recording.Metadata.recManualProgram != null)
                    {
                        recording.Type = RecordingType.Manual;
                        recording.Description = mediaNamingConvention.GetManualDescription(recording);
                        recording.RecordedOnDate = DateTime.Parse(recording.Metadata.recManualProgramAiring.jsonForClient.airDate);

                        if (recording.Metadata.recManualProgramAiring.jsonForClient.video.state.ToLower() != "finished")
                            recording.HasFinishedRecording = false;
                    }
                    else //If this is not a recognized recording type
                    {
                        continue; // Skip the remainder of this iteration.
                    }

                    recordings.Add(recording);
                    i++;
                    progressBar.Report(new ProgressBarInfo() { Maximum = null, Value = i });
                    await Task.Delay(10);
                }
            }
            // });
            progressFileCountLabel.Report(String.Format("Loading metadata for recordings found: {0}", foundCount));
            progressBar.Report(new ProgressBarInfo() { Maximum = null, Value = 0 });
            await Task.Delay(10);
            return recordings;
        }

        private async Task<List<int>> GetRecordingList(IPAddress IPAddress)
        {
            List<int> recordingIDs = new List<int>();
            string webPageText;
            using (WebClient client = new WebClient())
            {
                webPageText = await client.DownloadStringTaskAsync(string.Format("http://{0}:18080/pvr", IPAddress));
            }

            foreach (var line in webPageText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (line.Contains("<tr><td class=\"n\"><a href=\""))
                {
                    string sRecordingID = line.Split(new string[] { "<a href=\"", "/\">" }, StringSplitOptions.None)[1];
                    int iRecordingID;
                    if (Int32.TryParse(sRecordingID, out iRecordingID))
                    {
                        recordingIDs.Add(iRecordingID);
                    }
                }
            }
            return recordingIDs;
        }

        private RecordingMetadata GetRecordingMetadata(IPAddress IPAddress, int RecordingID)
        {
            string metadata;
            using (WebClient client = new WebClient())
            {
                try
                {
                    metadata = client.DownloadString(string.Format("http://{0}:18080/pvr/{1}/meta.txt", IPAddress, RecordingID));
                }
                catch
                {
                    return null;
                }
            }
            return JsonConvert.DeserializeObject<RecordingMetadata>(metadata);
        }
    }
}
