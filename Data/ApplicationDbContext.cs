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
        public DbSet<RequestItem> RequestItems { get; set; }
        public DbSet<RequestOffers> RequestOffers { get; set; }
        public DbSet<RequestOfferItem> RequestOfferItems { get; set; }
        public DbSet<Reviews> Reviews { get; set; }
        public DbSet<ItemsModel> Items { get; set; }
        public DbSet<Forms> Forms { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Countries> Countries { get; set; }
        public DbSet<Districts> Districts { get; set; }
        public DbSet<Towns> Towns { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("cartitems");
                entity.HasKey(e => e.CartItemID);
                entity.Property(e => e.CartItemID).ValueGeneratedOnAdd();
                entity.HasOne(e => e.Company)
                      .WithMany()
                      .HasForeignKey(e => e.CompanyID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");
                entity.HasKey(e => e.OrderID);
                entity.Property(e => e.OrderID).ValueGeneratedOnAdd();
                entity.HasMany(e => e.OrderItems)
                      .WithOne(e => e.Order)
                      .HasForeignKey(e => e.OrderID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("orderitems");
                entity.HasKey(e => e.OrderItemID);
                entity.Property(e => e.OrderItemID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("companies");
                entity.HasKey(e => e.CompanyID);
                entity.Property(e => e.CompanyID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Addresses>(entity =>
            {
                entity.ToTable("addresses");
                entity.HasKey(e => e.AddressID);
                entity.Property(e => e.AddressID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<CompanyTypes>(entity =>
            {
                entity.ToTable("companytypes");
                entity.HasKey(e => e.CompanyTypeID);
            });

            modelBuilder.Entity<MembershipTypes>(entity =>
            {
                entity.ToTable("membershiptypes");
                entity.HasKey(e => e.MembershipTypeID);
            });

            modelBuilder.Entity<Requests>(entity =>
            {
                entity.ToTable("requests");
                entity.HasKey(e => e.RequestID);
                entity.Property(e => e.RequestID).ValueGeneratedOnAdd();

                entity.HasMany(e => e.Offers)
                    .WithOne(o => o.Request)
                    .HasForeignKey(o => o.RequestID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Items)
                    .WithOne(i => i.Request)
                    .HasForeignKey(i => i.RequestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RequestItem>(entity =>
            {
                entity.ToTable("request_items");
                entity.HasKey(e => e.RequestItemID);
                entity.Property(e => e.RequestItemID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<RequestOffers>(entity =>
            {
                entity.ToTable("request_offers");
                entity.HasKey(e => e.OfferID);
                entity.Property(e => e.OfferID).ValueGeneratedOnAdd();

                entity.HasOne(e => e.Supplier)
                    .WithMany()
                    .HasForeignKey(e => e.SupplierID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.OfferItems)
                    .WithOne(oi => oi.Offer)
                    .HasForeignKey(oi => oi.OfferID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RequestOfferItem>(entity =>
            {
                entity.ToTable("request_offer_items");
                entity.HasKey(e => e.OfferItemID);
                entity.Property(e => e.OfferItemID).ValueGeneratedOnAdd();

                entity.HasOne(e => e.RequestItem)
                    .WithMany()
                    .HasForeignKey(e => e.RequestItemID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Reviews>(entity =>
            {
                entity.ToTable("reviews");
                entity.HasKey(e => e.ReviewID);
                entity.Property(e => e.ReviewID).ValueGeneratedOnAdd();
                entity.HasOne(e => e.Order)
                      .WithMany()
                      .HasForeignKey(e => e.OrderID)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.TargetCompany)
                      .WithMany()
                      .HasForeignKey(e => e.CompanyID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ReviewerCompany)
                      .WithMany()
                      .HasForeignKey(e => e.ReviewerCompanyID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ItemsModel>(entity =>
            {
                entity.ToTable("items");
                entity.HasKey(e => e.ItemID);
                entity.Property(e => e.ItemID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Forms>(entity =>
            {
                entity.ToTable("forms");
                entity.HasKey(e => e.FormId);
                entity.Property(e => e.FormId).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Countries>(entity =>
            {
                entity.ToTable("countries");
                entity.HasKey(e => e.CountryID);
                entity.Property(e => e.CountryID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("category");
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Districts>(entity =>
            {
                entity.ToTable("district");
                entity.HasKey(e => e.DistrictID);
                entity.Property(e => e.DistrictID).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Towns>(entity =>
            {
                entity.ToTable("towns");
                entity.HasKey(e => e.TownID);
                entity.Property(e => e.TownID).ValueGeneratedOnAdd();
            });

            // PostgreSQL büyük/küçük harf duyarlılığı için tüm tablo ve kolon adlarını küçük harfe eşle
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    entity.SetTableName(tableName.ToLowerInvariant());
                }

                foreach (var property in entity.GetProperties())
                {
                    var columnName = property.GetColumnName();
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        property.SetColumnName(columnName.ToLowerInvariant());
                    }
                }

                foreach (var key in entity.GetKeys())
                {
                    var keyName = key.GetName();
                    if (!string.IsNullOrEmpty(keyName))
                    {
                        key.SetName(keyName.ToLowerInvariant());
                    }
                }

                foreach (var key in entity.GetForeignKeys())
                {
                    var constraintName = key.GetConstraintName();
                    if (!string.IsNullOrEmpty(constraintName))
                    {
                        key.SetConstraintName(constraintName.ToLowerInvariant());
                    }
                }

                foreach (var index in entity.GetIndexes())
                {
                    var indexName = index.GetDatabaseName();
                    if (!string.IsNullOrEmpty(indexName))
                    {
                        index.SetDatabaseName(indexName.ToLowerInvariant());
                    }
                }
            }
        }
    }
}