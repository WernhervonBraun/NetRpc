﻿using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetRpc
{
    internal sealed class OnceCallFactory : IOnceCallFactory
    {
        private readonly IClientConnectionFactory _factory;
        private readonly ILogger _logger;

        public OnceCallFactory(IClientConnectionFactory factory, ILoggerFactory loggerFactory)
        {
            _factory = factory;
            _logger = loggerFactory.CreateLogger("NetRpc");
        }

        public ValueTask DisposeAsync()
        {
            if (_factory != null)
                return _factory.DisposeAsync();
            return new ValueTask();
        }

        public Task<IOnceCall> CreateAsync(int timeoutInterval)
        {
            return Task.FromResult<IOnceCall>(new OnceCall(new BufferClientOnceApiConvert(_factory.Create(), _logger), timeoutInterval, _logger));
        }
    }
}