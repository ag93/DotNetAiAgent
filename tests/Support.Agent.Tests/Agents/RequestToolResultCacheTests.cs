
using Support.Agent.Api.Agents;

namespace Support.Agent.Tests.Agents;

public class RequestToolResultCacheTests
{
    [Fact]
    public async Task SameLookup_ExecutesOnlyOnce()
    {
        var cache = new RequestToolResultCache<string>();
        var executionCount = 0;

        async Task<string> ExecuteAsync()
        {
            executionCount++;
            await Task.Yield();
            return "Customer found";
        }

        var first = await cache.GetOrAddAsync(
            "get_customer", "C-1002",
            ExecuteAsync, _ => true);

        var second = await cache.GetOrAddAsync(
            "get_customer", "c-1002",
            ExecuteAsync, _ => true);

        Assert.Equal("Customer found", first);
        Assert.Equal(first, second);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task DifferentLookups_ExecuteSeparately()
    {
        var cache = new RequestToolResultCache<string>();
        var executionCount = 0;

        Task<string> ExecuteAsync()
        {
            executionCount++;
            return Task.FromResult("Result");
        }

        await cache.GetOrAddAsync(
            "get_customer", "C-1002",
            ExecuteAsync, _ => true);

        await cache.GetOrAddAsync(
            "get_order", "C-1002",
            ExecuteAsync, _ => true);

        Assert.Equal(2, executionCount);
    }

    [Fact]
    public async Task FailedResult_IsNotCached()
    {
        var cache = new RequestToolResultCache<string>();
        var executionCount = 0;

        Task<string> ExecuteAsync()
        {
            executionCount++;
            return Task.FromResult("Error");
        }

        await cache.GetOrAddAsync(
            "get_customer", "C-1002",
            ExecuteAsync, result => result != "Error");

        await cache.GetOrAddAsync(
            "get_customer", "C-1002",
            ExecuteAsync, result => result != "Error");

        Assert.Equal(2, executionCount);
    }
}
