using System.Collections.Generic;

namespace TabloRecordingExtractor
{
    public class Relationships
    {
        public int recSeason { get; set; }
        public int recSeries { get; set; }
        public int recMovie { get; set; }
        public int recChannel { get; set; }
        public int recManualProgram { get; set; }
    }

    public class Video
    {
        public string state { get; set; }
        public long size { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public double duration { get; set; }
        public double scheduleOffsetStart { get; set; }
        public double scheduleOffsetEnd { get; set; }
    }

    public class User
    {
        public string type { get; set; }
        public bool watched { get; set; }
        public bool @protected { get; set; }
        public double position { get; set; }
    }

    public class JsonForClient
    {
        public string airDate { get; set; }
        public string description { get; set; }
        public int episodeNumber { get; set; }
        public string originalAirDate { get; set; }
        public double scheduleDuration { get; set; }
        public int seasonNumber { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public Relationships relationships { get; set; }
        public Video video { get; set; }
        public User user { get; set; }
        public int objectID { get; set; }
        public List<string> qualifiers { get; set; }
    }

    public class Image
    {
        public string type { get; set; }
        public int imageID { get; set; }
        public string imageType { get; set; }
        public string imageStyle { get; set; }
    }

    public class ImageJson
    {
        public List<Image> images { get; set; }
    }

    public class QualityRating
    {
        public string ratingsBody { get; set; }
        public string value { get; set; }
    }
    
    public class Program
    {
        public string descriptionLang { get; set; }
        public string entityType { get; set; }
        public int episodeNum { get; set; }
        public string episodeTitle { get; set; }
        public List<string> genres { get; set; }
        public string longDescription { get; set; }
        public string origAirDate { get; set; }
        public string rootId { get; set; }
        public int seasonNum { get; set; }
        public string seriesId { get; set; }
        public string shortDescription { get; set; }
        public string subType { get; set; }
        public string title { get; set; }
        public string titleLang { get; set; }
        public string tmsId { get; set; }
        public List<string> topCast { get; set; }
        public QualityRating qualityRating { get; set; }
        public int releaseYear { get; set; }
    }

    public class Rating
    {
        public string body { get; set; }
        public string code { get; set; }
    }

    public class Station
    {
        public string callSign { get; set; }
        public string stationId { get; set; }
    }

    public class JsonFromTribune
    {
        public List<string> channels { get; set; }
        public int duration { get; set; }
        public string endTime { get; set; }
        public Program program { get; set; }
        public List<string> qualifiers { get; set; }
        public List<Rating> ratings { get; set; }
        public string startTime { get; set; }
        public Station station { get; set; }
        public string stationId { get; set; }
    }

    public class RecEpisode
    {
        public JsonForClient jsonForClient { get; set; }
        public ImageJson imageJson { get; set; }
        public JsonFromTribune jsonFromTribune { get; set; }
    }

    public class RecMovieAiring
    {
        public JsonForClient jsonForClient { get; set; }
        public ImageJson imageJson { get; set; }
        public JsonFromTribune jsonFromTribune { get; set; }
    }

    public class RecManualProgramAiring
    {
        public JsonForClient jsonForClient { get; set; }
        public ImageJson imageJson { get; set; }
    }
    
    public class Award
    {
        public string category { get; set; }
        public string name { get; set; }
        public string nominee { get; set; }
        public bool won { get; set; }
        public int year { get; set; }
    }

    public class Relationships2
    {
        public List<int> genres { get; set; }
    }

    public class JsonForClient2
    {
        public string description { get; set; }
        public double duration { get; set; }
        public string originalAirDate { get; set; }
        public string title { get; set; }
        public List<string> cast { get; set; }
        public List<Award> awards { get; set; }
        public Relationships2 relationships { get; set; }
        public int objectID { get; set; }
        public string type { get; set; }
        public string mpaaRating { get; set; }
        public string plot { get; set; }
        public int releaseYear { get; set; }
        public double runtime { get; set; }
        public List<string> directors { get; set; }
        public double qualityRating { get; set; }
    }

    public class Image2
    {
        public string type { get; set; }
        public int imageID { get; set; }
        public string imageType { get; set; }
        public string imageStyle { get; set; }
    }

    public class ImageJson2
    {
        public List<Image2> images { get; set; }
    }

    public class Rating2
    {
        public string body { get; set; }
        public string code { get; set; }
    }

    public class Recommendation
    {
        public string tmsId { get; set; }
        public string rootId { get; set; }
        public string title { get; set; }
    }

    public class PreferredImage
    {
        public string uri { get; set; }
        public string height { get; set; }
        public string width { get; set; }
        public string primary { get; set; }
        public string category { get; set; }
        public string tier { get; set; }
        public Caption caption { get; set; }
    }

    public class Award2
    {
        public string awardId { get; set; }
        public string awardName { get; set; }
        public string category { get; set; }
        public string awardCatId { get; set; }
        public string year { get; set; }
        public string recipient { get; set; }
        public string personId { get; set; }
        public string won { get; set; }
    }

    public class QualityRating2
    {
        public string ratingsBody { get; set; }
        public string value { get; set; }
    }

    public class Cast
    {
        public string personId { get; set; }
        public string nameId { get; set; }
        public string name { get; set; }
        public string role { get; set; }
        public string characterName { get; set; }
        public string billingOrder { get; set; }
    }

    public class Crew
    {
        public string personId { get; set; }
        public string nameId { get; set; }
        public string name { get; set; }
        public string role { get; set; }
        public string billingOrder { get; set; }
    }

    public class Keywords
    {
        public List<string> Mood { get; set; }
        public List<string> Theme { get; set; }
        public List<string> Subject { get; set; }
        public List<string> Setting { get; set; }
        public List<string> Character { get; set; }
    }

    public class JsonFromTribune2
    {
        public string tmsId { get; set; }
        public string rootId { get; set; }
        public string seriesId { get; set; }
        public string title { get; set; }
        public string titleLang { get; set; }
        public string shortDescription { get; set; }
        public string longDescription { get; set; }
        public string descriptionLang { get; set; }
        public string subType { get; set; }
        public List<Rating2> ratings { get; set; }
        public List<string> genres { get; set; }
        public List<Recommendation> recommendations { get; set; }
        public PreferredImage preferredImage { get; set; }
        public string origAirDate { get; set; }
        public List<Award2> awards { get; set; }
        public List<Cast> cast { get; set; }
        public List<Crew> crew { get; set; }
        public Keywords keywords { get; set; }
        public string totalSeasons { get; set; }
        public int totalEpisodes { get; set; }
        public string entityType { get; set; }
        public int releaseYear { get; set; }
        public QualityRating2 qualityRating { get; set; }
        public List<string> advisories { get; set; }
        public List<string> directors { get; set; }
        public string runTime { get; set; }
    }

    public class Caption
    {
        public string content { get; set; }
        public string lang { get; set; }
    }

    public class Caption2
    {
        public string content { get; set; }
        public string lang { get; set; }
    }

    public class ImageJsonFromTribune
    {
        public string uri { get; set; }
        public string height { get; set; }
        public string width { get; set; }
        public string primary { get; set; }
        public string category { get; set; }
        public string tier { get; set; }
        public Caption caption { get; set; }
    }

    public class RecSeries
    {
        public JsonForClient2 jsonForClient { get; set; }
        public ImageJson2 imageJson { get; set; }
        public JsonFromTribune2 jsonFromTribune { get; set; }
        public List<ImageJsonFromTribune> imageJsonFromTribune { get; set; }
    }

    public class Relationships3
    {
        public int recSeries { get; set; }
    }

    public class JsonForClient3
    {
        public int seasonNumber { get; set; }
        public Relationships3 relationships { get; set; }
        public int objectID { get; set; }
        public string type { get; set; }
    }

    public class RecSeason
    {
        public JsonForClient3 jsonForClient { get; set; }
    }

    public class RecMovie
    {
        public JsonForClient2 jsonForClient { get; set; }
        public ImageJson2 imageJson { get; set; }
        public JsonFromTribune2 jsonFromTribune { get; set; }
        public List<ImageJsonFromTribune> imageJsonFromTribune { get; set; }
    }
    
    public class RecManualProgram
    {
        public JsonForClient2 jsonForClient { get; set; }
    }

    public class RecordingMetadata
    {
        public RecEpisode recEpisode { get; set; }
        public RecSeries recSeries { get; set; }
        public RecSeason recSeason { get; set; }
        public RecMovieAiring recMovieAiring { get; set; }
        public RecMovie recMovie { get; set; }
        public RecManualProgramAiring recManualProgramAiring { get; set; }
        public RecManualProgram recManualProgram { get; set; }
    }
}
