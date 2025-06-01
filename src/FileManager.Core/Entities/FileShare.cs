using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Core.Entities;

public class SharedFileLink : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string Token { get; set; } = Guid.NewGuid().ToString("N");


    [Required]
    public DateTime ExpirationDate { get; set; } = DateTime.UtcNow.AddHours(24);

    public int AccessCount { get; set; } = 0;
    public int MaxAccessCount { get; set; } = 1;

    [Required]
    [MaxLength(500)]
    public string TempFilePath { get; set; } = string.Empty;

    [Required]
    [ForeignKey(nameof(File))]
    public int FileEntityId { get; set; }

    // Навигационные свойства
    public FileEntity File { get; set; } = null!;
}