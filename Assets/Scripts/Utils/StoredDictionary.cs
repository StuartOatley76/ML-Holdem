using System.Collections.Generic;
using UnityEngine;

namespace Utils {
    /// <summary>
    /// Wrapper class for dictionary to implement automatic loading and saving in Unity
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    public class StoredDictionary<TKey, TValue> {

        /// <summary>
        /// The dictionary that is loaded and saved
        /// </summary>
        protected Dictionary<TKey, TValue> dictionary;

        /// <summary>
        /// Whether the contents of the dictionary have changed since loaded
        /// </summary>
        protected bool hasDictionaryChanged = false;

        /// <summary>
        /// The path for the dictionary
        /// </summary>
        private string path;

        //Impementation of Dictionary's getters
        public int Count { get { return dictionary.Count; } }
        public Dictionary<TKey, TValue>.KeyCollection Keys { get { return dictionary.Keys; } }
        public Dictionary<TKey, TValue>.ValueCollection Values { get { return dictionary.Values; } }

        public IEqualityComparer<TKey> Comparer { get { return dictionary.Comparer; } }

        /// <summary>
        /// Filename needed so we hide default constructor
        /// </summary>
        private StoredDictionary() {
        }

        /// <summary>
        /// Loads the dictionary if possible. Allows for the creation of a new dictionary that will be saved.
        /// If null or empty string is passed in, will act as a normal dictionary, with no loading or saving
        /// </summary>
        /// <param name="dictionaryFileName">Filename for the dictionary</param>
        public StoredDictionary(in string dictionaryFileName) {

            if (dictionaryFileName == null || dictionaryFileName == string.Empty) {
                dictionary = new Dictionary<TKey, TValue>();
                return;
            }

            Application.quitting += Save;
            path = Application.streamingAssetsPath + dictionaryFileName;
            HelperFunctions.Helpers.LoadDictionary(path, out dictionary);
            if (dictionary.Count == 0) {
                hasDictionaryChanged = true;
            }

        }

        /// <summary>
        /// Saves the dictionary if it has changed and we have a path
        /// </summary>
        private void Save() {

            if (path == null || path == string.Empty) {
                return;
            }

            if (hasDictionaryChanged) {
                HelperFunctions.Helpers.SaveDictionary(path, dictionary);
            }
        }

        // Implementation of Dictionary's normal functions. Calls the dictionary's functions, marking hasDictionaryChanged to true if the function changes the dictionary

        public void Add(TKey hashCode, TValue value) {
            hasDictionaryChanged = true;
            if (dictionary.ContainsKey(hashCode)) {
                dictionary[hashCode] = value;
                return;
            }
            dictionary.Add(hashCode, value);
        }

        public void Clear() {
            hasDictionaryChanged = true;
            dictionary.Clear();
        }

        public bool ContainsKey(TKey hashCode) {
            return dictionary.ContainsKey(hashCode);
        }

        public bool ContainsValue(TValue value) {
            return dictionary.ContainsValue(value);
        }

        public override bool Equals(object obj) {
            if (obj.GetType() != GetType()) {
                return false;
            }
            StoredDictionary<TKey, TValue> objDict = (StoredDictionary<TKey, TValue>)obj;
            if (objDict.path != path) {
                return false;
            }
            return objDict.dictionary.Equals(dictionary);
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() {
            return dictionary.GetEnumerator();
        }

        public override int GetHashCode() {
            return dictionary.GetHashCode();
        }

        public bool Remove(TKey key) {
            bool removed = dictionary.Remove(key);
            if (removed) {
                hasDictionaryChanged = true;
            }
            return removed;
        }

        public override string ToString() {
            return dictionary.ToString();
        }

        public bool TryGetValue(TKey hashCode, out TValue found) {
            return dictionary.TryGetValue(hashCode, out found);
        }


    }
}