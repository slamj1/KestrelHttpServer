// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.AspNet.Server.Kestrel.Https
{
    public class TracingStream : Stream
    {
        private static int _lastStreamId = 0;

        private readonly Stream _innerStream;
        private readonly int _streamId;

        public TracingStream(Stream innerStream)
        {
            _innerStream = innerStream;
            _streamId = Interlocked.Increment(ref _lastStreamId);
        }

        public override bool CanRead
        {
            get
            {
                Console.WriteLine("CanRead");
                return _innerStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                Console.WriteLine("CanSeek");
                return _innerStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                Console.WriteLine("CanWrite");
                return _innerStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                Console.WriteLine("Length");
                return _innerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                Console.WriteLine("getPosition");
                return _innerStream.Position;
            }

            set
            {
                Console.WriteLine("setPosition");
                _innerStream.Position = value;
            }
        }

        public override void Flush()
        {
            Console.WriteLine("Flush()");
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Console.WriteLine($"{_streamId}: ReadStart({count})");
            var result = _innerStream.Read(buffer, offset, count);
            Console.WriteLine($"{_streamId}: ReadComplete({result})");
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Console.WriteLine("Seek()");
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Console.WriteLine("SetLength()");
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Console.WriteLine($"{_streamId}: WriteStart({count})");
            _innerStream.Write(buffer, offset, count);
            Console.WriteLine($"{_streamId}: WriteComplete()");
        }
    }
}
