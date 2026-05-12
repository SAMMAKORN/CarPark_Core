using CarPark.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPark.Shared
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(CreateByUser))]
        public Guid? CreateBy { get; set; }

        public User? CreateByUser { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UpdateByUser))]
        public Guid? UpdateBy { get; set; }

        public User? UpdateByUser { get; set; }
        public DateTime? UpdateAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(DeletedByUser))]
        public Guid? DeletedBy { get; set; }

        public User? DeletedByUser { get; set; }
        public DateTime? DeleteAt { get; set; }
    }
}
