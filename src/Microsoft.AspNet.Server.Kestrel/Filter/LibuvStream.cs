// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class LibuvStream : Stream, IConsumeSocketInput
    {
        private readonly SocketInput _input;
        private readonly ISocketOutput _output;
        private readonly IMemoryPool _memory;

        private readonly TaskCompletionSource<object> _copyToTcs = new TaskCompletionSource<object>();
        private Stream _copyToStream;

        private readonly object _consmeLock = new object();

        public LibuvStream(SocketInput input, ISocketOutput output, ConnectionContext context)
        {
            _input = input;
            _output = output;
            _memory = context.Memory;
            _buffer = new ArraySegment<byte>(_memory.Empty);
        }

        public void Consume()
        {
            if (_input.RemoteIntakeFin)
            {
                IntakeFin(_input.Buffer.Count);
            }
            else
            {
                Intake(_input.Buffer.Count);
            }
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            // Flush is a lie.
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(new ArraySegment<byte>(buffer, offset, count)).Result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var segment = new ArraySegment<byte>(buffer, offset, count);
            _output.Write(segment, (state, ex) => { }, state: null);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            lock (_consmeLock)
            {
                _copyToStream = destination;
            }

            return _copyToTcs.Task;
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }

#if DNX451
        //public override void Close()
        //{
        //}

        //public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        //{
        //    throw new NotImplementedException();
        //}

        //public override int EndRead(IAsyncResult asyncResult)
        //{
        //    throw new NotImplementedException();
        //}
#endif

        // MessageBodyExchanger.cs
        private static readonly WaitCallback _completePending = CompletePending;

        private ArraySegment<byte> _buffer;
        private Queue<ReadOperation> _reads = new Queue<ReadOperation>();

        public bool LocalIntakeFin { get; set; }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(new ArraySegment<byte>(buffer, offset, count));
        }

        public Task<int> ReadAsync(ArraySegment<byte> buffer)
        {
            Task<int> result = null;
            while (result == null)
            {
                while (CompletePending())
                {
                    // earlier reads have priority
                }
                lock (_consmeLock)
                {
                    if (_buffer.Count != 0 || buffer.Count == 0 || LocalIntakeFin)
                    {
                        // there is data we can take right now
                        if (_reads.Count != 0)
                        {
                            // someone snuck in, try again
                            continue;
                        }

                        var count = Math.Min(buffer.Count, _buffer.Count);
                        Array.Copy(_buffer.Array, _buffer.Offset, buffer.Array, buffer.Offset, count);
                        _buffer = new ArraySegment<byte>(_buffer.Array, _buffer.Offset + count, _buffer.Count - count);
                        result = Task.FromResult(count);
                    }
                    else
                    {
                        // add ourselves to the line
                        var tcs = new TaskCompletionSource<int>();
                        _reads.Enqueue(new ReadOperation
                        {
                            Buffer = buffer,
                            CompletionSource = tcs,
                        });
                        result = tcs.Task;
                    }
                }
            }
            return result;
        }

        static void CompletePending(object state)
        {
            while (((LibuvStream)state).CompletePending())
            {
                // loop until none left
            }
        }

        bool CompletePending()
        {
            ReadOperation read;
            int count;
            lock (_consmeLock)
            {
                if (_buffer.Count == 0 && !LocalIntakeFin)
                {
                    return false;
                }

                if (_copyToStream != null)
                {
                    if (_buffer.Count == 0)
                    {
                        _copyToTcs.TrySetResult(null);
                    }
                    else
                    {
                        _copyToStream.Write(_buffer.Array, _buffer.Offset, _buffer.Count);
                        _buffer = new ArraySegment<byte>(_memory.Empty);
                    }

                    return false;
                }

                if (_reads.Count == 0)
                {
                    return false;
                }
                read = _reads.Dequeue();

                count = Math.Min(read.Buffer.Count, _buffer.Count);
                Array.Copy(_buffer.Array, _buffer.Offset, read.Buffer.Array, read.Buffer.Offset, count);
                _buffer = new ArraySegment<byte>(_buffer.Array, _buffer.Offset + count, _buffer.Count - count);
            }
            if (read.CompletionSource != null)
            {
                read.CompletionSource.SetResult(count);
            }
            return true;
        }

        public void Intake(int count)
        {
            Transfer(count, false);
        }

        public void IntakeFin(int count)
        {
            Transfer(count, true);
        }

        public void Transfer(int count, bool fin)
        {
            if (count == 0 && !fin)
            {
                return;
            }
            var input = _input;
            lock (_consmeLock)
            {
                // NOTE: this should not copy each time
                var oldBuffer = _buffer;
                var newData = _input.Take(count);

                var newBuffer = new ArraySegment<byte>(
                    _memory.AllocByte(oldBuffer.Count + newData.Count),
                    0,
                    oldBuffer.Count + newData.Count);

                Array.Copy(oldBuffer.Array, oldBuffer.Offset, newBuffer.Array, newBuffer.Offset, oldBuffer.Count);
                Array.Copy(newData.Array, newData.Offset, newBuffer.Array, newBuffer.Offset + oldBuffer.Count, newData.Count);

                _buffer = newBuffer;
                _memory.FreeByte(oldBuffer.Array);

                if (fin)
                {
                    LocalIntakeFin = true;
                }
                if (_reads.Count != 0 || _copyToStream != null)
                {
                    ThreadPool.QueueUserWorkItem(_completePending, this);
                }
            }
        }

        public struct ReadOperation
        {
            public TaskCompletionSource<int> CompletionSource;
            public ArraySegment<byte> Buffer;
        }
    }
}
