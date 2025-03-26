// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AdminService.Data
{
 
    public class SessionEntity : TableEntity
    {
        public static int sessionCount = 0;
        public static int pendingOrder = 0;
        public SessionEntity() { }

        public SessionEntity(DateTime sessionStart)
        {
            this.PartitionKey = (++sessionCount).ToString();
            this.RowKey = sessionStart.ToString("ddMMyyyyHHmmss");

        }

        public string TeamName { get; set; }
        public int MemberCount {  get; set; }
        public int RoomTime1 { get; set; }
        public int RoomTime2 { get; set; }
        public int RoomTime3 { get; set; }
        public int TotalRoomTimes { get; set; }
        public int SolutionTime { get; set; }
        public int TotalSessionTime { get; set; }
        public int PendingOrder { get; set; }
    }


    internal class CosmosDbCache : ISessionCache
    {
        /// <summary>
        /// Super basic partitioning scheme
        /// </summary>
        private const int partitionSize = 500;

        /// <summary>
        /// The database cache.
        /// </summary>
        private readonly CloudTable dbCache;

        /// <summary>
        /// The anchor numbering index.
        /// </summary>
        //private long lastAnchorNumberIndex = 0;

        // To ensure our asynchronous initialization code is only ever invoked once, we employ two manualResetEvents
        ManualResetEventSlim initialized = new ManualResetEventSlim();
        ManualResetEventSlim initializing = new ManualResetEventSlim();

        private async Task InitializeAsync()
        {
            if (!this.initialized.Wait(0))
            {
                if (!this.initializing.Wait(0))
                {
                    this.initializing.Set();
                    await this.dbCache.CreateIfNotExistsAsync();
                    this.initialized.Set();
                }

                this.initialized.Wait();
            }
        }

        public CosmosDbCache(string storageConnectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            this.dbCache = tableClient.GetTableReference("SessionCache");
        }

        /// <summary>
        /// Determines whether the cache contains the specified anchor identifier.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}" /> containing true if the identifier is found; otherwise false.</returns>

        public async Task<bool> CreateSession(string teamName, int member_count)
        {
            bool success = false;

            try
            {
                await this.InitializeAsync();

                SessionEntity sessionEntity = new SessionEntity(DateTime.UtcNow);
                {

                };

                sessionEntity.TeamName = teamName;
                sessionEntity.MemberCount = member_count;
                sessionEntity.RoomTime1 = 0;
                sessionEntity.RoomTime2 = 0;
                sessionEntity.RoomTime3 = 0;
                sessionEntity.TotalRoomTimes = 0;
                sessionEntity.SolutionTime = 0;
                sessionEntity.TotalSessionTime = 0;
                sessionEntity.PendingOrder = ++SessionEntity.pendingOrder;

                await this.dbCache.ExecuteAsync(TableOperation.Insert(sessionEntity));

                success = true;
            }catch (Exception ex)
            {
                success = false;
            }

            return success;

        }
        public async Task<bool> ContainsSessionForTeamAsync(string team_name)
        {
            await this.InitializeAsync();

            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("TeamName", QueryComparisons.Equal, team_name));
            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);

            return segment.Results.Count > 0;
        }

        public async Task<SessionEntity[]> GetPendingSessionsAsync()
        {
            await this.InitializeAsync();

            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterConditionForInt("PendingOrder", QueryComparisons.NotEqual, 0 ));
            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);

            return segment.Results.OrderBy(x => x.PendingOrder).ToArray();
        }

        /// <summary>
        /// Gets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The anchor key.</returns>
        public async Task<int> CalculateTotalRoomTime(string team_name)
        {
            await this.InitializeAsync();
            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("TeamName", QueryComparisons.Equal, team_name));
            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);

            SessionEntity session = segment.Results[0];
            int rm1, rm2, rm3;
            rm1 = session.RoomTime1;
            rm2 = session.RoomTime2;
            rm3 = session.RoomTime3;
            session.TotalRoomTimes = rm1 + rm2 + rm3;

            int trt_result = session.TotalRoomTimes;
    
            try
            {
                await dbCache.ExecuteAsync(TableOperation.Merge(session));
            }
            catch (System.Exception ex)
            {
                Trace.TraceError(ex.Message);

            }

            return trt_result;
        }

        public async Task<int> CalculateTotalSessionTime(string team_name)
        {
            await this.InitializeAsync();
            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("TeamName", QueryComparisons.Equal, team_name));
            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);

            SessionEntity session = segment.Results[0];
            int total_room_time, solution_time;
            total_room_time = session.TotalRoomTimes;
            solution_time = session.SolutionTime;
            session.TotalSessionTime = total_room_time + solution_time;

            int sess_time_result = session.TotalSessionTime;

            try
            {
                await dbCache.ExecuteAsync(TableOperation.Merge(session));
            }
            catch (System.Exception ex)
            {
                Trace.TraceError(ex.Message);

            }

            return sess_time_result;
        }

        /// <summary>
        /// Gets the last anchor asynchronously.
        /// </summary>
        /// <returns>The anchor.</returns>
        public async Task<SessionEntity> GetLatestTeamAsync()
        {
            await this.InitializeAsync();

            List<SessionEntity> results = new List<SessionEntity>();
            TableQuery<SessionEntity> tableQuery = new TableQuery<SessionEntity>();
            TableQuerySegment<SessionEntity> previousSegment = null;
            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<SessionEntity> currentSegment = await this.dbCache.ExecuteQuerySegmentedAsync<SessionEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            return results.OrderByDescending(x => x.Timestamp).DefaultIfEmpty(null).First();
        }

        public async Task<string> GetAllTeamsAsCDLAsync()
        {
            await this.InitializeAsync();

            List<SessionEntity> results = new List<SessionEntity>();
            TableQuery<SessionEntity> tableQuery = new TableQuery<SessionEntity>();
            TableQuerySegment<SessionEntity> previousSegment = null;

            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<SessionEntity> currentSegment = await this.dbCache.ExecuteQuerySegmentedAsync<SessionEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            string keys = "0";// new string[results.Count];
            int n = 0;
            foreach (var result in results)
            {
                keys = keys + "," + result.TeamName;
            }

            return keys;
        }

        public async Task<string[]> GetAllTeamNamesAsync()
        {
            await this.InitializeAsync();

            List<SessionEntity> results = new List<SessionEntity>();
            TableQuery<SessionEntity> tableQuery = new TableQuery<SessionEntity>();
            TableQuerySegment<SessionEntity> previousSegment = null;

            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<SessionEntity> currentSegment = await this.dbCache.ExecuteQuerySegmentedAsync<SessionEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            string[] keys = new string[results.Count];
            int n = 0;
            foreach (var result in results)
            {
                keys[n++] = result.TeamName;
            }

            return keys;
        }

        public async Task<SessionEntity[]> GetAllSessionRecordsAsync()
        {
            await this.InitializeAsync();

            List<SessionEntity> results = new List<SessionEntity>();
            TableQuery<SessionEntity> tableQuery = new TableQuery<SessionEntity>();
            TableQuerySegment<SessionEntity> previousSegment = null;

            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<SessionEntity> currentSegment = await this.dbCache.ExecuteQuerySegmentedAsync<SessionEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            SessionEntity[] keys = new SessionEntity[results.Count];
            int n = 0;
            foreach (var result in results)
            {
                keys[n++] = result;
            }

            return keys;
        }

        /// <summary>
        /// Sets the anchor key asynchronously.
        /// </summary>
        /// <param name="teamName">The anchor key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the anchor identifier.</returns>


        public async Task<bool> SetSolutionTime(string team_name, int seconds)
        {
            await this.InitializeAsync();

            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("TeamName", QueryComparisons.Equal, team_name));
            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);

            var team_sessions = segment.Results;

            if (team_sessions.Count != 1 && seconds >= 0)
            {
                Trace.TraceError( "SolutionTime Update for "+ team_name +" w/ "+ team_sessions.Count+" results & "+ seconds+" is an invalid entry");
                return false;
            }

            SessionEntity team_record = team_sessions[0];
            team_record.SolutionTime = seconds;

            bool success;
            try
            {
                await dbCache.ExecuteAsync(TableOperation.Merge(team_record));
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                Trace.TraceError(ex.Message);
            }

            return success;
        }

        public async Task<bool> SetRoomTime(string team_name, int room_number, int seconds)
        {
            await this.InitializeAsync();

            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("TeamName", QueryComparisons.Equal, team_name));
            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);

            var team_sessions = segment.Results;

            if (team_sessions.Count != 1)
                return false;

            SessionEntity team_record = team_sessions[0];

            switch (room_number)
            {
                case 1: team_record.RoomTime1 = seconds; break;

                case 2: team_record.RoomTime2 = seconds; break;

                case 3: team_record.RoomTime3 = seconds; break;

            }

            bool success;
            try
            {
                await dbCache.ExecuteAsync(TableOperation.Merge(team_record));
                success = true;

            }
            catch (System.Exception ex)
            {
                success = false;

            }

            return success;
        }

        public async Task<SessionEntity> GetSessionForTeamAsync(string team_name)
        {
            await this.InitializeAsync();

            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("TeamName", QueryComparisons.Equal, team_name));

            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);
            var sessions = segment.Results;
            SessionEntity team_session = sessions[0];

            return team_session;
        }

        public async Task<string[]> GetTeamNamesAsync()
        {
            await this.InitializeAsync();

            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, "0"));

            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);
            var myEntities = segment.Results;
            string[] names = new string[myEntities.Count];
            int n = 0;

            if (myEntities.Count > 0)
            {
                foreach (var entity in myEntities)
                {
         
                    names[n++] = entity.TeamName;

                }
            }


            return names;
        }

        public async Task<SessionEntity> GetLastSessionCreatedAsync()
        {
            await this.InitializeAsync();

            List<SessionEntity> results = new List<SessionEntity>();
            TableQuery<SessionEntity> tableQuery = new TableQuery<SessionEntity>();
            TableQuerySegment<SessionEntity> previousSegment = null;
            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<SessionEntity> currentSegment = await this.dbCache.ExecuteQuerySegmentedAsync<SessionEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            return results.OrderByDescending(x => x.Timestamp).DefaultIfEmpty(null).First();
        }


        public async Task<bool> DeleteTeamAsync(string team_name)
        {
            await this.InitializeAsync();

            bool success = false;
            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("TeamName", QueryComparisons.Equal, team_name));

            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);
            var teams = segment.Results;

            if (teams.Count > 0)
            {
                foreach (var team in teams)
                {
                    try
                    {
                        await dbCache.ExecuteAsync(TableOperation.Delete(team));
                        success = true;

                    }
                    catch (System.Exception ex)
                    {
                        success = false;
                        Trace.TraceError(ex.Message);
                    }
                }
            }


            return success;
        }

        public async Task<bool> DeleteAllSessionsAsync()
        {
            await this.InitializeAsync();

            bool success = false;
            var query = new TableQuery<SessionEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, "0"));

            var segment = await dbCache.ExecuteQuerySegmentedAsync(query, null);
            var myEntities = segment.Results;

            if (myEntities.Count > 0)
            {
                foreach (var entity in myEntities)
                {
                    try
                    {
                        await dbCache.ExecuteAsync(TableOperation.Delete(entity));
                        success = true;

                    }
                    catch (System.Exception ex)
                    {
                        success = false;
                        break;
                    }
                }
            }


            return success;
        }


    }
}