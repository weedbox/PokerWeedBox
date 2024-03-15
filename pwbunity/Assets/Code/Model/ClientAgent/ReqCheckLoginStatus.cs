using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model.ClientAgent
{
    public class ReqCheckLoginStatus
    {
        [JsonProperty("phones")] public List<string> Phones;
    }
}