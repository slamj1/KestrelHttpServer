﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// Implementation of <see cref="Listener"/> that uses UNIX domain sockets as its transport.
    /// </summary>
    public class PipeListener : Listener
    {
        public PipeListener(ServiceContext serviceContext) : base(serviceContext)
        {
        }

        /// <summary>
        /// Creates the socket used to listen for incoming connections
        /// </summary>
        protected override UvStreamHandle CreateListenSocket(string host, int port)
        {
            var socket = new UvPipeHandle(Log);
            socket.Init(Thread.Loop, false);
            socket.Bind(host);
            socket.Listen(Constants.ListenBacklog, ConnectionCallback, this);
            return socket;
        }

        /// <summary>
        /// Handles an incoming connection
        /// </summary>
        /// <param name="listenSocket">Socket being used to listen on</param>
        /// <param name="status">Connection status</param>
        protected override void OnConnection(UvStreamHandle listenSocket, int status)
        {
            var acceptSocket = new UvPipeHandle(Log);
            acceptSocket.Init(Thread.Loop, false);

            try
            {
                listenSocket.Accept(acceptSocket);
            }
            catch (UvException ex)
            {
                Log.LogError("PipeListener.OnConnection", ex);
                return;
            }

            DispatchConnection(acceptSocket);
        }
    }
}
