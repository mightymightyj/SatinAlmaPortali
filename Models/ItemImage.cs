using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    [Table("itemimages")]
    public class ItemImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ImageID { get; set; }

        public int ItemID { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public string? PublicId { get; set; }

        public bool IsMain { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ItemID")]
        public virtual ItemsModel? Item { get; set; }
    }
}
