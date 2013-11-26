using Newtonsoft.Json;

namespace SpotifyRemote.Model
{
    public class Resource
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("location")]
        public Location Location { get; set; }
    }
}