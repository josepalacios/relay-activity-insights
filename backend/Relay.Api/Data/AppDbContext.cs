using Microsoft.EntityFrameworkCore;

namespace Relay.Api.Data;

// Persistence is configured for PostgreSQL; model and migrations are intentionally deferred.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
