using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model.Game
{
    public class RespListGame
    {
        [JsonProperty("games")] public List<Game> Games;
    }

    public class Game
    {
        [JsonProperty("id")] public string ID;
        [JsonProperty("name")] public string Name;
        [JsonProperty("image_url")] public string ImageURL;
        [JsonProperty("categories")] public List<Category> Categories;
    }

    public class Category
    {
        [JsonProperty("id")] public string ID;
        [JsonProperty("name")] public string Name;
        [JsonProperty("image_url")] public string ImageURL;
        [JsonProperty("tag")] public string Tag;
        [JsonProperty("url")] public string URL;
        [JsonProperty("is_active")] public bool IsActive;
    }
}