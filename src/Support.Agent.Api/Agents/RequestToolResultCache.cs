namespace Support.Agent.Api.Agents;

public sealed class RequestToolResultCache<T>
{
    private readonly Dictionary<(string ToolName, string Id), T> _results = new();

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<T> GetOrAddAsync(string toolName, 
        string id, 
        Func<Task<T>> executeAsync,
        Func<T, bool> shouldCache,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A lookup ID is required.", nameof(id));
        }

        var key = (ToolName: toolName, Id: id.Trim().ToUpperInvariant());

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_results.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var result = await executeAsync();

            if (shouldCache(result))
            {
                _results[key] = result;
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }
}
