using Newtonsoft.Json;

namespace Code.Model.Game.NotificationEvent
{
    public class AutoModeUpdated
    {
        [JsonProperty("competition_id")] public string CompetitionID;
        [JsonProperty("table_id")] public string TableID;
        [JsonProperty("is_on")] public bool IsOn;
    }
}