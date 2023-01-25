using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils {

    /// <summary>
    /// Generic class to create a singleton pool of T
    /// </summary>
    /// <typeparam name="T">Poolable object</typeparam>
    public class ObjectPool<T> : Factory<T> where T : PoolableObject //TODO - When Unity supports default interface methods update this to be where T : Monobehaviour, IPoolableObject 
    {
        /// <summary>
        /// The pool
        /// </summary>
        private Stack<T> pool;

        /// <summary>
        /// The starting size of the pool.
        /// </summary>
        [SerializeField] private int initialPoolSize = 20;

        /// <summary>
        /// number of times the pool has been expanded.
        /// The pool is expanded by initialPoolSize*numberOfExpands
        /// </summary>
        private int numberOfExpands = 1;

        private void Start() {
            pool = new Stack<T>(initialPoolSize);
            ExpandPool();
        }

        /// <summary>
        /// Returns an instance of T. Expands the pool if none left
        /// </summary>
        /// <returns></returns>
        public override T GetInstance() {
            if(pool.Count == 0) {
                ExpandPool();
            }
            T instance = pool.Pop();
            instance.gameObject.SetActive(true);
            return instance;
        }

        /// <summary>
        /// Increases the pool size by initialPoolSize * numberOfExpands
        /// </summary>
        private void ExpandPool() {
            for(int i = 0; i < initialPoolSize * numberOfExpands; i++) {
                pool.Push(CreateNew());
            }
            numberOfExpands++;
        }

        /// <summary>
        /// Creates a new T, connects it to the pool and deactivates it
        /// </summary>
        /// <returns></returns>
        private T CreateNew() {
            T newObject = Instantiate(prefab);
            newObject.AttachToPool(ReturnToPool);
            newObject.gameObject.SetActive(false);
            return newObject;
        }

        /// <summary>
        /// Method called by PoolableObject's ReturnToPool event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturnToPool(object sender, EventArgs e) {
            pool.Push(sender as T);
        }
    }
}
