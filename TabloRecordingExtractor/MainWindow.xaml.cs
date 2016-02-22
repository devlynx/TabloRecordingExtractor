#define LimitRecordingsFound

namespace TabloRecordingExtractor
{
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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SortAdorner listViewSortAdorner = null;
        private GridViewColumnHeader listViewSortCol = null;
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

            Progress<ProgressBarInfo> primaryProgressBar = new Progress<ProgressBarInfo>(ReportPrimaryProgress);
            Progress<string> primaryProgressLabel = new Progress<string>(ReportPrimaryLabel);
            Progress<ProgressBarInfo> secondaryProgressBar = new Progress<ProgressBarInfo>(ReportSecondaryProgress);
            Progress<string> secondaryProgressLabel = new Progress<string>(ReportSecondaryLabel);

            List<Recording> selectedRecordings = new List<Recording>();
            foreach (var item in lvRecordingsFound.SelectedItems)
            {
                Recording recording = item as Recording;
                selectedRecordings.Add(recording);
            }

            ExtractTabloRecordings extractor = new ExtractTabloRecordings()
            {
                TabloIPAddress = txtTabloIPAddress.Text,
                SelectedRecordings = selectedRecordings,
                OutputDirectory = OutputDirectory.Text,
                FFMPEGLocation = txtFFMPEGLocation.Text,
            };

            await extractor.ExtractRecordings(primaryProgressBar, primaryProgressLabel, secondaryProgressBar, secondaryProgressLabel);
        }

        internal void ReportPrimaryProgress(ProgressBarInfo progressInfo)
        {
            if (progressInfo.Maximum != null)
                extractProgress.Maximum = progressInfo.Maximum.Value;
            if (progressInfo.Value != null)
                extractProgress.Value = progressInfo.Value.Value;
        }
        
        internal void ReportSecondaryProgress(ProgressBarInfo progressInfo)
        {
            if (progressInfo.Maximum != null)
                downloadTsFilesProgress.Maximum = progressInfo.Maximum.Value;
            if (progressInfo.Value != null)
                downloadTsFilesProgress.Value = progressInfo.Value.Value;
        }

        internal void ReportSecondaryLabel(string message)
        {
            extractingFileCountLabel.Content = message;
        }

        internal void ReportPrimaryLabel(string message)
        {
            extractingRecordingLabel.Content = message;
        }

        private async void btnFindRecordings_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            try
            {
                downloadTsFilesProgress.Minimum = 0;
                downloadTsFilesProgress.Value = 0;

                Progress<ProgressBarInfo> secondaryProgressBar = new Progress<ProgressBarInfo>(ReportSecondaryProgress);
                Progress<string> secondaryProgressLabel = new Progress<string>(ReportSecondaryLabel);

                FindTabloRecordings tabloRecordings = new FindTabloRecordings()
                {
                    TabloIPAddress = txtTabloIPAddress.Text
                };

                ObservableCollection<Recording> recordings = await tabloRecordings.Find(secondaryProgressBar, secondaryProgressLabel);

                lvRecordingsFound.ItemsSource = recordings;
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvRecordingsFound.ItemsSource);
                view.SortDescriptions.Add(new SortDescription("Description", ListSortDirection.Ascending));
                btnSelectAll.IsEnabled = true;
                btnDeselectAll.IsEnabled = true;
                btnSelectSimilar.IsEnabled = true;
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }
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
