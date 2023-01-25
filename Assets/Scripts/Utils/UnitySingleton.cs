using System;
using UnityEngine;

namespace Utils {

    /// <summary>
    /// Generic singleton for Unity
    /// </summary>
    /// <typeparam name="T">Type of singleton</typeparam>
    public abstract class UnitySingleton<T> : MonoBehaviour where T : UnitySingleton<T> {

        /// <summary>
        /// Singleton instance
        /// </summary>
        protected static T instance;

        [SerializeField] private bool dontDestroyOnLoad;
        public static T Instance {
            get { return instance; }
            protected set { instance = value; }
        }

        /// <summary>
        /// Sets up the singleton - Always call base.Awake() if overridden
        /// </summary>
        protected virtual void Awake() {
            if (instance != null) {
                Destroy(this);
                return;
            }
            instance = this as T;
            if (dontDestroyOnLoad) {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
