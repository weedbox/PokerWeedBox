using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model.Game
{
    public class RespListCompetitions
    {
        [JsonProperty("total")] public int Total;
        [JsonProperty("data")] public List<ListCompetition> Data;
    }

    public class ListCompetition
    {
        [JsonProperty("competition_id")] public string CompetitionID;
        [JsonProperty("name")] public string Name;
        [JsonProperty("mode")] public string Mode;
    }
}