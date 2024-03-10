using Newtonsoft.Json;

namespace Code.Model.Auth
{
    public class ReqVerifyLoginCode
    {
        [JsonProperty("phone")] public string Phone;
        [JsonProperty("code")] public string Code;
    }
}