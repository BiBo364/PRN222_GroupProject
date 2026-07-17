using Microsoft.AspNetCore.Http;

namespace RagEdu.Tests.Infrastructure;

internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = new(StringComparer.Ordinal);

    public bool IsAvailable { get; set; } = true;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public IEnumerable<string> Keys => _values.Keys;
    public bool WasCleared { get; private set; }
    public bool WasCommitted { get; private set; }
    public bool WasLoaded { get; private set; }

    public void Clear()
    {
        _values.Clear();
        WasCleared = true;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WasCommitted = true;
        return Task.CompletedTask;
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WasLoaded = true;
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _values.Remove(key);
    }

    public void Set(string key, byte[] value)
    {
        _values[key] = value.ToArray();
    }

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_values.TryGetValue(key, out var stored))
        {
            value = stored.ToArray();
            return true;
        }

        value = Array.Empty<byte>();
        return false;
    }
}
