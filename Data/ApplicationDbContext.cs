using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Models;

namespace satinalmaportali2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Addresses> Addresses { get; set; }
        public DbSet<CompanyTypes> CompanyTypes { get; set; }
        public DbSet<MembershipTypes> MembershipTypes { get; set; }
        public DbSet<Requests> Requests { get; set; }
        public DbSet<Reviews> Reviews { get; set; }
        public DbSet<ItemsModel> Items { get; set; }
        public DbSet<Forms> Forms { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Countries> Countries { get; set; }
        public DbSet<Districts> Districts { get; set; }
        public DbSet<Towns> Towns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("Companies");
                entity.HasKey(e => e.CompanyID);
                entity.Property(e => e.CompanyID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Addresses>(entity =>
            {
                entity.ToTable("Addresses");
                entity.HasKey(e => e.AddressID);
                entity.Property(e => e.AddressID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<CompanyTypes>(entity =>
            {
                entity.ToTable("CompanyTypes");
                entity.HasKey(e => e.CompanyTypeID);
            });

            modelBuilder.Entity<MembershipTypes>(entity =>
            {
                entity.ToTable("MembershipTypes");
                entity.HasKey(e => e.MembershipTypeID);
            });

            modelBuilder.Entity<Requests>(entity =>
            {
                entity.ToTable("Requests");
                entity.HasKey(e => e.RequestID);
            });

            modelBuilder.Entity<Reviews>(entity =>
            {
                entity.ToTable("Reviews");
                entity.HasKey(e => e.ReviewID);
            });

            modelBuilder.Entity<ItemsModel>(entity =>
            {
                entity.ToTable("Items");
                entity.HasKey(e => e.ItemID);
            });

            modelBuilder.Entity<Forms>(entity =>
            {
                entity.ToTable("Forms");
                entity.HasKey(e => e.FormId);
            });

            modelBuilder.Entity<Countries>(entity =>
            {
                entity.ToTable("Countries");
                entity.HasKey(e => e.CountryID);
                entity.Property(e => e.CountryID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Districts>(entity =>
            {
                entity.ToTable("District");
                entity.HasKey(e => e.DistrictID);
                entity.Property(e => e.DistrictID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Towns>(entity =>
            {
                entity.ToTable("Towns");
                entity.HasKey(e => e.TownID);
                entity.Property(e => e.TownID).ValueGeneratedOnAdd();
            });
        }
    }
}