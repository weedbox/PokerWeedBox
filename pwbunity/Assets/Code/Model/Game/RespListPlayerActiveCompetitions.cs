using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model.Game
{
    public class ListPlayerActiveCompetitions
    {
        [JsonProperty("active_competitions")] public List<ActiveCompetitions> ActiveCompetitions;
    }

    public class ActiveCompetitions
    {
        [JsonProperty("competition_id")] public string CompetitionID;
        [JsonProperty("competition_mode")] public string CompetitionMode;
        [JsonProperty("competition_name")] public string CompetitionName;
        [JsonProperty("table_id")] public string TableID;
        [JsonProperty("table_name")] public string TableName;
        [JsonProperty("scene")] public string Scene;
        [JsonProperty("is_afk")] public bool IsAfk;
        [JsonProperty("player_status")] public string PlayerStatus;
        [JsonProperty("competition_status")] public string CompetitionStatus;
    }
}