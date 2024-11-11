using EFCore.MockBuilder.Demo.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.MockBuilder.Demo.Tests;

public class DbContextBuilderTests
{
    [Fact]
    public void CreateMockDbContext_WithEntitiesAndRelationships()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SchoolDbContext>()
            .UseInMemoryDatabase("TestDatabase")
            .Options;

        using var dbContext = new SchoolDbContext(options);
        var dbContextBuilder = new DbContextBuilder<SchoolDbContext>(dbContext);

        // Act
        // Add a single Student with customized properties
        var student = dbContextBuilder.Add<Student>().With(s => s.Name = "Alice Johnson");

        // Add multiple Course entities with generated data
        var course = dbContextBuilder.Add<Course>(3); // Generates random 3 courses
        var courseX= dbContextBuilder.Add<Course>(); 
        var courseY = dbContextBuilder.Add<Course>();

        // Add a new Enrollment and relate it to the specific student and first course
        var enrollment1 = student.AddRelated<Enrollment>()
            .With(e => e.EnrollmentDate = DateTime.Today)
            .RelateWith(courseX.Entity);

        // Add another Enrollment and relate it to the same student and second course
        var enrollment2 = student.AddRelated<Enrollment>()
            .With(e => e.EnrollmentDate = DateTime.Today.AddDays(-30))
            .RelateWith(courseY.Entity);

        // Add another student and relate to an existing course
        var anotherStudent = dbContextBuilder.Add<Student>().With(s => s.Name = "Bob Smith");
        var enrollment3 = anotherStudent.AddRelated<Enrollment>()
            .With(e => e.EnrollmentDate = DateTime.Today.AddDays(-10))
            .RelateWith(courseY.Entity);

        dbContextBuilder.Build();  // Save all entities and relationships

        // Assert
        var students = dbContext.Students.Include(s => s.Enrollments).ToList();
        var retrievedCourses = dbContext.Courses.Include(c => c.Enrollments).ToList();

        Assert.Equal(2, students.Count);
        Assert.Equal("Alice Johnson", students[0].Name);
        Assert.Equal("Bob Smith", students[1].Name);
        Assert.Equal(5, retrievedCourses.Count); // Three courses generated

        // Check the enrollments and relationships
        Assert.Equal(2, students[0].Enrollments.Count); // Alice has 2 enrollments
        Assert.Single(students[1].Enrollments); // Bob has 1 enrollment
        Assert.Contains(students[0].Enrollments, e => e.CourseId == retrievedCourses[3].Id);
        Assert.Contains(students[0].Enrollments, e => e.CourseId == retrievedCourses[4].Id);
    }
}