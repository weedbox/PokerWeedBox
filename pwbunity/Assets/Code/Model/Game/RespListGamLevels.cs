using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model.Game
{
    public class RespListGameLevels
    {
        [JsonProperty("total")] public int Total;
        [JsonProperty("data")] public List<GameLevel> Data;
    }

    public class GameLevel
    {
        [JsonProperty("id")] public string ID;
        [JsonProperty("created_at")] public long CreatedAt;
        [JsonProperty("updated_at")] public long UpdatedAt;
        [JsonProperty("game_category_id")] public string GameCategoryID;
        [JsonProperty("name")] public string Name;
        [JsonProperty("note")] public string Note;
        [JsonProperty("image_url")] public string ImageURL;
        [JsonProperty("table_count")] public int TableCount;
        [JsonProperty("player_count")] public int PlayerCount;
    }
}