using MountFeastMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft;
using System.IO;
using System.Data.Entity;

namespace HotelWebsite.Controllers
{
    [Authorize]

    public class CompanyController : Controller
    {
        HotelDBEntities db = new HotelDBEntities();

        // GET: Company
        public ActionResult Index()
        {
            var company = db.Companies.FirstOrDefault();
            return View(company);
        }

        // POST: Company/Save
        [HttpPost]
        public JsonResult Save(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    db.Companies.Add(company);
                }
                else
                {
                    db.Entry(company).State = System.Data.EntityState.Modified;
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }


}
