using Code.Model.Game.NotificationEvent;
using Newtonsoft.Json;

namespace Code.Model.Game
{
    public class RespCompetitionGetLatest
    {
        [JsonProperty("competition")] public Competition Competition;
    }
}