// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class StreamSocketOutput : ISocketOutput
    {
        private readonly Stream _outputStream;

        public StreamSocketOutput(Stream outputStream)
        {
            _outputStream = outputStream;
        }

        public void Write(ArraySegment<byte> buffer, Action<Exception, object> callback, object state, bool immediate = true)
        {

            // TODO: Use _outputStream.WriteAsync

            _outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
            callback(null, state);
        }
    }
}
