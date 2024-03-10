using Newtonsoft.Json;

namespace Code.Model.Auth
{
    public class RespLoginCodeSend
    {
        [JsonProperty("success")] public bool IsSuccess;
        [JsonProperty("is_sign_up")] public bool IsSignUp;
    }
}