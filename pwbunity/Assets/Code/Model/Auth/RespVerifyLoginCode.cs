using Newtonsoft.Json;

namespace Code.Model.Auth
{
    public class RespVerifyLoginCode
    {
        [JsonProperty("id")] public string ID;
        [JsonProperty("uid")] public string Uid;
        [JsonProperty("name")] public string Name;
        [JsonProperty("phone")] public string Phone;
        [JsonProperty("token")] public string Token;
        [JsonProperty("refresh_token")] public string RefreshToken;
    }
}