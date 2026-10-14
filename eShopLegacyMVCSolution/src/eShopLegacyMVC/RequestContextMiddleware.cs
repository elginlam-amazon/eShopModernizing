using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace eShopLegacyMVC
{
    /// <summary>
    /// Replaces Global.asax Session_Start: track the machine name and the start time inside the current session.
    /// </summary>
    public class SessionStartMiddleware
    {
        public const string MachineNameKey = "MachineName";
        public const string SessionStartTimeKey = "SessionStartTime";

        private readonly RequestDelegate _next;

        public SessionStartMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var session = context.Session;
            if (session.GetString(MachineNameKey) == null)
            {
                session.SetString(MachineNameKey, Environment.MachineName);
                session.SetString(SessionStartTimeKey, DateTime.Now.ToString());
            }

            return _next(context);
        }
    }

    /// <summary>
    /// Replaces Global.asax Application_BeginRequest: sets the log4net context properties used by the log layout.
    /// </summary>
    public class RequestLogContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLogContextMiddleware> _log;

        public RequestLogContextMiddleware(RequestDelegate next, ILogger<RequestLogContextMiddleware> log)
        {
            _next = next;
            _log = log;
        }

        public Task InvokeAsync(HttpContext context)
        {
            //set the property to our new object
            LogicalThreadContext.Properties["activityid"] = new ActivityIdHelper();

            LogicalThreadContext.Properties["requestinfo"] = new WebRequestInfo(context);

            _log.LogDebug("WebApplication_BeginRequest");

            return _next(context);
        }
    }

    public class ActivityIdHelper
    {
        public override string ToString()
        {
            if (Trace.CorrelationManager.ActivityId == Guid.Empty)
            {
                Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            }

            return Trace.CorrelationManager.ActivityId.ToString();
        }
    }

    public class WebRequestInfo
    {
        private readonly string _requestInfo;

        public WebRequestInfo(HttpContext context)
        {
            // Captured eagerly so the value stays valid even if logged after the request completes.
            _requestInfo = context?.Request?.GetEncodedPathAndQuery() + ", " + context?.Request?.Headers.UserAgent;
        }

        public override string ToString()
        {
            return _requestInfo;
        }
    }
}
