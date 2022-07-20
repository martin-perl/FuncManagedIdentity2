using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Identity;
using Azure.Core;
using System.Collections.Generic;

namespace FuncManagedIdentity
{
    public static class GetCities
    {
        [FunctionName("GetCities")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            log.LogInformation($"Got name {name}.");

            string apimUrl = req.Query["apimUrl"];
            apimUrl = apimUrl ?? data?.apimUrl;
            log.LogInformation($"Got apimUrl {apimUrl}.");

            string userAssignedClientId = name;
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId });
            var accessToken = credential.GetToken(new TokenRequestContext(new[] { "https://management.azure.com/.default" }));
            // To print the token, you can convert it to string 
            String accessTokenString = accessToken.Token.ToString();

            List<City> cities = new List<City>();
            HttpClient client = new HttpClient();

            using (var requestMessage =
            new HttpRequestMessage(HttpMethod.Get, apimUrl))
            {
                requestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessTokenString);

                HttpResponseMessage response = await client.SendAsync(requestMessage);
                if (response.IsSuccessStatusCode)
                {
                    cities = await response.Content.ReadAsAsync<List<City>>();
                }
            }

            string responseMessage = string.IsNullOrEmpty(name)
                ? $"This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.\r\n{accessTokenString}\r\n{cities[0].ZipCode} {cities[0].Name}";

            return new OkObjectResult(responseMessage);
        }
    }
}
