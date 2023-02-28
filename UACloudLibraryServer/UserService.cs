/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Cloud.Library.Interfaces;

    public class UserService : IUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public UserService(UserManager<IdentityUser> userManager, ILoggerFactory logger, IConfiguration config)
        {
            _userManager = userManager;
            _logger = logger.CreateLogger("UserService");
            _config = config;
        }

        public async Task<IEnumerable<Claim>> ValidateCredentialsAsync(string username, string password)
        {
            // check for admin
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                string passwordFromEnvironment = Environment.GetEnvironmentVariable("ServicePassword");
                if (string.IsNullOrEmpty(passwordFromEnvironment))
                {
                    passwordFromEnvironment = _config.GetValue<string>("ServicePassword");
                }
                if (string.IsNullOrEmpty(passwordFromEnvironment))
                {
                    _logger.LogError("ServicePassword env variable not set, please set it before trying to log in with admin credentials!");
                    return null;
                }
                if (!password.Equals(passwordFromEnvironment, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                List<Claim> claims = new() {
                            new Claim(ClaimTypes.Name, username)
                        };

                claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                return claims;
            }
            else
            {
                if (password == null)
                {
                    return null;
                }
                IdentityUser user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(username).ConfigureAwait(false);
                }

                if (user == null)
                {
                    return null;
                }
                PasswordVerificationResult result = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                if (result != PasswordVerificationResult.Success)
                {
                    return null;
                }
                List<Claim> claims = new();
                claims.Add(new Claim(ClaimTypes.Name, username));
                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
                claims.AddRange(await this._userManager.GetClaimsAsync(user));
                return claims;
            }
        }
    }
}
