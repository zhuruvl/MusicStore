using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    /// <summary>
    /// Summary description for ManageController
    /// </summary>
    [Authorize]
    public class ManageController : Controller
    {
        [Activate]
        public UserManager<ApplicationUser> UserManager { get; set; }

        [Activate]
        public SignInManager<ApplicationUser> SignInManager { get; set; }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(CancellationToken requestAborted, ManageMessageId? message = null)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var user = await GetCurrentUserAsync(requestAborted);
            var model = new IndexViewModel
            {
                HasPassword = await UserManager.HasPasswordAsync(user, cancellationToken: requestAborted),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(user, cancellationToken: requestAborted),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(user, cancellationToken: requestAborted),
                Logins = await UserManager.GetLoginsAsync(user, cancellationToken: requestAborted),
                BrowserRemembered = await SignInManager.IsTwoFactorClientRememberedAsync(user, cancellationToken: requestAborted)
            };

            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(CancellationToken requestAborted, string loginProvider, string providerKey)
        {
            ManageMessageId? message = ManageMessageId.Error;
            var user = await GetCurrentUserAsync(requestAborted);
            if (user != null)
            {
                var result = await UserManager.RemoveLoginAsync(user, loginProvider, providerKey, cancellationToken: requestAborted);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, cancellationToken: requestAborted);
                    message = ManageMessageId.RemoveLoginSuccess;
                }
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Account/AddPhoneNumber
        public IActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Account/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(CancellationToken requestAborted, AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync(requestAborted);
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(user, model.Number, cancellationToken: requestAborted);
            var message = new IdentityMessage
            {
                Destination = model.Number,
                Body = "Your security code is: " + code
            };
            await UserManager.SendMessageAsync("SMS", message, cancellationToken: requestAborted);

            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorAuthentication(CancellationToken requestAborted)
        {
            var user = await GetCurrentUserAsync(requestAborted);
            if (user != null)
            {
                await UserManager.SetTwoFactorEnabledAsync(user, true, cancellationToken: requestAborted);
                // TODO: flow remember me somehow?
                await SignInManager.SignInAsync(user, isPersistent: false, cancellationToken: requestAborted);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication(CancellationToken requestAborted)
        {
            var user = await GetCurrentUserAsync(requestAborted);
            if (user != null)
            {
                await UserManager.SetTwoFactorEnabledAsync(user, false, cancellationToken: requestAborted);
                await SignInManager.SignInAsync(user, isPersistent: false, cancellationToken: requestAborted);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Account/VerifyPhoneNumber
        public async Task<IActionResult> VerifyPhoneNumber(CancellationToken requestAborted, string phoneNumber)
        {
            // This code allows you exercise the flow without actually sending codes
            // For production use please register a SMS provider in IdentityConfig and generate a code here.
#if DEMO
            ViewBag.Code = await UserManager.GenerateChangePhoneNumberTokenAsync(await GetCurrentUserAsync(requestAborted), phoneNumber, cancellationToken: requestAborted);
#endif
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Account/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhoneNumber(CancellationToken requestAborted, VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync(requestAborted);
            if (user != null)
            {
                var result = await UserManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code, cancellationToken: requestAborted);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, cancellationToken: requestAborted);
                    return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
                }
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // GET: /Account/RemovePhoneNumber
        public async Task<IActionResult> RemovePhoneNumber(CancellationToken requestAborted)
        {
            var user = await GetCurrentUserAsync(requestAborted);
            if (user != null)
            {
                var result = await UserManager.SetPhoneNumberAsync(user, null, cancellationToken: requestAborted);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, cancellationToken: requestAborted);
                    return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
                }
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        //
        // GET: /Manage/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(CancellationToken requestAborted, ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync(requestAborted);
            if (user != null)
            {
                var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword, cancellationToken: requestAborted);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, cancellationToken: requestAborted);
                    return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
                }
                AddErrors(result);
                return View(model);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        //
        // GET: /Manage/SetPassword
        public IActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(CancellationToken requestAborted, SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await GetCurrentUserAsync(requestAborted);
            if (user != null)
            {
                var result = await UserManager.AddPasswordAsync(user, model.NewPassword, cancellationToken: requestAborted);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, cancellationToken: requestAborted);
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
                return View(model);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Manage/RememberBrowser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RememberBrowser(CancellationToken requestAborted)
        {
            var user = await GetCurrentUserAsync(requestAborted);
            if (user != null)
            {
                await SignInManager.RememberTwoFactorClientAsync(user, cancellationToken: requestAborted);
                await SignInManager.SignInAsync(user, isPersistent: false, cancellationToken: requestAborted);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/ForgetBrowser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgetBrowser()
        {
            await SignInManager.ForgetTwoFactorClientAsync();
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Account/Manage
        public async Task<IActionResult> ManageLogins(CancellationToken requestAborted, ManageMessageId? message = null)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.AddLoginSuccess ? "The external login was added."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await GetCurrentUserAsync(requestAborted);
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(user, cancellationToken: requestAborted);
            var otherLogins = SignInManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action("LinkLoginCallback", "Manage");
            var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, User.Identity.GetUserId());
            return new ChallengeResult(provider, properties);
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback(CancellationToken requestAborted)
        {
            var user = await GetCurrentUserAsync(requestAborted);
            if (user == null)
            {
                return View("Error");
            }

            var loginInfo = await SignInManager.GetExternalLoginInfoAsync(User.Identity.GetUserId(), cancellationToken: requestAborted);
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }

            var result = await UserManager.AddLoginAsync(user, loginInfo);
            var message = result.Succeeded ? ManageMessageId.AddLoginSuccess : ManageMessageId.Error;
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            AddLoginSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        private async Task<ApplicationUser> GetCurrentUserAsync(CancellationToken requestAborted)
        {
            return await UserManager.FindByIdAsync(Context.User.Identity.GetUserId(), cancellationToken: requestAborted);
        }

        #endregion
    }
}