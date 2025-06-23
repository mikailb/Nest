using Microsoft.AspNetCore.Mvc;

namespace Instagram.Controllers
{
    public class HomeController : Controller
    {
        
        public IActionResult Index()
        {
            //When user is authenticated you get directed to home page.
            if (User?.Identity?.IsAuthenticated == true)
            {
              
                return Redirect("~/Picture/Home");
            }

            return View();
        }
    }
}
