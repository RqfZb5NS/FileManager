using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Core.Entities
{
    public class UserFavoriteFile // Не наследуем от BaseEntity, т.к. CreatedAt/UpdatedAt здесь могут быть избыточны или иметь другое значение
    {
        [Required]
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [Required]
        public int FileEntityId { get; set; }
        [ForeignKey(nameof(FileEntityId))]
        public virtual FileEntity File { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow; // Дата добавления в избранное
    }
}