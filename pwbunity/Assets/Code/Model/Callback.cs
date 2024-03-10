using UnityEngine.Events;

namespace Code.Model
{
    public class Callback<T>
    {
        public readonly string Method;
        public readonly UnityAction<T> Action;

        public Callback(string method, UnityAction<T> action)
        {
            Method = method;
            Action = action;
        }
    }
}