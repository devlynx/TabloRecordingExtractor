using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TabloRecordingExtractor
{
    class RecordingWatch
    {
        public string token { get; set; }
        public DateTime expires { get; set; }
        public string playlist_url { get; set; }
        public string bif_url_sd { get; set; }
        public string bif_url_hd { get; set; }
    }
}
