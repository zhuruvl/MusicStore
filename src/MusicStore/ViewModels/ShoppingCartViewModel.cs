using System.Collections.Generic;
using MusicStore.Models;

namespace MusicStore.ViewModels
{
    public class ShoppingCartViewModel
    {
        public IEnumerable<CartItem> CartItems { get; set; }
        public decimal CartTotal { get; set; }
    }
}
