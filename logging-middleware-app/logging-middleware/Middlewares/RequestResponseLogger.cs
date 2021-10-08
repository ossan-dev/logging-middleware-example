using logging_middleware.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace logging_middleware.Middlewares
{
    public class RequestResponseLogger
    {
        private readonly RequestDelegate _next;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public RequestResponseLogger(RequestDelegate next)
        {
            _next = next;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // get request info
            var request = await GetRequestAsync(context);
            LogContext.PushProperty("RequestBody", request);

            // log request and response
            await LogRequestResponseAsync(context);
        }

        #region Private
        private async Task<string> GetRequestAsync(HttpContext context)
        {
            context.Request.EnableBuffering();

            await using var requestStream = _recyclableMemoryStreamManager.GetStream();
            await context.Request.Body.CopyToAsync(requestStream);

            context.Request.Body.Position = 0;

            var requestBody = StreamHelper.ReadStreamInChunks(requestStream);
            return requestBody;
        }

        private async Task LogRequestResponseAsync(HttpContext context)
        {
            using var originalBodyStream = context.Response.Body;

            try
            {
                using var responseBodyStream = _recyclableMemoryStreamManager.GetStream();

                context.Response.Body = responseBodyStream;

                // the request has reached the end, the response has been created and has reached back this point (before returning to the client)
                await _next(context);

                // we can change the actual response that will be returned to the client from here
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseTxt = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while processing the request");
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
        #endregion
    }
}
