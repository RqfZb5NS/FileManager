using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Core.Entities;

public class FileEntity : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required] 
    public StorageType StorageType { get; set; }
    
    public int? OwnerId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;
    
    public int? FolderId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string OriginalName { get; set; } = null!;

    [Required]
    public long Size { get; set; }

    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = "application/octet-stream";
    
    // Навигационные свойства
    [ForeignKey(nameof(OwnerId))] 
    public User? Owner { get; set; }
    
    [ForeignKey(nameof(FolderId))] 
    public Folder? Folder { get; set; }
    
    public List<SharedFileLink> SharedLinks { get; set; } = new();
}