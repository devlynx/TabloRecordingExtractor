namespace TabloRecordingExtractor
{
    using System.Collections.ObjectModel;

    internal interface IMediaNamingConvention
    {
        string GetEpisodeDescription(Recording recording);
        string GetMovieDescription(Recording recording);
        string GetSportsDescription(Recording recording);
        string GetManualDescription(Recording recording);
        string GetS00E00Description(string outputDirectory, Recording recording, ObservableCollection<Recording> recordings);

        string GetEpisodeOutputFileName(string outputDirectory, Recording recording);
        string GetMovieOutputFileName(string outputDirectory, Recording recording);
        string GetSportsOutputFileName(string outputDirectory, Recording recording);
        string GetManualOutputFileName(string outputDirectory, Recording recording);
        string GetOtherOutputFileName(string outputDirectory, Recording recording);
    }
}