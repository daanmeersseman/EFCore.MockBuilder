# EFCore.MockBuilder

A fluent API for building mocked DbContext instances with entities and relationships, designed for efficient unit testing in EF Core.

EFCore.MockBuilder is a simple, flexible library that helps you create mocked `DbContext` instances with entities and relationships for unit testing Entity Framework Core. It provides a fluent API to define entities, set up relationships, and customize properties for various test scenarios.

## Features

- **Fluent API** for setting up `DbContext` with entities and relationships.
- **Automatic Foreign Key Detection** for easier relationship setup.
- **Entity Customization** with support for overriding specific properties.
- **Bogus Integration** for realistic dummy data generation.
- **Flexible Relationship Setup**: Includes `AddRelated` for new relationships and `RelateWith` for linking existing entities.

## Installation

Install the package from NuGet:

```bash
dotnet add package EFCore.MockBuilder
```

Or add it directly in your `.csproj`:

```xml
<PackageReference Include="EFCore.MockBuilder" Version="1.0.0" />
```

## Getting Started

### 1. Setting Up Your DbContext and Entities

Define your `DbContext` and entities as you normally would for EF Core:

```csharp
public class SchoolDbContext : DbContext
{
    public DbSet<StudentStudents =Set<Student>();
    public DbSet<CourseCourses =Set<Course>();
    public DbSet<EnrollmentEnrollments =Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrollment>()
            .HasOne(e =e.Student)
            .WithMany(s =s.Enrollments)
            .HasForeignKey(e =e.StudentId);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e =e.Course)
            .WithMany(c =c.Enrollments)
            .HasForeignKey(e =e.CourseId);
    }
}

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<EnrollmentEnrollments { get; set; }
}

public class Course
{
    public int Id { get; set; }
    public string CourseName { get; set; }
    public ICollection<EnrollmentEnrollments { get; set; }
}

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public Student Student { get; set; }
    public Course Course { get; set; }
}
```

### 2. Using EFCore.MockBuilder in Tests

Use `EFCore.MockBuilder` to set up a test `DbContext` with entities and relationships.

```csharp
using EFCore.MockBuilder;
using Xunit;

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
        var student = dbContextBuilder.Add<Student>().With(s =s.Name = "Alice Johnson");
        var course = dbContextBuilder.Add<Course>().With(c =c.CourseName = "Mathematics 101");

        // Add a new Enrollment and relate it to existing student and course
        student.AddRelated<Enrollment>()
               .With(e =e.EnrollmentDate = DateTime.Today)
               .RelateWith(course);

        dbContextBuilder.Build();  // Save changes

        // Assert
        Assert.NotNull(dbContext.Students.FirstOrDefault(s =s.Name == "Alice Johnson"));
        Assert.NotNull(dbContext.Courses.FirstOrDefault(c =c.CourseName == "Mathematics 101"));
        Assert.NotEmpty(dbContext.Enrollments);
    }
}
```

### 3. Fluent API Overview

#### Adding and Customizing Entities

- **`Add<TEntity>()`**: Adds an entity with generated data.
  ```csharp
  var student = dbContextBuilder.Add<Student>().With(s =s.Name = "Custom Name");
  ```

- **`Add<TEntity>(count)`**: Adds multiple entities with generated data.
  ```csharp
  var students = dbContextBuilder.Add<Student>(5);
  ```

#### Setting Relationships

- **`AddRelated<TRelated>()`**: Adds a related entity to an existing entity and links them by foreign keys.
  ```csharp
  var enrollment = student.AddRelated<Enrollment>().With(e =e.EnrollmentDate = DateTime.Today);
  ```

- **`RelateWith()`**: Links two existing entities if they share a foreign key.
  ```csharp
  student.RelateWith(course);
  ```

## License

EFCore.MockBuilder is licensed under the [MIT License](LICENSE).

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests for new features, bug fixes, or improvements.
