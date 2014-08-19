using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace MusicStore.Models
{
    public partial class ShoppingCart
    {
        MusicStoreContext _db;
        string ShoppingCartId { get; set; }

        public ShoppingCart(MusicStoreContext db)
        {
            _db = db;
        }

        public static ShoppingCart GetCart(MusicStoreContext db, HttpContext context)
        {
            var cart = new ShoppingCart(db);
            cart.ShoppingCartId = cart.GetCartId(context);
            return cart;
        }

        public async Task AddToCart(Album album)
        {
            // Get the matching cart and album instances
            var cartItem = await _db.CartItems.SingleOrDefaultAsync(
                                    c => c.CartId == ShoppingCartId
                                    && c.AlbumId == album.AlbumId);

            if (cartItem == null)
            {
                // Create a new cart item if no cart item exists
                cartItem = new CartItem
                {
                    AlbumId = album.AlbumId,
                    CartId = ShoppingCartId,
                    Count = 1,
                    DateCreated = DateTime.Now
                };

                _db.CartItems.Add(cartItem);
            }
            else
            {
                // If the item does exist in the cart, then add one to the quantity
                cartItem.Count++;
            }
        }

        public async Task<int> RemoveFromCartAsync(int id)
        {
            // Get the cart
            var cartItem = await _db.CartItems.SingleAsync(
                                    cart => cart.CartId == ShoppingCartId
                                    && cart.CartItemId == id);

            int itemCount = 0;

            if (cartItem != null)
            {
                if (cartItem.Count > 1)
                {
                    cartItem.Count--;
                    itemCount = cartItem.Count;
                }
                else
                {
                    _db.CartItems.Remove(cartItem);
                }
            }

            return itemCount;
        }

        public void EmptyCart()
        {
            var cartItems = _db.CartItems.Where(cart => cart.CartId == ShoppingCartId);
            _db.CartItems.RemoveRange(cartItems);
        }

        public int GetCartItemCount()
        {
            int sum = 0;

            // Get the count of each item in the cart and sum them up
            var cartItemCounts = (from cartItems in _db.CartItems
                         where cartItems.CartId == ShoppingCartId
                         select cartItems.Count);

            foreach(var carItemCount in cartItemCounts)
            {
                sum += carItemCount;
            }

            // Return 0 if all entries are null
            return sum;
        }

        public IQueryable<CartItem> GetCartItems()
        {
            var query = from cartItem in _db.CartItems
                        join album in _db.Albums on cartItem.AlbumId equals album.AlbumId
                        where cartItem.CartId == ShoppingCartId
                        select new CartItem()
                        {
                            CartItemId = cartItem.CartItemId,
                            AlbumId = cartItem.AlbumId,
                            CartId = ShoppingCartId,
                            Count = cartItem.Count,
                            DateCreated = cartItem.DateCreated,
                            Album = new Album()
                            {
                                ArtistId = album.ArtistId,
                                AlbumArtUrl = album.AlbumArtUrl,
                                AlbumId = album.AlbumId,
                                GenreId = album.GenreId,
                                Price = album.Price,
                                Title = album.Title
                            }
                        };

            return query;
        }

        public decimal GetTotalPrice()
        {
            // Multiply album price by count of that album to get 
            // the current price for each of those albums in the cart
            // sum all album price totals to get the cart total

            // TODO Collapse to a single query once EF supports querying related data
            decimal total = 0;
            
            var subTotalsQuery = from cartItem in _db.CartItems
                                 join album in _db.Albums on cartItem.AlbumId equals album.AlbumId
                                 where cartItem.CartId == ShoppingCartId
                                 select cartItem.Count * album.Price;

            //TODO: workaround for the bug: https://github.com/aspnet/EntityFramework/issues/557
            foreach (var subTotal in subTotalsQuery)
            {
                total += subTotal;
            }

            return total;
        }

        public int CreateOrder(Order order)
        {
            decimal orderTotal = 0;

            var items = from ci in _db.CartItems
                        join al in _db.Albums
                        on ci.AlbumId equals al.AlbumId
                        select new { ci.Count, al.AlbumId, al.Price };

            // Iterate over the items in the cart, adding the order details for each
            foreach (var item in items)
            {
                var orderDetail = new OrderDetail
                {
                    AlbumId = item.AlbumId,
                    OrderId = order.OrderId,
                    UnitPrice = item.Price,
                    Quantity = item.Count,
                };

                // Set the order total of the shopping cart
                orderTotal += (item.Count * item.Price);

                _db.OrderDetails.Add(orderDetail);
            }

            // Set the order's total to the orderTotal count
            order.Total = orderTotal;

            // Empty the shopping cart
            EmptyCart();

            // Return the OrderId as the confirmation number
            return order.OrderId;
        }

        // We're using HttpContextBase to allow access to cookies.
        public string GetCartId(HttpContext context)
        {
            var sessionCookie = context.Request.Cookies.Get("Session");
            string cartId = null;

            if (string.IsNullOrWhiteSpace(sessionCookie))
            {
                //A GUID to hold the cartId. 
                cartId = Guid.NewGuid().ToString();

                // Send cart Id as a cookie to the client.
                context.Response.Cookies.Append("Session", cartId);
            }
            else
            {
                cartId = sessionCookie;
            }

            return cartId;
        }
    }
}