using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Code.Model
{
    public class RPCResponse<T>
    {
        [JsonProperty("jsonrpc")] public string JsonRpc;

        [JsonProperty("id")] public int Id;

        [JsonProperty("result")] [CanBeNull] public T Result;
        
        [JsonProperty("method")] [CanBeNull] public string Method;

        [JsonProperty("error")] [CanBeNull] public Error Error;
    }
}