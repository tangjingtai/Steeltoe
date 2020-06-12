﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class LoggersHandler : ActuatorHandler<LoggersEndpoint, Dictionary<string, object>, LoggersChangeRequest>
    {
        public LoggersHandler(LoggersEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<LoggersHandler> logger = null)
            : base(endpoint, securityServices, mgmtOptions, new List<HttpMethod> { HttpMethod.Get, HttpMethod.Post }, false, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public LoggersHandler(LoggersEndpoint endpoint, IEnumerable<ISecurityService> securityServices, ILogger<LoggersHandler> logger = null)
            : base(endpoint, securityServices, new List<HttpMethod> { HttpMethod.Get, HttpMethod.Post }, false, logger)
        {
        }

        public override void HandleRequest(HttpContextBase context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(LoggersEndpoint).Name);
            if (context.Request.HttpMethod == "GET")
            {
                // GET request
                var endpointResponse = _endpoint.Invoke(null);
                _logger?.LogTrace("Returning: {EndpointResponse}", endpointResponse);
                context.Response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
                context.Response.Write(Serialize(endpointResponse));
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                return;
            }
            else
            {
                // POST - change a logger level
                _logger?.LogDebug("Incoming logger path: {0}", context.Request.Path);

                var psPath = context.Request.Path;
                var epPath = _endpoint.Path;

                if (psPath.StartsWithSegments(epPath, _mgmtOptions.Select(p => p.Path), out var remaining) && !string.IsNullOrEmpty(remaining))
                {
                    var loggerName = remaining.TrimStart('/');

                    var change = ((LoggersEndpoint)_endpoint).DeserializeRequest(context.Request.InputStream);

                    change.TryGetValue("configuredLevel", out string level);

                    _logger?.LogDebug("Change Request: {Logger}, {Level}", loggerName, level ?? "RESET");
                    if (!string.IsNullOrEmpty(loggerName))
                    {
                        var changeReq = new LoggersChangeRequest(loggerName, level);
                        _endpoint.Invoke(changeReq);
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        return;
                    }
                }
            }

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
}