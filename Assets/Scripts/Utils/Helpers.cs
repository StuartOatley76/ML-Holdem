using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Utils {

    namespace HelperFunctions {

        /// <summary>
        /// Class to provide static helper functions
        /// </summary>
        public static class Helpers {
            
            /// <summary>
            /// Creates a new array consisting of all elements from all arrays
            /// If all arrays are null, will return null
            /// </summary>
            /// <typeparam name="T">Type of arrays to be merged</typeparam>
            /// <param name="arrays">Arrays to be merged</param>
            /// <returns>Array containing all elements from the provided arrays</returns>
            public static T[] MergeArrays<T>(params T[][] arrays) {
                int size = 0;
                for(int i = 0; i < arrays.Length; i++) {
                    if(arrays[i] == null) {
                        continue;
                    }
                    size += arrays[i].Length;
                }

                if(size == 0) {
                    return null;
                }

                T[] newArray = new T[size];
                int position = 0;

                for(int i = 0; i < arrays.Length; i++) {
                    if(arrays[i] == null) {
                        continue;
                    }
                    arrays[i].CopyTo(newArray, position);
                    position += arrays[i].Length;
                }

                return newArray;
            }

            /// <summary>
            /// Tries to load a dictionary from the JSON file at the path provided. 
            /// </summary>
            /// <typeparam name="T">Type of the dictionary key</typeparam>
            /// <typeparam name="U">type of the dictionary values</typeparam>
            /// <param name="path">Path for the dictionary file</param>
            /// <returns>Whether the load was successful. dictionary will be an empty dictionary if it fails</returns>
            public static bool LoadDictionary<T, U>(string path, out Dictionary<T, U> dictionary) {
                try {
                    dictionary = JsonConvert.DeserializeObject<Dictionary<T, U>>(File.ReadAllText(path));
                } catch (Exception e) {
                    Debug.Log("Load failed - " + e.Message);
                    dictionary = new Dictionary<T, U>();
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Tries to save the dictionary to a JSON file at the path provided.
            /// </summary>
            /// <typeparam name="T">Type of the dictionary key</typeparam>
            /// <typeparam name="U">type of the dictionary value</typeparam>
            /// <param name="dictionary">Dictionary to be saved</param>
            /// <param name="path">Path to save the dictionary to</param>
            /// <returns>Whether the save was successful</returns>
            public static bool SaveDictionary<T, U>(string path, Dictionary<T, U> dictionary) {
                try {
                    string json = JsonConvert.SerializeObject(dictionary);
                    File.WriteAllText(path, json);
                } catch (Exception e) {
                    Debug.Log("Save failed - " + e.Message);
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Searches down through the gameobject's heirarchy for a gameobject with the given tag 
            /// </summary>
            /// <param name="parent">The gameobject</param>
            /// <param name="tag">The tag</param>
            /// <returns>The first gameobject found with the tag, or null if not found</returns>
            public static GameObject FindChildWithTag(GameObject parent, string tag) {
                return GetChildWithTag(parent.transform, tag);
            }

            /// <summary>
            /// Recursively searches through transform hierarchy for first gameobject with the correct tag 
            /// </summary>
            /// <param name="parent">Gameobject's transform</param>
            /// <param name="tag">The tag to search for</param>
            /// <returns>The first gameobject found with the tag, or null if not found</returns>
            private static GameObject GetChildWithTag(Transform parent, string tag) {

                for(int i = 0; i < parent.childCount; i++) {
                    Transform child = parent.GetChild(i);
                    if(child.tag == tag) {
                        return child.gameObject;
                    }

                    GameObject result = null;
                    result = GetChildWithTag(child, tag);
                    if(result != null) {
                        return result;
                    }
                }

                return null;
            }


            /// <summary>
            /// Searches down through the gameobject's heirarchy for all gameobjects with the given tag
            /// </summary>
            /// <param name="parent">The gameobject</param>
            /// <param name="tag">The tag</param>
            /// <returns>List of gameobjects with the required tag</returns>
            public static List<GameObject> FindChildrenWithTag(GameObject parent, string tag) {
                List<GameObject> gameobjectsWithTag = new List<GameObject>();
                GetChildrenWithTag(parent.transform, tag, gameobjectsWithTag);
                return gameobjectsWithTag;
            }

            /// <summary>
            /// Recursively searches through transform hierarchy for all gameobjects with the correct tag 
            /// </summary>
            /// <param name="parent">Gameobject's transform</param>
            /// <param name="tag">The tag to search for</param>
            /// <returns>A list of all gameobjects with the tag</returns>
            private static void GetChildrenWithTag(Transform parent, string tag, List<GameObject> gameobjectsWithTag) {
                for (int i = 0; i < parent.childCount; i++) {
                    Transform child = parent.GetChild(i);
                    if (child.tag == tag) {
                        gameobjectsWithTag.Add(child.gameObject);
                    }
                    GetChildrenWithTag(child, tag, gameobjectsWithTag);
                }
            }
        }
    }
}
