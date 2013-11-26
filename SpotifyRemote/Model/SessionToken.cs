using Newtonsoft.Json;

namespace SpotifyRemote.Model
{
    public class SessionToken
    {
        [JsonProperty("error")]
        public Error Error { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}