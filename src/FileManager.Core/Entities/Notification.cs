using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Core.Entities
{
    public class Notification : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } // Используем Guid для Id уведомления

        [Required]
        public int SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public virtual User Sender { get; set; } = null!;

        [Required]
        public int ReceiverId { get; set; }
        [ForeignKey(nameof(ReceiverId))]
        public virtual User Receiver { get; set; } = null!;

        [Required]
        public int FileEntityId { get; set; } // Ссылка на FileEntity
        [ForeignKey(nameof(FileEntityId))]
        public virtual FileEntity File { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        // SentDate будет использоваться из BaseEntity.CreatedAt
        // public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveryDate { get; set; } // Дата фактической доставки (если была задержка)
    }
}