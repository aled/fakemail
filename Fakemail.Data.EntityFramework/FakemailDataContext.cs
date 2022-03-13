using Fakemail.DataModels;

using Microsoft.EntityFrameworkCore;

using System.Linq;

namespace Fakemail.Data.EntityFramework
{
    public class FakemailDataContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={"c:\\temp\\fakemail.db"}");

        public DbSet<User> Users { get; set; }

        public DbSet<Email> Emails { get; set; }

        public DbSet<Attachment> Attachments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .Property(p => p.UserId)
                .ValueGeneratedOnAdd();

            builder.Entity<User>()
                .HasIndex(x => x.Username).IsUnique();

            builder.Entity<Email>()
                .Property(p => p.EmailId)
                .ValueGeneratedOnAdd();

            builder.Entity<Attachment>()
                .Property(p => p.AttachmentId)
                .ValueGeneratedOnAdd();
        }
    }
}
