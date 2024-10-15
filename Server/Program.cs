using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Server.Contexts;
using Server.Models;
using Server.Utility;

var mySqlVersion = new MySqlServerVersion(new Version(8, 0, 37));
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
{
    opt.UseMySql(configuration.GetConnectionString("DecisionOdysseyDb"), mySqlVersion);
    #if DEBUG
    opt.EnableSensitiveDataLogging();
    #endif
});

builder.Services.AddIdentity<User, ApplicationRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 15;
            options.Password.RequiredUniqueChars = 1;
        })
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
       .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Token validation parameters
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }

                    return Task.CompletedTask;
                }
            };
        });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    #if DEBUG
    options.AddPolicy("AllowSpecificOrigin",
        policyBuilder =>
        {
            policyBuilder.WithOrigins("https://localhost:7266")
                         .WithExposedHeaders("Token-Expired")
                         .AllowAnyHeader()
                         .AllowAnyMethod();
        });
    #else
    options.AddPolicy("AllowSpecificOrigin",
        policyBuilder =>
        {
            // TODO: Remove localhost and hard-coded IP origins
            policyBuilder.WithOrigins(
                    "https://decisionodyssey.com",
                    "https://localhost:5000",
                    "https://localhost:443",
                    "https://localhost:80",
                    "https://3.141.31.200",
                    "https://3.141.31.200:443",
                    "https://3.141.31.200:80",
                    "https://3.141.31.200:5000")
                    .WithExposedHeaders("Token-Expired")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
        });
    #endif
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Decision Odyssey API";
});

#if DEBUG

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("Logs/server.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

#else
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/server.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

#endif

builder.Host.UseSerilog();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
app.UseCors("AllowSpecificOrigin");
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await Initialization.Initialize(app.Services);

app.Run();

public partial class Program { } // so it can be referenced from tests