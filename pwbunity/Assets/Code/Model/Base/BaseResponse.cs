using Newtonsoft.Json;

namespace Code.Model.Base
{
    public class BaseResponse<T>
    {
        [JsonProperty("data")] public T Data;
    }
}