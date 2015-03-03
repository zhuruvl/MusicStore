using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Http.Core.Collections;
using Xunit;

namespace MusicStore.Models
{
    public class ShoppingCartTest
    {
        [Fact]
        public void GetCartId_ReturnsCartIdFromCookies()
        {
            // Arrange
            var cartId = "cartId_A";

            var httpContext = new DefaultHttpContext();
            httpContext.SetFeature<IRequestCookiesFeature>(new CookiesFeature("Session=" + cartId));

            var cart = new ShoppingCart(new MusicStoreContext());

            // Act
            var result = cart.GetCartId(httpContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cartId, result);
        }

        private class CookiesFeature : IRequestCookiesFeature
        {
            private RequestCookiesCollection cookies;

            public CookiesFeature(string cookiesHeader)
            {
                cookies = new RequestCookiesCollection();
                cookies.Reparse(cookiesHeader);
            }

            public IReadableStringCollection Cookies
            {
                get { return cookies; }
            }
        }
    }
}
