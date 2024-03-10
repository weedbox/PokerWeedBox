using Newtonsoft.Json;

namespace Code.Model.PlayerInfo
{
    public class RespGetPlayerLatest
    {
        [JsonProperty("id")] public string ID;
        [JsonProperty("uid")] public string Uid;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("cpp")] public string CPP;
        [JsonProperty("tickets")] public int Tickets;
    }
}