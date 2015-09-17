// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Filter;

namespace Microsoft.AspNet.Server.Kestrel.Https
{
    public class HttpsConnectionFilter : IConnectionFilter
    {
        private readonly X509Certificate _cert;
        private readonly IConnectionFilter _next;

        public HttpsConnectionFilter(X509Certificate cert, IConnectionFilter next)
        {
            _cert = cert;
            _next = next;
        }

        public async Task OnConnection(ConnectionFilterContext context)
        {
            if (string.Equals(context.Address.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                var sslStream = new SslStream(new TracingStream(context.Connection));
                await sslStream.AuthenticateAsServerAsync(_cert);
                context.Connection = sslStream;
            }

            await _next.OnConnection(context);
        }
    }
}
