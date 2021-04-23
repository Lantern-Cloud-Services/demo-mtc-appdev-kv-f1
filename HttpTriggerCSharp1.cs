using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;

namespace Company.Function
{
    public static class HttpTriggerCSharp1
    {
        [FunctionName("HttpTriggerCSharp1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Get Application settings
            var appParameter= "KeyVaultURI";
            string kvURI = System.Environment.GetEnvironmentVariable($"{appParameter}");

            log.LogInformation("C# HTTP trigger function processed a request.");

            string secretName = req.Query["secret"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            secretName = secretName ?? data?.secretName;

            if (!string.IsNullOrEmpty(secretName))
            {
                SecretClientOptions options = new SecretClientOptions()
                {
                    Retry =
                    {
                        Delay= TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(16),
                        MaxRetries = 2,
                        Mode = RetryMode.Exponential
                    }
                };

                var client = new SecretClient(new Uri(kvURI), new DefaultAzureCredential(),options);
                
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                string secretValue = secret.Value;

                return new OkObjectResult("Retrieved value: " + secretValue + " for secret: " + secretName);
            }

            return new OkObjectResult("Secret getter-o-matic");
        }
    }
}
