namespace TabloRecordingExtractor
{
    using System;
    using Newtonsoft.Json;
    using RestSharp;
    using System.Collections.Generic;

    static class TabloAPI
    {
        public static List<CPES> GetCpesList()
        {
            List<CPES> emptyList = new List<CPES>();

            RestClient client = new RestClient("http://api.Tablotv.com");
            RestRequest request = new RestRequest("assocserver/getipinfo/", Method.GET);

            IRestResponse response = client.Execute(request);
            string content = response.Content;

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressJsonConverter());
            settings.Formatting = Formatting.Indented;

            try
            {
                CpesWrapper wrapper = JsonConvert.DeserializeObject<CpesWrapper>(content, settings);
                if (wrapper.success)
                {
                    return wrapper.cpes;
                }
            }
            catch(Exception e)
            {
                return emptyList;
            }
            return emptyList;
        }
    }
}
