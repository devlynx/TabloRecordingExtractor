namespace TabloRecordingExtractor
{
    using System;
    class RecordingWatch
    {
        public string token { get; set; }
        public DateTime expires { get; set; }
        public string playlist_url { get; set; }
        public string bif_url_sd { get; set; }
        public string bif_url_hd { get; set; }
    }
}
