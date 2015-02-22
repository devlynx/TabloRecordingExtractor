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

namespace TabloRecordingExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string tabloIPAddress = "192.168.1.184";
        bool showMovies = false;
        bool showTV = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void FindRecordings_Click(object sender, RoutedEventArgs e)
        {
            List<int> foundRecordings = GetRecordingList(tabloIPAddress);
            List<Recording> recordings = new List<Recording>();

            foreach (var foundRecording in foundRecordings)
            {
                RecordingMetadata metadata = GetRecordingMetadata(tabloIPAddress, foundRecording);
                if(metadata != null)
                {
                    /*
                    ListBoxItem newItem = new ListBoxItem();
                    newItem.Tag = foundRecording;
                    if(metadata.recEpisode != null)
                    {
                        newItem.Content = String.Format("{0} - S{1}E{2} - {3} ({4}).mp4", metadata.recSeries.jsonForClient.title, metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"), metadata.recEpisode.jsonForClient.episodeNumber.ToString("00"), metadata.recEpisode.jsonForClient.title, metadata.recEpisode.jsonForClient.airDate);
                        lbRecordingsFound.Items.Add(newItem);
                    }
                    else if (metadata.recMovie != null)
                    {
                        newItem.Content = String.Format("{0} ({1}).mp4", metadata.recMovie.jsonForClient.title, metadata.recMovie.jsonForClient.releaseYear);
                        lbRecordingsFound.Items.Add(newItem);
                    }
                    */

                    Recording recording = new Recording();
                    recording.Id = foundRecording;
                    if (metadata.recEpisode != null)
                    {
                        recording.Description = String.Format("{0} - S{1}E{2} - {3}", metadata.recSeries.jsonForClient.title, metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"), metadata.recEpisode.jsonForClient.episodeNumber.ToString("00"), metadata.recEpisode.jsonForClient.title);
                        recording.RecordedOnDate = DateTime.Parse(metadata.recEpisode.jsonForClient.airDate);
                    }
                    else if (metadata.recMovie != null)
                    {
                        recording.Description = String.Format("{0} ({1})", metadata.recMovie.jsonForClient.title, metadata.recMovie.jsonForClient.releaseYear);
                        recording.RecordedOnDate = DateTime.Parse(metadata.recMovieAiring.jsonForClient.airDate);
                    }
                    recordings.Add(recording);
                }
            }
            lvRecordingsFound.ItemsSource = recordings;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvRecordingsFound.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Description", ListSortDirection.Ascending));
        }

        private void Extract_Click(object sender, RoutedEventArgs e)
        {
            //// Instantiate the dialog box
            //StatusDialogWindow dlg = new StatusDialogWindow();

            //// Configure the dialog box
            //dlg.Owner = this;
            ////dlg.

            //// Open the dialog box modally 
            //dlg.ShowDialog();

            foreach (ListBoxItem itemToExtract in lbRecordingsToExtract.Items)
            {
                int recordingID = (int)itemToExtract.Tag;
                RecordingMetadata metadata = GetRecordingMetadata(tabloIPAddress, recordingID);
                string OutputFile = String.Format("{0}\\{1} - S{2}E{3} - {4}.mp4", OutputDirectory.Text, CleanFileName(metadata.recSeries.jsonForClient.title), metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"), metadata.recEpisode.jsonForClient.episodeNumber.ToString("00"), CleanFileName(metadata.recEpisode.jsonForClient.title));
                GetRecordingVideo(tabloIPAddress, recordingID, OutputFile);
            }
        }

        private void OutputDirectory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            OutputDirectory.Text = dialog.SelectedPath;
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

        private void GetRecordingVideo(string IPAddress, int RecordingID, string OutputFile)
        {
            if (!Directory.Exists(Path.GetTempPath() + "\\TempTabloExtract"))
            {
                Directory.CreateDirectory(Path.GetTempPath() + "\\TempTabloExtract");
            }

            string webPageText;
            using (WebClient client = new WebClient())
            {
                webPageText = client.DownloadString("http://" + IPAddress + ":18080/pvr/" + RecordingID.ToString() + "/segs/");
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
            foreach (var tsFileName in tsFileNames)
            {
                using (WebClient Client = new WebClient())
                {
                    string downloadURL = String.Format("http://{0}:18080/pvr/{1}/segs/{2}", IPAddress, RecordingID.ToString(), tsFileName);
                    string outputFileName = String.Format("{0}\\TempTabloExtract\\{1}-{2}", Path.GetTempPath(), RecordingID.ToString(), tsFileName);

                    Client.DownloadFile(downloadURL, outputFileName);
                    outputFileNames.Add(outputFileName);
                }
            }

            ProcessVideosInFFMPEG(outputFileNames, OutputFile);

            foreach (var outputFileName in outputFileNames)
            {
                if (File.Exists(outputFileName))
                {
                    File.Delete(outputFileName);
                }
            }

            ProcessVideoInHandbrake(OutputFile, OutputFile.Replace(".mp4", ".mkv"));

            if (File.Exists(OutputFile))
            {
                File.Delete(OutputFile);
            }

        }

        private void ProcessVideosInFFMPEG(List<string> tsFileNames, string OutputFile)
        {
            string fileNamesConcatString = String.Join("|", tsFileNames.Select(x => x.ToString()).ToArray());
            
            using(Process proc = new Process())
            { 
                proc.StartInfo.FileName = "C:\\Temp\\ffmpeg\\bin\\ffmpeg.exe";
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
            using(Process proc = new Process())
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

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            //ListBoxItem itemToMove = (ListBoxItem)lbRecordingsFound.SelectedItem;
            //ListBoxItem newItem = new ListBoxItem();
            //newItem.Tag = itemToMove.Tag;
            //newItem.Content = itemToMove.Content;
            //lbRecordingsToExtract.Items.Add(newItem);
            //lbRecordingsFound.Items.RemoveAt(lbRecordingsFound.SelectedIndex);
            List<Recording> recordings = new List<Recording>();
            

            foreach (var selectedItem in lvRecordingsFound.SelectedItems)
            {
                Recording recording = selectedItem as Recording;

            }
            /*
                    recording.Id = foundRecording;
                    if (metadata.recEpisode != null)
                    {
                        recording.Description = String.Format("{0} - S{1}E{2} - {3}", metadata.recSeries.jsonForClient.title, metadata.recEpisode.jsonForClient.seasonNumber.ToString("00"), metadata.recEpisode.jsonForClient.episodeNumber.ToString("00"), metadata.recEpisode.jsonForClient.title);
                        recording.RecordedOnDate = DateTime.Parse(metadata.recEpisode.jsonForClient.airDate);
                    }
                    else if (metadata.recMovie != null)
                    {
                        recording.Description = String.Format("{0} ({1})", metadata.recMovie.jsonForClient.title, metadata.recMovie.jsonForClient.releaseYear);
                        recording.RecordedOnDate = DateTime.Parse(metadata.recMovieAiring.jsonForClient.airDate);
                    }
                    recordings.Add(recording);
             * */
            lvRecordingsFound.ItemsSource = recordings;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvRecordingsFound.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Description", ListSortDirection.Ascending));
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            //ListBoxItem itemToMove = (ListBoxItem)lbRecordingsToExtract.SelectedItem;
            //ListBoxItem newItem = new ListBoxItem();
            //newItem.Tag = itemToMove.Tag;
            //newItem.Content = itemToMove.Content;
            //lbRecordingsFound.Items.Add(newItem);
            //lbRecordingsToExtract.Items.RemoveAt(lbRecordingsToExtract.SelectedIndex);

        }
    }

    public class Recording
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime RecordedOnDate { get; set; }
    }
}
