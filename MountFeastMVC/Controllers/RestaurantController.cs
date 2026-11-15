using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MountFeastMVC.Controllers
{
    public class RestaurantController : Controller
    {
        [Route("~/restaurant-in-Murree", Name = "restaurantinMurree")]

        // GET: Restaurant
        public ActionResult Index()
        {
            return View();
        }
        [Route("~/islamabad-restaurant", Name = "restaurantinIslamabad")]
        public ActionResult IslamabadRestuarant()
        {
            return View();
        }
        [Route("~/restaurant", Name = "restaurantinNaran")]
        public ActionResult NaranRestuarant()
        {
            return View();
        }
        [Route("~/restaurant-in-skardu", Name = "restaurantinSkardu")]
        public ActionResult SkarduRestuarant()
        {
            return View();
        }
    }
}