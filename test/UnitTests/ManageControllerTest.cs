using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Http.Core.Security;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    public class ManageControllerTest
    {
        private readonly IServiceProvider _serviceProvider;
        public ManageControllerTest()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework()
                    .AddInMemoryStore()
                    .AddDbContext<MusicStoreContext>();

            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<MusicStoreContext>()
                    .AddDefaultTokenProviders()
                    .AddMessageProvider<EmailMessageProvider>()
                    .AddMessageProvider<SmsMessageProvider>();

            // UserManager and SignInManager need IHttpContextAccessor
            services.AddInstance<IHttpContextAccessor>(new TestHttpContextAcccessor());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Index_ReturnsViewBagMessagesExpected()
        {
            // Arrange
            var userId = "TestUserA";
            var phone = "abcdefg";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };

            var userManager = _serviceProvider.GetService<UserManager<ApplicationUser>>();
            await userManager.CreateAsync(
                new ApplicationUser
                    { Id = userId, UserName = "Test", TwoFactorEnabled = true, PhoneNumber = phone},
                "Pass@word1");

            var signInManager = _serviceProvider.GetService<SignInManager<ApplicationUser>>();

            var httpContext = _serviceProvider.GetRequiredService<IHttpContextAccessor>().Value;
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var controller = new ManageController()
            {
                ActionContext = new ActionContext(
                    httpContext,
                    new RouteData(),
                    new ActionDescriptor()),
                UserManager = userManager,
                SignInManager = signInManager,
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);

            var model = Assert.IsType<IndexViewModel>(viewResult.ViewData.Model);
            Assert.True(model.TwoFactor);
            Assert.Equal(phone, model.PhoneNumber);
            Assert.True(model.HasPassword);
        }

        private class TestHttpContextAcccessor : IHttpContextAccessor
        {
            private HttpContext _httpContext;

            public TestHttpContextAcccessor()
            {
                _httpContext = new TestHttpContext();
            }

            public HttpContext Value
            {
                get
                {
                    return _httpContext;
                }

                set
                {
                    _httpContext = value;
                }
            }
        }

        private class TestHttpContext : DefaultHttpContext
        {
            public override async Task<IEnumerable<AuthenticationResult>>
                AuthenticateAsync(IEnumerable<string> authenticationTypes)
            {
                return await Task.Run<IEnumerable<AuthenticationResult>>(
                    () =>
                     new AuthenticateContext(authenticationTypes).Results);
            }
        }
    }
}