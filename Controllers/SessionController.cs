// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.AspNetCore.Mvc;
using AdminService.Data;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AdminService.Controllers
{
    [Route("api/sessions")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly ISessionCache SessionCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionController"/> class.
        /// </summary>
        /// <param name="sessionCache">The anchor key cache.</param>
        public SessionController(ISessionCache sessionCache)
        {
            this.SessionCache = sessionCache;
        }

        // GET api/sessions/[n]
        [HttpGet("{team_name}")]
        public async Task<ActionResult<SessionEntity>> GetAsync(string team_name)
        {
            return await this.SessionCache.GetSessionForTeamAsync(team_name);
        }

        [HttpGet("pending_teams")]
        public async Task<ActionResult<SessionEntity[]>> GetPendingAsync()
        {
            return await this.SessionCache.GetPendingSessionsAsync();
        }
        // GET api/sessions/lastSession
        [HttpGet("latestSession")]
        public async Task<ActionResult<SessionEntity>> GetLatestSessionAsync()
        {
            return await this.SessionCache.GetLastSessionCreatedAsync();
        }

        // GET api/sessions/allSessions
        [HttpGet("allSessions")]
        public async Task<ActionResult<SessionEntity[]>> GetAllAsync()
        {
            // Get all keys
            SessionEntity[] sessions = await this.SessionCache.GetAllSessionRecordsAsync();


            return sessions;
        }

        // GET api/sessions/all_as_string
        [HttpGet("team_names_array")]
        public async Task<ActionResult<string[]>> GetTeamNamesAsync()
        {
            // Get all keys as a string, not json
            string[] teams = await this.SessionCache.GetTeamNamesAsync();

            return teams;
        }
        // GET api/sessions/delete_all
        [HttpGet("delete_all_sessions")]
        public async Task<ActionResult<bool>> DeleteAllAsync()
        {
            bool success = await this.SessionCache.DeleteAllSessionsAsync();

            return success;
        }

        // GET api/anchors/delete
        [HttpGet("delete")]
        public async Task<ActionResult<bool>> DeleteAsync(string team_name)
        {
            bool success = await this.SessionCache.DeleteTeamAsync(team_name);

            return success;
        }

        [HttpGet("get_total_room_time")]
        public async Task<ActionResult<int>> GetRoomTimesAsync(string team_name)
        {
            return await this.SessionCache.CalculateTotalRoomTime(team_name);
        }

        [HttpGet("get_total_session_time")]
        public async Task<ActionResult<int>> GetSessionTimeAsync(string team_name)
        {
            return await this.SessionCache.CalculateTotalSessionTime(team_name);
        }
        // POST api/sessions/add_team
        [HttpPost("add_team")]
        public async Task<ActionResult<bool>> PostAsync(string team_name, int member_count)
        {

            if (team_name.Equals(string.Empty))
                return this.BadRequest();
           
            if (member_count <= 0)
                return this.BadRequest();
             

            // Set the key and return the anchor number
            return await this.SessionCache.CreateSession(team_name, member_count);
        }

        // POST api/anchors/add_time
        [HttpPost("add_solution_time")]
        public async Task<ActionResult<bool>> SetSolutionTime(string team_name, int time_in_seconds)
        {
            if ( time_in_seconds < 0)
            {
                return this.BadRequest();
            }

            // Set the key and return the anchor number
            return await this.SessionCache.SetSolutionTime(team_name, time_in_seconds);
        }

        // POST api/anchors/add_time
        [HttpPost("add_room_time")]
        public async Task<ActionResult<bool>> SetRoomTime(string team_name, int room_number, int time_in_seconds)
        {
            if (time_in_seconds < 0)
            {
                return this.BadRequest();
            }

            // Set the key and return the anchor number
            return await this.SessionCache.SetRoomTime(team_name, room_number, time_in_seconds);
        }


        [HttpPost("has_team")]
        public async Task<ActionResult<bool>> HasTeam(string team_name)
        {
            if (team_name == string.Empty)
            {
                return this.BadRequest();
            }

            // Set the key and return the anchor number
            return await this.SessionCache.ContainsSessionForTeamAsync(team_name);
        }

    }
}
