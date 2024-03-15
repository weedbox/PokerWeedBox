using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model.ClientAgent
{
    public class RespCheckLoginStatus
    {
        [JsonProperty("online_states")] public Dictionary<string, bool> OnlineStates;
    }
}