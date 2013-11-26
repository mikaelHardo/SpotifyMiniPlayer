using Newtonsoft.Json;

namespace SpotifyRemote.Model
{
    public class Error
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}