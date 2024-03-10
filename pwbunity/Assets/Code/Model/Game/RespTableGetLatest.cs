using Code.Model.Game.NotificationEvent;
using Newtonsoft.Json;

namespace Code.Model.Game
{
    public class RespTableGetLatest
    {
        [JsonProperty("table")] public Table Table;
    }
}