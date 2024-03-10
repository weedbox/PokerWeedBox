using Newtonsoft.Json;

namespace Code.Model.Auth
{
    public class ReqSignUp
    {
        [JsonProperty("phone")] public string Phone;
    }
}