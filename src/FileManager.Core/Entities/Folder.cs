using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Core.Entities;

public class Folder : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required] 
    public StorageType StorageType { get; set; }
    
    public int? OwnerId { get; set; }
    
    public int? ParentFolderId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string VirtualPath { get; set; } = string.Empty;
    
    // Навигационные свойства
    [ForeignKey(nameof(OwnerId))] 
    public User? Owner { get; set; }
    
    [ForeignKey(nameof(ParentFolderId))] 
    public Folder? ParentFolder { get; set; }
    
    public List<FileEntity> Files { get; set; } = new();
    public List<Folder> Subfolders { get; set; } = new();
}