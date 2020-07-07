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
            new ApiScope[]
            {
                new ApiScope("api1","My API"), 
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientId = "client",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    AllowedScopes = { "api1" }
                },
                // add mvc client
                new Client
                {
                    ClientId = "mvc client",
                    AllowedGrantTypes = GrantTypes.Code,
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    // 登录成功后调转地址
                    RedirectUris =
                    {
                        "https://localhost:5002/signin-oidc"
                    },
                    FrontChannelLogoutUri = "https://localhost:5002/signout-oidc",
                    // 登出后调整地址
                    PostLogoutRedirectUris =
                    {
                        "https://localhost:5002/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        // 表示也可以访问上面定义的ApiScope api1资源
                        "api1",
                        // 需要访问openid的资源时必须有OpenId
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Address,
                        IdentityServerConstants.StandardScopes.Phone,
                        // 没有offline_access scope时是没有resfresh_token的
                        IdentityServerConstants.StandardScopes.OfflineAccess
                    },
                    // 允许刷新token的设定 offline_access
                    AllowOfflineAccess = true,
                    // 运行身份信息中返回OpenId中的身份信息
                    AlwaysIncludeUserClaimsInIdToken = true
                }, 
            };
    }
}