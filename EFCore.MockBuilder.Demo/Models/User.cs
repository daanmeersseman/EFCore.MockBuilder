using System.ComponentModel.DataAnnotations;

namespace EFCore.MockBuilder.Demo.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    [EmailAddress]
    public string Email { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    // Navigation Property
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ValueObject? CustomValue { get; set; }
}