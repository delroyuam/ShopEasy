using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Data
    {
    public class AppDbContext : DbContext
        {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
            {
            }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(user => user.Email)
                .IsUnique();

            modelBuilder.Entity<UserRole>()
                .HasIndex(userRole => new { userRole.UserId, userRole.Name })
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(category => category.Name)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(product => product.Name)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<OrderItem>()
                .HasIndex(orderItem => new { orderItem.OrderId, orderItem.ProductId })
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasMany(category => category.Products)
                .WithOne(product => product.Category)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(user => user.UserRoles)
                .WithOne(userRole => userRole.User)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(user => user.Orders)
                .WithOne(order => order.User)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasMany(order => order.OrderItems)
                .WithOne(orderItem => orderItem.Order)
                .HasForeignKey(orderItem => orderItem.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany<OrderItem>()
                .WithOne(orderItem => orderItem.Product)
                .HasForeignKey(orderItem => orderItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }
