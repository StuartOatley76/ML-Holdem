using System.Collections.Generic;
using UnityEngine;

namespace Utils {

    /// <summary>
    /// Generic factory for random creation of T from list of instances
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RandomFactory<T> : Factory<T> where T : MonoBehaviour {

        /// <summary>
        /// List of instances
        /// </summary>
        [SerializeField] protected List<T> instances;

        /// <summary>
        /// returns an instance from list, or null if list entry is null
        /// </summary>
        /// <returns></returns>
        public override T GetInstance() {
            int index = Random.Range(0, instances.Count);
            if (instances[index] != null) {
                return Instantiate(instances[index]);
            }
            return null;

        }
    }
}
