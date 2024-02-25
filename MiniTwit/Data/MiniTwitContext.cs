using Microsoft.EntityFrameworkCore;
using MiniTwit.Models.DataModels;

namespace MiniTwit.Data
{
    public class MiniTwitContext : DbContext
    {
        public MiniTwitContext(DbContextOptions<MiniTwitContext> options)
            : base(options) { }

        //public DbSet<Models.Twit> Twits { get; set; }

        /* protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) */
        /* { */
        /*     optionsBuilder.UseSqlite("./minitwit.db"); */
        /* } */

        public DbSet<Twit> Twits => Set<Twit>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Follower> Followers => Set<Follower>();
    }
}
