using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // client 需要使用identitymodel nuget包
            // 第一步：发现端点
            var client=new  HttpClient();
            var discoveryDocumentAsync = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
            if (discoveryDocumentAsync.IsError)
            {
                Console.WriteLine(discoveryDocumentAsync.Error);
                return;
            }
            // 第二步：获取access_token
            var  clientCredentialsTokenAsync =await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryDocumentAsync.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1"
            });
            if (clientCredentialsTokenAsync.IsError)
            {
                Console.WriteLine(clientCredentialsTokenAsync.Error);
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("获取token成功：{0}",clientCredentialsTokenAsync.Json);
            // 第三步：获取受保护资源
            var resourceClient = new HttpClient();
            // 携带token
            // resourceClient.SetBearerToken(clientCredentialsTokenAsync.AccessToken);
            resourceClient.SetToken("Bearer",clientCredentialsTokenAsync.AccessToken);
            var resourceResponseMessage = await client.GetAsync("https://localhost:6001/identity");
            if (!resourceResponseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine(resourceResponseMessage.StatusCode);
            }
            else
            {
                var resContent = await resourceResponseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(resContent);
            }

            Console.ReadKey();
        }
    }
}
