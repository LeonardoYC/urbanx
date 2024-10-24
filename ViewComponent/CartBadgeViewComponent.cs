using Microsoft.AspNetCore.Mvc;
using urbanx.Service;

namespace urban_leo.ViewComponents
{
    public class CartBadgeViewComponent : ViewComponent
    {
        private readonly CartService _cartService;

        public CartBadgeViewComponent(CartService cartService)
        {
            _cartService = cartService;
        }

        public IViewComponentResult Invoke()
        {
            return View(_cartService.TotalItems);
        }
    }
}