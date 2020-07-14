using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MVCClient.Models;

namespace MVCClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _factory;
        private readonly OpenIdConnectOptions _oidcOptions;

        public HomeController(ILogger<HomeController> logger,
            IHttpClientFactory factory,
            IOptions<OpenIdConnectOptions> options)
        {
            _logger = logger;
            _factory = factory;
            _oidcOptions = options?.Value ?? new OpenIdConnectOptions();
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Privacy()
        {
            // 获取相关数据
            var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            var idToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
            var refreshToekn = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
            ViewBag.AcccessToken = accessToken;
            ViewBag.IdToken = idToken;
            ViewBag.RefreshToken = refreshToekn;

            #region 请求API

            // 请求token成功后进行获取“资源”
            var api1Client = _factory.CreateClient("api1");
            api1Client.SetBearerToken(accessToken);
            api1Client.BaseAddress = new Uri("https://localhost:6001/");
            var identityResponse = await api1Client.GetAsync("identity");
            if (!identityResponse.IsSuccessStatusCode)
            {
                if (identityResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // TODO: 刷新token
                    await RenewTokensAsync();
                    return RedirectToAction();
                }

                throw new UnauthorizedAccessException(identityResponse.ReasonPhrase);
            }
            else
            {
                ViewBag.IdentityResponse = await identityResponse.Content.ReadAsStringAsync();
            }

            #endregion

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            // 清除本地cookie
            var signOut = await Task.FromResult<SignOutResult>(SignOut(
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme));
            return signOut;
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        /// <returns></returns>
        private async Task<string> RenewTokensAsync()
        {
            // TODO 1、发现端点
            var client = new HttpClient();
            var discoveryDocument = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
            // TODO 2、刷新Token
            var refreshToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
            var tokenResponse = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = discoveryDocument.TokenEndpoint,
                ClientSecret = "secret",
                ClientId = "mvc client",
                Scope = "api1 openid profile address email phone offline_access",
                // 疑惑：这里注入的IOptions<OpenIdConnectOptions>为什么是默认值，并不是配置后的？？？？
                // ClientSecret = _oidcOptions.ClientSecret,
                // ClientId = _oidcOptions.ClientId,
                // Scope = string.Join(" ", _oidcOptions.Scope),
                // 刷新Token这里得类型需要为RefreshToken
                GrantType = OpenIdConnectGrantTypes.RefreshToken,
                // 刷新Token需要携带RefreshToken,也就是当前Token的RefreshToken
                RefreshToken = refreshToken
            });
            // TODO 3、处理返回结果
            // Token过期时间
            var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResponse.ExpiresIn);
            var tokens = new[]
            {
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.IdToken,
                    Value = tokenResponse.IdentityToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = tokenResponse.AccessToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = tokenResponse.RefreshToken
                },
                new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = expiresAt.ToString("o", CultureInfo.InvariantCulture)
                },
            };
            // TODO 4、获取身份认证的结果，包含当前的pricipal和properties
            // 这里mvc使用cookie认证的，其他认证方式这里需要对应给Scheme
            var authenticateResult =
                await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // TODO 5、把新的tokens存起来
            authenticateResult.Properties.StoreTokens(tokens);
            // TODO 6、重新登录
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                authenticateResult.Principal, authenticateResult.Properties);
            // TODO 7、返回结果
            return tokenResponse.AccessToken;
        }
    }
}