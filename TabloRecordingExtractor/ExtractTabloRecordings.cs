//#define LimitTSFilesInVideo

namespace TabloRecordingExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.Practices.ServiceLocation;
    using Newtonsoft.Json;

    public class ExtractTabloRecordings
    {
        public string TabloIPAddress { get; set; }
        public List<Recording> SelectedRecordings { get; set; }
        public string OutputDirectory { get; set; }
        public string FFMPEGLocation { get; set; }

        public async Task ExtractRecordings(
            IProgress<ProgressBarInfo> primaryProgressBar,
            IProgress<string> primaryProgressLabel,
            IProgress<ProgressBarInfo> secondaryProgressBar,
            IProgress<string> secondaryLabel)
        {
            primaryProgressBar.Report(new ProgressBarInfo() { Maximum = SelectedRecordings.Count(), Value = 0 });
            await Task.Delay(5);

            int i = 1;
            foreach (var recording in SelectedRecordings)
            {
                if (recording != null)
                {
                    primaryProgressLabel.Report(recording.Description);
                    await Task.Delay(5);
                    await GetRecordingVideo(TabloIPAddress, recording, secondaryProgressBar, secondaryLabel);
                    i++;
                    primaryProgressBar.Report(new ProgressBarInfo() { Maximum = null, Value = i });
                    await Task.Delay(5);
                }
            }
        }

        private async Task GetRecordingVideo(string IPAddress, Recording recording,
            IProgress<ProgressBarInfo> secondaryProgressBar,
            IProgress<string> secondaryLabel)
        {
            try
            {
                if (!Directory.Exists(Path.GetTempPath() + "\\TempTabloExtract"))
                {
                    Directory.CreateDirectory(Path.GetTempPath() + "\\TempTabloExtract");
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(string.Format("Unable to create temporary directory at '{0}\\TempTabloExtract'", Path.GetTempPath()));
                return;
            }

            try
            {
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(string.Format("Unable to create output directory at '{0}'", OutputDirectory));
                return;
            }

            IMediaNamingConvention mediaNamingConvention = ServiceLocator.Current.GetInstance<IMediaNamingConvention>();
            string OutputFile;
            if (recording.Type == RecordingType.Episode)
            {
                OutputFile = mediaNamingConvention.GetEpisodeOutputFileName(OutputDirectory, recording);
                string dir = Path.GetDirectoryName(OutputFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            else if (recording.Type == RecordingType.Movie)
            {
                OutputFile = mediaNamingConvention.GetMovieOutputFileName(OutputDirectory, recording);
                string dir = Path.GetDirectoryName(OutputFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            else if (recording.Type == RecordingType.Sports)
            {
                OutputFile = mediaNamingConvention.GetSportsOutputFileName(OutputDirectory, recording);
                string dir = Path.GetDirectoryName(OutputFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            else if (recording.Type == RecordingType.Manual)
                OutputFile = mediaNamingConvention.GetManualOutputFileName(OutputDirectory, recording);
            else
                OutputFile = mediaNamingConvention.GetOtherOutupuFileName(OutputDirectory, recording);

            if (File.Exists(OutputFile))
            {
                //logListBox.Items.Add(String.Format("File {0} already exists - skipping", OutputFile));
                return;
            }

            if (!File.Exists(FFMPEGLocation))
            {
                MessageBox.Show("FFMPEG could not be found. It must be located before you can proceed.");
                return;
            }
            try
            {
                FileInfo fileInfo = new FileInfo(FFMPEGLocation);
                if (fileInfo.Name != "ffmpeg.exe")
                {
                    MessageBox.Show("The file name provided for FFMPEG was not \"ffpmeg.exe\". It must be located before you can proceed.");
                    return;
                }
            }
            catch
            {
                MessageBox.Show("There was a problem reading from the FFMPEG exe. It must be located before you can proceed.");
                return;
            }

            string webPageText;
            using (WebClient client = new WebClient())
            {
                webPageText = client.DownloadString(string.Format("http://{0}:18080/pvr/{1}/segs/", IPAddress, recording.Id));
            }

            List<string> tsFileNames = new List<string>();
            foreach (var line in webPageText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (line.Contains("video/MP2T"))
                {
                    string tsFileName = line.Split(new string[] { "<a href=\"", ".ts" }, StringSplitOptions.None)[1] + ".ts";
                    tsFileNames.Add(tsFileName);
                }
#if LimitTSFilesInVideo
                if (tsFileNames.Count == 3)
                    break;
#endif
            }

            secondaryLabel.Report(String.Format("TS file count: {0}", tsFileNames.Count()));
            await Task.Delay(5);

            List<string> tsVideoFileNames = new List<string>();

            int i = 0;
            secondaryProgressBar.Report(new ProgressBarInfo() { Maximum = tsFileNames.Count(), Value = 0 });
            await Task.Delay(5);
            foreach (var tsFileName in tsFileNames)
            {
                //progress.Report(String.Format("  Downloading '{0}'...", tsFileName));
                using (WebClient Client = new WebClient())
                {
                    string downloadURL = String.Format("http://{0}:18080/pvr/{1}/segs/{2}", IPAddress, recording.Id, tsFileName);
                    string outputFileName = String.Format("{0}\\TempTabloExtract\\{1}-{2}", Path.GetTempPath(), recording.Id, tsFileName);

                    await Client.DownloadFileTaskAsync(downloadURL, outputFileName);
                    tsVideoFileNames.Add(outputFileName);
                }
                i++;
                secondaryProgressBar.Report(new ProgressBarInfo() { Maximum = null, Value = i });
                await Task.Delay(5);
            }

            secondaryProgressBar.Report(new ProgressBarInfo() { Maximum = 1, Value = 0 });

            //progress.Report(String.Format("Processing in FFMPEG to create '{0}'...", OutputFile));

            ProcessVideosInFFMPEG(tsVideoFileNames, OutputFile, FFMPEGLocation);

            string recordingJson = JsonConvert.SerializeObject(recording, Formatting.Indented);
            string recordingOutputFile = Path.ChangeExtension(OutputFile, ".json");
            if (File.Exists(recordingOutputFile))
                File.Delete(recordingOutputFile);
            File.WriteAllText(recordingOutputFile, recordingJson);

            foreach (var outputFileName in tsVideoFileNames)
            {
                if (File.Exists(outputFileName))
                {
                    File.Delete(outputFileName);
                }
            }

            //ProcessVideoInHandbrake(OutputFile, OutputFile.Replace(".mp4", ".mkv"));

            //if (File.Exists(OutputFile))
            //{
            //    File.Delete(OutputFile);
            //}
            //});
        }

        private void ProcessVideosInFFMPEG(List<string> tsFileNames, string OutputFile, string FFMPEGLocation)
        {
            string fileNamesConcatString = String.Join("|", tsFileNames.Select(x => x).ToArray());

            using (Process process = new Process())
            {
                process.StartInfo.FileName = FFMPEGLocation;
                process.StartInfo.Arguments = string.Format("-y -i \"concat:{0}\" -bsf:a aac_adtstoasc -c copy \"{1}\"", fileNamesConcatString, OutputFile);
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                if (!process.Start())
                {
                    MessageBox.Show("Error starting ffmpeg");
                    return;
                }
                StreamReader reader = process.StandardError;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    //lbRecordingIDs.Items.Add(line);
                    //Console.WriteLine(line);
                }
                process.Close();
            }
        }
    }
}
