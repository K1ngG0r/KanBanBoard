using Application.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure; 

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Domain.Models.Task> Tasks { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
}
