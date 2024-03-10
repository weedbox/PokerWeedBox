using Object = UnityEngine.Object;

namespace Code.Helper
{
    public class ConnectionHelper : RPCHelper
    {
        private static volatile ConnectionHelper _instance;
        private static readonly Object SyncRoot = new();

        public static ConnectionHelper Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (SyncRoot)
                {
                    if (_instance != null) return _instance;
                    _instance = new ConnectionHelper();
                }

                return _instance;
            }
        }
    }
}