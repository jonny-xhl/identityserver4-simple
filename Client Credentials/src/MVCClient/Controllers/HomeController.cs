using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MVCClient.Models;

namespace MVCClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _factory;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory factory)
        {
            _logger = logger;
            _factory = factory;
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
            if (identityResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                // TODO: 刷新token
                throw new UnauthorizedAccessException("Unauthorized");
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
    }
}