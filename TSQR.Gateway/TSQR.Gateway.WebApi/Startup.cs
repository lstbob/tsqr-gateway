using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TSQR.Gateway.WebApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Add services for dependency injection
            services.AddControllers();
            // Register application services, e.g., GatewayService
            // services.AddScoped<IGatewayService, GatewayService>();
            // Add other necessary services, e.g., authentication, logging, etc.
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // Use custom middleware for request handling
            app.UseMiddleware<RequestHandlingMiddleware>();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}