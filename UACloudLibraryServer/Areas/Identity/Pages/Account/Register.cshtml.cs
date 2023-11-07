// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Opc.Ua.Cloud.Library.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly Interfaces.ICaptchaValidation _captchaValidation;
        private readonly CaptchaSettings _captchaSettings;

        public bool AllowSelfRegistration { get; set; } = true;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IConfiguration configuration,
            Interfaces.ICaptchaValidation captchaValidation)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _configuration = configuration;
            _captchaValidation = captchaValidation;

            _captchaSettings = new CaptchaSettings();
            configuration.GetSection("CaptchaSettings").Bind(_captchaSettings);

            AllowSelfRegistration = configuration.GetValue<bool>(nameof(AllowSelfRegistration)) != false;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// Populate values for cshtml to use
        /// </summary>
        public CaptchaSettings CaptchaSettings { get { return _captchaSettings; } }

        /// <summary>
        /// Populate a token returned from client side call to Google Captcha
        /// </summary>
        [BindProperty]
        public string CaptchaResponseToken { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync().ConfigureAwait(false)).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (!AllowSelfRegistration)
            {
                return Page();
            }
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync().ConfigureAwait(false)).ToList();

            //Captcha validate
            var captchaResult = await _captchaValidation.ValidateCaptcha(CaptchaResponseToken);
            if (!string.IsNullOrEmpty(captchaResult)) ModelState.AddModelError("CaptchaResponseToken", captchaResult);

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None).ConfigureAwait(false);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None).ConfigureAwait(false);
                var result = await _userManager.CreateAsync(user, Input.Password).ConfigureAwait(false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    //notify registering user
                    StringBuilder sbBody = new StringBuilder();
                    sbBody.AppendLine("<h1>Welcome to the CESMII UA Cloud Library</h1>");
                    sbBody.AppendLine("<p>Thank you for creating an account on the CESMII UA Cloud Library. ");
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        sbBody.AppendLine($"<b>Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.</b></p>");
                    }
                    sbBody.AppendLine("<p>The CESMII UA Cloud Library is hosted by <a href='https://www.cesmii.org/'>CESMII</a>, the Clean Energy Smart Manufacturing Institute! This Cloud Library contains curated node sets created by CESMII or its members, as well as node sets from the <a href='https://uacloudlibrary.opcfoundation.org/'>OPC Foundation Cloud Library</a>.</p>");
                    sbBody.AppendLine("<p>Sincerely,<br />CESMII DevOps Team</p>");

                    await _emailSender.SendEmailAsync(Input.Email, "CESMII | Cloud Library | New Account Confirmation",
                        sbBody.ToString());

                    //notify CESMII dev ops as well
                    StringBuilder sbBody2 = new StringBuilder();
                    sbBody2.AppendLine("<h1>CESMII UA Cloud Library - New Account Sign Up</h1>");
                    sbBody2.AppendLine($"<p>User <b>'{Input.Email}'</b> created an account on the CESMII UA Cloud Library. ");
                    sbBody2.AppendLine("<p>The CESMII UA Cloud Library is hosted by <a href='https://www.cesmii.org/'>CESMII</a>, the Clean Energy Smart Manufacturing Institute! This Cloud Library contains curated node sets created by CESMII or its members, as well as node sets from the <a href='https://uacloudlibrary.opcfoundation.org/'>OPC Foundation Cloud Library</a>.</p>");
                    sbBody2.AppendLine("<p>Sincerely,<br />CESMII DevOps Team</p>");
                    await _emailSender.SendEmailAsync("devops@cesmii.org", "CESMII | Cloud Library | New Account Sign Up", sbBody2.ToString()).ConfigureAwait(false);

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
