using EFCore.MockBuilder.Demo.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.MockBuilder.Demo.Tests;

public class UsageTests
{
    [Fact]
    public void MockingLibrary_AllFunctionalities_ShouldWorkAsExpected()
    {
        // Create in-memory DbContext
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase("TestDatabase")
            .Options;
        var context = new DbContext(options);

        // Initialize DbContextBuilder
        var builder = new DbContextBuilder<DbContext>(context);

        // Add a User with dummy data
        var userBuilder = builder.Add<User>();

        // Customize User properties
        userBuilder.With(user =>
        {
            user.Username = "testuser";
            user.Email = "testuser@example.com";
            user.DateOfBirth = new DateTime(1990, 1, 1);
        });

        // Add an Order related to the User
        userBuilder.AddRelated<Order>()
            .With(order =>
            {
                order.TotalAmount = 100.50m;
                order.OrderDate = DateTime.UtcNow;
            });

        // Add multiple Orders related to the User
        for (int i = 0; i < 2; i++)
        {
            userBuilder.AddRelated<Order>()
                .With(order =>
                {
                    order.TotalAmount = (i + 1) * 50m;
                    order.OrderDate = DateTime.UtcNow.AddDays(-i);
                });
        }

        // Add a second User
        var secondUserBuilder = builder.Add<User>()
            .With(u => u.Username = "seconduser");

        // Add an Order related to the second User
        secondUserBuilder.AddRelated<Order>()
            .With(order =>
            {
                order.TotalAmount = 250.00m;
                order.OrderDate = DateTime.UtcNow.AddDays(-5);
            });

        // Build the DbContext
        var dbContext = builder.Build();

        // Delete 'testuser' and related Orders
        var userToDelete = dbContext.Users.First(u => u.Username == "testuser");
        dbContext.Users.Remove(userToDelete);
        dbContext.SaveChanges();

        // Verify 'testuser' is deleted
        Assert.DoesNotContain(dbContext.Users, u => u.Id == userToDelete.Id);
        // Verify related Orders are deleted
        Assert.DoesNotContain(dbContext.Orders, o => o.UserId == userToDelete.Id);

        // Ensure other Users and Orders still exist
        Assert.NotEmpty(dbContext.Users); // Should contain 'seconduser'
        Assert.NotEmpty(dbContext.Orders); // Should contain orders related to 'seconduser'

        // Verify the remaining User is 'seconduser'
        var remainingUser = dbContext.Users.Single();
        Assert.Equal("seconduser", remainingUser.Username);

        // Verify Orders belong to 'seconduser'
        var remainingOrders = dbContext.Orders.ToList();
        Assert.All(remainingOrders, order =>
        {
            Assert.Equal(remainingUser.Id, order.UserId);
        });
    }
}
