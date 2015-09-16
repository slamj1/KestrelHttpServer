// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Filter;

namespace Microsoft.AspNet.Server.Kestrel.Https
{
    public class HttpsConnectionFilter : IConnectionFilter
    {
        private readonly IConnectionFilter _next;

        public HttpsConnectionFilter(IConnectionFilter next)
        {
            _next = next;
        }

        public async Task OnConnection(ConnectionFilterContext context)
        {
            var sslStream = new SslStream(new TracingStream(context.Connection));

            await sslStream.AuthenticateAsServerAsync(new X509Certificate2("testCert.cer"));

            context.Connection = sslStream;
            await _next.OnConnection(context);
        }
    }
}
