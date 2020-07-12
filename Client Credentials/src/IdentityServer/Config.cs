// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;
using IdentityServer4;

namespace IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Address(),
                new IdentityResources.Email(),
                new IdentityResources.Profile(),
                new IdentityResources.Phone(),
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new[]
            {
                new ApiScope("api1", "My API"),
            };

        public static IEnumerable<ApiResource> ApiResources =>
            new[]
            {
                new ApiResource("api1", "my api1")
                {
                    Scopes = { "api1" }
                },
            };

        public static IEnumerable<Client> Clients =>
            new[]
            {
                new Client
                {
                    ClientId = "client",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    AllowedScopes = {"api1"}
                },
                // add mvc client
                new Client
                {
                    ClientId = "mvc client",
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    // 登录成功后调转地址
                    RedirectUris = { "https://localhost:5002/signin-oidc" },
                    FrontChannelLogoutUri = "https://localhost:5002/signout-oidc",
                    // 登出后调整地址
                    PostLogoutRedirectUris = { "https://localhost:5002/signout-callback-oidc" },
                    AllowedScopes =
                    {
                        "api1",
                        // 需要访问openid的资源时必须有OpenId
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Phone
                    },
                    // 允许刷新token的设定 offline_access
                    AllowOfflineAccess = true,
                    // token的有效时间，默认1小时;便与测试改动为1分钟
                    AccessTokenLifetime = 60,
                    // 运行身份信息中返回OpenId中的身份信息
                    AlwaysIncludeUserClaimsInIdToken = true
                },
            };
    }
}