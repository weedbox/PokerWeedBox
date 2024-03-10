using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model.Game
{
    public class ReqListCompetitions
    {
        [JsonProperty("page")] public int Page;
        [JsonProperty("limit")] public int Limit;
        [JsonProperty("game_category_id")] public string GameCategoryID;
        [JsonProperty("game_level_id")] public string GameLevelID;
        [JsonProperty("ticket_cpp_value")] public string TicketCPPValue;
        [JsonProperty("statuses")] public List<string> Statuses;

        public ReqListCompetitions(int page, int limit, string gameCategoryID, string gameLevelID, string ticketCPPValue, List<string> statuses)
        {
            Page = page;
            Limit = limit;
            GameCategoryID = gameCategoryID;
            GameLevelID = gameLevelID;
            TicketCPPValue = ticketCPPValue;
            Statuses = statuses;
        }
    }
}