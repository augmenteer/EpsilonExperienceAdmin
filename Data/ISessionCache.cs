// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;

namespace AdminService.Data
{
    /// <summary>
    /// An interface representing an anchor key cache.
    /// </summary>
    public interface ISessionCache
    {
        /// <summary>
        /// Determines whether the cache contains the specified anchor identifier.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}"/> containing true if the identifier is found; otherwise false.</returns>
        Task<bool> ContainsSessionForTeamAsync(string team_name);

        /// <summary>
        /// Gets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>The anchor key.</returns>
        Task<SessionEntity> GetSessionForTeamAsync(string team_name);


        Task<SessionEntity> GetLastSessionCreatedAsync();

        Task<SessionEntity[]> GetPendingSessionsAsync();
        Task<SessionEntity[]> GetAllSessionRecordsAsync();

        Task<string[]> GetTeamNamesAsync();
        /// <summary>
        /// Sets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorKey">The anchor key.</param>
        /// <returns>An <see cref="Task{System.Int64}"/> representing the anchor identifier.</returns>
        Task<bool> CreateSession(string team_name, int member_count);

        Task<bool> SetSolutionTime(string team_name, int seconds);

        Task<bool> SetRoomTime(string team_name, int room_number, int seconds);

        Task<int> CalculateTotalRoomTime(string team_name);

        Task<int> CalculateTotalSessionTime(string team_name);

        Task<bool> DeleteTeamAsync(string anchorKey);

        Task<bool> DeleteAllSessionsAsync();
    }
}
