using System.ComponentModel.DataAnnotations;

namespace EFCore.MockBuilder.Demo.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Foreign Key
        public int UserId { get; set; }

        public DateTime OrderDate { get; set; }

        [Range(0.0, 10000.0)]
        public decimal TotalAmount { get; set; }

        // Navigation Property
        public User User { get; set; } = null!;
    }
}