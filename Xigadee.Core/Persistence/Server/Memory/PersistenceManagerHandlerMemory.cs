﻿#region using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion
namespace Xigadee
{
    #region PersistenceManagerHandlerMemory<K, E>
    /// <summary>
    /// This persistence class is used to hold entities in memory during the lifetime of the 
    /// Microservice and does not persist to any backing store.
    /// This class is used extensively by the Unit test projects. The class inherits from Json base
    /// and so employs the same logic as that used by the Azure Storage and DocumentDb persistence classes.
    /// </summary>
    /// <typeparam name="K">The key type.</typeparam>
    /// <typeparam name="E">The entity type.</typeparam>
    public class PersistenceManagerHandlerMemory<K, E>: PersistenceManagerHandlerMemory<K, E, PersistenceStatistics>
        where K : IEquatable<K>
    {
        #region Constructor
        /// <summary>
        /// This is the document db persistence agent.
        /// </summary>
        /// <param name="connection">The documentDb connection.</param>
        /// <param name="database">The is the databaseId name. If the Db does not exist it will be created.</param>
        /// <param name="keyMaker">This function creates a key of type K from an entity of type E</param>
        /// <param name="databaseCollection">The is the collection name. If the collection does it exist it will be created. This will be used by the sharding policy to create multiple collections.</param>
        /// <param name="entityName">The entity name to be used in the collection. By default this will be set through reflection.</param>
        /// <param name="versionMaker">This function should be set to enforce optimistic locking.</param>
        /// <param name="defaultTimeout">This is the default timeout period to be used when connecting to documentDb.</param>
        /// <param name="shardingPolicy">This is sharding policy used to choose the appropriate collection from the key presented.</param>
        /// <param name="retryPolicy"></param>
        public PersistenceManagerHandlerMemory(Func<E, K> keyMaker
            , Func<string, K> keyDeserializer
            , string entityName = null
            , VersionPolicy<E> versionPolicy = null
            , TimeSpan? defaultTimeout = null
            , PersistenceRetryPolicy persistenceRetryPolicy = null
            , ResourceProfile resourceProfile = null
            , ICacheManager<K, E> cacheManager = null
            , Func<E, IEnumerable<Tuple<string, string>>> referenceMaker = null
            , Func<K, string> keySerializer = null
            )
            : base(keyMaker, keyDeserializer
                  , entityName: entityName
                  , versionPolicy: versionPolicy
                  , defaultTimeout: defaultTimeout
                  , persistenceRetryPolicy: persistenceRetryPolicy
                  , resourceProfile: resourceProfile
                  , cacheManager: cacheManager
                  , referenceMaker: referenceMaker
                  , keySerializer: keySerializer
                  )
        {
        }
        #endregion
    }
    #endregion
    /// <summary>
    /// This persistence class is used to hold entities in memory during the lifetime of the 
    /// Microservice and does not persist to any backing store.
    /// This class is used extensively by the Unit test projects. The class inherits from Json base
    /// and so employs the same logic as that used by the Azure Storage and DocumentDb persistence classes.
    /// </summary>
    /// <typeparam name="K">The key type.</typeparam>
    /// <typeparam name="E">The entity type.</typeparam>
    /// <typeparam name="S">An extended statistics class.</typeparam>
    public abstract class PersistenceManagerHandlerMemory<K, E, S>: PersistenceManagerHandlerJsonBase<K, E, S, PersistenceCommandPolicy>
        where K : IEquatable<K>
        where S : PersistenceStatistics, new()
    {
        #region Declarations
        /// <summary>
        /// This container holds the entities.
        /// </summary>
        protected ConcurrentDictionary<K, JsonHolder<K>> mContainer;
        /// <summary>
        /// This container holds the key references.
        /// </summary>
        protected ConcurrentDictionary<string, K> mContainerReference;

        protected ReaderWriterLockSlim mReferenceModifyLock;
        #endregion
        #region Constructor
        protected PersistenceManagerHandlerMemory(Func<E, K> keyMaker
            , Func<string, K> keyDeserializer
            , string entityName = null
            , VersionPolicy<E> versionPolicy = null
            , TimeSpan? defaultTimeout = default(TimeSpan?)
            , PersistenceRetryPolicy persistenceRetryPolicy = null
            , ResourceProfile resourceProfile = null
            , ICacheManager<K, E> cacheManager = null
            , Func<E, IEnumerable<Tuple<string, string>>> referenceMaker = null
            , Func<K, string> keySerializer = null)
            : base(keyMaker, keyDeserializer, entityName, versionPolicy, defaultTimeout, persistenceRetryPolicy, resourceProfile, cacheManager, referenceMaker, keySerializer)
        {
        } 
        #endregion


        protected override void StartInternal()
        {
            mContainer = new ConcurrentDictionary<K, JsonHolder<K>>();
            mContainerReference = new ConcurrentDictionary<string, K>();
            mReferenceModifyLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            base.StartInternal();
        }

        protected override void StopInternal()
        {
            mReferenceModifyLock.Dispose();
            mReferenceModifyLock = null;
            mContainerReference.Clear();
            mContainer.Clear();
            mContainerReference = null;
            mContainer = null;
            base.StopInternal();
        }


        protected override async Task<IResponseHolder<E>> InternalCreate(PersistenceRequestHolder<K, E> holder)
        {
            var jsonHolder = mTransform.JsonMaker(holder.rq.Entity);

            bool success = mContainer.TryAdd(holder.rq.Key, jsonHolder);

            if (success)
                return new PersistenceResponseHolder<E>()
                {
                      StatusCode = 201
                    , Content = jsonHolder.Json
                    , IsSuccess = true
                    , Entity = mTransform.EntityDeserializer(jsonHolder.Json)
                };
            else
                return new PersistenceResponseHolder<E>()
                {
                      StatusCode = 400
                    , IsSuccess = false
                    , IsTimeout = false
                };
        }

        protected override Task<IResponseHolder<E>> InternalRead(K key, PersistenceRequestHolder<K, E> holder)
        {
            return base.InternalRead(key, holder);
        }

        protected override Task<IResponseHolder<E>> InternalReadByRef(Tuple<string, string> reference, PersistenceRequestHolder<K, E> holder)
        {
            return base.InternalReadByRef(reference, holder);
        }

        protected override Task<IResponseHolder<E>> InternalUpdate(PersistenceRequestHolder<K, E> holder)
        {
            return base.InternalUpdate(holder);
        }

        protected override Task<IResponseHolder> InternalDelete(K key, PersistenceRequestHolder<K, Tuple<K, string>> holder)
        {
            return base.InternalDelete(key, holder);
        }

        protected override Task<IResponseHolder> InternalDeleteByRef(Tuple<string, string> reference, PersistenceRequestHolder<K, Tuple<K, string>> holder)
        {
            return base.InternalDeleteByRef(reference, holder);
        }

        protected override Task<IResponseHolder> InternalVersion(K key, PersistenceRequestHolder<K, Tuple<K, string>> holder)
        {
            return base.InternalVersion(key, holder);
        }

        protected override Task<IResponseHolder> InternalVersionByRef(Tuple<string, string> reference, PersistenceRequestHolder<K, Tuple<K, string>> holder)
        {
            return base.InternalVersionByRef(reference, holder);
        }

    }
}
