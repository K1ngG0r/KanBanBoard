using Api.MVC.Requests;
using Api.Requests;
using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Api.MVC.Controllers;

[ApiController]
[Route("[controller]")]
public class TaskController(
    IApplicationDbContext dbContext,
    ITokenService tokenService
    ) : ControllerBase
{
    [HttpPost("[action]")]
    [Authorize]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request, CancellationToken cancellationToken = default)
    {

        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token!");


        var userExists = await dbContext.Accounts.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
            return BadRequest("Unsuitable token!");

        var newTask = new Domain.Models.Task(
            request.Name, 
            request.Description, 
            request.ShortDescription, 
            "To Do"
        );

        newTask.UserId = userId; 

        await dbContext.Tasks.AddAsync(newTask, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(newTask.Id);
    }

    [HttpGet("[action]")]
    [Authorize]
    public async Task<IActionResult> GetAllMyTasks(CancellationToken cancellationToken = default)
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token!");

 
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(x => x.UserId == userId) 
            .ToListAsync(cancellationToken);

 
        if (!tasks.Any())
            return Ok(new List<object>());
        return Ok(tasks);
    }

    [HttpPost("[action]")]
    [Authorize]
    public async Task<IActionResult> DeleteTask([FromBody] DeleteTaskRequest request, CancellationToken cancellationToken = default)
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token!");

        var task = await dbContext.Tasks
            .Where(x => x.Id == request.TaskId)
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
            return BadRequest($"Unsuitable task Id: {request.TaskId}!");

        if (task.UserId != userId)
            return BadRequest("User doesn't have this task!");

        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(task.Id);
    }
}

