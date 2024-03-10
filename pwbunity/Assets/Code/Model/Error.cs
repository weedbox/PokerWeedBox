using Newtonsoft.Json;

namespace Code.Model
{
    public class Error
    {
        [JsonProperty("code")] public int Code;

        [JsonProperty("message")] public string Message;

        public Error(int code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}