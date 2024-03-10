using Newtonsoft.Json;

namespace Code.Model.System
{
    public class RespDeepPing
    {
        [JsonProperty("client_timestamp")] public readonly long ClientTimestamp;

        [JsonProperty("server_timestamp")] public readonly long ServerTimestamp;
    }
}