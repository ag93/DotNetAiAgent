namespace Support.Agent.Api.Agents;

public sealed class RequestToolCallBudget
{
    private readonly int _maximumCalls;
    private int _usedCalls;

    public RequestToolCallBudget(int maximumCalls)
    {
        if (maximumCalls <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCalls));
        
        _maximumCalls = maximumCalls;
    }

    public int UsedCalls => Volatile.Read(ref _usedCalls);

    public void Consume()
    {
        while (true)
        {
            var current = Volatile.Read(ref _usedCalls);

            if (current >= _maximumCalls)
                throw new InvalidOperationException("The MCP tool execution limit was reached.");
            
            if (Interlocked.CompareExchange(ref _usedCalls, current + 1, current) == current)
                return;
        }
    }
}
