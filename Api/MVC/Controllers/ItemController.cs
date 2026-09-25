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
        // 1. Получаем ID пользователя
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token!");

        // 2. Проверяем, существует ли пользователь (оптимизировано через AnyAsync)
        var userExists = await dbContext.Accounts.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
            return BadRequest("Unsuitable token!");

        // 3. Создаем задачу
        var newTask = new Domain.Models.Task(
            request.Name, 
            request.Description, 
            request.ShortDescription, 
            "To Do"
        );

        // !!! ИСПРАВЛЕНИЕ 1: Привязываем задачу к пользователю !!!
        // Если у вас свойство называется AccountId, замените UserId на AccountId
        newTask.UserId = userId; 

        await dbContext.Tasks.AddAsync(newTask, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(newTask.Id);
    }

    [HttpGet("[action]")]
    [Authorize]
    public async Task<IActionResult> GetAllMyTasks(CancellationToken cancellation = default)
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid token!");

        // !!! ИСПРАВЛЕНИЕ 2: Запрашиваем задачи напрямую из DbSet Tasks !!!
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(x => x.UserId == userId) // Замените на x.AccountId == userId, если нужно
            .ToListAsync(cancellation);

        // !!! ИСПРАВЛЕНИЕ 3: Проверяем на пустоту, а не на null !!!
        if (!tasks.Any())
            return Ok(new List<object>()); // Или return NotFound("У вас пока нет задач");

        Console.WriteLine(tasks.Count);

        return Ok(tasks);
    }
    // [HttpGet("[action]")]
    // [Authorize]
    // public async Task<IActionResult> GetAllUserItems([FromQuery] string login, CancellationToken cancellation = default)
    // {
    //     var items = await dbContext.Accounts
    //         .AsNoTracking()
    //         .Where(x => x.Login == login)
    //         .Select(x => x.Items)
    //         .ToListAsync(cancellation);

    //     if (items is null)
    //         return BadRequest("Unsuitable Id!");

    //     return Ok(items);
    // }

    // [HttpGet("[action]")]
    // [Authorize]
    // public async Task<IActionResult> GetAllItems(CancellationToken cancellation = default)
    // {
    //     var items = await dbContext.Items
    //         .AsNoTracking()
    //         .ToListAsync(cancellation);

    //     return Ok(items);
    // }

    // [HttpPost("[action]")]
    // [Authorize]
    // public async Task<IActionResult> AddItemToUser([FromBody] AddItemToUserRequest request, CancellationToken cancellationToken = default)
    // {
    //     var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    //     var admin = await dbContext.Accounts
    //         .Where(x => x.Id == userId)
    //         .Where(x => x.Role == "admin")
    //         .FirstOrDefaultAsync(cancellationToken);

    //     var user = await dbContext.Accounts
    //         .Where(x => x.Id == request.UserId)
    //         .FirstOrDefaultAsync(cancellationToken);

    //     if (user is null || admin is null)
    //         return BadRequest($"Unsuitable token: {request.UserId}!");

    //     var item = await dbContext.Items
    //         .AsNoTracking()
    //         .Where(x => x.Id == request.ItemId)
    //         .FirstOrDefaultAsync(cancellationToken);

    //     if (item is null)
    //         return BadRequest($"Unsuitable item Id: {request.ItemId}!");

    //     user.Items.Add(item);
    //     await dbContext.SaveChangesAsync(cancellationToken);

    //     return Ok(item.Id);
    // }

    // [HttpPost("[action]")]
    // [Authorize]
    // public async Task<IActionResult> DeleteItemToUser([FromBody] AddItemToUserRequest request, CancellationToken cancellationToken = default)
    // {
    //     var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    //     var admin = await dbContext.Accounts
    //         .Where(x => x.Id == userId)
    //         .Where(x => x.Role == "admin")
    //         .FirstOrDefaultAsync(cancellationToken);

    //     var user = await dbContext.Accounts
    //         .Where(x => x.Id == request.UserId)
    //         .FirstOrDefaultAsync(cancellationToken);

    //     if (user is null || admin is null)
    //         return BadRequest($"Unsuitable token: {request.UserId}!");

    //     var item = await dbContext.Items
    //         .Where(x => x.Id == request.ItemId)
    //         .FirstOrDefaultAsync(cancellationToken);

    //     if (item is null)
    //         return BadRequest($"Unsuitable item Id: {request.ItemId}!");

    //     if (user.Items.FirstOrDefault(x => x == item) is null)
    //         return BadRequest($"User don't have this item!");

    //     user.Items.Remove(item);
    //     await dbContext.SaveChangesAsync(cancellationToken);

    //     return Ok(item.Id);
    // }

    // [HttpPost("[action]")]
    // [Authorize]
    // public async Task<IActionResult> TransefItem([FromBody] TransferItemRequest request, CancellationToken cancellationToken = default)
    // {
    //     var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    //     var admin = await dbContext.Accounts
    //         .Where(x => x.Id == userId)
    //         .Where(x => x.Role == "admin")
    //         .FirstOrDefaultAsync(cancellationToken);
    //     if (admin is null)
    //         return BadRequest($"Unsuitable admin token!");

    //     var sender = await dbContext.Accounts
    //         .Where(x => x.Id == request.SenderId)
    //         .FirstOrDefaultAsync(cancellationToken);
    //     if (sender is null)
    //         return BadRequest($"Unsuitable sender id!");

    //     var recipient = await dbContext.Accounts
    //         .Where(x => x.Id == request.RecipientId)
    //         .FirstOrDefaultAsync(cancellationToken);
    //     if (recipient is null)
    //         return BadRequest($"Unsuitable recipient id!");

    //     var item = await dbContext.Items
    //         .Where(x => x.Id == request.ItemId)
    //         .FirstOrDefaultAsync(cancellationToken);
    //     if (item is null)
    //         return BadRequest($"Unsuitable item Id: {request.ItemId}!");
    //     Console.WriteLine(item.Id);

    //     if (sender.Items.FirstOrDefault(z => z.Id == item.Id) is null)
    //         return BadRequest($"User don't have this item!");

    //     recipient.Items.Add(item);
    //     sender.Items.Remove(item);

    //     await dbContext.SaveChangesAsync(cancellationToken);

    //     return Ok();
    // }

    // [HttpPost("[action]")]
    // [Authorize]
    // public async Task<IActionResult> FreezeItem([FromBody] FreezeItemRequest request, CancellationToken cancellationToken = default)
    // {
    //     var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    //     var admin = await dbContext.Accounts
    //         .Where(x => x.Id == userId)
    //         .Where(x => x.Role == "admin")
    //         .FirstOrDefaultAsync(cancellationToken);
    //     if (admin is null)
    //         return BadRequest($"Unsuitable admin token!");

    //     var item = await dbContext.Items
    //         .Where(x => x.Id == request.ItemId)
    //         .FirstOrDefaultAsync(cancellationToken);
    //     if (item is null)
    //         return BadRequest($"Unsuitable item Id: {request.ItemId}!");

    //     item.isFrozen = true;

    //     await dbContext.SaveChangesAsync(cancellationToken);

    //     return Ok();
    // }

    // [HttpPost("[action]")]
    // [Authorize]
    // public async Task<IActionResult> DefrostItem([FromBody] FreezeItemRequest request, CancellationToken cancellationToken = default)
    // {
    //     var userId = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    //     var admin = await dbContext.Accounts
    //         .Where(x => x.Id == userId)
    //         .Where(x => x.Role == "admin")
    //         .FirstOrDefaultAsync(cancellationToken);
    //     if (admin is null)
    //         return BadRequest($"Unsuitable admin token!");

    //     var item = await dbContext.Items
    //         .Where(x => x.Id == request.ItemId)
    //         .FirstOrDefaultAsync(cancellationToken);
    //     if (item is null)
    //         return BadRequest($"Unsuitable item Id: {request.ItemId}!");

    //     item.isFrozen = false;

    //     await dbContext.SaveChangesAsync(cancellationToken);

    //     return Ok();
    // }

    // [HttpGet("{id}")] // Маршрут: GET /Item/{guid}
    // [Authorize]
    // public async Task<IActionResult> GetItemById(Guid id, CancellationToken cancellationToken = default)
    // {
    //     return Ok(await dbContext.Items
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
    //         ?? throw new KeyNotFoundException("Item not found"));
    // }
}

