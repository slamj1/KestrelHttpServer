// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class SocketInputStream : Stream
    {
        private readonly SocketInput _socketInput;
        private readonly IConsumeSocketInput _consumer;

        public SocketInputStream(SocketInput socketInput, IConsumeSocketInput consumer)
        {
            _socketInput = socketInput;
            _consumer = consumer;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            // No-op
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ArraySegment<byte> inputBuffer = _socketInput.Available(count);

            Array.Copy(buffer, offset, inputBuffer.Array, inputBuffer.Offset, count);

            _socketInput.Extend(count);

            _consumer.Consume();
        }
    }
}
