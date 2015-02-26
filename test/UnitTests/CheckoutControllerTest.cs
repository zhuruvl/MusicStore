using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Http.Core.Collections;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;
using MusicStore.Controllers;
using MusicStore.Models;

namespace UnitTests
{
    public class CheckoutControllerTest
    {
        IServiceProvider _serviceProvider;

        public CheckoutControllerTest()
        {
            var services = new ServiceCollection();

            services.AddEntityFramework()
                      .AddInMemoryStore()
                      .AddDbContext<MusicStoreContext>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void AddressAndPaymente_ReturnsDefaultView()
        {
            // Arrange
            var controller = new CheckoutController();

            // Act
            var result = controller.AddressAndPayment();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public void AddressAndPayment_ReturnsOrderIfInvalidPromoCode()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Form =
                new FormCollection(new Dictionary<string, string[]>());

            var controller = new CheckoutController()
            {
                ActionContext = new ActionContext(
                    context,
                    new RouteData(),
                    new ActionDescriptor()),
            };

            var order = new Order();

            // Act
            var result = controller.AddressAndPayment(order, new CancellationToken(false)).Result;

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(viewResult.ViewData);
            Assert.Same(order, viewResult.ViewData.Model);
        }

        [Fact]
        public void AddressAndPayment_ReturnsOrderIfRequestCanceled()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Form =
                new FormCollection(new Dictionary<string, string[]>());

            var controller = new CheckoutController()
            {
                ActionContext = new ActionContext(
                    context,
                    new RouteData(),
                    new ActionDescriptor()),
            };

            var order = new Order();

            // Act
            var result = controller.AddressAndPayment(order, new CancellationToken(true)).Result;

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(viewResult.ViewData);
            Assert.Same(order, viewResult.ViewData.Model);
        }

        [Fact]
        public void Complete_ReturnsOrderIdIfValid()
        {
            // Arrange
            var orderId = 100;
            var userName = "TestUserA";
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, userName) };

            var httpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims)),
            };

            var dbContext =
                _serviceProvider.GetRequiredService<MusicStoreContext>();
            dbContext.Add(new Order()
            {
                OrderId = orderId,
                Username = userName
            });
            dbContext.SaveChanges();

            var controller = new CheckoutController()
            {
                ActionContext = new ActionContext(
                    httpContext,
                    new RouteData(),
                    new ActionDescriptor()),
                DbContext = dbContext,
            };

            // Act
            var result = controller.Complete(orderId).Result;

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(viewResult.ViewData);
            Assert.Equal(orderId, viewResult.ViewData.Model);
        }

        [Fact]
        public void Complete_RetrunsErrorIfInvalidOrder()
        {
            // Arrange
            var dbContext =
                _serviceProvider.GetRequiredService<MusicStoreContext>();

            var controller = new CheckoutController()
            {
                ActionContext = new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ActionDescriptor()),
                DbContext = dbContext,
            };

            // Act
            var result = controller.Complete(100).Result;

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal("Error", viewResult.ViewName);
        }
    }
}