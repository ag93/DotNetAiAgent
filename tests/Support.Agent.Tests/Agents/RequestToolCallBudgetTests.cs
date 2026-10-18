
using Support.Agent.Api.Agents;

namespace Support.Agent.Tests.Agents;

public class RequestToolCallBudgetTests
{
    [Fact]
    public void Consume_WithinLimit_Succeeds()
    {
        var budget = new RequestToolCallBudget(4);
        budget.Consume();
        budget.Consume();
        budget.Consume();
        budget.Consume();
        Assert.Equal(4, budget.UsedCalls);
    }

    [Fact]
    public void Consume_AfterLimit_Throws()
    {
        var budget = new RequestToolCallBudget(2);

        budget.Consume();
        budget.Consume();

        Assert.Throws<InvalidOperationException>(() => budget.Consume());
        Assert.Equal(2, budget.UsedCalls);
    }

    [Fact]
    public void Consume_ConcurrentCalls_DoesNotExceedLimit()
    {
        var budget = new RequestToolCallBudget(4);
        var successes = 0;

        Parallel.For(0, 20, _ =>
        {
            try
            {
                budget.Consume();
                Interlocked.Increment(ref successes);
            }
            catch (InvalidOperationException)
            {
                // Expected once the budget is exhausted.
            }
        });

        Assert.Equal(4, successes);
        Assert.Equal(4, budget.UsedCalls);
    }
}
