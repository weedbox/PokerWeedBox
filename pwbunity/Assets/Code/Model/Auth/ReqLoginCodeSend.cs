using Newtonsoft.Json;

namespace Code.Model.Auth
{
    public class ReqLoginCodeSend
    {
        [JsonProperty("phone")] public string Phone;
        [JsonProperty("language")] public string Language;
    }
}