﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc;

public interface IOnceCall
{
    ConnectionInfo ConnectionInfo { get; }

    Task<object?> CallAsync(Dictionary<string, object?> header, MethodContext methodContext, Func<object?, Task>? callback, CancellationToken token,
        Stream? stream,
        params object?[] pureArgs);

    Task StartAsync(Dictionary<string, object?> headers);

    event EventHandler? SendRequestStreamStarted;

    event EventHandler? SendRequestStreamEndOrFault;
}