using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model.Match
{
    public class ReqUpdateCompetitionEventSubscribeStates
    {
        [JsonProperty("event_subscribe_states")] public List<UpdateCompetitionEventSubscribeStates> EventSubscribeStates = new();
    }
    
    public class UpdateCompetitionEventSubscribeStates
    {
        [JsonProperty("competition_id")] public string CompetitionID;
        [JsonProperty("is_event_subscribed")] public bool IsEventSubscribed;

        public UpdateCompetitionEventSubscribeStates(string competitionID, bool isEventSubscribed)
        {
            CompetitionID = competitionID;
            IsEventSubscribed = isEventSubscribed;
        }
    }
}