using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MountFeastMVC.Controllers
{
    public class LocationController : Controller
    {
        // GET: Location
        public ActionResult Index()
        {
            return View();
        }
        [Route("~/hotel-naran", Name = "hotelNaran")]
        public ActionResult Naran()
        {
            return View();
        }
        [Route("~/hotel-in-islamabad-pakistan", Name = "hotelIslamabad")]
        public ActionResult Islamabad()
        {
            return View();
        }
        [Route("~/hotel-in-skardu-gilgit-baltistan", Name = "hotelSkardu")]
        public ActionResult Skardu()
        {
            return View();
        }

    }
}