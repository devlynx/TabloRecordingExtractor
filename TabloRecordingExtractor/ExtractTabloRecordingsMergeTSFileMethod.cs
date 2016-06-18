 #define LimitTSFilesInVideo

namespace TabloRecordingExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using log4net;
    using Microsoft.Practices.ServiceLocation;
    using Newtonsoft.Json;

    public class ExtractTabloRecordingsMergeTSFileMethod : IExtractTabloRecordings
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
                log.Debug(text, ex);
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
                log.Debug(text, ex);
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

            string webPageText;
            using (WebClient client = new WebClient())
            {
                string webAddress = string.Format("http://{0}:18080/pvr/{1}/segs/", TabloEndPoint.Address, recording.Id);
                log.InfoFormat("Downloading web resource from: {0}", webAddress);
                webPageText = client.DownloadString(webAddress);
            }
            log.Info("Getting TS files...");
            List<string> tsFileNames = new List<string>();
            foreach (var line in webPageText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (line.Contains("video/MP2T"))
                {
                    string tsFileName = line.Split(new string[] { "<a href=\"", ".ts" }, StringSplitOptions.None)[1] + ".ts";
                    tsFileNames.Add(tsFileName);
                }
#if LimitTSFilesInVideo
                if (tsFileNames.Count == 100)
                    break;
#endif
            }

            log.InfoFormat("Found {0} TS files for {1}.", tsFileNames.Count(), recording.Description);
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
                    string downloadURL = String.Format("http://{0}:18080/pvr/{1}/segs/{2}", TabloEndPoint.Address, recording.Id, tsFileName);
                    //  string outputFileName = String.Format("{0}\\TempTabloExtract\\{1}-{2}", Path.GetTempPath(), recording.Id, tsFileName);
                    string outputFileName = String.Format("{0}\\TempTabloExtract\\{1}", Path.GetTempPath(), tsFileName);

                    log.InfoFormat("Downloading TS file: {0} to {1}", downloadURL, outputFileName);
                    await Client.DownloadFileTaskAsync(downloadURL, outputFileName);
                    tsVideoFileNames.Add(outputFileName);
                }
                i++;
                secondaryProgressBar.Report(new ProgressBarInfo() { Maximum = null, Value = i });
                await Task.Delay(5);
            }

            secondaryProgressBar.Report(new ProgressBarInfo() { Maximum = 1, Value = 0 });
            //progress.Report(String.Format("Processing in FFMPEG to create '{0}'...", OutputFile));

            ProcessVideosInFFMPEG(tsVideoFileNames, recording, OutputFile, FFMPEGLocation);

            //ProcessVideoInHandbrake(String.Format("{0}\\TempTabloExtract", Path.GetTempPath()), OutputFile);

            string recordingJson = JsonConvert.SerializeObject(recording, Formatting.Indented);
            string recordingOutputFile = Path.ChangeExtension(OutputFile, ".json");
            if (File.Exists(recordingOutputFile))
                File.Delete(recordingOutputFile);
            File.WriteAllText(recordingOutputFile, recordingJson);
            if (!File.Exists(recordingOutputFile))
                log.ErrorFormat("Recording file: {0} was not written!", recordingOutputFile);
            else
                log.InfoFormat("Recording file: {0} written to disk.", recordingOutputFile);

            foreach (var outputFileName in tsVideoFileNames)
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);
        }

        private void ProcessVideoInHandbrake(string InputPath, string outputFile)
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = "C:\\SD\\Program Files\\Handbrake\\HandBrakeCLI.exe";
                proc.StartInfo.Arguments = String.Format("-i \"{0}\" -f -a 1 -E copy -f mkv -O -e x264 -q 22.0 --loose-anamorphic --modulus 2 -m --x264-preset medium --h264-profile high --h264-level 4.1 --decomb --denoise=weak -v -o \"{1}\"", InputPath, outputFile);
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                if (!proc.Start())
                {
                    MessageBox.Show("Error starting Handbrake");
                    return;
                }
                StreamReader reader = proc.StandardError;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    //lbRecordingIDs.Items.Add(line);
                    //Console.WriteLine(line);
                }
                proc.Close();
            }
        }

        private void ProcessVideosInFFMPEG(List<string> tsFileNames, Recording recording, string OutputFile, string FFMPEGLocation)
        {
            log.InfoFormat("ProcessVideosInFFMPEG: {0}", recording);
            StringBuilder tsFileContents = new StringBuilder();
            foreach (string tsFileName in tsFileNames)
            {
                tsFileContents.AppendFormat("file '{0}'\r\n", tsFileName);
            }

            string FfmpegInputFileName = String.Format("{0}\\TempTabloExtract\\FFMPEG_Input_{1}.txt", Path.GetTempPath(), recording.Id);
            //string FfmpegStdOutFileName = Path.ChangeExtension(OutputFile, ".log");
            File.WriteAllText(FfmpegInputFileName, tsFileContents.ToString().TrimEnd());

            using (Process process = new Process())
            {
                process.StartInfo.FileName = FFMPEGLocation;

                //-hide_banner -y -f concat -i "C:\Users\mreit\AppData\Local\Temp\TempTabloExtract\FFMPEG_Input_123639.txt" -c copy -bsf:a aac_adtstoasc "C:\SD\Tablo\Movies\Air Cadet (1951)\Air Cadet (1951).mp4"
                process.StartInfo.Arguments = string.Format("-y -f concat -i {0} -c copy  -bsf:a aac_adtstoasc \"{1}\"", FfmpegInputFileName, OutputFile);
                // -codec copy -strict -2 -c:a aac -threads 0
                // -y -i http://10.50.4.103:80/stream/pl.m3u8?9dZXqi37g-Q17Zg4PjPdYQ -codec copy -strict -2 -c:a aac -threads 0 "C:\Tablo\tmp\Foreground_rip.mp4"
                log.InfoFormat("FFMEPG command line: {0}", process.StartInfo.Arguments);
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                log.InfoFormat("Starting FFMPEG for recording: {0}", recording);
                if (!process.Start())
                {
                    string text = "Error starting FFMPEG";
                    log.Info(text);
                    MessageBox.Show(text);
                    return;
                }

                //string stdOut = process.StandardOutput.ReadToEnd();

                //process.WaitForExit();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                //MessageBox.Show("Waiting for the process to exit....");
                log.Info("WaitForExit");
                process.WaitForExit();
                if (process.HasExited)
                {
                    log.Info("FFMPEG Has Exited");
                    process.CancelErrorRead();
                    process.CancelOutputRead();
                    process.Close();
                    //MessageBox.Show("Closed");
                }
            }
            //catch (Exception ex)
            //{
            //    MessageBox.Show("1.\n" + ex.Message);
            //}

            //if (process.ExitCode > 0)
            //{
            //    MessageBox.Show(String.Format("ffmpeg failure: {0}", process.ExitCode));

            //    StreamReader reader = process.StandardError;
            //    string line;
            //    while ((line = reader.ReadLine()) != null)
            //    {
            //        //lbRecordingIDs.Items.Add(line);
            //        //Console.WriteLine(line);
            //    }
            //}

            //  process.Close();

            //if (File.Exists(FfmpegStdOutFileName))
            //    File.Delete(FfmpegStdOutFileName);
            //File.WriteAllText(FfmpegStdOutFileName, stdOut);

            if (!File.Exists(OutputFile))
                log.ErrorFormat("Video file: {0} was not written!", OutputFile);
            else
            {
                FileInfo fileInfo = new FileInfo(OutputFile);
                log.InfoFormat("Video file: {0} written to disk (size: {0}).", OutputFile, fileInfo.Length);
            }

            if (File.Exists(FfmpegInputFileName))
                File.Delete(FfmpegInputFileName);
        }
    }
}
