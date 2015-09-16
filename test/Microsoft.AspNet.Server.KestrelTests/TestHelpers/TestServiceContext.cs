// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.KestrelTests.TestHelpers
{
    public class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            AppShutdown = new ShutdownNotImplemented();
            Memory = new MemoryPool();
            Log = new KestrelTrace(new TestLogger());
        }
    }
}
