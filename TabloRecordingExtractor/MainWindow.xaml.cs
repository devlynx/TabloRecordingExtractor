#define LimitRecordingsFound
#define LimitTSFilesInVideo

namespace TabloRecordingExtractor
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
    using Microsoft.Practices.ServiceLocation;
    using System.Threading.Tasks;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SortAdorner listViewSortAdorner = null;
        private GridViewColumnHeader listViewSortCol = null;
        private ObservableCollection<Recording> recordings = null;
        private Recording selectedRecording = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        private static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            ShowOutputFolderSelectDialog();
        }

        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            lvRecordingsFound.SelectedItems.Clear();
        }

        private async void btnExtract_Click(object sender, RoutedEventArgs e)
        {
            extractProgress.Minimum = 0;
            extractProgress.Value = 0;
            extractProgress.Maximum = 100;

            Progress<ProgressBarInfo> progressBar = new Progress<ProgressBarInfo>(ReportExtractProgress);
            Progress<string> progressExtractLabel = new Progress<string>(ReportExtractLabel);

            await extractRecordings(progressBar, progressExtractLabel);
        }

        private async Task extractRecordings(IProgress<ProgressBarInfo> progressInfo, IProgress<string> progressExtractLabel)
        {
            string tabloIPAddress = txtTabloIPAddress.Text;

            List<Recording> selectedRecordings = new List<Recording>();
            foreach (var item in lvRecordingsFound.SelectedItems)
            {
                Recording recording = item as Recording;
                selectedRecordings.Add(recording);
            }
            extractProgress.Maximum = selectedRecordings.Count();
            extractProgress.Minimum = 0;
            extractProgress.Value = 0;
            int i = 0;
            foreach (var recording in selectedRecordings)
            {
                if (recording != null)
                {
                    i++;
                    extractProgress.Value = i;
                    extractingRecordingLabel.Content = recording.Description;
                    await GetRecordingVideo(tabloIPAddress, recording, progressInfo, progressExtractLabel);
                }
            }
        }

        internal void ReportExtractProgress(ProgressBarInfo progressInfo)
        {
            if (progressInfo.Maximum != null)
                extractProgress.Maximum = progressInfo.Maximum.Value;
            if (progressInfo.Value != null)
                extractProgress.Value = progressInfo.Value.Value;
        }
        
        internal void ReportDownloadProgress(ProgressBarInfo progressInfo)
        {
            if (progressInfo.Maximum != null)
                downloadTsFilesProgress.Maximum = progressInfo.Maximum.Value;
            if (progressInfo.Value != null)
                downloadTsFilesProgress.Value = progressInfo.Value.Value;
        }

        internal void ReportFileCountLabel(string message)
        {
            extractingFileCountLabel.Content = message;
        }

        internal void ReportExtractLabel(string message)
        {
            extractingRecordingLabel.Content = message;
        }

        private async void btnFindRecordings_Click(object sender, RoutedEventArgs e)
        {
            downloadTsFilesProgress.Minimum = 0;
            downloadTsFilesProgress.Value = 0;
            downloadTsFilesProgress.Maximum = 100;

            Progress<ProgressBarInfo> progressBar = new Progress<ProgressBarInfo>(ReportDownloadProgress);
            Progress<string> progressFileCountLabel = new Progress<string>(ReportFileCountLabel);

            await findRecordings(progressBar, progressFileCountLabel);
        }

        private async Task findRecordings(IProgress<ProgressBarInfo> progressInfo, IProgress<string> progressFileCountLabel)
        {
            progressFileCountLabel.Report("Initalizing finding recordings...");
            await Task.Delay(10);
            string tabloIPAddress = txtTabloIPAddress.Text;
            recordings = new ObservableCollection<Recording>();

            IMediaNamingConvention mediaNamingConvention = ServiceLocator.Current.GetInstance<IMediaNamingConvention>();

            progressFileCountLabel.Report(String.Format("Finding recordings on Tablo at IP {0}...", tabloIPAddress));
            List<int> foundRecordings = await GetRecordingList(tabloIPAddress);
            progressFileCountLabel.Report(String.Format("Found {0} files - getting recording metadata...", foundRecordings.Count()));

            int foundCount = foundRecordings.Count();
            int i = 0;
            progressInfo.Report(new ProgressBarInfo() { Maximum = foundCount, Value = i });
            progressFileCountLabel.Report("Loading metadata for recordings found...");

            foreach (var foundRecording in foundRecordings)
            {
                RecordingMetadata metadata = GetRecordingMetadata(tabloIPAddress, foundRecording);
                if (metadata != null)
                {
                    Recording recording = new Recording() { Id = foundRecording, Metadata = metadata };
                    if (recording.Metadata.recEpisode != null)
                    {
                        recording.Type = RecordingType.Episode;
                        recording.Description = mediaNamingConvention.GetEpisodeDescription(recording);
                        recording.Plot = recording.Metadata.recEpisode.jsonForClient.description;
                        recording.RecordedOnDate = DateTime.Parse(recording.Metadata.recEpisode.jsonForClient.airDate);

                        if (recording.Metadata.recEpisode.jsonForClient.video.state.ToLower() != "finished")
                            recording.IsNotFinished = true;
                    }
                    else if (recording.Metadata.recMovie != null)
                    {
                        recording.Type = RecordingType.Movie;
                        recording.Description = mediaNamingConvention.GetMovieDescription(recording);
                        recording.RecordedOnDate = DateTime.Parse(recording.Metadata.recMovieAiring.jsonForClient.airDate);
                        recording.Plot = recording.Metadata.recMovie.jsonForClient.plot;

                        if (recording.Metadata.recMovieAiring.jsonForClient.video.state.ToLower() != "finished")
                            recording.IsNotFinished = true;
                    }
                    else if (recording.Metadata.recSportEvent != null)
                    {
                        recording.Type = RecordingType.Sports;
                        recording.Description = mediaNamingConvention.GetSportsDescription(recording);
                        recording.RecordedOnDate = DateTime.Parse(recording.Metadata.recSportEvent.jsonForClient.airDate);
                        recording.Plot = recording.Metadata.recSportEvent.jsonForClient.description;

                        if (recording.Metadata.recSportEvent.jsonForClient.video.state.ToLower() != "finished")
                            recording.IsNotFinished = true;
                    }
                    else if (recording.Metadata.recManualProgram != null)
                    {
                        recording.Type = RecordingType.Manual;
                        recording.Description = mediaNamingConvention.GetManualDescription(recording);
                        recording.RecordedOnDate = DateTime.Parse(recording.Metadata.recManualProgramAiring.jsonForClient.airDate);

                        if (recording.Metadata.recManualProgramAiring.jsonForClient.video.state.ToLower() != "finished")
                            recording.IsNotFinished = true;
                    }
                    else //If this is not a recognized recording type
                    {
                        continue; // Skip the remainder of this iteration.
                    }

                    recordings.Add(recording);
                    i++;
                    progressInfo.Report(new ProgressBarInfo() { Maximum = null, Value = i });
                    await Task.Delay(10);

#if LimitRecordingsFound
                    if (recordings.Count == 30)
                        break;
#endif // LimitRecordingsFound
                }
            }
            // });
            progressFileCountLabel.Report(String.Empty);
            progressInfo.Report(new ProgressBarInfo() { Maximum = null, Value = 0 });
            await Task.Delay(10);

            lvRecordingsFound.ItemsSource = recordings;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvRecordingsFound.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Description", ListSortDirection.Ascending));
            btnSelectAll.IsEnabled = true;
            btnDeselectAll.IsEnabled = true;
            btnSelectSimilar.IsEnabled = true;
        }

        private void btnLocateFFMPEG_Click(object sender, RoutedEventArgs e)
        {
            ShowFFMPEGFindDialog();
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            btnDeselectAll_Click(sender, e);
            lvRecordingsFound.SelectAll();
        }
        private void btnSelectSimilar_Click(object sender, RoutedEventArgs e)
        {
            Recording similarToRecording = selectedRecording;
            if (similarToRecording == null)
            {
                MessageBox.Show("Please select a recording.");
                return;
            }

            btnDeselectAll_Click(sender, e);

            foreach (var item in lvRecordingsFound.Items)
            {
                Recording recording = item as Recording;
                if (similarToRecording.Type == RecordingType.Episode)
                {
                    if ((recording.Type == similarToRecording.Type) &&
                        String.Compare(similarToRecording.Metadata.recSeries.jsonForClient.title,
                            recording.Metadata.recSeries.jsonForClient.title, true) == 0)
                        lvRecordingsFound.SelectedItems.Add(recording);
                }
                else if (recording.Type == similarToRecording.Type)
                    lvRecordingsFound.SelectedItems.Add(recording);
            }
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
                        string webPageText = client.DownloadString(string.Format("http://{0}:18080", txtTabloIPAddress.Text));
                    }
                    MessageBox.Show(string.Format("Tablo validated at {0}.", tabloIPaddress));
                    txtTabloIPAddress.IsEnabled = false;
                    btnValidateTablo.IsEnabled = false;
                    btnFindRecordings.IsEnabled = true;
                }
                catch (WebException ex)
                {
                    MessageBox.Show("Tablo data not found at the IP Address entered. Please try again.");
                }
            }
            else
            {
                MessageBox.Show("The text entered is not a valid IP address. Please try again.");
            }
        }

        private async Task<List<int>> GetRecordingList(string IPAddress)
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

        private RecordingMetadata GetRecordingMetadata(string IPAddress, int RecordingID)
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

        private async Task GetRecordingVideo(string IPAddress, Recording recording, IProgress<ProgressBarInfo> progressInfo, IProgress<string> progressExtractLabel)
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
                MessageBox.Show(string.Format("Unable to create temporary directory at '{0}\\TempTabloExtract'", Path.GetTempPath()));
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
                MessageBox.Show(string.Format("Unable to create output directory at '{0}'", outputDirectory));
                return;
            }

            IMediaNamingConvention mediaNamingConvention = ServiceLocator.Current.GetInstance<IMediaNamingConvention>();
            string OutputFile;
            if (recording.Type == RecordingType.Episode)
            {
                OutputFile = mediaNamingConvention.GetEpisodeOutputFileName(outputDirectory, recording);
                string dir = Path.GetDirectoryName(OutputFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            else if (recording.Type == RecordingType.Movie)
            {
                OutputFile = mediaNamingConvention.GetMovieOutputFileName(outputDirectory, recording);
                string dir = Path.GetDirectoryName(OutputFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            else if (recording.Type == RecordingType.Sports)
            {
                OutputFile = mediaNamingConvention.GetSportsOutputFileName(outputDirectory, recording);
                string dir = Path.GetDirectoryName(OutputFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            else if (recording.Type == RecordingType.Manual)
                OutputFile = mediaNamingConvention.GetManualOutputFileName(outputDirectory, recording);
            else
                OutputFile = mediaNamingConvention.GetOtherOutupuFileName(outputDirectory, recording);

            if (File.Exists(OutputFile))
            {
                logListBox.Items.Add(String.Format("File {0} already exists - skipping", OutputFile));
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
                if (tsFileNames.Count == 5)
                    break;
#endif
            }

            extractingFileCountLabel.Content = String.Format("TS file count: {0}", tsFileNames.Count());
            List<string> tsVideoFileNames = new List<string>();
            downloadTsFilesProgress.Minimum = 0;
            downloadTsFilesProgress.Value = 0;
            downloadTsFilesProgress.Maximum = tsFileNames.Count();

            //DoWorkWithModal(progress =>
            //{
            int i = 1;
            //progress.Report(String.Format("Extracting Tablo recording TS files for '{0}'...", recording.Description));
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
                //SetProgressValue setValue = new SetProgressValue(DoSetProgress);
                downloadTsFilesProgress.Value = i;
            }

            //progress.Report(String.Format("Processing in FFMPEG to create '{0}'...", OutputFile));

            ProcessVideosInFFMPEG(tsVideoFileNames, OutputFile, FFMPEGLocation);

            string recordingJson = JsonConvert.SerializeObject(recording, Formatting.Indented);
            string recordingOutputFile = Path.ChangeExtension(OutputFile, ".json");
            if (File.Exists(recordingOutputFile))
                File.Delete(recordingOutputFile);
            File.WriteAllText(recordingOutputFile, recordingJson);

            //progress.Report("Deleting TS files...");

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

        private void lvRecordingsFound_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                lvRecordingsFound.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            lvRecordingsFound.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
            //ICollectionView view = CollectionViewSource.GetDefaultView(lvRecordingsFound.ItemsSource);
            //view.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private void lvRecordingsFound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
                selectedRecording = e.AddedItems[0] as Recording;

            if (lvRecordingsFound.SelectedItems.Count > 0)
            {
                btnExtract.IsEnabled = true;
            }
            else
            {
                btnExtract.IsEnabled = false;
            }
        }

        private void OutputDirectory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowOutputFolderSelectDialog();
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
        private void ShowFFMPEGFindDialog()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "FFMPEG Application|ffmpeg.exe";
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            txtFFMPEGLocation.Text = dialog.FileName;
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
    }
}
