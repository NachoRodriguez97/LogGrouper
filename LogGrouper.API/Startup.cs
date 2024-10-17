using LogGrouper.Models.Global;
using Loginter.Common.Tools.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace LogGrouper.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Encrypter.SetOptionChain(ChainOption.ChainOptionOne);

            AppSettings.MainConnString = Encrypter.Decrypt(this.Configuration.GetConnectionString("ConnString"));
            AppSettings.ZebraPrinter = Encrypter.Decrypt(this.Configuration.GetConnectionString("ZebraPrinter"));
            AppSettings.Schema = Encrypter.Decrypt(this.Configuration.GetValue("Schema", ""));
            AppSettings.PrinterApi = Encrypter.Decrypt(this.Configuration.GetValue("PrinterApi", ""));
            AppSettings.PrintersSrv = Encrypter.Decrypt(this.Configuration.GetValue("PrintersSrv", ""));
            AppSettings.PrinterApiToken = this.Configuration.GetValue("PrinterApiToken", "");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowOrigin", builder =>
                {
                    builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
            });
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "LogGrouper.API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LogGrouper.API v1"));
            }

            app.UseRouting();

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });

            app.UseAuthorization();

            app.UseMiddleware<CorsMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
