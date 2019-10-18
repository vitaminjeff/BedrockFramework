﻿using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Bedrock.Framework
{
    internal class ClientConnectionContext : ConnectionContext
    {
        private readonly ConnectionContext _connection;
        private readonly TaskCompletionSource<object> _executionTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task _middlewareTask;

        public ClientConnectionContext(ConnectionContext connection, ConnectionDelegate connectionDelegate)
        {
            _connection = connection;

            // Doing this in the constructor feels bad
            _middlewareTask = connectionDelegate(this);
        }

        public TaskCompletionSource<ConnectionContext> Initialized { get; set; } = new TaskCompletionSource<ConnectionContext>();

        public Task ExecutionTask => _executionTcs.Task;

        public override string ConnectionId
        {
            get => _connection.ConnectionId;
            set => _connection.ConnectionId = value;
        }

        public override IFeatureCollection Features => _connection.Features;

        public override IDictionary<object, object> Items
        {
            get => _connection.Items;
            set => _connection.Items = value;
        }

        public override IDuplexPipe Transport
        {
            get => _connection.Transport;
            set => _connection.Transport = value;
        }

        public override EndPoint LocalEndPoint
        {
            get => _connection.LocalEndPoint;
            set => _connection.LocalEndPoint = value;
        }

        public override EndPoint RemoteEndPoint
        {
            get => _connection.RemoteEndPoint;
            set => _connection.RemoteEndPoint = value;
        }

        public override CancellationToken ConnectionClosed
        {
            get => _connection.ConnectionClosed;
            set => _connection.ConnectionClosed = value;
        }

        public override void Abort()
        {
            _connection.Abort();
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            _connection.Abort(abortReason);
        }

        public override async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();

            _executionTcs.TrySetResult(null);

            await _middlewareTask;
        }
    }
}
