using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MountFeastMVC.Controllers
{
    public class PlacesandActivitiesController : Controller
    {
        [Route("~/places-to-visit-in-Naran", Name = "placestovisitinNaran")]
        // GET: PopularActivities
        public ActionResult Index()
        {
            return View();
        }
        [Route("~/places-to-visit-in-skardu-and-popular-activities", Name = "placestovisitinSkardu")]
        // GET: Gallery
        public ActionResult Skardu()
        {
            return View();
        }
        [Route("~/places-to-visit-in-islamabad-and-popular-activities", Name = "placestovisitinIslamabad")]
        // GET: Gallery
        public ActionResult Islamabad()
        {
            return View();
        }
    }
}