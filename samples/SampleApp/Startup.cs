// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Filter;
#if DNX451
using Microsoft.AspNet.Server.Kestrel.Https;
#endif
using Microsoft.Framework.Logging;

namespace SampleApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Debug;

            loggerFactory.AddConsole(LogLevel.Debug);

            var serverInfo = app.ServerFeatures.Get<IKestrelServerInformation>();
            if (serverInfo != null)
            {
                //serverInfo.ThreadCount = 4;

#if DNX451
                serverInfo.ConnectionFilter = new HttpsConnectionFilter(new NoOpConnectionFilter());
                //serverInfo.ConnectionFilter = new NoOpConnectionFilter();
#endif
            }

            app.Run(context =>
            {
                //Console.WriteLine("{0} {1}{2}{3}",
                //    context.Request.Method,
                //    context.Request.PathBase,
                //    context.Request.Path,
                //    context.Request.QueryString);

                context.Response.ContentLength = 11;
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("Hello world");
            });
        }
    }
}
