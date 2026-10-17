using Support.Business.Api.Services;

namespace Support.Business.Tests;
public class OrderServiceTests
{
    [Fact]
    public void GetById_WhenOrderExists_ReturnsOrder()
    {
        // Arrange
        var service = new OrderService();

        // Act
        var order = service.GetById("O-2001");

        // Assert
        Assert.NotNull(order);
        Assert.Equal("O-2001", order.Id);
        Assert.Equal("C-1002", order.CustomerId);
        Assert.Equal("Shipped", order.Status);
        Assert.Equal(129.99m, order.TotalAmount);
    }

    [Fact]
    public void GetById_WhenOrderDoesNotExist_ReturnsNull()
    {
        var service = new OrderService();

        var order = service.GetById("UNKNOWN");

        Assert.Null(order);
    }

    [Fact]
    public void GetById_WhenIdUsesLowercase_FindsOrder()
    {
        var service = new OrderService();

        var order = service.GetById("o-2001");

        Assert.NotNull(order);
        Assert.Equal("O-2001", order.Id);
    }
}
