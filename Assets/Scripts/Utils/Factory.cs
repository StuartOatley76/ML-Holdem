using UnityEngine;

namespace Utils {

    /// <summary>
    /// Generic factory for creation of monobehaviours. Singleton
    /// </summary>
    /// <typeparam name="T">Type for factory to create</typeparam>
    public class Factory<T> : UnitySingleton<Factory<T>> where T : MonoBehaviour {

        [SerializeField] protected T prefab;

        /// <summary>
        /// Prefab path in resources folder. Only used if prefab hasn't been added to serialized field
        /// </summary>
        [SerializeField] protected string prefabPath;

        /// <summary>
        /// Checks if prefab already exists from prefab being serialized, if not loads it from the prefab path
        /// </summary>
        protected override void Awake() {
            base.Awake();
            if(prefab != null) {
                return;
            }
            GameObject go = Resources.Load(prefabPath) as GameObject;
            prefab = go.GetComponent<T>();

        }

        /// <summary>
        /// Creates an instance of the prefab with T component and returns it, or null if no prefab exists
        /// </summary>
        /// <returns></returns>
        public virtual T GetInstance() {
            if (prefab != null) {
                return Instantiate(prefab);
            }
            return null;
        }
    }
}
