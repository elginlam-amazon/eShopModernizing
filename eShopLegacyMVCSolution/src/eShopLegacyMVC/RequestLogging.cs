using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;

namespace eShopLegacyMVC
{
    public static class SessionKeys
    {
        public const string MachineName = "MachineName";
        public const string SessionStartTime = "SessionStartTime";
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
        private readonly string rawUrl;
        private readonly string userAgent;

        public WebRequestInfo(HttpRequest request)
        {
            rawUrl = request.PathBase + request.Path + request.QueryString;
            userAgent = request.Headers.UserAgent.ToString();
        }

        public override string ToString()
        {
            return rawUrl + ", " + userAgent;
        }
    }
}
