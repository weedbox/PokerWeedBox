using Code.Base;

namespace Code.Prefab.Common
{
    public class CommonCloseablePrefab : BasePrefabWithCommonSound
    {
        public void Close()
        {
            Destroy(gameObject);
        }
    }
}