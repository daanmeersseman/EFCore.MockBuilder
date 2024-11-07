
namespace EFCore.MockBuilder.Demo.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; }
}