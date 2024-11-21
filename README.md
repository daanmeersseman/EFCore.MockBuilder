# EFCore.MockBuilder

A fluent API to easily build mocked `DbContext` instances with entities and relationships for unit testing in Entity Framework Core (EF Core).

---

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Getting Started](#getting-started)
- [Fluent API Overview](#fluent-api-overview)
  - [Adding Entities](#adding-entities)
  - [Customizing Entities](#customizing-entities)
  - [Establishing Relationships](#establishing-relationships)
    - [Automatic Relationship Establishment](#automatic-relationship-establishment)
    - [Manual Relationship Establishment](#manual-relationship-establishment)
    - [Relating with Existing Entities](#relating-with-existing-entities)
- [License](#license)
- [Contributing](#contributing)

---

## Features

- **Fluent API** for setting up `DbContext` with entities and relationships.
- **Automatic Entity Generation** that respects data annotations like `[MaxLength]`, `[Range]`, `[Required]`.
- **Entity Customization** to override specific properties.
- **Automatic and Manual Relationship Establishment** between entities.

---

## Installation

Install via NuGet:

```bash
dotnet add package EFCore.MockBuilder
```

Or add directly in your `.csproj`:

```xml
<PackageReference Include="EFCore.MockBuilder" Version="1.0.0" />
```

---

## Getting Started

Initialize the `DbContextBuilder` with your EF Core `DbContext`:

```csharp
using EFCore.MockBuilder;
using Microsoft.EntityFrameworkCore;

// Create an in-memory DbContext for testing
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase("TestDatabase")
    .Options;
var context = new ApplicationDbContext(options);

// Initialize DbContextBuilder
var builder = new DbContextBuilder<ApplicationDbContext>(context);
```

---

## Fluent API Overview

### Adding Entities

- **`Add<TEntity>()`**: Add an entity with generated data.

  ```csharp
  var userBuilder = builder.Add<User>();
  ```

- **`Add<TEntity>(count)`**: Add multiple entities with generated data.

  ```csharp
  var usersBuilder = builder.Add<User>(5); // Adds 5 users
  ```

### Customizing Entities

- **`With(Action<TEntity>)`**: Customize properties of the entity.

  ```csharp
  userBuilder.With(u => u.Username = "customusername");
  ```

### Establishing Relationships

#### Automatic Relationship Establishment

- **`AddRelated<TRelated>()`**: Add a related entity and automatically establish the relationship based on foreign keys defined in your EF Core model.

  ```csharp
  userBuilder.AddRelated<Order>();
  ```

#### Manual Relationship Establishment

- **`AddRelated<TRelated>(mainEntityKeySelector, relatedEntityKeySelector)`**: Add a related entity and establish a manual relationship using specified properties.

  ```csharp
  userBuilder.AddRelated<Order>(
      mainEntityKeySelector: u => u.Id,
      relatedEntityKeySelector: o => o.UserId);
  ```

#### Relating with Existing Entities

- **`RelateWith<TRelated>(relatedEntity)`**: Establish a relationship with an existing entity automatically.

  ```csharp
  userBuilder.RelateWith(existingOrder);
  ```

- **`RelateWith<TRelated>(relatedEntity, mainEntityKeySelector, relatedEntityKeySelector)`**: Manually establish a relationship with an existing entity using specified properties.

  ```csharp
  userBuilder.RelateWith(
      existingOrder,
      mainEntityKeySelector: u => u.Id,
      relatedEntityKeySelector: o => o.UserId);
  ```

---

## License

EFCore.MockBuilder is licensed under the [MIT License](LICENSE).

---

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests for new features, bug fixes, or improvements.

---
