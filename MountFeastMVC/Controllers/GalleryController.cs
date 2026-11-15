using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MountFeastMVC.Controllers
{
    public class GalleryController : Controller
    {
        [Route("~/gallery-hotel-naran", Name = "galleryhotelNaran")]
        // GET: Gallery
        public ActionResult Index()
        {
            return View();
        }
        [Route("~/skardu-hotel-gallery", Name = "galleryhotelSkardu")]
        // GET: Gallery
        public ActionResult Skardu()
        {
            return View();
        }
        [Route("~/islamabad-hotel-gallery", Name = "galleryhotelIslamabad")]
        // GET: Gallery
        public ActionResult Islamabad()
        {
            return View();
        }
    }
}