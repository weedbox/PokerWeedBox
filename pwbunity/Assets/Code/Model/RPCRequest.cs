using System.Collections.Generic;
using Newtonsoft.Json;

namespace Code.Model
{
    public class RPCRequest
    {
        [JsonProperty("jsonrpc")] public string JsonRpc;

        // [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonProperty("id")] public int Id;

        [JsonProperty("method")] public string Method;

        [JsonProperty("params")] public List<object> Parameters;
    }
}