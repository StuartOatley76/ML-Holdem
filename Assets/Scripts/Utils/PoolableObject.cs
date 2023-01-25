using System;
using UnityEngine;

namespace Utils {

    /// <summary>
    /// Abstract class to allow use with ObjectPools
    /// TODO - When Unity supports default interface methods change this to an interface
    /// </summary>
    public abstract class PoolableObject : MonoBehaviour {
        /// <summary>
        /// Event triggered when the object should be returned to the pool
        /// </summary>
        private EventHandler returnToPoolEvent;

        public void AttachToPool(EventHandler eventHandler) {
            returnToPoolEvent += eventHandler;
        }

        /// <summary>
        /// Resets the object, returns it to the pool, and deactivates it
        /// </summary>
        public void ReturnToPool() {
            Reset();
            returnToPoolEvent?.Invoke(this, EventArgs.Empty);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// override to do any work that needs doing to reset the object to it's initial state. Called by ReturnToPool
        /// </summary>
        protected virtual void Reset() {
        }
    }
}