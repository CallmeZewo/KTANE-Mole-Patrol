using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ZToolsKtane
{
    public static class ZRandom
    {
        private static readonly Random _defaultRng = new Random();

        /// <summary>
        /// Shuffles the list in-place using the Fisher–Yates algorithm.
        /// Every permutation is equally likely.
        /// </summary>
        public static void Shuffle<T>(List<T> list, Random rng = null)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "Input list cannot be null.");

            if (rng == null)
                rng = new Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Returns a new list with the elements shuffled.
        /// Uses OrderBy with random keys (less uniform than Fisher–Yates).
        /// </summary>
        public static List<T> ShuffleSorted<T>(List<T> list, Random rng = null)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "Input list cannot be null.");

            if (rng == null)
                rng = _defaultRng;

            return list.OrderBy(x => rng.Next()).ToList();
        }

        /// <summary>
        /// Shuffles only the first N elements of the list in-place.
        /// Useful when you want a random subset shuffled quickly.
        /// </summary>
        public static void ShuffleFirstN<T>(List<T> list, int count, Random rng = null)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "Input list cannot be null.");
            if (count < 0 || count > list.Count)
                throw new ArgumentOutOfRangeException(nameof(list), "Count must be between 0 and list.Count.");

            if (rng == null)
                rng = _defaultRng;

            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }


        /// <summary>
        /// Picks a random element from the list.
        /// Throws if the list is empty or null.
        /// </summary>
        public static T PickRandom<T>(List<T> list, Random rng = null)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List cannot be null or empty.", nameof(list));

            if (rng == null)
                rng = _defaultRng;

            int index = rng.Next(list.Count);
            return list[index];
        }

        /// <summary>
        /// Picks N unique random elements from the list.
        /// Throws if count is out of range.
        /// </summary>
        public static List<T> PickRandomN<T>(List<T> list, int count, Random rng = null)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "Input list cannot be null.");
            if (count < 0 || count > list.Count)
                throw new ArgumentOutOfRangeException(nameof(list), "Count must be between 0 and list.Count.");

            if (rng == null)
                rng = _defaultRng;

            // Copy list so original stays intact
            List<T> copy = new List<T>(list);
            Shuffle(copy, rng);
            return copy.GetRange(0, count);
        }
    }
    public static class ZList
    {
        /// <summary>
        /// Returns the most frequently occurring elements in the list,
        /// based on a key selector.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <typeparam name="TKey">The type of the key to group by.</typeparam>
        /// <param name="list">The input list.</param>
        /// <param name="keySelector">Function to extract the grouping key from each element.</param>
        /// <returns>A list of elements with the most frequent key.</returns>
        public static List<T> GetMostFrequentBy<T, TKey>(List<T> list, Func<T, TKey> keySelector)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "Input list cannot be null.");

            if (list.Count == 0)
                return new List<T>();

            Dictionary<TKey, int> counts = new Dictionary<TKey, int>();
            Dictionary<TKey, T> firstItems = new Dictionary<TKey, T>();

            foreach (var item in list)
            {
                TKey key = keySelector(item);

                if (counts.ContainsKey(key))
                    counts[key]++;
                else
                {
                    counts[key] = 1;
                    firstItems[key] = item;
                }
            }

            int maxCount = counts.Values.Max();

            List<T> result = new List<T>();
            foreach (var pair in counts)
            {
                if (pair.Value == maxCount)
                    result.Add(firstItems[pair.Key]);
            }

            return result;
        }

        /// <summary>
        /// Returns a formatted string containing all elements in the list.
        /// </summary>
        public static string ListToString<T>(List<T> list, Func<T, string> toString = null)
        {
            if (list == null)
                return "null";

            Func<T, string> formatter;

            if (toString == null)
            {
                formatter = x => x == null ? "null" : x.ToString();
            }
            else
            {
                formatter = toString;
            }

            return "[" + string.Join(", ", list.Select(formatter).ToArray()) + "]";
        }
    }
}
