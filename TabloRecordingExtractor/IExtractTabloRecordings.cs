namespace TabloRecordingExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    public interface IExtractTabloRecordings
    {
        IPEndPoint TabloEndPoint { get; set; }
        List<Recording> SelectedRecordings { get; set; }
        string OutputDirectory { get; set; }
        string FFMPEGLocation { get; set; }
        Task ExtractRecordings(IProgress<ProgressBarInfo> primaryProgressBar, IProgress<string> primaryProgressLabel, IProgress<ProgressBarInfo> secondaryProgressBar, IProgress<string> secondaryLabel);
    }
}
