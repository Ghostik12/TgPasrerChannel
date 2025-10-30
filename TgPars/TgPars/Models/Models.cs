using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgPars.Models
{
    public class ChatToParse
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public string ChatTitle { get; set; }
    }

    public class FilterKeyword
    {
        public int Id { get; set; }
        public string Keyword { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<ChatToParse> ChatsToParse { get; set; }
        public DbSet<FilterKeyword> FilterKeywords { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatToParse>().HasKey(c => c.Id);
            modelBuilder.Entity<FilterKeyword>().HasKey(f => f.Id);
        }
    }
}
