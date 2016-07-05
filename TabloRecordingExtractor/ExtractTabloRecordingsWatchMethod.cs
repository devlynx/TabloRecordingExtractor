//#define LimitTSFilesInVideo

namespace TabloRecordingExtractor
{
    using log4net;
    using Microsoft.Practices.ServiceLocation;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows;

    public class ExtractTabloRecordingsWatchMethod : IExtractTabloRecordings
    {
        private static readonly ILog log = LogManager.GetLogger("ExtractTabloRecordings");

        public IPEndPoint TabloEndPoint { get; set; }

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
                    log.InfoFormat("Extracting: {0}", recording);
                    primaryProgressLabel.Report(recording.Description);
                    await Task.Delay(5);
                    await GetRecordingVideo(TabloEndPoint, recording, secondaryProgressBar, secondaryLabel);
                    i++;
                    primaryProgressBar.Report(new ProgressBarInfo() { Maximum = null, Value = i });
                    await Task.Delay(5);
                    log.Info("========================================================================");
                }
            }
        }

        private async Task GetRecordingVideo(IPEndPoint ipEndPoint, Recording recording,
            IProgress<ProgressBarInfo> secondaryProgressBar,
            IProgress<string> secondaryLabel)
        {
            try
            {
                string path = string.Format("{0}\\TempTabloExtract", Path.GetTempPath());
                if (!Directory.Exists(path))
                {
                    log.InfoFormat("Creating directory: {0}", path);
                    Directory.CreateDirectory(path);
                }
            }
            catch (IOException ex)
            {
                string text = string.Format("Unable to create temporary directory at '{0}\\TempTabloExtract'", Path.GetTempPath());
                log.Error(text, ex);
                MessageBox.Show(text);
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
                string text = string.Format("Unable to create output directory at '{0}'", OutputDirectory);
                log.Error(text, ex);
                MessageBox.Show(text);
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
                OutputFile = mediaNamingConvention.GetOtherOutputFileName(OutputDirectory, recording);

            if (File.Exists(OutputFile))
            {
                log.InfoFormat(String.Format("File {0} already exists - skipping", OutputFile));
                return;
            }

            if (!File.Exists(FFMPEGLocation))
            {
                string notFound = "FFMPEG could not be found. It must be located before you can proceed.";
                log.InfoFormat(notFound);
                MessageBox.Show(notFound);
                return;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(FFMPEGLocation);
                if (fileInfo.Name != "ffmpeg.exe")
                {
                    string notFound = "The file name provided for FFMPEG was not \"ffpmeg.exe\". It must be located before you can proceed.";
                    log.InfoFormat(notFound);
                    MessageBox.Show(notFound);
                    return;
                }
            }
            catch (Exception ex)
            {
                string notFound = "There was a problem reading from the FFMPEG exe. It must be located before you can proceed.";
                log.Info(notFound, ex);
                MessageBox.Show(notFound);
                return;
            }

            RecordingWatch recordingWatch = TabloAPI.GetRecordingWatch(recording, TabloEndPoint);
            if (String.IsNullOrWhiteSpace(recordingWatch.playlist_url))
            {
                log.ErrorFormat("recordingWatch.playlist_url for {0} is empty.", recording);
            }
            else if (await ProcessVideosInFFMPEG(recordingWatch.playlist_url, recording, OutputFile, FFMPEGLocation, secondaryProgressBar, secondaryLabel))
            {
                log.InfoFormat("playlist_url: {0}", recordingWatch.playlist_url);
                string recordingJson = JsonConvert.SerializeObject(recording, Formatting.Indented);
                string recordingOutputFile = Path.ChangeExtension(OutputFile, ".json");
                if (File.Exists(recordingOutputFile))
                    File.Delete(recordingOutputFile);
                File.WriteAllText(recordingOutputFile, recordingJson);
                if (!File.Exists(recordingOutputFile))
                    log.InfoFormat("Recording file: {0} written to disk.", recordingOutputFile);
            }
        }

        private async Task<bool> ProcessVideosInFFMPEG(string playListURL, Recording recording, string OutputFile, string FFMPEGLocation, IProgress<ProgressBarInfo> secondaryProgressBar,
            IProgress<string> secondaryLabel)
        {
            log.InfoFormat("ProcessVideosInFFMPEG: {0}", recording);

            string ffmpegOutputFileName = String.Format("{0}\\TabloRecordingExtractor\\ffmpeg.txt",  Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

            using (Process process = new Process())
            {
                process.StartInfo.FileName = FFMPEGLocation;

                // -y -i http://10.50.4.103:80/stream/pl.m3u8?oR_Qzu7PBQCL0BZ-tyysuw -codec copy -strict -2 -c:a aac -threads 0 "C:\SD\Temp\Tablo\tmp\Foreground_rip.mp4"
                process.StartInfo.Arguments = string.Format(
                    "-y -i {0} -codec copy -strict -2 -c:a aac -threads 0 \"{1}\" > \"{2}\" 2>&1", playListURL, OutputFile, ffmpegOutputFileName);

                log.InfoFormat("FFMEPG command line: {0}", process.StartInfo.Arguments);
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.CreateNoWindow = false;

                log.InfoFormat("Starting FFMPEG for recording: {0}", recording);
                if (!process.Start())
                {
                    string text = "Error starting FFMPEG";
                    log.Info(text);
                    MessageBox.Show(text);
                    return false;
                }

                secondaryLabel.Report(String.Format("Extracting: {0}", recording));
                log.Info("WaitForExit");
                process.WaitForExit();
                if (process.HasExited)
                {
                   log.Info(File.ReadAllText(ffmpegOutputFileName));

                    log.InfoFormat("FFMPEG Has Exited with ExitCode: {0}", process.ExitCode);
                   process.Close();
                }
            }

            if (File.Exists(ffmpegOutputFileName))
                File.Delete(ffmpegOutputFileName);

            if (!File.Exists(OutputFile))
            {
                log.ErrorFormat("Video file: {0} was not written!", OutputFile);
                return false;
            }
            else
            {
                FileInfo fileInfo = new FileInfo(OutputFile);
                log.InfoFormat("Video file: {0} written to disk (size: {1}).", OutputFile, fileInfo.Length);
                return true;
            }
        }
    }
}
