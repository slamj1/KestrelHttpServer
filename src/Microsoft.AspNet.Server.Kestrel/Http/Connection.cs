// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.Server.Kestrel.Filter;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class Connection : ConnectionContext, IConnectionControl, IConsumeSocketInput
    {
        private static long _lastConnectionId;

        private readonly UvStreamHandle _socket;
        private LibuvSocketInputReader _rawInputReader;
        private Frame _frame;
        private long _connectionId = 0;

        private readonly object _stateLock = new object();
        private ConnectionState _connectionState;

        public Connection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            ConnectionControl = this;

            _connectionId = Interlocked.Increment(ref _lastConnectionId);
        }

        public void Start()
        {
            Log.ConnectionStart(_connectionId);

            var rawSocketInput = new SocketInput(Memory);
            var rawSocketOutput = new SocketOutput(Thread, _socket, _connectionId, Log);

            if (ConnectionFilter != null)
            {
                var libuvStream = new LibuvStream(rawSocketInput, rawSocketOutput, this);
                _rawInputReader = new LibuvSocketInputReader(rawSocketInput, _socket, libuvStream, Log, _connectionId);
                _rawInputReader.ReadStart();

                var connectionFilterContext = new ConnectionFilterContext
                {
                    Address = null,
                    Connection = libuvStream,
                };

                ConnectionFilter.OnConnection(connectionFilterContext).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Log.LogError("ConnectionFilter.OnConnection Error", t.Exception);
                    }
                    else
                    {
                        SocketInput = new SocketInput(Memory);
                        SocketOutput = new StreamSocketOutput(connectionFilterContext.Connection);

                        _frame = new Frame(this);

                        var socketInputStream = new SocketInputStream(SocketInput, this);

                        connectionFilterContext.Connection.CopyToAsync(socketInputStream).ContinueWith(t2 =>
                        {
                            if (t2.Exception != null)
                            {
                                Log.LogError("Connection.CopyToAsync Error", t2.Exception);
                            }
                        });
                    }
                });
            }
            else
            {
                _rawInputReader = new LibuvSocketInputReader(rawSocketInput, _socket, this, Log, _connectionId);
                _rawInputReader.ReadStart();

                SocketInput = rawSocketInput;
                SocketOutput = rawSocketOutput;

                _frame = new Frame(this);
            }
        }

        public void Consume()
        {
            try
            {
                _frame.Consume();
            }
            catch (Exception ex)
            {
                Log.LogError("Connection._frame.Consume ", ex);
                throw;
            }
        }

        void IConnectionControl.Pause()
        {
            Log.ConnectionPause(_connectionId);
            _socket.ReadStop();
        }

        void IConnectionControl.Resume()
        {
            Log.ConnectionResume(_connectionId);
            _rawInputReader.ReadStart();
        }

        void IConnectionControl.End(ProduceEndType endType)
        {
            lock (_stateLock)
            {
                switch (endType)
                {
                    case ProduceEndType.SocketShutdownSend:
                        if (_connectionState != ConnectionState.Open)
                        {
                            return;
                        }
                        _connectionState = ConnectionState.Shutdown;

                        Log.ConnectionWriteFin(_connectionId);
                        Thread.Post(
                            state =>
                            {
                                var self = (Connection)state;
                                var shutdown = new UvShutdownReq(self.Log);
                                shutdown.Init(self.Thread.Loop);
                                shutdown.Shutdown(self._socket, (req, status, state2) =>
                                {
                                    var self2 = (Connection)state2;
                                    self2.Log.ConnectionWroteFin(_connectionId, status);
                                    req.Dispose();
                                }, this);
                            },
                            this);
                        break;
                    case ProduceEndType.ConnectionKeepAlive:
                        if (_connectionState != ConnectionState.Open)
                        {
                            return;
                        }

                        Log.ConnectionKeepAlive(_connectionId);
                        _frame = new Frame(this);
                        Thread.Post(
                            state => ((Frame)state).Consume(),
                            _frame);
                        break;
                    case ProduceEndType.SocketDisconnect:
                        if (_connectionState == ConnectionState.Disconnected)
                        {
                            return;
                        }
                        _connectionState = ConnectionState.Disconnected;

                        Log.ConnectionDisconnect(_connectionId);
                        Thread.Post(
                            state =>
                            {
                                Log.ConnectionStop(_connectionId);
                                ((UvHandle)state).Dispose();
                            },
                            _socket);
                        break;
                }
            }
        }

        private enum ConnectionState
        {
            Open,
            Shutdown,
            Disconnected
        }
    }
}
