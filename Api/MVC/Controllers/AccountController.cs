using Api.Requests;
using Api.Response;
using Application;
using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController(
    IApplicationDbContext dbContext,
    ITokenService tokenService
    ) : ControllerBase
{
    [HttpPost("[action]")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Accounts.AsNoTracking()
            .Where(x => x.Login == request.Login)
            .FirstOrDefaultAsync(cancellationToken);
        if (user != null )
            return Ok($"Login is taken!");

        var newUser = new Account(null, request.Login, request.Password);

        await dbContext.Accounts.AddAsync(newUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        (var accessToken, var newRefreshToken) = GenerateTokens(newUser.Id, cancellationToken);

        HttpContext.Response.Cookies.Append(AuthConstants.AccessToken, accessToken);
        HttpContext.Response.Cookies.Append(AuthConstants.RefreshToken, newRefreshToken.Token);

        var result = new LoginResult()
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token
        };

        return Ok(result);
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Accounts.AsNoTracking()
            .Where(x => x.Login == request.Login)
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || user.HashPassword != request.Password)
            return BadRequest($"Uncorrect login or password!");

        (var accessToken, var newRefreshToken) = GenerateTokens(user.Id, cancellationToken);

        HttpContext.Response.Cookies.Append(AuthConstants.AccessToken, accessToken);
        HttpContext.Response.Cookies.Append(AuthConstants.RefreshToken, newRefreshToken.Token);

        var result = new LoginResult()
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token
        };

        return Ok(result);
    }

    [HttpPost("[action]")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

        var refreshTokens = await dbContext.RefreshTokens
            .Where(x => x.AccountId == userId)
            .Where(x => !x.IsExpired)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.Invalidate();
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var refreshToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(x => x.Token == request.RefreshToken)
            .FirstOrDefaultAsync(cancellationToken);

        if (refreshToken == null)
            return StatusCode(401);

        if (DateTime.UtcNow > refreshToken.ExpirateAt)
        {
            refreshToken.Invalidate();
            await dbContext.SaveChangesAsync(cancellationToken);
            return StatusCode(401);
        }

        (var accessToken, var newRefreshToken) = GenerateTokens(refreshToken.AccountId, cancellationToken);
        dbContext.RefreshTokens.Add(newRefreshToken);

        var result = new LoginResult()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token
        };

        return Ok(result);
    }

    private (string, RefreshToken) GenerateTokens(Guid id, CancellationToken cancellationToken = default)
    {
        var user = dbContext.Accounts.AsNoTracking()
            .FirstOrDefault(x => x.Id == id);

        var accessToken = tokenService.GetAccessToken(user);
        var refreshToken = tokenService.GetRefreshToken();

        var refreshTokenEntity = new RefreshToken(user.Id, DateTime.UtcNow + TimeSpan.FromHours(2), refreshToken);
        dbContext.RefreshTokens.Add(refreshTokenEntity);
        dbContext.SaveChangesAsync(cancellationToken);

        return (accessToken, refreshTokenEntity);
    }
}
