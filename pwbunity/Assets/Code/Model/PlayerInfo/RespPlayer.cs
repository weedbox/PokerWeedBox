using Newtonsoft.Json;

namespace Code.Model.PlayerInfo
{
    public class RespPlayer
    {
        [JsonProperty("id")] public string ID;
        [JsonProperty("uid")] public string Uid;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("avatar_url")] public string AvatarURL;
    }
}