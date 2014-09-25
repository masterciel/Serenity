﻿using System;
using Serenity.Abstractions;

namespace Serenity
{
    /// <summary>
    /// Contains helper functions to use local and distributed cache in sync with optional cache invalidation.
    /// </summary>
    public static class TwoLevelCache
    {
        private static readonly TimeSpan GenerationCacheExpiration = TimeSpan.FromSeconds(5);
        private const string GenerationSuffix = "$Generation$";
        private static readonly Random GenerationRandomizer;

        static TwoLevelCache()
        {
            GenerationRandomizer = new Random(GetSeed());
        }

        /// <summary>
        /// Tries to read a value from local cache. If it is not found there, tries the distributed cache. 
        /// If neither contains the specified key, produces value by calling a loader function and adds the
        /// value to local and distributed cache for a given expiration time. By using a generation (item version)
        /// key, all items on both cache types that depend on this generation can be expired at once. </summary>
        /// <remarks>
        /// To not check generation number every time an item is requested, generation number itself is also
        /// cached in local cache. Thus, when a generation number changes, local cached items might expire
        /// after about one minute. This means that, if you use this strategy in a web farm setup, when a change 
        /// occurs in one server, other servers might continue to use old local cached data for one minute more.
        /// If this is a problem for your configuration, use DistributedCache directly.
        /// </remarks>
        /// <typeparam name="TItem">Data type</typeparam>
        /// <param name="cacheKey">The item key for local and distributed cache</param>
        /// <param name="localExpiration">Local expiration</param>
        /// <param name="remoteExpiration">Distributed cache expiration (is usually same with local expiration)</param>
        /// <param name="globalGenerationKey">Global generation (version) key. Can be used to expire all items
        /// that depend on it. This can be a table name. When a table changes, you change its version, and all
        /// cached data that depends on that table is expired.</param>
        /// <param name="loader">The delegate that will be called to generate value, if not found in local cache,
        /// or distributed cache, or all found items are expired.</param>
        public static TItem Get<TItem>(string cacheKey, TimeSpan localExpiration, TimeSpan remoteExpiration, 
            string globalGenerationKey, Func<TItem> loader)
            where TItem : class
        {
            return GetInternal<TItem, TItem>(cacheKey, localExpiration, remoteExpiration, 
                globalGenerationKey, loader, x => x, x => x);
        }

        /// <summary>
        /// Tries to read a value from local cache. If it is not found there, tries the distributed cache. 
        /// If neither contains the specified key, produces value by calling a loader function and adds the
        /// value to local and distributed cache for a given expiration time. By using a generation (item version)
        /// key, all items on both cache types that depend on this generation can be expired at once. </summary>
        /// <remarks>
        /// To not check generation number every time an item is requested, generation number itself is also
        /// cached in local cache. Thus, when a generation number changes, local cached items might expire
        /// after about one minute. This means that, if you use this strategy in a web farm setup, when a change 
        /// occurs in one server, other servers might continue to use old local cached data for one minute more.
        /// If this is a problem for your configuration, use DistributedCache directly.
        /// </remarks>
        /// <typeparam name="TItem">Data type</typeparam>
        /// <param name="cacheKey">The item key for local and distributed cache</param>
        /// <param name="localExpiration">Local expiration</param>
        /// <param name="remoteExpiration">Distributed cache expiration (is usually same with local 
        /// expiration)</param>
        /// <param name="globalGenerationKey">Global generation (version) key. Can be used to expire all items
        /// that depend on it. This can be a table name. When a table changes, you change its version, and all
        /// cached data that depends on that table is expired.</param>
        /// <param name="loader">The delegate that will be called to generate value, if not found in local cache,
        /// or distributed cache, or all found items are expired.</param>
        /// <param name="serialize">A function used to serialize items before cached.</param>
        /// <param name="deserialize">A function used to deserialize items before cached.</param>
        public static TItem GetWithCustomSerializer<TItem, TSerialized>(string cacheKey, TimeSpan localExpiration, 
            TimeSpan remoteExpiration, string globalGenerationKey, Func<TItem> loader, 
            Func<TItem, TSerialized> serialize, Func<TSerialized, TItem> deserialize)
            where TItem : class
            where TSerialized : class
        {
            if (serialize == null)
                throw new ArgumentNullException("serialize");

            if (deserialize == null)
                throw new ArgumentNullException("deserialize");

            return GetInternal<TItem, TSerialized>(cacheKey, localExpiration, remoteExpiration, 
                globalGenerationKey, loader, serialize, deserialize);
        }

        /// <summary>
        /// Tries to read a value from local cache. If it is not found there produces value by calling a loader 
        /// function and adds the value to local cache for a given expiration time. By using a generation 
        /// (item version) key, all items on local cache that depend on this generation can be expired 
        /// at once. </summary>
        /// <remarks>
        /// The difference between this and Get method is that this one only caches items in local cache, but 
        /// uses distributed cache for versioning. To not check generation number every time an item is requested, 
        /// generation number itself is also cached in local cache. Thus, when a generation number changes, local 
        /// cached items might expire after about one minute. This means that, if you use this strategy in a web farm 
        /// setup, when a change occurs in one server, other servers might continue to use old local cached data for 
        /// one minute more. If this is a problem for your configuration, use DistributedCache directly.
        /// </remarks>
        /// <typeparam name="TItem">Data type</typeparam>
        /// <param name="cacheKey">The item key for local and distributed cache</param>
        /// <param name="localExpiration">Local expiration</param>
        /// <param name="remoteExpiration">Distributed cache expiration (is usually same with 
        /// local expiration)</param>
        /// <param name="globalGenerationKey">Global generation (version) key. Can be used to expire all items
        /// that depend on it. This can be a table name. When a table changes, you change its version, and all
        /// cached data that depends on that table is expired.</param>
        /// <param name="loader">The delegate that will be called to generate value, if not found in local cache,
        /// or distributed cache, or all found items are expired.</param>
        /// <param name="serialize">A function used to serialize items before cached.</param>
        /// <param name="deserialize">A function used to deserialize items before cached.</param>        
        public static TItem GetLocalStoreOnly<TItem>(string cacheKey, TimeSpan localExpiration, 
            string globalGenerationKey, Func<TItem> loader)
            where TItem : class
        {
            return GetInternal<TItem, TItem>(cacheKey, localExpiration, TimeSpan.FromSeconds(0), 
                globalGenerationKey, loader, null, null);
        }

        private static TItem GetInternal<TItem, TSerialized>(string cacheKey, TimeSpan localExpiration, TimeSpan remoteExpiration, 
            string globalGenerationKey, Func<TItem> loader, Func<TItem, TSerialized> serialize, Func<TSerialized, TItem> deserialize)
            where TItem : class
            where TSerialized : class
        {
            ulong? globalGeneration = null;
            ulong? globalGenerationCache = null;

            string itemGenerationKey = cacheKey + GenerationSuffix;

            // retrieves distributed cache global generation number lazily
            Func<ulong> getGlobalGenerationValue = delegate()
            {
                if (globalGeneration != null)
                    return globalGeneration.Value;

                globalGeneration = DistributedCache.Get<ulong?>(globalGenerationKey);
                if (globalGeneration == null || globalGeneration == 0)
                {
                    globalGeneration = RandomGeneration();
                    DistributedCache.Set(globalGenerationKey, globalGeneration.Value);
                }

                globalGenerationCache = globalGeneration.Value;
                // local cache e ekle, 1 dk boyunca buradan kullan
                LocalCache.Add(globalGenerationKey, globalGenerationCache, GenerationCacheExpiration);

                return globalGeneration.Value;
            };

            // retrieves local cache global generation number lazily
            Func<ulong> getGlobalGenerationCacheValue = delegate()
            {
                if (globalGenerationCache != null)
                    return globalGenerationCache.Value;

                // check cached local value of global generation key 
                // it expires in 1 minute and read from server again
                globalGenerationCache = Dependency.Resolve<ILocalCache>().Get<object>(globalGenerationKey) as ulong?;

                // if its in local cache, return it
                if (globalGenerationCache != null)
                    return globalGenerationCache.Value;

                return getGlobalGenerationValue();
            };

            // first check local cache, if item exists and not expired (global version = item version) return it
            var cachedObj = Dependency.Resolve<ILocalCache>().Get<object>(cacheKey);
            if (cachedObj != null)
            {
                // check local cache, if exists, compare version with global one
                var itemGenerationCache = Dependency.Resolve<ILocalCache>().Get<object>(itemGenerationKey) as ulong?;
                if (itemGenerationCache != null &&
                    itemGenerationCache == getGlobalGenerationCacheValue())
                {
                    // local cached item is not expired yet

                    if (cachedObj == DBNull.Value)
                        return null;

                    return (TItem)cachedObj;
                }

                // local cached item is expired, remove all information
                if (itemGenerationCache != null)
                    Dependency.Resolve<ILocalCache>().Remove(itemGenerationKey);

                Dependency.Resolve<ILocalCache>().Remove(cacheKey);

                cachedObj = null;
            }

            // if serializer is null, than this is a local store only item
            if (serialize != null)
            {
                // no item in local cache or expired, now check distributed cache
                var itemGeneration = DistributedCache.Get<ulong?>(itemGenerationKey);

                // if item has version number in distributed cache and this is equal to global version
                if (itemGeneration != null &&
                    itemGeneration.Value == getGlobalGenerationValue())
                {
                    // get item from distributed cache
                    var serialized = DistributedCache.Get<TSerialized>(cacheKey);
                    // if item exists in distributed cache
                    if (serialized != null)
                    {
                        cachedObj = deserialize(serialized);
                        LocalCache.Add(cacheKey, (object)cachedObj ?? DBNull.Value, localExpiration);
                        LocalCache.Add(itemGenerationKey, getGlobalGenerationValue(), localExpiration);
                        return (TItem)cachedObj;
                    }
                }
            }

            // couldn't find valid item in local or distributed cache, produce value by calling loader
            var item = loader();

            // add item and its version to cache
            LocalCache.Add(cacheKey, (object)item ?? DBNull.Value, localExpiration);
            LocalCache.Add(itemGenerationKey, getGlobalGenerationValue(), localExpiration);

            if (serialize != null)
            {
                var serializedItem = serialize(item);

                // add item and generation to distributed cache
                if (remoteExpiration == TimeSpan.Zero)
                {
                    DistributedCache.Set(cacheKey, serializedItem);
                    DistributedCache.Set(itemGenerationKey, getGlobalGenerationValue());
                }
                else
                {
                    DistributedCache.Set(cacheKey, serializedItem, DateTime.Now.Add(remoteExpiration));
                    DistributedCache.Set(itemGenerationKey, getGlobalGenerationValue(), DateTime.Now.Add(remoteExpiration));
                }
            }

            return item;
        }

        /// <summary>
        /// Generates a seed for Random object.
        /// </summary>
        /// <returns>Random 32 bit seed</returns>
        private static int GetSeed()
        {
            byte[] raw = Guid.NewGuid().ToByteArray();
            int i1 = BitConverter.ToInt32(raw, 0);
            int i2 = BitConverter.ToInt32(raw, 4);
            int i3 = BitConverter.ToInt32(raw, 8);
            int i4 = BitConverter.ToInt32(raw, 12);
            long val = i1 + i2 + i3 + i4;
            while (val > int.MaxValue)
                val -= int.MaxValue;
            return (int)val;
        }

        /// <summary>
        /// Generates a 64 bit random generation number (version key)
        /// </summary>
        /// <returns>Random 64 bit number</returns>
        private static ulong RandomGeneration()
        {
            var buffer = new byte[sizeof(ulong)];
            GenerationRandomizer.NextBytes(buffer);
            var value = BitConverter.ToUInt64(buffer, 0);

            // random değer 0 olmasın
            if (value == 0)
                return ulong.MaxValue;

            return value;
        }


        /// <summary>
        /// Changes a global generation value, so that all items that depend on it are expired.
        /// </summary>
        /// <param name="globalGenerationKey">Generation key</param>
        public static void ChangeGlobalGeneration(string globalGenerationKey)
        {
            Dependency.Resolve<ILocalCache>().Remove(globalGenerationKey);
            DistributedCache.Set<object>(globalGenerationKey, null);
        }

        /// <summary>
        /// Removes a key from local, distributed caches, and removes their generation version information.
        /// </summary>
        /// <param name="cacheKey">Cache key</param>
        public static void Remove(string cacheKey)
        {
            string itemGenerationKey = cacheKey + GenerationSuffix;

            Dependency.Resolve<ILocalCache>().Remove(cacheKey);
            Dependency.Resolve<ILocalCache>().Remove(itemGenerationKey);
            DistributedCache.Set<object>(cacheKey, null);
            DistributedCache.Set<object>(itemGenerationKey, null);
        }
    }
}