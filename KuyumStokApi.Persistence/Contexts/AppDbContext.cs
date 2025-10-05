using System;
using System.Collections.Generic;
using KuyumStokApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KuyumStokApi.Persistence.Contexts;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BankTransactions> BankTransactions { get; set; }

    public virtual DbSet<Banks> Banks { get; set; }

    public virtual DbSet<Branches> Branches { get; set; }

    public virtual DbSet<Customers> Customers { get; set; }

    public virtual DbSet<LifecycleActions> LifecycleActions { get; set; }

    public virtual DbSet<Limits> Limits { get; set; }

    public virtual DbSet<PaymentMethods> PaymentMethods { get; set; }

    public virtual DbSet<ProductCategories> ProductCategories { get; set; }

    public virtual DbSet<ProductLifecycles> ProductLifecycles { get; set; }

    public virtual DbSet<ProductTypes> ProductTypes { get; set; }

    public virtual DbSet<ProductVariants> ProductVariants { get; set; }

    public virtual DbSet<PurchaseDetails> PurchaseDetails { get; set; }

    public virtual DbSet<Purchases> Purchases { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<SaleDetails> SaleDetails { get; set; }

    public virtual DbSet<Sales> Sales { get; set; }

    public virtual DbSet<Stocks> Stocks { get; set; }

    public virtual DbSet<Stores> Stores { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BankTransactions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bank_transactions_pkey");

            entity.ToTable("bank_transactions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BankId).HasColumnName("bank_id");
            entity.Property(e => e.CommissionRate).HasColumnName("commission_rate");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpectedAmount).HasColumnName("expected_amount");
            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Bank).WithMany(p => p.BankTransactions)
                .HasForeignKey(d => d.BankId)
                .HasConstraintName("bank_transactions_bank_id_fkey");

            entity.HasOne(d => d.Sale).WithMany(p => p.BankTransactions)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("bank_transactions_sale_id_fkey");
        });

        modelBuilder.Entity<Banks>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("banks_pkey");

            entity.ToTable("banks");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Branches>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("branches_pkey");

            entity.ToTable("branches");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.StoreId).HasColumnName("store_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Store).WithMany(p => p.Branches)
                .HasForeignKey(d => d.StoreId)
                .HasConstraintName("branches_store_id_fkey");
        });

        modelBuilder.Entity<Customers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customers_pkey");

            entity.ToTable("customers");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<LifecycleActions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lifecycle_actions_pkey");

            entity.ToTable("lifecycle_actions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Limits>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("limits_pkey");

            entity.ToTable("limits");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.MaxThreshold).HasColumnName("max_threshold");
            entity.Property(e => e.MinThreshold).HasColumnName("min_threshold");
            entity.Property(e => e.ProductVariantId).HasColumnName("product_variant_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Branch).WithMany(p => p.Limits)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("limits_branch_id_fkey");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.Limits)
                .HasForeignKey(d => d.ProductVariantId)
                .HasConstraintName("limits_product_variant_id_fkey");
        });

        modelBuilder.Entity<PaymentMethods>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payment_methods_pkey");

            entity.ToTable("payment_methods");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<ProductCategories>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_categories_pkey");

            entity.ToTable("product_categories");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProductLifecycles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_lifecycles_pkey");

            entity.ToTable("product_lifecycles");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActionId).HasColumnName("action_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.StockId).HasColumnName("stock_id");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("timestamp");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Action).WithMany(p => p.ProductLifecycles)
                .HasForeignKey(d => d.ActionId)
                .HasConstraintName("product_lifecycles_action_id_fkey");

            entity.HasOne(d => d.Stock).WithMany(p => p.ProductLifecycles)
                .HasForeignKey(d => d.StockId)
                .HasConstraintName("product_lifecycles_stock_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ProductLifecycles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("product_lifecycles_user_id_fkey");
        });

        modelBuilder.Entity<ProductTypes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_types_pkey");

            entity.ToTable("product_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.ProductTypes)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("product_types_category_id_fkey");
        });

        modelBuilder.Entity<ProductVariants>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_variants_pkey");

            entity.ToTable("product_variants");

            entity.HasIndex(e => new { e.ProductTypeId, e.Name, e.Brand, e.Ayar, e.Color }, "uq_product_variants_type_name_brand_ayar_color").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Ayar).HasColumnName("ayar");
            entity.Property(e => e.Brand).HasColumnName("brand");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ProductTypeId).HasColumnName("product_type_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ProductType).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductTypeId)
                .HasConstraintName("product_variants_product_type_id_fkey");
        });

        modelBuilder.Entity<PurchaseDetails>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("purchase_details_pkey");

            entity.ToTable("purchase_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PurchaseId).HasColumnName("purchase_id");
            entity.Property(e => e.PurchasePrice).HasColumnName("purchase_price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.StockId).HasColumnName("stock_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseDetails)
                .HasForeignKey(d => d.PurchaseId)
                .HasConstraintName("purchase_details_purchase_id_fkey");

            entity.HasOne(d => d.Stock).WithMany(p => p.PurchaseDetails)
                .HasForeignKey(d => d.StockId)
                .HasConstraintName("purchase_details_stock_id_fkey");
        });

        modelBuilder.Entity<Purchases>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("purchases_pkey");

            entity.ToTable("purchases");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.Purchases)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("purchases_branch_id_fkey");

            entity.HasOne(d => d.Customer).WithMany(p => p.Purchases)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("purchases_customer_id_fkey");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Purchases)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("purchases_payment_method_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Purchases)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("purchases_user_id_fkey");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<SaleDetails>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sale_details_pkey");

            entity.ToTable("sale_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.SoldPrice).HasColumnName("sold_price");
            entity.Property(e => e.StockId).HasColumnName("stock_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Sale).WithMany(p => p.SaleDetails)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("sale_details_sale_id_fkey");

            entity.HasOne(d => d.Stock).WithMany(p => p.SaleDetails)
                .HasForeignKey(d => d.StockId)
                .HasConstraintName("sale_details_stock_id_fkey");
        });

        modelBuilder.Entity<Sales>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sales_pkey");

            entity.ToTable("sales");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.Sales)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("sales_branch_id_fkey");

            entity.HasOne(d => d.Customer).WithMany(p => p.Sales)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("sales_customer_id_fkey");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Sales)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("sales_payment_method_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Sales)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("sales_user_id_fkey");
        });

        modelBuilder.Entity<Stocks>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stocks_pkey");

            entity.ToTable("stocks");

            entity.HasIndex(e => e.Barcode, "stocks_barcode_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Barcode).HasColumnName("barcode");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Carat).HasColumnName("carat");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Gram).HasColumnName("gram");
            entity.Property(e => e.Milyem).HasColumnName("milyem");
            entity.Property(e => e.ProductVariantId).HasColumnName("product_variant_id");
            entity.Property(e => e.QrCode).HasColumnName("qr_code");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.StoneType).HasColumnName("stone_type");
            entity.Property(e => e.Thickness).HasColumnName("thickness");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.Width).HasColumnName("width");

            entity.HasOne(d => d.Branch).WithMany(p => p.Stocks)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("stocks_branch_id_fkey");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.Stocks)
                .HasForeignKey(d => d.ProductVariantId)
                .HasConstraintName("stocks_product_variant_id_fkey");
        });

        modelBuilder.Entity<Stores>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stores_pkey");

            entity.ToTable("stores");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.PasswordSalt)
                .HasDefaultValueSql("''::text")
                .HasColumnName("password_salt");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username).HasColumnName("username");

            entity.HasOne(d => d.Branch).WithMany(p => p.Users)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("users_branch_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("users_role_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
