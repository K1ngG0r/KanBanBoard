using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Application.Interfaces;

public interface IApplicationDbContext
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Domain.Models.Task> Tasks { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}