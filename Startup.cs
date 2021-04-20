using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace PayPage
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
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(12);
            });


            services.AddMvc().AddNewtonsoftJson();
            services.AddControllersWithViews();
            
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            Config.Timeout = (int)Configuration.GetValue(typeof(int), "Timeout");
            Config.ReturnUrl = (string)Configuration.GetValue(typeof(string), "ReturnUrl");
            if (!Config.ReturnUrl.EndsWith("/"))
                Config.ReturnUrl += "/";
            Config.DbPrefixSb = Configuration.GetSection("DbPrefix").GetSection("Bank").Value;
            Config.DbPrefixRc = Configuration.GetSection("DbPrefix").GetSection("Local").Value;

            string connectionSb = Configuration.GetConnectionString("DbExternal");
            string connectionRc = Configuration.GetConnectionString("DbLocal");
           
            services.AddDbContext<Rc.ApplicationContext>(options =>
                options.UseSqlServer(connectionRc));
            services.AddDbContext<Sb.ApplicationContext>(options =>
                options.UseSqlServer(connectionSb));
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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSession();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("GetRes", "GetRes", new { controller = "Home", action = "GetRes" });
                
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                
            });
        }
    }
}
