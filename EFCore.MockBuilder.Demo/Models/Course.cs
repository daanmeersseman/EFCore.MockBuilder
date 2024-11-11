namespace EFCore.MockBuilder.Demo.Models;

public class Course
{
    public int Id { get; set; }
    public string CourseName { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; }
}