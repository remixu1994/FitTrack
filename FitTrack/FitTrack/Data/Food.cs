using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitTrack.Data;

public class Food
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// 品牌，例如：麦当劳、肯德基、兰州拉面；为空表示通用食品
    /// </summary>
    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = "100g";

    [Column(TypeName = "decimal(5,1)")]
    public decimal Calories { get; set; }

    [Column(TypeName = "decimal(5,1)")]
    public decimal Protein { get; set; }

    [Column(TypeName = "decimal(5,1)")]
    public decimal Fat { get; set; }

    [Column(TypeName = "decimal(5,1)")]
    public decimal Carbs { get; set; }

    public bool IsDefault { get; set; } = true;

    public ICollection<DailyFoodRecord>? DailyRecords { get; set; }
}