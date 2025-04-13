using DotnetAuthWebAPi.Domain.Contract;
using DotnetAuthWebAPi.Infrastructure.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DotnetAuthWebAPi.Extensions
{
    public static partial class ApplicationService
    {
        //Allow any origin ,method and header
        public static void ConfigureCors(this IServiceCollection services) {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();


                });
            });
                
        }
        public static void ConfigureIdnetity(this IServiceCollection services)
        {
            services.AddIdentityCore<IdentityUser>(o =>
            {
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = false;
                o.Password.RequiredLength = 8;
            }).AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        }
        public static void ConfigureJwt(this IServiceCollection service,IConfiguration configuration)
        {
            var jwtSetting = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            if (jwtSetting == null || string.IsNullOrEmpty(jwtSetting.Key))

            {
                throw new InvalidOperationException("JWT secret key is not configured.");
            }
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.Key));
            service.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters(){
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSetting.ValidIssuer,
                    ValidAudience = jwtSetting.ValidAudience,
                    IssuerSigningKey = secretKey
                };
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "You are not authorized to access this resource .Please" +
                            "autenticate"
                        });
                        return context.Response.WriteAsync(result);
                    }
                };
            });
        }
    }
}
