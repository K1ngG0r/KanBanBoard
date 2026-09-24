using Application;
using Application.Interfaces;
using Domain.Models;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.

builder.Services.AddInfastructure();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("kiggorkrutoykiggorkrutoykiggorkrutoykiggorkrutoykiggorkrutoykiggorkrutoy")),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents()
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[AuthConstants.AccessToken];
                return System.Threading.Tasks.Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                if (context.Handled) return;

                // Пытаемся обновить токен с помощью refresh‑токена из cookie
                var refreshToken = context.Request.Cookies[AuthConstants.RefreshToken];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    context.HandleResponse();
                    await context.HttpContext.Response.WriteAsync("Токен отсутствует в куках");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var jwtService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();

                var storedToken = await dbContext.RefreshTokens
                    .Include(rt => rt.Account)
                    .Where(x => x.Token == refreshToken)
                    .Where(x => !x.IsExpired)
                    .FirstOrDefaultAsync();

                if (storedToken == null)
                {
                    context.HandleResponse();
                    await context.HttpContext.Response.WriteAsync("Токен отсутствует в бд");
                    return;
                }

                var newAccessToken = jwtService.GetAccessToken(storedToken.Account);
                var newRefreshToken = jwtService.GetRefreshToken();

                storedToken.Invalidate();
                var newRefreshTokenEntity = new RefreshToken(storedToken.AccountId, DateTime.UtcNow + TimeSpan.FromHours(1), newRefreshToken);
                dbContext.RefreshTokens.Add(newRefreshTokenEntity);
                await dbContext.SaveChangesAsync();

                context.Response.Cookies.Append(AuthConstants.AccessToken, newAccessToken);
                context.Response.Cookies.Append(AuthConstants.RefreshToken, newRefreshToken);

                context.HandleResponse();

                context.Response.Redirect(context.Request.Path + context.Request.QueryString);
            }
        };
    });

builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate(); // применит все ожидающие миграции
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseForwardedHeaders();

app.MapControllers();

app.Run();
