using Microsoft.EntityFrameworkCore;
using DotNetRefreshApp.Models;

namespace DotNetRefreshApp.Data
{
    // The DbContext is the bridge between your application code and the database.
    // It manages the connection and maps your classes (Models) to database tables.
    public class AppDbContext : DbContext
    {
        // Constructor that accepts configuration options (like connection string)
        // and passes them to the base DbContext class.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // This DbSet represents the "Users" table in the database.
        // You can query this like a collection: _context.Users.Where(u => u.Role == "Admin").ToList();
        public DbSet<User> Users { get; set; }

        public DbSet<Record> Records { get; set; }

        // This DbSet represents the "Conversations" table for storing complete chat conversations
        // Each conversation stores all messages as JSON in a single record
        public DbSet<Conversation> Conversations { get; set; }

    }
}
