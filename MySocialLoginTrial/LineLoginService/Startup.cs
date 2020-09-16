using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace LineLoginService
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
            services.AddControllersWithViews();
            // OpenID Connect �� Cookie �ƃZ�b�g�Ŏg���K�v������
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                // Identity�̃T���v���R�[�h�̗�ɕ����sosicalloginsettings����������B
                IConfigurationSection lineSec =
                    Configuration.GetSection("OutsideAuthentication:Line");

                // LINE ��LoginAPI�p�̃`�����l������ClientId/ ClientSecret�Ƃ��Đݒ肷��B
                options.ClientId = lineSec["ClientId"];     // 1654923826
                options.ClientSecret = lineSec["ClientSecret"];      // c0842477f7e483536377f08960069fa7
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UseTokenLifetime = true;
                options.SaveTokens = true;

                // Discovery �����̐ݒ��ǉ�����
                options.Configuration = new OpenIdConnectConfiguration
                {
                    Issuer = lineSec["Issuer"], // https://access.line.me
                    AuthorizationEndpoint = lineSec["AuthorizationEndpoint"], // https://access.line.me/oauth2/v2.1/authorize
                    TokenEndpoint = lineSec["TokenEndpoint"], // https://api.line.me/oauth2/v2.1/token
                };

                // LINE Login �̏����� HS256 �Ȃ̂� IssuerSigningKey �� SymmetricSecurityKey �Őݒ肷��
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(options.ClientSecret)),
                    NameClaimType = "name",
                    ValidAudience = options.ClientId
                };
            });

            // Add Session service.
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

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

            app.UseRouting();

            // to specify to use Authentication Middleware 
            app.UseAuthentication();
            app.UseAuthorization();

            // Add middleware for session.
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
