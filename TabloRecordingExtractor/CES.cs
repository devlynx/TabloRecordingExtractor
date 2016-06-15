namespace TabloRecordingExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    public class CPES
    {
        public int http { get; set; }
        public IPAddress public_ip { get; set; }
        public int ssl { get; set; }
        public string host { get; set; }
        public IPAddress private_ip { get; set; }
        public int slip { get; set; }
        public string serverid { get; set; }
        public DateTime inserted { get; set; }
        public string board_type { get; set; }
        public string server_version { get; set; }
        public string name { get; set; }
        public DateTime modified { get; set; }
        public bool roku { get; set; }
        public DateTime last_seen { get; set; }

        public override string ToString()
        {
            return String.Format("{0} ({1})", name, private_ip);
        }
    }

    class CpesWrapper
    {
        public bool success { get; set; }
        public List<CPES> cpes { get; set; }
    }

    // example
    //  {
    //    "cpes": [
    //    {
    //      "http": 21000,
    //      "public_ip": "###.###.###.###",
    //      "ssl": 21001,
    //      "host": "tablo-dual",
    //      "private_ip": "###.###.###.###",
    //      "slip": 21002,
    //      "serverid": "SID_#############",
    //      "inserted": "2014-11-26 01:33:00.580696+00:00",
    //      "board_type": "dual",
    //      "server_version": "2.2.8rc1532715",
    //      "name": "Periwinkle Tablo",
    //      "modified": "2016-06-06 23:25:23.236685+00:00",
    //      "roku": 1,
    //      "last_seen": "2016-06-06 23:25:23.235059+00:00"
    //    }
    //  ],
    //  "success": true
    //}
}
