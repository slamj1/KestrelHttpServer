// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class StreamSocketInput : SocketInput
    {
        private Stream _inputStream;

        public StreamSocketInput(IMemoryPool memoryPool, Stream inputStream)
            : base(memoryPool)
        {
            _inputStream = inputStream;
        }
    }
}
