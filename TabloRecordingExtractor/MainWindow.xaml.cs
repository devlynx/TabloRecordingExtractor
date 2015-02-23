using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace TabloRecordingExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        private void btnFindRecordings_Click(object sender, RoutedEventArgs e)
        {
            string tabloIPAddress = txtTabloIPAddress.Text;
            List<Recording> recordings = new List<Recording>();

            DoWorkWithModal(progress =>
            {
                progress.Report(String.Format("Finding recordings on Tablo at IP {0}...", tabloIPAddress));
                List<int> foundRecordings = GetRecordingList(tabloIPAddress);

                progress.Report("Loading metadata for recordings found...");
                foreach (var foundRecording in foundRecordings)
                {
                    RecordingMetadata metadata = GetRecordingMetadata(tabloIPAddress, foundRecording);
                    if (metadata != null)
                    {
                        Recording recording = new Recording();
                        recording.Id = foundRecording;
                        if (metadata.recEpisode != null)
                        {
                            recording.Type = RecordingType.Episode;
                            recording.Description = String.Format("{0} - S{1}E{2} - {3}", metadata.recSeries.jsonForClient.title, metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"), metadata.recEpisode.jsonForClient.episodeNumber.ToString("00"), metadata.recEpisode.jsonForClient.title);
                            recording.RecordedOnDate = DateTime.Parse(metadata.recEpisode.jsonForClient.airDate);
                        }
                        else if (metadata.recMovie != null)
                        {
                            recording.Type = RecordingType.Movie;
                            recording.Description = String.Format("{0} ({1})", metadata.recMovie.jsonForClient.title, metadata.recMovie.jsonForClient.releaseYear);
                            recording.RecordedOnDate = DateTime.Parse(metadata.recMovieAiring.jsonForClient.airDate);
                        }
                        recordings.Add(recording);

                        if ((recordings.Count % 20) == 0)
                        {
                            progress.Report(String.Format("{0} recordings found...", recordings.Count));
                        }
                    }
                }
            });

            lvRecordingsFound.ItemsSource = recordings;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvRecordingsFound.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Description", ListSortDirection.Ascending));
        }

        private void btnExtract_Click(object sender, RoutedEventArgs e)
        {
            string tabloIPAddress = txtTabloIPAddress.Text;

            List<Recording> selectedRecordings = new List<Recording>();
            foreach (var item in lvRecordingsFound.SelectedItems)
            {
                Recording recording = item as Recording;
                selectedRecordings.Add(recording);
            }

            foreach (var recording in selectedRecordings)
            {
                if (recording != null)
                {
                    GetRecordingVideo(tabloIPAddress, recording);
                }

            }
        }

        private List<int> GetRecordingList(string IPAddress)
        {
            List<int> recordingIDs = new List<int>();
            string webPageText;
            using (WebClient client = new WebClient())
            {
                webPageText = client.DownloadString("http://" + IPAddress + ":18080/pvr");
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

        private RecordingMetadata GetRecordingMetadata(string IPAddress, int RecordingID)
        {
            string metadata;
            using (WebClient client = new WebClient())
            {
                try
                {
                    metadata = client.DownloadString("http://" + IPAddress + ":18080/pvr/" + RecordingID.ToString() + "/meta.txt");
                }
                catch
                {
                    return null;
                }
            }
            return JsonConvert.DeserializeObject<RecordingMetadata>(metadata);
        }

        private void GetRecordingVideo(string IPAddress, Recording recording)
        {
            string outputDirectory = OutputDirectory.Text;
            string FFMPEGLocation = txtFFMPEGLocation.Text;

            try
            {
                if (!Directory.Exists(Path.GetTempPath() + "\\TempTabloExtract"))
                {
                    Directory.CreateDirectory(Path.GetTempPath() + "\\TempTabloExtract");
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show("Unable to create temporary directory at '" + Path.GetTempPath() + "\\TempTabloExtract'");
                return;
            }

            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show("Unable to create output directory at '" + outputDirectory + "'");
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
                webPageText = client.DownloadString("http://" + IPAddress + ":18080/pvr/" + recording.Id.ToString() + "/segs/");
            }

            List<string> tsFileNames = new List<string>();
            foreach (var line in webPageText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (line.Contains("video/MP2T"))
                {
                    string tsFileName = line.Split(new string[] { "<a href=\"", ".ts" }, StringSplitOptions.None)[1] + ".ts";
                    tsFileNames.Add(tsFileName);
                }
            }

            List<string> outputFileNames = new List<string>();

            DoWorkWithModal(progress =>
            {
                progress.Report(String.Format("Extracting Tablo recording TS files for '{0}'...", recording.Description));
                foreach (var tsFileName in tsFileNames)
                {
                    progress.Report(String.Format("  Downloading '{0}'...", tsFileName));
                    using (WebClient Client = new WebClient())
                    {
                        string downloadURL = String.Format("http://{0}:18080/pvr/{1}/segs/{2}", IPAddress, recording.Id.ToString(), tsFileName);
                        string outputFileName = String.Format("{0}\\TempTabloExtract\\{1}-{2}", Path.GetTempPath(), recording.Id.ToString(), tsFileName);

                        Client.DownloadFile(downloadURL, outputFileName);
                        outputFileNames.Add(outputFileName);
                    }
                }
                string OutputFile = String.Format("{0}\\{1}.mp4", outputDirectory, recording.Description);

                progress.Report(String.Format("Processing in FFMPEG to create '{0}'...", OutputFile));

                ProcessVideosInFFMPEG(outputFileNames, OutputFile, FFMPEGLocation);

                progress.Report("Deleting TS files...");

                foreach (var outputFileName in outputFileNames)
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
            });
        }

        private void ProcessVideosInFFMPEG(List<string> tsFileNames, string OutputFile, string FFMPEGLocation)
        {
            string fileNamesConcatString = String.Join("|", tsFileNames.Select(x => x.ToString()).ToArray());

            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = FFMPEGLocation;
                proc.StartInfo.Arguments = String.Format("-y -i \"concat:{0}\" -bsf:a aac_adtstoasc -c copy \"{1}\"", fileNamesConcatString, OutputFile);
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                if (!proc.Start())
                {
                    MessageBox.Show("Error starting ffmpeg");
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

        private void ProcessVideoInHandbrake(string InputMP4File, string OutputMKVFile)
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = "C:\\Program Files\\Handbrake\\HandBrakeCLI.exe";
                proc.StartInfo.Arguments = String.Format("-i \"{0}\" -f -a 1 -E copy -f mkv -O -e x264 -q 22.0 --loose-anamorphic --modulus 2 -m --x264-preset medium --h264-profile high --h264-level 4.1 --decomb --denoise=weak -v -o \"{1}\"", InputMP4File, OutputMKVFile);
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

        private static string CleanFileName(string fileName)
        {
            return System.IO.Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        private void btnValidateTablo_Click(object sender, RoutedEventArgs e)
        {
            IPAddress tabloIPaddress;

            if (IPAddress.TryParse(txtTabloIPAddress.Text, out tabloIPaddress))
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        string webPageText = client.DownloadString("http://" + txtTabloIPAddress.Text + ":18080");
                    }
                    txtTabloIPAddress.IsEnabled = false;
                    btnValidateTablo.IsEnabled = false;
                    btnFindRecordings.IsEnabled = true;
                }
                catch (System.Net.WebException ex)
                {
                    MessageBox.Show("Tablo data not found at the IP Address entered. Please try again.");
                }
            }
            else
            {
                MessageBox.Show("The text entered is not a valid IP address. Please try again.");
            }
        }

        private void lvRecordingsFound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvRecordingsFound.SelectedItems.Count > 0)
            {
                btnExtract.IsEnabled = true;
            }
            else
            {
                btnExtract.IsEnabled = false;
            }
        }

        public static void DoWorkWithModal(Action<IProgress<string>> work)
        {
            StatusDialogWindow statusWindow = new StatusDialogWindow();

            statusWindow.Loaded += (_, args) =>
            {
                BackgroundWorker worker = new BackgroundWorker();

                Progress<string> progress = new Progress<string>(
                    data => statusWindow.AddStatusText(data));

                worker.DoWork += (s, workerArgs) => work(progress);

                worker.RunWorkerCompleted +=
                    (s, workerArgs) => statusWindow.Close();

                worker.RunWorkerAsync();
            };

            statusWindow.ShowDialog();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            ShowOutputFolderSelectDialog();
        }

        private void OutputDirectory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowOutputFolderSelectDialog();
        }

        private void ShowOutputFolderSelectDialog()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            OutputDirectory.Text = dialog.SelectedPath;
        }

        private void txtFFMPEGLocation_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowFFMPEGFindDialog();
        }

        private void btnLocateFFMPEG_Click(object sender, RoutedEventArgs e)
        {
            ShowFFMPEGFindDialog();
        }

        private void ShowFFMPEGFindDialog()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "FFMPEG Application|ffmpeg.exe";
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            txtFFMPEGLocation.Text = dialog.FileName;
        }


    }

    public enum RecordingType { Episode, Movie };

    public class Recording
    {
        public RecordingType Type { get; set; }
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime RecordedOnDate { get; set; }
    }
}
