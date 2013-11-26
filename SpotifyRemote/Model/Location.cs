using Newtonsoft.Json;

namespace SpotifyRemote.Model
{
    public class Location
    {
        [JsonProperty("og")]
        public string Og { get; set; }
    }
}