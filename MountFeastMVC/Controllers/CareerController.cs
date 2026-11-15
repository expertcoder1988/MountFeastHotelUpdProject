using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MountFeastMVC.Controllers
{
    public class CareerController : Controller
    {
        [Route("~/hotel-jobs-in-pakistan", Name = "Career")]
        public ActionResult Index()
        {
            return View();
        }
    }
}