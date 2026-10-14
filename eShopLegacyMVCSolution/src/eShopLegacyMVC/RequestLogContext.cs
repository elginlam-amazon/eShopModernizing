using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Diagnostics;

namespace eShopLegacyMVC
{
    /// <summary>
    /// log4net property value that lazily returns the correlation activity id of the current request.
    /// </summary>
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

    /// <summary>
    /// log4net property value that renders the raw url and user agent of the current request.
    /// </summary>
    public class WebRequestInfo
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public WebRequestInfo(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public override string ToString()
        {
            var request = httpContextAccessor.HttpContext?.Request;
            return request?.GetEncodedPathAndQuery() + ", " + request?.Headers.UserAgent;
        }
    }
}
