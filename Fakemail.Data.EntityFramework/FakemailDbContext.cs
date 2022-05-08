using System.Linq;
using System;
using System.Reflection.Emit;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Fakemail.Data.EntityFramework
{
    /// <summary>
    /// This is needed for the dotnet ef tools to work
    /// </summary>
    public class FakemailContextFactory : IDesignTimeDbContextFactory<FakemailDbContext>
    {
        public FakemailDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FakemailDbContext>();
            optionsBuilder.UseSqlite("Data Source=fakemail-design.sqlite");

            return new FakemailDbContext(optionsBuilder.Options);
        }
    }

    public class FakemailDbContext : DbContext
    {
        /// <summary>
        /// This constructor used by the DI container
        /// </summary>
        /// <param name="options"></param>
        public FakemailDbContext(DbContextOptions<FakemailDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<SmtpUser> SmtpUsers { get; set; }

        public DbSet<Email> Emails { get; set; }

        public DbSet<Attachment> Attachments { get; set; }

        public override int SaveChanges()
        {
            var utcNow = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is BaseEntity baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        baseEntity.CreatedTimestampUtc = utcNow;
                    }

                    baseEntity.UpdatedTimestampUtc = utcNow;
                }
            }

            return base.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder builder)
{
            //This will singularize all table names
            foreach (IMutableEntityType entityType in builder.Model.GetEntityTypes())
            {
                entityType.SetTableName(entityType.GetDefaultTableName());
            }

            // Seed data for SMTP Alias table - this is required by the smtp server in order to deliver mail, 
            // using the linux 'fakemail' account.
            builder.Entity<SmtpAlias>().HasData(new SmtpAlias() { Account = "fakemail"});

            builder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            builder.Entity<Email>().HasOne(x => x.SmtpUser).WithMany(x => x.Emails).HasForeignKey(x => x.SmtpUsername);
        }
    }
}
