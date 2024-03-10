using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Code.Model
{
    public class UpdateEvent<T>
    {
        [JsonProperty("event_name")] public string EventName;

        [JsonProperty("event")] [CanBeNull] public T Event;
    }
}