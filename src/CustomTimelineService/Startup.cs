using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CustomTimelineService.Models;
using CustomTimelineService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomTimelineService
{
    public class Startup
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(opt =>
                {
                    opt.Filters.Add(new ProducesAttribute("application/xml"));
                })
                .AddXmlSerializerFormatters();
            services.AddControllers();
            
            // Register TimesheetLoader implementation
            services.AddSingleton(container =>
            {
                var logger = container.GetRequiredService<ILogger<TimesheetLoader>>();
                return new TimesheetLoader(Path.Combine(_webHostEnvironment.ContentRootPath, "input"), logger);
            });
            // "Forward" the interfaces (see https://andrewlock.net/how-to-register-a-service-with-multiple-interfaces-for-in-asp-net-core-di/)
            services.AddHostedService(x => x.GetRequiredService<TimesheetLoader>());
            services.AddSingleton<ITimesheetSource>(x => x.GetRequiredService<TimesheetLoader>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
