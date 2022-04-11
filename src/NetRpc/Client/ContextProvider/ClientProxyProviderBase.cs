﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NetRpc;

public abstract class ClientProxyProviderBase : IClientProxyProvider
{
    private readonly ConcurrentDictionary<string, Lazy<object?>> _caches = new(StringComparer.Ordinal);

    protected abstract ClientProxy<TService>? CreateProxyInner<TService>(string optionsName) where TService : class;

    public ClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class
    {
        var key = $"{optionsName}_{typeof(TService).FullName}";
        var clientProxy = (ClientProxy<TService>?)_caches.GetOrAdd(key, new Lazy<object?>(() =>
           CreateProxyInner<TService>(optionsName), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        return clientProxy;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            DisposeManaged();
    }

    private void DisposeManaged()
    {
        foreach (var proxy in _caches.Values)
        {
            var disposable = proxy.Value as IDisposable;
            disposable?.Dispose();
        }
    }
}
