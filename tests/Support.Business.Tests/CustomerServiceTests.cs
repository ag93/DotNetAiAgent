using Support.Business.Api.Services;

namespace Support.Business.Tests;
public class CustomerServiceTests
{
    [Fact]
    public void GetById_WhenCustomerExists_ReturnsCustomer()
    {
        // Arrange
        var service = new CustomerService();

        // Act
        var customer = service.GetById("C-1002");

        // Assert
        Assert.NotNull(customer);
        Assert.Equal("C-1002", customer.Id);
        Assert.Equal("Alex Morgan", customer.Name);
    }

    [Fact]
    public void GetById_WhenCustomerDoesNotExist_ReturnsNull()
    {
        // Arrange
        var service = new CustomerService();

        // Act
        var customer = service.GetById("UNKNOWN");

        // Assert
        Assert.Null(customer);
    }

    [Fact]
    public void GetById_WhenIdUsesLowercase_FindsCustomer()
    {
        // Arrange
        var service = new CustomerService();

        // Act
        var customer = service.GetById("c-1002");

        // Assert
        Assert.NotNull(customer);
        Assert.Equal("C-1002", customer.Id);
    }
}
