using EFCore.MockBuilder.Demo.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.MockBuilder.Demo.Tests
{
    public class DbContextBuilderTests
    {
        // Helper method to create an in-memory DbContext
        private DbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
                .Options;

            return new DbContext(options);
        }

        /// <summary>
        /// 1. Adding Users with Dummy Data
        /// </summary>
        [Fact]
        public void AddUsers_WithDummyData_ShouldAddUsersToContext()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            builder.Add<User>(2); // Adds two Users with dummy data
            var dbContext = builder.Build();

            // Assert
            Assert.Equal(2, dbContext.Users.Count());
            var users = dbContext.Users.ToList();
            Assert.All(users, user =>
            {
                Assert.False(string.IsNullOrEmpty(user.Username));
                Assert.False(string.IsNullOrEmpty(user.Email));
                Assert.NotEqual(default, user.DateOfBirth);
            });
        }

        /// <summary>
        /// 2. Customizing User Properties
        /// </summary>
        [Fact]
        public void AddUser_WithCustomProperties_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            builder.Add<User>()
                .With(user =>
                {
                    user.Username = "testuser";
                    user.Email = "testuser@example.com";
                    user.DateOfBirth = new DateTime(1990, 1, 1);
                });

            var dbContext = builder.Build();

            // Assert
            var user = dbContext.Users.First();
            Assert.Equal("testuser", user.Username);
            Assert.Equal("testuser@example.com", user.Email);
            Assert.Equal(new DateTime(1990, 1, 1), user.DateOfBirth);
        }

        /// <summary>
        /// 3. Adding Orders with Dummy Data
        /// </summary>
        [Fact]
        public void AddOrders_WithDummyData_ShouldAddOrdersToContext()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            builder.Add<Order>(3); // Adds three Orders with dummy data
            var dbContext = builder.Build();

            // Assert
            Assert.Equal(3, dbContext.Orders.Count());
            var orders = dbContext.Orders.ToList();
            Assert.All(orders, order =>
            {
                Assert.NotEqual(default, order.OrderDate);
                Assert.InRange(order.TotalAmount, 0, 10000);
            });
        }

        /// <summary>
        /// 4. Automatically Establishing Relationships
        /// </summary>
        [Fact]
        public void AddUser_WithRelatedOrders_ShouldEstablishRelationshipsAutomatically()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            builder.Add<User>()
                .AddRelated<Order>() // Adds an Order related to the User
                .With(order => order.TotalAmount = 500);

            var dbContext = builder.Build();

            // Assert
            var user = dbContext.Users.Include(u => u.Orders).First();
            Assert.Single(user.Orders);
            var order = user.Orders.First();
            Assert.Equal(500, order.TotalAmount);
            Assert.Equal(user.Id, order.UserId);
        }

        /// <summary>
        /// 5. Manually Establishing Relationships
        /// </summary>
        [Fact]
        public void AddUserAndOrder_WithManualRelationship_ShouldRelateEntities()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            var userBuilder = builder.Add<User>()
                .With(u => u.Username = "manualuser");

            userBuilder.AddRelated<Order>(
                mainEntityKeySelector: u => u.Id,
                relatedEntityKeySelector: o => o.UserId)
                .With(o => o.TotalAmount = 750);

            var dbContext = builder.Build();

            // Assert
            var user = dbContext.Users.First();
            var order = dbContext.Orders.First();
            Assert.Equal(user.Id, order.UserId);
            Assert.Equal(750, order.TotalAmount);
        }

        /// <summary>
        /// 6. Relating with Existing Entities
        /// </summary>
        [Fact]
        public void RelateExistingEntities_ShouldEstablishRelationship()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Add a User
            var userBuilder = builder.Add<User>()
                .With(u => u.Username = "existinguser");

            // Add an Order separately
            builder.Add<Order>()
                .With(o => o.TotalAmount = 300);

            var dbContext = builder.Build();

            // Act
            var user = dbContext.Users.First();
            var order = dbContext.Orders.First();

            // Establish relationship manually
            userBuilder.RelateWith(order,
                mainEntityKeySelector: u => u.Id,
                relatedEntityKeySelector: o => o.UserId);

            dbContext.SaveChanges();

            // Assert
            Assert.Equal(user.Id, order.UserId);
        }

        /// <summary>
        /// 7. Adding Multiple Related Entities
        /// </summary>
        [Fact]
        public void AddUser_WithMultipleOrders_ShouldAddAllRelatedEntities()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            var userBuilder = builder.Add<User>()
                .With(u => u.Username = "multiorderuser");

            // Add multiple Orders related to the User
            for (int i = 0; i 3; i++)
            {
                userBuilder.AddRelated<Order>()
                    .With(o => o.TotalAmount = (i + 1) * 100);
            }

            var dbContext = builder.Build();

            // Assert
            var user = dbContext.Users.Include(u => u.Orders).First();
            Assert.Equal(3, user.Orders.Count);
            Assert.All(user.Orders, order =>
            {
                Assert.Equal(user.Id, order.UserId);
            });
        }

        /// <summary>
        /// 8. Testing Data Annotations Compliance
        /// </summary>
        [Fact]
        public void GeneratedEntities_ShouldRespectDataAnnotations()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            builder.Add<User>(1);
            builder.Add<Order>(1);
            var dbContext = builder.Build();

            // Assert for User
            var user = dbContext.Users.First();
            Assert.True(user.Username.Length <= 50);
            Assert.False(string.IsNullOrEmpty(user.Email));
            Assert.True(user.DateOfBirth <= DateTime.Now);

            // Assert for Order
            var order = dbContext.Orders.First();
            Assert.InRange(order.TotalAmount, 0, 10000);
        }

        /// <summary>
        /// 9. Testing Primary Key Assignment
        /// </summary>
        [Fact]
        public void AddedEntities_ShouldHaveUniquePrimaryKeys()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            builder.Add<User>(2);
            builder.Add<Order>(2);
            var dbContext = builder.Build();

            // Assert
            var userIds = dbContext.Users.Select(u => u.Id).ToList();
            var orderIds = dbContext.Orders.Select(o => o.Id).ToList();

            Assert.Equal(2, userIds.Count);
            Assert.Equal(2, orderIds.Count);
            Assert.Equal(userIds.Distinct().Count(), userIds.Count);
            Assert.Equal(orderIds.Distinct().Count(), orderIds.Count);
        }

        /// <summary>
        /// 10. Testing Cascade Delete Behavior
        /// </summary>
        [Fact]
        public void DeletingUser_ShouldCascadeDeleteOrders()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var builder = new DbContextBuilder<DbContext>(context);

            // Act
            var userBuilder = builder.Add<User>()
                .With(u => u.Username = "cascadeuser");

            userBuilder.AddRelated<Order>()
                .With(o => o.TotalAmount = 200);

            var dbContext = builder.Build();

            // Delete the user
            var user = dbContext.Users.First();
            dbContext.Users.Remove(user);
            dbContext.SaveChanges();

            // Assert
            Assert.Empty(dbContext.Users);
            Assert.Empty(dbContext.Orders);
        }
    }
}
