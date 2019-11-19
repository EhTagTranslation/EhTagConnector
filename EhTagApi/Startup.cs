﻿using EhTagClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EhTagApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new RepoClient());

            services.AddSingleton<Database>();

            services.AddScoped<Filters.GitETagFilter>();
            services.AddScoped<Filters.GitHubIdentityFilter>();

            services.AddResponseCompression(options => options.EnableForHttps = true);

            services.AddSingleton(Consts.SerializerSettings);
            services.AddSingleton(serviceProvider
                => Newtonsoft.Json.JsonSerializer.Create(serviceProvider.GetRequiredService<Newtonsoft.Json.JsonSerializerSettings>()));

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder => builder
                    .AllowAnyOrigin()
                    .WithHeaders("If-Match", "If-None-Match", "Content-Type", "X-Token")
                    .WithExposedHeaders("ETag", "Location")
                    .WithMethods("OPTIONS", "HEAD", "GET", "PUT", "POST", "DELETE")
                    .SetPreflightMaxAge(TimeSpan.FromDays(1))
                    .DisallowCredentials()
                    .Build());
            });

            services.AddControllers(options =>
                {
                    options.OutputFormatters.Add(new Formatters.FullOutputFormatter(options));
                    options.OutputFormatters.Add(new Formatters.RawOutputFormatter(options));
                    options.OutputFormatters.Add(new Formatters.TextOutputFormatter(options));
                    options.OutputFormatters.Add(new Formatters.HtmlOutputFormatter(options));
                    options.OutputFormatters.Add(new Formatters.AstOutputFormatter(options));

                    options.InputFormatters.Add(new Formatters.TextFormatter());
                }).AddNewtonsoftJson(options =>
                {
                    var serializer = Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings
                    {
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                    });
                    var tw = new StringWriter();
                    serializer.Serialize(tw, Consts.SerializerSettings);
                    serializer.Populate(new StringReader(tw.ToString()), options.SerializerSettings);
                })
                .AddFormatterMappings(options =>
                {
                    options.SetMediaTypeMappingForFormat("raw.json", "application/raw+json");
                    options.SetMediaTypeMappingForFormat("text.json", "application/text+json");
                    options.SetMediaTypeMappingForFormat("html.json", "application/html+json");
                    options.SetMediaTypeMappingForFormat("ast.json", "application/ast+json");
                })
                .AddXmlSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
            }
            app.UseHttpsRedirection();
            app.UseCors();
            app.UseRouting();
            app.UseResponseCompression();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
