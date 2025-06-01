using FileManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FileManager.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<FileEntity> Files => Set<FileEntity>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<SharedFileLink> FileShares => Set<SharedFileLink>();
    
    // Новые DbSets
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserFavoriteFile> UserFavoriteFiles => Set<UserFavoriteFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Эта строка применяет все конфигурации из классов, реализующих IEntityTypeConfiguration<TEntity>
        // в сборке, где находится AppDbContext. Если у вас есть такие классы, она их подхватит.
        // Если их нет, или вы предпочитаете Fluent API здесь, то она ничего не сломает.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            // Убрано, так как CreatedAt устанавливается в SaveChangesAsync
            // entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // FileEntity configuration
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasIndex(f => f.StoragePath).IsUnique();
            
            entity.Property(f => f.StoragePath)
                .IsRequired()
                .HasMaxLength(500);
                
            entity.Property(f => f.StorageType)
                .IsRequired()
                .HasConversion<string>();
            
            entity.HasOne(f => f.Owner)
                .WithMany(u => u.Files) // Связь с существующим свойством User.Files
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(f => f.Folder)
                .WithMany(fld => fld.Files) // Связь с существующим свойством Folder.Files
                .HasForeignKey(f => f.FolderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Folder configuration
        modelBuilder.Entity<Folder>(entity =>
        {
            entity.HasIndex(f => f.VirtualPath).IsUnique();
            
            entity.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(f => f.VirtualPath)
                .IsRequired()
                .HasMaxLength(1000);
                
            entity.Property(f => f.StorageType)
                .IsRequired()
                .HasConversion<string>();
                
            entity.HasOne(f => f.Owner)
                .WithMany(u => u.Folders) // Связь с существующим свойством User.Folders
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(f => f.ParentFolder)
                .WithMany(fld => fld.Subfolders) // Связь с существующим свойством Folder.Subfolders
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SharedFileLink configuration
        modelBuilder.Entity<SharedFileLink>(entity =>
        {
            entity.ToTable("SharedFileLinks");

            entity.HasIndex(fs => fs.Token).IsUnique();
            
            entity.Property(fs => fs.Token)
                .IsRequired()
                .HasMaxLength(64);
                
            entity.Property(fs => fs.TempFilePath)
                .IsRequired()
                .HasMaxLength(500);
                
            entity.HasOne(fs => fs.File)
                .WithMany(f => f.SharedLinks) // Связь с существующим свойством FileEntity.SharedLinks
                .HasForeignKey(fs => fs.FileEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification configuration (НОВАЯ)
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.HasOne(n => n.Sender)
                .WithMany(u => u.SentNotifications) // Связь с новым свойством User.SentNotifications
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict); 

            entity.HasOne(n => n.Receiver)
                .WithMany(u => u.ReceivedNotifications) // Связь с новым свойством User.ReceivedNotifications
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade); 

            entity.HasOne(n => n.File)
                .WithMany() // FileEntity не имеет прямой коллекции уведомлений
                .HasForeignKey(n => n.FileEntityId)
                .OnDelete(DeleteBehavior.Cascade); 
        });

        // UserFavoriteFile configuration (НОВАЯ)
        modelBuilder.Entity<UserFavoriteFile>(entity =>
        {
            entity.ToTable("UserFavoriteFiles");
            entity.HasKey(uff => new { uff.UserId, uff.FileEntityId }); // Составной первичный ключ

            entity.HasOne(uff => uff.User)
                .WithMany(u => u.FavoriteFilesLink) // Связь с новым свойством User.FavoriteFilesLink
                .HasForeignKey(uff => uff.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uff => uff.File)
                .WithMany() // FileEntity не имеет прямой коллекции избранных ссылок
                .HasForeignKey(uff => uff.FileEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && 
                (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            ((BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
            
            if (entry.State == EntityState.Added)
            {
                ((BaseEntity)entry.Entity).CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}