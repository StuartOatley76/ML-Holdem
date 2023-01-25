using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
namespace Utils {

    namespace ExtentionMethods {
        /// <summary>
        /// Class to implement extension methods
        /// </summary>
        public static class Extentions {


            /// <summary>
            /// Extention method to allow searching a NativeHashMap for a specific value
            /// </summary>
            /// <typeparam name="K">Type of Key used in the NativeHashMap</typeparam>
            /// <typeparam name="V">Type of Value used in the NativeHashMap</typeparam>
            /// <param name="map">This NativeHashMap</param>
            /// <param name="valueToSearchFor">The Value being searched for</param>
            /// <param name="allocator">The type of allocator to be used for the nativearray of keys that will be returned</param>
            /// <returns>A NativeArray of the keys that hold the required value</returns>
            public static NativeArray<K> FindValue<K, V>(this NativeHashMap<K,V> map, V valueToSearchFor, Allocator allocator = Allocator.Temp) where K : struct, IEquatable<K> where V : struct, IEquatable<V> {
                NativeArray<K> keys = new NativeArray<K>(map.Count(), allocator);
                foreach(KeyValue<K, V> keyValue in map) {
                    if (keyValue.Value.Equals(valueToSearchFor)) {
                        keys[keys.Length] = keyValue.Key;
                    }
                }
                return keys;

            }

            /// <summary>
            /// Extention method to sort arrays into decending order
            /// </summary>
            /// <typeparam name="T">Any type that implements IComparable</typeparam>
            /// <param name="toSort">Array to be sorted</param>
            public static void SortDescending<T>(this T[] toSort) where T : IComparable {

                InsertionSortDescending(ref toSort);
                //if(toSort.Length < 10) {
                //    InsertionSortDescending(ref toSort);
                //} else {
                //    QuickSortDescending(ref toSort, 0, toSort.Length - 1);
                //}

            }

            //Note - whilst FixedLists and NativeArrays can be converted into arrays, which would allow these to comply with DRY,
            //we do not do this as fixed lists and native arrays are typically used in threaded code and allocating managed memory inside
            //a thread will have massive performance costs

            /// <summary>
            /// Extention method to sort fixed lists into decending order
            /// </summary>
            /// <typeparam name="T">Unmanaged type that implements IComparable</typeparam>
            /// <param name="toSort"></param>
            public static void SortDescending<T>(this FixedList64Bytes<T> toSort) where T : unmanaged, IComparable {
                if (toSort.Length < 10) {
                    InsertionSortDescending(ref toSort);
                } else {
                    QuickSortDescending(ref toSort, 0, toSort.Length - 1);
                }
            }

            /// <summary>
            /// Extention method to sort native arrays into decending order
            /// </summary>
            /// <typeparam name="T">Struct that implements IComparable</typeparam>
            /// <param name="toSort"></param>
            public static void SortDescending<T>(this NativeArray<T> toSort) where T : unmanaged, IComparable {
                if (toSort.Length < 10) {
                    InsertionSortDescending(ref toSort);
                } else {
                    QuickSortDescending(ref toSort, 0, toSort.Length - 1);
                }
            }

            /// <summary>
            /// Descending quicksort method for arrays
            /// </summary>
            /// <typeparam name="T">Any type that implements IComparable</typeparam>
            /// <param name="toSort">Array to be sorted</param>
            /// <param name="left"></param>
            /// <param name="right"></param>
            private static void QuickSortDescending<T>(ref T[] toSort, int left, int right) where T : IComparable {

                if(left < right) {
                    int partition = PartitionDescending<T>(ref toSort, left, right);
                    QuickSortDescending(ref toSort, left, partition);
                    QuickSortDescending(ref toSort, partition + 1, right);

                }
            }

            private static int PartitionDescending<T>(ref T[] toSort, int left, int right) where T : IComparable {
                T pivot = toSort[(left + right) / 2];

                while (true) {
                    while(toSort[left].CompareTo(pivot) > 0 && left < right) {
                        left++;
                    }
                    while(toSort[right].CompareTo(pivot) < 0 && right > 0) {
                        right--;
                    }
                    if(left >= right) {
                        break;
                    }
                    if(toSort[left].CompareTo(toSort[right]) == 0) {
                        break;
                    }
                    Swap(ref toSort, left, right);
                }
                return right;
            }

            private static void Swap<T>(ref T[] array, int first, int second) {
                T temp = array[first];
                array[first] = array[second];
                array[second] = temp;
            }

            /// <summary>
            /// Desending quicksort method for Fixed lists
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="toSort"></param>
            /// <param name="left"></param>
            /// <param name="right"></param>
            private static void QuickSortDescending<T>(ref FixedList64Bytes<T> toSort, int left, int right) where T : unmanaged, IComparable {

                if (left < right) {
                    int partition = PartitionDescending<T>(ref toSort, left, right);
                    QuickSortDescending(ref toSort, left, partition);
                    QuickSortDescending(ref toSort, partition + 1, right);

                }
            }

            private static int PartitionDescending<T>(ref FixedList64Bytes<T> toSort, int left, int right) where T : unmanaged, IComparable {
                T pivot = toSort[left];

                while (true) {
                    while (toSort[left].CompareTo(pivot) > 0) {
                        left++;
                    }
                    while (toSort[right].CompareTo(pivot) < 0) {
                        right--;
                    }
                    if (left >= right) {
                        break;
                    }
                    Swap(ref toSort, left, right);
                }
                return right;
            }

            private static void Swap<T>(ref FixedList64Bytes<T> array, int i, int j) where T : unmanaged, IComparable {
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }

            /// <summary>
            /// Descending quicksort method for native arrays
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="toSort"></param>
            /// <param name="left"></param>
            /// <param name="right"></param>
            private static void QuickSortDescending<T>(ref NativeArray<T> toSort, int left, int right) where T : unmanaged, IComparable {

                if (left < right) {
                    int partition = PartitionDescending<T>(ref toSort, left, right);
                    QuickSortDescending(ref toSort, left, partition);
                    QuickSortDescending(ref toSort, partition + 1, right);

                }
            }

            private static int PartitionDescending<T>(ref NativeArray<T> toSort, int left, int right) where T : unmanaged, IComparable {
                T pivot = toSort[left];

                while (true) {
                    while (toSort[left].CompareTo(pivot) > 0) {
                        left++;
                    }
                    while (toSort[right].CompareTo(pivot) < 0) {
                        right--;
                    }
                    if (left >= right) {
                        break;
                    }
                    Swap(ref toSort, left, right);
                }
                return right;
            }

            private static void Swap<T>(ref NativeArray<T> array, int i, int j) where T : unmanaged, IComparable {
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }

            /// <summary>
            /// Descending insertion sort method for arrays
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="toSort"></param>
            private static void InsertionSortDescending<T>(ref T[] toSort) where T : IComparable {

                T temp;

                for (int i = 0; i < toSort.Length; i++) {
                    temp = toSort[i];

                    int j = i - 1;
                    while (j >= 0 && toSort[j].CompareTo(temp) < 0) {
                        toSort[j + 1] = toSort[j];
                        j--;
                    }
                    toSort[j + 1] = temp;
                }
            }

            /// <summary>
            /// Descending Insertion sort method for fixed lists
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="toSort"></param>
            private static void InsertionSortDescending<T>(ref FixedList64Bytes<T> toSort) where T : unmanaged, IComparable {

                T temp;

                for (int i = 0; i < toSort.Length; i++) {
                    temp = toSort[i];

                    int j = i - 1;
                    while (j >= 0 && toSort[j].CompareTo(temp) < 0) {
                        toSort[j + 1] = toSort[j];
                        j--;
                    }
                    toSort[j + 1] = temp;
                }
            }

            /// <summary>
            /// Descending insertion sort method for native arrays
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="toSort"></param>
            private static void InsertionSortDescending<T>(ref NativeArray<T> toSort) where T : struct, IComparable {

                T temp;

                for (int i = 0; i < toSort.Length; i++) {
                    temp = toSort[i];

                    int j = i - 1;
                    while (j >= 0 && toSort[j].CompareTo(temp) < 0) {
                        toSort[j + 1] = toSort[j];
                        j--;
                    }
                    toSort[j + 1] = temp;
                }
            }
        }
    }
}