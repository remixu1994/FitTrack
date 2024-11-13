using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitTrack.Data;

public class DailyFoodRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public ApplicationUser User { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    [ForeignKey(nameof(Food))]
    public int FoodId { get; set; }

    public Food Food { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Quantity { get; set; } = 1m;

    [Column(TypeName = "decimal(6,1)")]
    public decimal TotalCalories { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; }
}