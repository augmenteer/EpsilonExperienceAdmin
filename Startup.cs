// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Comment out the next line to use CosmosDb instead of InMemory for the anchor cache.
//#define INMEMORY_DEMO

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AdminService.Data;
using System;
using Microsoft.Extensions.Azure;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Core.Extensions;

namespace AdminService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Register the anchor key cache.
#if INMEMORY_DEMO
            services.AddSingleton<IAnchorKeyCache>(new MemoryAnchorCache());
#else
            services.AddSingleton<ISessionCache>(new CosmosDbCache(this.Configuration.GetValue<string>("StorageConnectionString")));
#endif

            // Add an http client
            services.AddHttpClient<AdminTokenService>();

            // Register the Swagger services
            services.AddSwaggerDocument(doc => doc.Title = $"{nameof(AdminService)} API");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // Register the Swagger generator and the Swagger UI middlewares
            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseRewriter(
                new RewriteOptions()
                    .AddRedirect("^$","swagger")
                );

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
