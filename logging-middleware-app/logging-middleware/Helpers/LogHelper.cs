using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace logging_middleware.Helpers
{
    public static class LogHelper
    {
        public static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
        {
            var request = httpContext.Request;

            string responseBodyPayload = ReadResponseBodyAsync(httpContext.Response).GetAwaiter().GetResult();

            diagnosticContext.Set("Host", request.Host);
            diagnosticContext.Set("RequestQueryString", request.QueryString);

            diagnosticContext.Set("ResponseBody", responseBodyPayload);
        }

        private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
        {
            response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            string responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, System.IO.SeekOrigin.Begin);

            return responseBody;
        }
    }
}
