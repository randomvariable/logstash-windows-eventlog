// Cache.cs
// compile with: /doc:XMLsample.xml
//-----------------------------------------------------------------------
// <copyright file="Cache.cs" company="Naadir Jeewa">
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.    
// </copyright>
//-----------------------------------------------------------------------
namespace Logstash.Windows.EventLog.Com
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// A rudimentary key-value cache.
    /// </summary>
    static class Cache
    {
        /// <summary>
        /// The cache object itself.
        /// </summary>
        private static IDictionary<string, string> cache = new Dictionary<string, string>(5000);
      
        /// <summary>
        /// A copy of the cache to be used during thread safe operations.
        /// </summary>
        private static IDictionary<string, string> cacheClone;
     
        /// <summary>
        /// A rudimentary lock.
        /// </summary>
        private static object cacheLock = new object();

        /// <summary>
        /// Finds a value in the store.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>The value corresponding to the key.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetData(string key)
        {
            string returnValue;
            if (cacheClone == null)
            {
                cache.TryGetValue(key, out returnValue);
            }
            else
            {
                try
                {
                    cacheClone.TryGetValue(key, out returnValue);
                }
                catch (System.ArgumentNullException)
                {
                    lock (cacheLock)
                    {
                        cache.TryGetValue(key, out returnValue);
                    }
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Adds a key value to the object cache.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="data">The value to add.</param>
        /// <returns>The value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AddData(string key, string data)
        {
            lock (cacheLock)
            {
                cacheClone = new Dictionary<string, string>(cache);
                if (!cache.ContainsKey(key))
                {
                    cache.Add(key, data); 
                }

                cacheClone = null;
            }

            return data;
        }
    }
}
