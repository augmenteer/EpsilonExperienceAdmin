// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.AspNetCore.Mvc;
using AdminService.Data;
using System.Threading.Tasks;

namespace AdminService.Controllers
{
    [Route("api/apptoken")]
    [ApiController]
    public class AppTokenController : ControllerBase
    {
        private AdminTokenService tokenService;

        public AppTokenController(AdminTokenService tokenService)
        {
            this.tokenService = tokenService;
        }

        // GET api/apptoken
        [HttpGet]
        public Task<string> GetAsync()
        {
            // TODO: Put your application-specific authorization and authentication logic here

            return this.tokenService.RequestToken();
        }
    }
}
