using Chroma.Browser.Models;

namespace Chroma.Browser.Services;

public sealed class SessionService
{
    private readonly JsonStore<BrowserSessionState> _store = new(AppPaths.SessionFile);

    public Task<BrowserSessionState> LoadAsync() => _store.LoadAsync();
    public Task SaveAsync(BrowserSessionState state) => _store.SaveAsync(state);
}

