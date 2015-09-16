// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class LibuvSocketInputReader
    {
        private static readonly Action<UvStreamHandle, int, Exception, object> _readCallback = ReadCallback;
        private static readonly Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback = AllocCallback;

        private readonly UvStreamHandle _socket;
        private readonly SocketInput _socketInput;
        private readonly IConsumeSocketInput _consumer;
        private readonly IKestrelTrace _logger;
        private readonly long _connectionId;

        public LibuvSocketInputReader(SocketInput socketInput, UvStreamHandle socket, IConsumeSocketInput consumer, IKestrelTrace logger, long connectionId)
        {
            _socketInput = socketInput;
            _socket = socket;
            _consumer = consumer;
            _logger = logger;
            _connectionId = connectionId;
        }

        public void ReadStart()
        {
            _socket.ReadStart(_allocCallback, _readCallback, this);
        }

        private static Libuv.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((LibuvSocketInputReader)state).OnAlloc(handle, suggestedSize);
        }

        private Libuv.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            return handle.Libuv.buf_init(
                _socketInput.Pin(2048),
                2048);
        }
        private static void ReadCallback(UvStreamHandle handle, int nread, Exception error, object state)
        {
            ((LibuvSocketInputReader)state).OnRead(handle, nread, error);
        }

        private void OnRead(UvStreamHandle handle, int status, Exception error)
        {
            _socketInput.Unpin(status);

            var normalRead = error == null && status > 0;
            var normalDone = status == 0 || status == Constants.ECONNRESET || status == Constants.EOF;
            var errorDone = !(normalDone || normalRead);

            if (normalRead)
            {
                _logger.ConnectionRead(_connectionId, status);
            }
            else if (normalDone || errorDone)
            {
                _logger.ConnectionReadFin(_connectionId);
                _socketInput.RemoteIntakeFin = true;
                _socket.ReadStop();

                if (errorDone && error != null)
                {
                    _logger.LogError("LibuvSocketInputFiller.OnRead", error);
                }
                else
                {
                    _logger.ConnectionReadFin(_connectionId);
                }
            }

            _consumer.Consume();
        }
    }
}

