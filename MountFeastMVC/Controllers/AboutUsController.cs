using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MountFeastMVC.Controllers
{
    public class AboutUsController : Controller
    {
       [Route("~/online-hotel-booking-in-pakistan", Name = "MountFeast")]
        // GET: AboutUs
        public ActionResult Index()
        {
            return View();
        }

        [Route("~/apartments-in-islamabad", Name = "abouthotelIsb")]
        public ActionResult Islamabad()
        {
            return View();
        }

        [Route("~/apartments-in-murree", Name = "abouthotelKarachi")]
        public ActionResult Skardu()
        {
            return View();
        }

    }
}