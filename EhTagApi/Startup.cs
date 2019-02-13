using EhTagClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddHttpsRedirection(options => options.RedirectStatusCode = 301);

            services.AddResponseCompression(options => options.EnableForHttps = true);

            services.AddSingleton(Consts.SerializerSettings);
            services.AddSingleton(serviceProvider
                => Newtonsoft.Json.JsonSerializer.Create(serviceProvider.GetRequiredService<Newtonsoft.Json.JsonSerializerSettings>()));

            services.AddCors(options =>
            {

                options.AddDefaultPolicy(builder => builder
                    .AllowAnyOrigin()
                    .WithHeaders("If-Match", "If-None-Match", "Content-Type", "Accept", "Accept-Encoding", "X-Token")
                    .WithExposedHeaders("E-Tag", "Location")
                    .WithMethods("HEAD", "GET", "PUT", "POST", "DELETE")
                    .Build());
            });

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options =>
                {
                    var serializer = Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings
                    {
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                    });
                    var tw = new StringWriter();
                    serializer.Serialize(tw, Consts.SerializerSettings);
                    serializer.Populate(new StringReader(tw.ToString()), options.SerializerSettings);
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseResponseCompression();
            app.UseMvc();
        }
    }
}
