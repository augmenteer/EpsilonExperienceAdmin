// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminService.Data
{
    internal class MemoryAnchorCache : ISessionCache
    {
        /// <summary>
        /// The entry cache options.
        /// </summary>
        private static readonly MemoryCacheEntryOptions entryCacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(48),
        };

        /// <summary>
        /// The memory cache.
        /// </summary>
        private readonly MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// The anchor numbering index.
        /// </summary>
        private long anchorNumberIndex = -1;

        /// <summary>
        /// Determines whether the cache contains the specified anchor identifier.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}" /> containing true if the identifier is found; otherwise false.</returns>
        public Task<bool> ContainsSessionNumberAsync(long anchorId)
        {
            return Task.FromResult(this.memoryCache.TryGetValue(anchorId, out _));
        }



        /// <summary>
        /// Gets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The anchor key.</returns>
        public Task<string> GetSessionForTeamAsync(long anchorId)
        {
            if (this.memoryCache.TryGetValue(anchorId, out string anchorKey))
            {
                return Task.FromResult(anchorKey);
            }

            return Task.FromException<string>(new KeyNotFoundException($"The {nameof(anchorId)} {anchorId} could not be found."));
        }

        /// <summary>
        /// Gets the last anchor key asynchronously.
        /// </summary>
        /// <returns>The anchor key.</returns>
        public Task<string> GetLastSessionCreatedAsync()
        {
            if (this.anchorNumberIndex >= 0 && this.memoryCache.TryGetValue(this.anchorNumberIndex, out string anchorKey))
            {
                return Task.FromResult(anchorKey);
            }

            return Task.FromResult<string>(null);
        }

        public Task<string[]> GetAllSessionRecordsAsync()
        {
            string[] keys;
            if ( this.anchorNumberIndex < 0)
            {
                keys = new string[0];
                //return Task.FromResult(keys);
            }
            else
            {
                keys = new string[ 1 + this.anchorNumberIndex ];
                long cnt = this.anchorNumberIndex;

                while (cnt >= 0)
                {
                    if (this.memoryCache.TryGetValue(cnt, out string anchorKey))
                    {
                        keys[cnt] = anchorKey;
                        --cnt;
                    }
                }
            }

            return Task.FromResult(keys);
        }

        public Task<string> GetAllTeamsAsync()
        {
            string keys = "0";
            if (this.anchorNumberIndex < 0)
            {
                keys = "0";
                //return Task.FromResult(keys);
            }
            else
            {
                //keys = new string[1 + this.anchorNumberIndex];
                long cnt = this.anchorNumberIndex;

                while (cnt >= 0)
                {
                    if (this.memoryCache.TryGetValue(cnt, out string anchorKey))
                    {
                        keys = keys + "," + anchorKey;
                        --cnt;
                    }
                }
            }

            return Task.FromResult(keys);
        }

        /// <summary>
        /// Sets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorKey">The anchor key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the anchor identifier.</returns>
        public Task<bool> CreateSession(string anchorKey, int member_count)
        {
            throw new NotImplementedException();
        }

        public Task<long> SetAnchorKeyRegistrationAsync(string anchorKey, string objectName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAllAnchorKeysAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteTeamAsync(string anchorKey)
        {
            throw new NotImplementedException();
        }

        public Task<long> SetSolutionTime(string team_name, int seconds)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsSessionNumberAsync(int session_number)
        {
            throw new NotImplementedException();
        }

        public Task<long> SetRoomTime(string team_name, int room_number, int seconds)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAllSessionsAsync()
        {
            throw new NotImplementedException();
        }

        Task<SessionEntity[]> ISessionCache.GetAllSessionRecordsAsync()
        {
            throw new NotImplementedException();
        }

        Task<bool> ISessionCache.SetRoomTime(string team_name, int room_number, int seconds)
        {
            throw new NotImplementedException();
        }

        Task<bool> ISessionCache.SetSolutionTime(string team_name, int seconds)
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetTeamNamesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsSessionForTeamAsync(string team_name)
        {
            throw new NotImplementedException();
        }

        public Task<int> CalculateTotalRoomTime(string team_name)
        {
            throw new NotImplementedException();
        }

        public Task<int> CalculateTotalSessionTime(string team_name)
        {
            throw new NotImplementedException();
        }

        public Task<SessionEntity> GetSessionForTeamAsync(string team_name)
        {
            throw new NotImplementedException();
        }

        Task<SessionEntity> ISessionCache.GetLastSessionCreatedAsync()
        {
            throw new NotImplementedException();
        }

        public Task<SessionEntity[]> GetPendingSessionsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
