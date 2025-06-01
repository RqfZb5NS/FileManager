using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Core.Entities;

public class User : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;
    
    // Навигационные свойства
    public List<FileEntity> Files { get; set; } = new List<FileEntity>();
    public List<Folder> Folders { get; set; } = new List<Folder>();
    // Навигационные свойства для связи с Notification (полученные и отправленные)
    public virtual ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
    public virtual ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();

    // Навигационное свойство для связи с UserFavoriteFile (избранные файлы пользователя)
    public virtual ICollection<UserFavoriteFile> FavoriteFilesLink { get; set; } = new List<UserFavoriteFile>();
}