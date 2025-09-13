using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;
using System.Text.Json;

namespace HybridCast_ServerRelay.Models
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string HEADER_API_KEY_NAME = "x-api-key";
        private const string CONFIG_API_KEY_NAME = "HybridCast-ApiKey";
        private const string CONFIG_HYBRIDCAST_ID_NAME = "HybridCast-Id";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var appSettings = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            var apiKey = appSettings.GetValue<string>(CONFIG_API_KEY_NAME);
            var idKey = appSettings.GetValue<string>(CONFIG_HYBRIDCAST_ID_NAME);

            if (apiKey == null || idKey == null)
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 500,
                    Content = "API not configured correctly. Please contact administrators."
                };

                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(HEADER_API_KEY_NAME, out var headerApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key was not provided"
                };

                return;
            }

            string? headerApiKeyValue = headerApiKey.FirstOrDefault();
            if(headerApiKeyValue == null)
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key was empty or null"
                };

                return;
            }

            if(!apiKey.Equals(headerApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key is not valid"
                };

                return;
            }

            string json = Encoding.UTF8.GetString(System.Convert.FromBase64String(headerApiKeyValue)).Split("::EndJson::")[0];
            var apiKeyData = JsonSerializer.Deserialize<ApiKeyMetaData>(json);

            if(apiKeyData == null)
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key data is not valid"
                };

                return;
            }

            if(apiKeyData.Id != new Guid(idKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key data is not valid"
                };

                return;
            }

            await next();
        }
    }
}
