namespace TabloRecordingExtractor
{
    internal interface IMediaNamingConvention
    {
        string GetEpisodeDescription(Recording recording);
        string GetMovieDescription(Recording recording);
        string GetSportsDescription(Recording recording);
        string GetManualDescription(Recording recording);

        string GetEpisodeOutputFileName(string outputDirectory, Recording recording);
        string GetMovieOutputFileName(string outputDirectory, Recording recording);
        string GetSportsOutputFileName(string outputDirectory, Recording recording);
        string GetManualOutputFileName(string outputDirectory, Recording recording);
        string GetOtherOutupuFileName(string outputDirectory, Recording recording);
    }
}