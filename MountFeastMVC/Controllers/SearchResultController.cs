using MountFeastMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Globalization;
using System.Data.SqlClient;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Data.Entity;
using System.Web.Configuration;
using System.Drawing.Printing;
using System.Web.UI.WebControls;
using System.Data.Metadata.Edm;



namespace MountFeastMVC.Controllers
{
    public class SearchResultController : Controller
    {
        HotelDBEntities HDE = new HotelDBEntities();
        Booking bi = new Booking();


        private readonly HotelDBEntities _dbContext; // Declare _dbContext with the correct type

        public SearchResultController()
        {
            _dbContext = new HotelDBEntities(); // Initialize it in the constructor
        }

        // GET: SearchResult
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SearchResults(string CheckIn, string CheckOut, int Adults, int PropertyId, int Children, int NoOfRooms)
        {
            try
            {
                using (var db = new HotelDBEntities())
                {
                    int id = 1;

                    // Parsing the check-in and check-out dates
                    string DateString = CheckIn;
                    IFormatProvider culture = new CultureInfo("en-US", true);
                    DateTime datestart = DateTime.ParseExact(DateString, "MM/dd/yyyy", culture);

                    string DateString2 = CheckOut;
                    DateTime dateend = DateTime.ParseExact(DateString2, "MM/dd/yyyy", culture);

                    // Calculate the number of days between check-in and check-out
                    int NoOfDays = (dateend - datestart).Days;
                    ViewBag.NoOfDays = NoOfDays;

                    // Executing the stored procedure to get search results
                    IEnumerable<PropertySearch> rowsAffected = db.Database.SqlQuery<PropertySearch>(
                        "exec sp_GetSearchResults {0}, {1}, {2}, {3}, {4}, {5}",
                        datestart, dateend, Adults, PropertyId, Children, NoOfRooms).ToList();

                    // Adjusting room availability based on vacant and blocked rooms
                    foreach (var itm in rowsAffected)
                    {
                        int v = VacantRooms(itm.RoomCode, itm.RoomCateCode, CheckIn);
                        int b = BlockedRooms(itm.RoomCode, itm.RoomCateCode, CheckIn, CheckOut);
                        itm.NoOfRooms = itm.NoOfRooms - v - b;

                        if (itm.NoOfRooms < 0)
                        {
                            itm.NoOfRooms = 0;
                        }
                        itm.Id = id;
                        id++;
                    }

                    // Store the search results in the session for later use
                    Session["results"] = rowsAffected;
                    Session["PropertyId"] = PropertyId;
                    //var selectedProperty = rowsAffected.FirstOrDefault();
                    //if (selectedProperty != null)
                    //{
                    //    Session["AdultAllowed"] = selectedProperty.AdultAllowed;
                    //    Session["NoOfChildAllowed"] = selectedProperty.NoOfChildAllowed;
                    //}

                    // Load room pictures for the property
                    RoomcatePics();

                    return View(rowsAffected); // Passing results to the view
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public int VacantRooms(int? roomCode, int? RoomCateCode, string startDate)
        {
            try
            {
                int vacantRooms = 0;
                //int TotalRooms = 0;
                DateTime _startDate = Convert.ToDateTime(startDate);

                // string jsonString = "";
                // var responseBasicProfile = new ResponsePropertySearch();
                using (var db = new HotelDBEntities())
                {
                    //TotalRooms = db.RoomNumbers.Count(x => x.RoomCode == roomCode && x.Status == false);

                    vacantRooms = db.Bookings.Count(x => x.RoomCode == roomCode && x.CheckIn == _startDate);
                }

                return vacantRooms; // Content(vacantRooms.ToString());
            }
            catch (Exception ex)
            {
                return 0;// Content(ex.Message);
            }
        }

        public int BlockedRooms(int? roomCode, int? RoomCateCode, string startDate, string endDate)
        {
            try
            {
                // Parse the start and end dates
                DateTime _startDate = DateTime.ParseExact(startDate, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                DateTime _endDate = DateTime.ParseExact(endDate, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);

                int blockedRooms = 0;

                using (var db = new HotelDBEntities())
                {
                    // Count rooms that have overlapping blocked dates
                    blockedRooms = db.BlockedRooms.Count(x =>
                        x.CateId == RoomCateCode &&
                        x.Status == true &&
                        (
                            (x.StartDate <= _endDate && x.EndDate >= _startDate) // Overlapping check
                        ));
                }

                return blockedRooms;
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                return 0;
            }
        }

        public ActionResult RoomDetails(int Id)
        {
            try
            {
                //if (Session["params"] != null)
                //{
                //    string Params = (string)Session["params"];
                //    string[] p = Params.Split('&');
                //    DateTime checkin = Convert.ToDateTime(p[0]);
                //}
                RoomDetail(Id);
                // RoomPrice(Id, checkin);
                RoomPictures(Id);
                return View();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public JsonResult RoomPics(int Roomcate)
        {
            try
            {
                //int Id = requestRoom.Id; // HttpContext.Request.Form["Id"];
                //string jsonString = "";
                // var responseBasicProfile = new ResponsePropertySearch();
                using (var db = new HotelDBEntities())
                {
                    var room = (from a in db.RoomPictures
                                where (a.RoomCode == Roomcate)
                                select new
                                {
                                    ImageName = a.Picture
                                }).ToList();

                    ;
                    return Json(room, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        public string RoomcatePics()
        {
            try
            {
                //int Id = requestRoom.Id; // HttpContext.Request.Form["Id"];
                //string jsonString = "";
                // var responseBasicProfile = new ResponsePropertySearch();
                using (var db = new HotelDBEntities())
                {
                    var room = (from a in db.RoomPictures
                                where (a.Active == true)
                                select new RoomImages
                                {
                                    Category = a.RoomCode,
                                    ImageName = a.Picture
                                }).ToList();

                    TempData["Images"] = room;

                }

                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public string RoomDetail(int Id)
        {
            try
            {
                //int Id = requestRoom.Id; // HttpContext.Request.Form["Id"];
                //string jsonString = "";
                // var responseBasicProfile = new ResponsePropertySearch();
                using (var db = new HotelDBEntities())
                {
                    var room = (from a in db.Rooms
                                join b in db.RoomCategories
                                on a.RoomCategory equals b.Id
                                where (b.Id == Id)
                                select new RoomDetails
                                {
                                    Id = a.ID,
                                    RoomCode = a.RoomCode,
                                    RoomCategoryId = a.RoomCategory,
                                    RoomCategory = b.Category,
                                    RoomDescription = a.RoomDescription,
                                    PropertyId = a.propertyID,
                                    BedType = a.BedType,
                                    CheckIn = a.CheckIn,
                                    CheckOut = a.CheckOut,
                                    NoOfRooms = a.NoOfRooms,
                                    Capacity = a.Capacity,
                                    Adult = a.Adult,
                                    NoOfChild = a.NoOfChild,
                                    ChildAge = a.ChildAge,
                                    ShortCode = a.ShortCode,
                                    MinimumPrice = a.MinimumPrice,
                                    MinimumStay = a.MinimumStay,
                                    MaximumStay = a.MaximumStay,
                                    Size = a.Size,
                                    Features = a.Features
                                }).FirstOrDefault();

                    //  TempData["room"] = room;
                    ViewBag.room = room;
                }



                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string RoomPrice(int Id, DateTime checkin)
        {
            try
            {
                // int Id = requestRoom.Id;
                // DateTime checkin = requestRoom.Checkin;
                //  string jsonString = "";
                // var responseBasicProfile = new ResponsePropertySearch();
                using (var db = new HotelDBEntities())
                {
                    var roomPrice = (from a in db.RateSlabs
                                     where (a.RoomCategoryCode == Id && (a.StartDate < checkin && a.EndDate > checkin))
                                     select new
                                     {
                                         RoomPrice = a.ExtraAdultPrice
                                     }).FirstOrDefault();
                    TempData["roomPrice"] = roomPrice;
                }

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public JsonResult RoomPolicy(int CancellationPolicy)
        {
            try
            {
                using (var db = new HotelDBEntities())
                {
                    var room = (from a in db.RatePlans
                                join b in db.CancellationPolicies
                                        on a.CancellationPolicy equals b.Id
                                where (b.Id == CancellationPolicy && a.Status == true)
                                select new
                                {
                                    RatePlanName = a.Name,
                                    CancellationPolicy = b.Name,
                                    FreeCancelBefore = b.PeriodForFreeCharge,
                                    Breakfst = a.Breakfast,
                                    ChargedForNoShow = b.ChargedForNoShow,
                                    GuestChargedForCancellation = b.GuestChargedForCancellation,
                                    HowlongFreeOfCharge = b.HowlongFreeOfCharge
                                }).ToList();

                    ;
                    return Json(room, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        public string RoomPictures(int Id)
        {
            try
            {
                using (var db = new HotelDBEntities())
                {
                    // Fetching active room pictures based on RoomCategory
                    var roomPictures = (from a in db.RoomPictures
                                        join b in db.Rooms
                                        on a.RoomCode equals b.RoomCategory
                                        where b.RoomCategory == Id && a.Active == true  // Ensure pictures are active
                                        select new Pictures
                                        {
                                            Picture = a.Picture
                                        }).ToList();

                    TempData["roomPictures"] = roomPictures;  // Store the pictures in TempData for use in the view
                }

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;  // Return exception message in case of an error
            }
        }


        public ActionResult PaymentError(string pp_ResponseCode, string pp_ResponseMessage)
        {
            try
            {
                TempData["pp_ResponseMessage"] = pp_ResponseMessage;
                TempData["pp_ResponseCode"] = pp_ResponseCode;
                return View();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // For Online Payment Booking Thank You Page Code
        public ActionResult Thankyou(string basket_id, string err_code, string err_msg, decimal Total = 0)
        {
            try
            {
                string confirmationId = "";
                string pp_ResponseCode = err_code;
                string pp_ResponseMessage = err_msg;

                if (Request.Form["basket_id"] != null)
                {
                    confirmationId = Request.Form["basket_id"];
                }
                else
                {
                    confirmationId = basket_id;
                }

                if (pp_ResponseCode == "00" || pp_ResponseCode == "000")
                {
                    var _booking = (from obj in HDE.Bookings
                                    join b in HDE.RoomCategories
                                    on obj.RoomCatCode equals b.Id
                                    where (obj.ConfirmationNo == confirmationId)
                                    select new FinalBooking
                                    {
                                        BookingID = obj.BookingID,
                                        PaxTitle = obj.PaxTitle,
                                        PaxFName = obj.PaxFName,
                                        PaxLName = obj.PaxLName,
                                        NoOfRooms = obj.NoOfRooms,
                                        RoomCode = obj.RoomCode,
                                        RoomCatCode = obj.RoomCatCode,
                                        RoomCategory = b.Category,
                                        RoomNum = obj.RoomNum,
                                        PaxEmail = obj.PaxEmail,
                                        PaxPhoneNumber = obj.PaxPhoneNumber,
                                        PaxDescription = obj.PaxDescription,
                                        Adults = obj.Adults,
                                        Children = obj.Children,
                                        CheckIn = obj.CheckIn,
                                        CheckOut = obj.CheckOut,
                                        RoomRate = obj.RoomRate,
                                        HotelCode = obj.HotelCode,
                                        ConfirmationNo = obj.ConfirmationNo,
                                        NoofDay = obj.NoofDay,
                                        RoomTax = obj.RoomTax,
                                        TotalRoomRate = obj.TotalRoomRate,
                                        VoucherAmount = obj.VoucherAmount,
                                        ReservationDate = obj.ReservationDate,
                                        PayableAmount = (obj.TotalRoomRate ?? 0) - (obj.VoucherAmount ?? 0)
                                    }).ToList();

                    Session["booking"] = _booking;
                    Session["confirmationId"] = confirmationId;

                    using (var db = new HotelDBEntities())
                    {
                        var myData = db.Bookings.Where(x => x.ConfirmationNo == confirmationId).ToList();
                        myData.ForEach(m => m.BookingStatus = "Booked");
                        db.SaveChanges();
                    }


                    decimal discountAmount = 0;
                    decimal taxAmount = 0;
                    int adultsAllowed = 0;
                    int childrenAllowed = 0;
                    int VoucherAmount = 0;
                    VoucherAmount = _booking[0].VoucherAmount ?? 0;

                    int propertyId = _booking[0].HotelCode ?? 0;
                    int roomCategoryCode = _booking[0].RoomCatCode ?? 0;
                    // Convert RoomCatCodes to integer (Room Category ID)
                    int roomCategoryId = Convert.ToInt32(roomCategoryCode);
                    string roomImagePath = "";
                    // Calculate number of days
                    TimeSpan t = Convert.ToDateTime(_booking[0].CheckOut) - Convert.ToDateTime(_booking[0].CheckIn);
                    int noOfDay = (int)t.TotalDays;
                    if (noOfDay < 1) noOfDay = 1;

                    using (var db = new HotelDBEntities())
                    {
                        //// Get Discount from RateSlab
                        //var rateSlab = db.RateSlabs.FirstOrDefault(r => r.PropertyId == propertyId && r.RoomCategoryCode == roomCategoryCode && r.Status == true);
                        //if (rateSlab != null)
                        //{
                        //    discountAmount = (rateSlab.Discount ?? 0) * noOfDay; // Multiply per-day discount by number of days
                        //}

                        discountAmount = _booking[0].TotalRoomRate ?? 0;

                        // Get Tax Percentage and calculate tax on total discount
                        var tax = db.Taxes.FirstOrDefault(x => x.PropertyId == propertyId && x.RoomCategory == roomCategoryCode && x.Status == true);
                        if (tax != null)
                        {
                            decimal taxPercentage = tax.Amount ?? 0;
                            taxAmount = (discountAmount * taxPercentage) / 100;
                        }
                        // ✅ Get Adults and NoOfChild allowed from Room table
                        var roomInfo = db.Rooms
                            .Where(r => r.propertyID == propertyId && r.RoomCategory == roomCategoryCode && r.Status == true)
                            .Select(r => new { r.Adult, r.NoOfChild })
                            .FirstOrDefault();
                        adultsAllowed = roomInfo?.Adult ?? 0;
                        childrenAllowed = roomInfo?.NoOfChild ?? 0;

                        // Get top 1 active image with PicNum == 1 for the selected room category
                        var roomImageName = db.RoomPictures.Where(x => x.RoomCode == roomCategoryId && x.Active == true)
                                            .OrderBy(x => x.PicNum) // You can change this to any relevant field for ordering
                                            .Select(x => x.Picture)
                                            .FirstOrDefault();

                        // Convert image name to a web-accessible path
                        roomImagePath = roomImageName != null ? Url.Content("~/uploads/" + roomImageName) : Url.Content("~/uploads/no-image.jpg");
                    }



                    // Store the image path in ViewBag (single image only)
                    ViewBag.RoomImage = roomImagePath;

                    // Optional: store list of image paths in session if needed for other purposes
                    List<string> roomImagePathList = GetRoomImagePaths(roomCategoryId);
                    Session["RoomImagePaths"] = roomImagePathList;


                    // ✅ Assign to ViewBag to use in view
                    ViewBag.AdultsAllowed = adultsAllowed;
                    ViewBag.ChildrenAllowed = childrenAllowed;
                    ViewBag.VoucherAmount = VoucherAmount;

                    // Pass to View
                    ViewBag.DiscountAmount = discountAmount - taxAmount;
                    ViewBag.TaxAmount = taxAmount;
                    ViewBag.NoOfDays = noOfDay; // Optional, if you want to display number of days
                    var category = _booking[0].RoomCategory;

                    try
                    {
                        int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                        StringBuilder htmlString = new StringBuilder();
                        htmlString.Append("<html lang=\"en\">");
                        htmlString.Append("<head>");
                        htmlString.Append("<meta charset=\"UTF-8\">");
                        htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                        htmlString.Append("<title>Booking Confirmation</title>");
                        htmlString.Append("<style>");
                        htmlString.Append("body {");
                        htmlString.Append("font-family: Arial, sans-serif;");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("padding: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".email-container {");
                        htmlString.Append("width: 100%;");
                        htmlString.Append("max-width: 650px;");
                        htmlString.Append("margin: 0 auto;");
                        htmlString.Append("background-color: #ffffff;");
                        htmlString.Append("border-radius: 8px;");
                        htmlString.Append("overflow: hidden;");
                        htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                        htmlString.Append("}");
                        htmlString.Append(".header {");
                        htmlString.Append("background-color: #f5f5f5;");
                        htmlString.Append("color: white;");
                        htmlString.Append("}");
                        htmlString.Append(".header h1 {");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".content {");
                        htmlString.Append("padding: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".content h2 {");
                        htmlString.Append("color: #333;");
                        htmlString.Append("}");
                        htmlString.Append(".details {");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("background-color: #f9f9f9;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("}");
                        htmlString.Append(".details p {");
                        htmlString.Append("margin: 5px 0;");
                        htmlString.Append("}");
                        htmlString.Append(".footer {");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("text-align: center;");
                        htmlString.Append("font-size: 12px;");
                        htmlString.Append("color: #777;");
                        htmlString.Append("}");
                        htmlString.Append(".footer a {");
                        htmlString.Append("color: #0066cc;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("}");
                        htmlString.Append(".button {");
                        htmlString.Append("background-color: #0066cc;");
                        htmlString.Append("color: white;");
                        htmlString.Append("padding: 10px 20px;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("display: inline-block;");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 20px;");
                        htmlString.Append("}");
                        htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                        htmlString.Append("@media (max-width: 768px) {");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                        htmlString.Append("}");
                        htmlString.Append("}");
                        htmlString.Append("</style>");

                        htmlString.Append("</head>");

                        htmlString.Append("<body>");
                        htmlString.Append("<div class=\"email-container\">");


                        htmlString.Append("<div class=\"header\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td class=\"responsive-padding\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:22px 0 0 0\"><a  style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                        htmlString.Append("<img alt=\"sintra\" height=\"60\" src=\"https://mountfeasthotel.com/Content/Assets/img/logos/logo.png\" style=\"display:block\" width=\"144\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                        htmlString.Append("</a></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#000\">");
                        htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"24\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");

                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td align=\"center\" width=\"100%\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                        htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                        htmlString.Append(" Thank you for your Booking, <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                        htmlString.Append("Your booking in Mount Feast Hotel is confirmed.");
                        htmlString.Append("</h1>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Booking Details</td>");

                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"16\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding: 0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-in");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(from 15:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-out");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(until 12:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Your reservation");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("You booked for");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + TotalGuests + " Adults" + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Booking Date");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<address style=\"font-style:normal\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToShortDateString() + "");
                        htmlString.Append("</address>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Phone");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Email");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxEmail + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Room");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].RoomCategory + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"32\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                        htmlString.Append("Price details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"16\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                        htmlString.Append(_booking[0].RoomCategory);
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("5% KPRA Tax.");
                        //htmlString.Append("" + _booking[0].RoomTax + " GST is included.");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        //confirmation of booking


                        //This is the bank details
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("Total price");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");




                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                        htmlString.Append("Important Details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is 2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                        htmlString.Append("</ul>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");
                        htmlString.Append("</body>");

                        htmlString.Append("</html>");

                        MailMessage mm = new MailMessage();
                        mm.IsBodyHtml = true;
                        mm.To.Add("info@mountfeasthotel.com");

                        mm.From = new MailAddress("info@mountfeasthotel.com");
                        mm.Subject = "Order Confirmation: " + confirmationId;
                        mm.Body = htmlString.ToString();

                        SmtpClient smc = new SmtpClient();
                        smc.Host = "smtp.gmail.com";
                        // smc.Host = "smtp.gmail.com";
                        smc.Port = 587;
                        smc.EnableSsl = true;
                        smc.UseDefaultCredentials = false;
                        smc.Credentials = new System.Net.NetworkCredential("info@mountfeasthotel.com", "xhhusxiqfemxcpan");
                        smc.DeliveryMethod = SmtpDeliveryMethod.Network;

                        smc.Send(mm);


                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                    }

                    if (_booking[0].PaxEmail != "")
                    {
                        try
                        {
                            int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                            StringBuilder htmlString = new StringBuilder();
                            htmlString.Append("<html lang=\"en\">");
                            htmlString.Append("<head>");
                            htmlString.Append("<meta charset=\"UTF-8\">");
                            htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                            htmlString.Append("<title>Booking Confirmation</title>");
                            htmlString.Append("<style>");
                            htmlString.Append("body {");
                            htmlString.Append("font-family: Arial, sans-serif;");
                            htmlString.Append("background-color: #f4f4f4;");
                            htmlString.Append("margin: 0;");
                            htmlString.Append("padding: 0;");
                            htmlString.Append("}");
                            htmlString.Append(".email-container {");
                            htmlString.Append("width: 100%;");
                            htmlString.Append("max-width: 650px;");
                            htmlString.Append("margin: 0 auto;");
                            htmlString.Append("background-color: #ffffff;");
                            htmlString.Append("border-radius: 8px;");
                            htmlString.Append("overflow: hidden;");
                            htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                            htmlString.Append("}");
                            htmlString.Append(".header {");
                            htmlString.Append("background-color: #f5f5f5;");
                            htmlString.Append("color: white;");
                            htmlString.Append("}");
                            htmlString.Append(".header h1 {");
                            htmlString.Append("margin: 0;");
                            htmlString.Append("}");
                            htmlString.Append(".content {");
                            htmlString.Append("padding: 20px;");
                            htmlString.Append("}");
                            htmlString.Append(".content h2 {");
                            htmlString.Append("color: #333;");
                            htmlString.Append("}");
                            htmlString.Append(".details {");
                            htmlString.Append("margin-top: 20px;");
                            htmlString.Append("background-color: #f9f9f9;");
                            htmlString.Append("padding: 15px;");
                            htmlString.Append("border-radius: 5px;");
                            htmlString.Append("}");
                            htmlString.Append(".details p {");
                            htmlString.Append("margin: 5px 0;");
                            htmlString.Append("}");
                            htmlString.Append(".footer {");
                            htmlString.Append("background-color: #f4f4f4;");
                            htmlString.Append("padding: 15px;");
                            htmlString.Append("text-align: center;");
                            htmlString.Append("font-size: 12px;");
                            htmlString.Append("color: #777;");
                            htmlString.Append("}");
                            htmlString.Append(".footer a {");
                            htmlString.Append("color: #0066cc;");
                            htmlString.Append("text-decoration: none;");
                            htmlString.Append("}");
                            htmlString.Append(".button {");
                            htmlString.Append("background-color: #0066cc;");
                            htmlString.Append("color: white;");
                            htmlString.Append("padding: 10px 20px;");
                            htmlString.Append("text-decoration: none;");
                            htmlString.Append("border-radius: 5px;");
                            htmlString.Append("display: inline-block;");
                            htmlString.Append("margin-top: 20px;");
                            htmlString.Append("}");
                            htmlString.Append(".responsive-padding {");
                            htmlString.Append("padding: 0px 20px;");
                            htmlString.Append("}");
                            htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                            htmlString.Append("@media (max-width: 768px) {");
                            htmlString.Append(".responsive-padding {");
                            htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                            htmlString.Append("}");
                            htmlString.Append("}");
                            htmlString.Append("</style>");

                            htmlString.Append("</head>");

                            htmlString.Append("<body>");
                            htmlString.Append("<div class=\"email-container\">");


                            htmlString.Append("<div class=\"header\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"center\">");
                            htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td class=\"responsive-padding\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:22px 0 0 0\"><a  style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                            htmlString.Append("<img alt=\"sintra\" height=\"60\" src=\"https://mountfeasthotel.com/Content/Assets/img/logos/logo.png\" style=\"display:block\" width=\"164\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                            htmlString.Append("</a></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#000\">");
                            htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"24\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</div>");

                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td align=\"center\" width=\"100%\">");
                            htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"20\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"center\">");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                            htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                            htmlString.Append("Thank you for your Booking, <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                            htmlString.Append("Your booking in Mount Feast Hotel is confirmed.");
                            htmlString.Append("</h1>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"20\">");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Booking Details</td>");

                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td height=\"16\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding: 0 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                            htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                            htmlString.Append("<tbody>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Check-in");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                            //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                            //htmlString.Append("(from 15:00)");
                            //htmlString.Append("</span>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Check-out");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                            //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                            //htmlString.Append("(until 12:00)");
                            //htmlString.Append("</span>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Your reservation");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("You booked for");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + TotalGuests + " Adults" + "");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Booking Date");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<address style=\"font-style:normal\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToShortDateString() + "");
                            htmlString.Append("</address>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Phone");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                            htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Email");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                            htmlString.Append("" + _booking[0].PaxEmail + "");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Room");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + _booking[0].RoomCategory + "");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td height=\"32\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                            htmlString.Append("Price details");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"16\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:0 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                            htmlString.Append(_booking[0].RoomCategory);
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                            htmlString.Append("5% KPRA Tax.");
                            //htmlString.Append("" + _booking[0].RoomTax + " GST is included.");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            //Conformation Booking


                            //this is bank details

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                            htmlString.Append("Total price");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"32\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");




                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:0px 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                            htmlString.Append("Important Details");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                            htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is 2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                            htmlString.Append("</ul>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"32\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</div>");
                            htmlString.Append("</body>");

                            htmlString.Append("</html>");

                            MailMessage mm = new MailMessage();
                            mm.IsBodyHtml = true;
                            mm.To.Add(_booking[0].PaxEmail);

                            mm.From = new MailAddress("info@mountfeasthotel.com");
                            mm.Subject = "Order Confirmation: " + confirmationId;
                            mm.Body = htmlString.ToString();
                            SmtpClient smc = new SmtpClient();
                            smc.Host = "smtp.gmail.com";
                            smc.Port = 587;
                            smc.UseDefaultCredentials = false;
                            smc.EnableSsl = true;
                            smc.Credentials = new System.Net.NetworkCredential("info@mountfeasthotel.com", "xhhusxiqfemxcpan");
                            smc.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smc.Send(mm);


                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                        }

                    }

                    return View();
                }
                else
                {
                    return RedirectToAction("PaymentError", "SearchResult", new { @pp_ResponseCode = pp_ResponseCode, @pp_ResponseMessage = pp_ResponseMessage });
                }



            }
            catch (Exception ex)
            {

                return Json(new { status = "error", message = ex.ToString(), JsonRequestBehavior.AllowGet });
            }
        }

        // For Offline Reservation Thank You Page Code
        public ActionResult ThankyouReservation(string basket_id, string err_code, string err_msg, decimal Total = 0)
        {
            try
            {
                string confirmationId = "";
                string pp_ResponseCode = err_code;
                string pp_ResponseMessage = err_msg;

                if (Request.Form["basket_id"] != null)
                {
                    confirmationId = Request.Form["basket_id"];
                }
                else
                {
                    confirmationId = basket_id;
                }

                if (pp_ResponseCode == "00" || pp_ResponseCode == "000")
                {
                    var _booking = (from obj in HDE.Bookings
                                    join b in HDE.RoomCategories
                                    on obj.RoomCatCode equals b.Id
                                    where (obj.ConfirmationNo == confirmationId)
                                    select new FinalBooking
                                    {
                                        BookingID = obj.BookingID,
                                        PaxTitle = obj.PaxTitle,
                                        PaxFName = obj.PaxFName,
                                        PaxLName = obj.PaxLName,
                                        NoOfRooms = obj.NoOfRooms,
                                        RoomCode = obj.RoomCode,
                                        RoomCatCode = obj.RoomCatCode,
                                        RoomCategory = b.Category,
                                        RoomNum = obj.RoomNum,
                                        PaxEmail = obj.PaxEmail,
                                        PaxPhoneNumber = obj.PaxPhoneNumber,
                                        PaxDescription = obj.PaxDescription,
                                        Adults = obj.Adults,
                                        Children = obj.Children,
                                        CheckIn = obj.CheckIn,
                                        CheckOut = obj.CheckOut,
                                        RoomRate = obj.RoomRate,
                                        HotelCode = obj.HotelCode,
                                        ConfirmationNo = obj.ConfirmationNo,
                                        NoofDay = obj.NoofDay,
                                        RoomTax = obj.RoomTax,
                                        TotalRoomRate = obj.TotalRoomRate,
                                        VoucherAmount = obj.VoucherAmount,
                                        ReservationDate = obj.ReservationDate,
                                        PayableAmount = (obj.TotalRoomRate ?? 0) - (obj.VoucherAmount ?? 0)
                                    }).ToList();

                    Session["booking"] = _booking;
                    Session["confirmationId"] = confirmationId;

                    using (var db = new HotelDBEntities())
                    {
                        var myData = db.Bookings.Where(x => x.ConfirmationNo == confirmationId).ToList();
                        myData.ForEach(m => m.BookingStatus = "Pending");
                        db.SaveChanges();
                    }

                    decimal discountAmount = 0;
                    decimal taxAmount = 0;
                    int adultsAllowed = 0;
                    int childrenAllowed = 0;
                    int VoucherAmount = 0;
                    VoucherAmount = _booking[0].VoucherAmount ?? 0;

                    int propertyId = _booking[0].HotelCode ?? 0;
                    int roomCategoryCode = _booking[0].RoomCatCode ?? 0;
                    // Convert RoomCatCodes to integer (Room Category ID)
                    int roomCategoryId = Convert.ToInt32(roomCategoryCode);
                    string roomImagePath = "";
                    // Calculate number of days
                    TimeSpan t = Convert.ToDateTime(_booking[0].CheckOut) - Convert.ToDateTime(_booking[0].CheckIn);
                    int noOfDay = (int)t.TotalDays;
                    if (noOfDay < 1) noOfDay = 1;

                    using (var db = new HotelDBEntities())
                    {

                        discountAmount = _booking[0].TotalRoomRate ?? 0;

                        // Get Tax Percentage and calculate tax on total discount
                        var tax = db.Taxes.FirstOrDefault(x => x.PropertyId == propertyId && x.RoomCategory == roomCategoryCode && x.Status == true);
                        if (tax != null)
                        {
                            decimal taxPercentage = tax.Amount ?? 0;
                            taxAmount = (discountAmount * taxPercentage) / 100;
                        }
                        // ✅ Get Adults and NoOfChild allowed from Room table
                        var roomInfo = db.Rooms
                            .Where(r => r.propertyID == propertyId && r.RoomCategory == roomCategoryCode && r.Status == true)
                            .Select(r => new { r.Adult, r.NoOfChild })
                            .FirstOrDefault();
                        adultsAllowed = roomInfo?.Adult ?? 0;
                        childrenAllowed = roomInfo?.NoOfChild ?? 0;

                        // Get top 1 active image with PicNum == 1 for the selected room category
                        var roomImageName = db.RoomPictures.Where(x => x.RoomCode == roomCategoryId && x.Active == true)
                                            .OrderBy(x => x.PicNum) // You can change this to any relevant field for ordering
                                            .Select(x => x.Picture)
                                            .FirstOrDefault();

                        // Convert image name to a web-accessible path
                        roomImagePath = roomImageName != null ? Url.Content("~/uploads/" + roomImageName) : Url.Content("~/uploads/no-image.jpg");
                    }



                    // Store the image path in ViewBag (single image only)
                    ViewBag.RoomImage = roomImagePath;

                    // Optional: store list of image paths in session if needed for other purposes
                    List<string> roomImagePathList = GetRoomImagePaths(roomCategoryId);
                    Session["RoomImagePaths"] = roomImagePathList;


                    // ✅ Assign to ViewBag to use in view
                    ViewBag.AdultsAllowed = adultsAllowed;
                    ViewBag.ChildrenAllowed = childrenAllowed;
                    ViewBag.VoucherAmount = VoucherAmount;

                    // Pass to View
                    ViewBag.DiscountAmount = discountAmount - taxAmount;
                    ViewBag.TaxAmount = taxAmount;
                    ViewBag.NoOfDays = noOfDay; // Optional, if you want to display number of days
                    var category = _booking[0].RoomCategory;

                    try
                    {
                        int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                        StringBuilder htmlString = new StringBuilder();
                        htmlString.Append("<html lang=\"en\">");
                        htmlString.Append("<head>");
                        htmlString.Append("<meta charset=\"UTF-8\">");
                        htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                        htmlString.Append("<title>Booking Confirmation</title>");
                        htmlString.Append("<style>");
                        htmlString.Append("body {");
                        htmlString.Append("font-family: Arial, sans-serif;");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("padding: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".email-container {");
                        htmlString.Append("width: 100%;");
                        htmlString.Append("max-width: 650px;");
                        htmlString.Append("margin: 0 auto;");
                        htmlString.Append("background-color: #ffffff;");
                        htmlString.Append("border-radius: 8px;");
                        htmlString.Append("overflow: hidden;");
                        htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                        htmlString.Append("}");
                        htmlString.Append(".header {");
                        htmlString.Append("background-color: #f5f5f5;");
                        htmlString.Append("color: white;");
                        htmlString.Append("}");
                        htmlString.Append(".header h1 {");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".content {");
                        htmlString.Append("padding: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".content h2 {");
                        htmlString.Append("color: #333;");
                        htmlString.Append("}");
                        htmlString.Append(".details {");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("background-color: #f9f9f9;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("}");
                        htmlString.Append(".details p {");
                        htmlString.Append("margin: 5px 0;");
                        htmlString.Append("}");
                        htmlString.Append(".footer {");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("text-align: center;");
                        htmlString.Append("font-size: 12px;");
                        htmlString.Append("color: #777;");
                        htmlString.Append("}");
                        htmlString.Append(".footer a {");
                        htmlString.Append("color: #0066cc;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("}");
                        htmlString.Append(".button {");
                        htmlString.Append("background-color: #0066cc;");
                        htmlString.Append("color: white;");
                        htmlString.Append("padding: 10px 20px;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("display: inline-block;");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 20px;");
                        htmlString.Append("}");
                        htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                        htmlString.Append("@media (max-width: 768px) {");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                        htmlString.Append("}");
                        htmlString.Append("}");
                        htmlString.Append("</style>");

                        htmlString.Append("</head>");

                        htmlString.Append("<body>");
                        htmlString.Append("<div class=\"email-container\">");


                        htmlString.Append("<div class=\"header\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td class=\"responsive-padding\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:22px 0 0 0\"><a  style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                        htmlString.Append("<img alt=\"sintra\" height=\"60\" src=\"https://mountfeasthotel.com/Content/Assets/img/logos/logo.png\" style=\"display:block\" width=\"144\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                        htmlString.Append("</a></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#000\">");
                        htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"24\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");

                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td align=\"center\" width=\"100%\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                        htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                        htmlString.Append("Thanks <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                        htmlString.Append("Your Reservation in Mount Feast Hotel is confirmed.");
                        htmlString.Append("</h1>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Reservation details</td>");

                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"16\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding: 0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-in");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(from 15:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-out");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(until 12:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Your reservation");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("You booked for");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + TotalGuests + " Adults" + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Booking Date");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<address style=\"font-style:normal\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToShortDateString() + "");
                        htmlString.Append("</address>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Phone");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Email");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxEmail + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Room");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].RoomCategory + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"32\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                        htmlString.Append("Price details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"16\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                        htmlString.Append(_booking[0].RoomCategory);
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("5% KPRA Tax.");
                        //htmlString.Append("" + _booking[0].RoomTax + " GST is included.");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("To confirm your booking please make a payment of 50% of the total amount within 1 hour and share a screenshot via WhatsApp ");
                        htmlString.Append("<a href=\"https://wa.me/+923111786446? text=Hi! MountFeast Hotel.\" target=\"_blank\">");
                        htmlString.Append("<b>+923111786446</b>");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        //confirmation of booking

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Account Name");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("MOUNT FEAST HOTEL");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Bank Name");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("HABIB BANK LIMITED PAKISTAN (H.B.L)");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Account Number");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("23017902291403");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("IBAN");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("PK12 HABB0023017902291 403");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        //This is the bank details
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("Total price");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");




                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                        htmlString.Append("Important Details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is 2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                        htmlString.Append("</ul>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");
                        htmlString.Append("</body>");

                        htmlString.Append("</html>");

                        MailMessage mm = new MailMessage();
                        mm.IsBodyHtml = true;
                        mm.To.Add("info@mountfeasthotel.com");

                        mm.From = new MailAddress("info@mountfeasthotel.com");
                        mm.Subject = "Order Confirmation: " + confirmationId;
                        mm.Body = htmlString.ToString();

                        SmtpClient smc = new SmtpClient();
                        smc.Host = "smtp.gmail.com";
                        // smc.Host = "smtp.gmail.com";
                        smc.Port = 587;
                        smc.EnableSsl = true;
                        smc.UseDefaultCredentials = false;
                        smc.Credentials = new System.Net.NetworkCredential("info@mountfeasthotel.com", "xhhusxiqfemxcpan");
                        smc.DeliveryMethod = SmtpDeliveryMethod.Network;

                        smc.Send(mm);


                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                    }

                    if (_booking[0].PaxEmail != "")
                    {
                        try
                        {
                            int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                            StringBuilder htmlString = new StringBuilder();
                            htmlString.Append("<html lang=\"en\">");
                            htmlString.Append("<head>");
                            htmlString.Append("<meta charset=\"UTF-8\">");
                            htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                            htmlString.Append("<title>Booking Confirmation</title>");
                            htmlString.Append("<style>");
                            htmlString.Append("body {");
                            htmlString.Append("font-family: Arial, sans-serif;");
                            htmlString.Append("background-color: #f4f4f4;");
                            htmlString.Append("margin: 0;");
                            htmlString.Append("padding: 0;");
                            htmlString.Append("}");
                            htmlString.Append(".email-container {");
                            htmlString.Append("width: 100%;");
                            htmlString.Append("max-width: 650px;");
                            htmlString.Append("margin: 0 auto;");
                            htmlString.Append("background-color: #ffffff;");
                            htmlString.Append("border-radius: 8px;");
                            htmlString.Append("overflow: hidden;");
                            htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                            htmlString.Append("}");
                            htmlString.Append(".header {");
                            htmlString.Append("background-color: #f5f5f5;");
                            htmlString.Append("color: white;");
                            htmlString.Append("}");
                            htmlString.Append(".header h1 {");
                            htmlString.Append("margin: 0;");
                            htmlString.Append("}");
                            htmlString.Append(".content {");
                            htmlString.Append("padding: 20px;");
                            htmlString.Append("}");
                            htmlString.Append(".content h2 {");
                            htmlString.Append("color: #333;");
                            htmlString.Append("}");
                            htmlString.Append(".details {");
                            htmlString.Append("margin-top: 20px;");
                            htmlString.Append("background-color: #f9f9f9;");
                            htmlString.Append("padding: 15px;");
                            htmlString.Append("border-radius: 5px;");
                            htmlString.Append("}");
                            htmlString.Append(".details p {");
                            htmlString.Append("margin: 5px 0;");
                            htmlString.Append("}");
                            htmlString.Append(".footer {");
                            htmlString.Append("background-color: #f4f4f4;");
                            htmlString.Append("padding: 15px;");
                            htmlString.Append("text-align: center;");
                            htmlString.Append("font-size: 12px;");
                            htmlString.Append("color: #777;");
                            htmlString.Append("}");
                            htmlString.Append(".footer a {");
                            htmlString.Append("color: #0066cc;");
                            htmlString.Append("text-decoration: none;");
                            htmlString.Append("}");
                            htmlString.Append(".button {");
                            htmlString.Append("background-color: #0066cc;");
                            htmlString.Append("color: white;");
                            htmlString.Append("padding: 10px 20px;");
                            htmlString.Append("text-decoration: none;");
                            htmlString.Append("border-radius: 5px;");
                            htmlString.Append("display: inline-block;");
                            htmlString.Append("margin-top: 20px;");
                            htmlString.Append("}");
                            htmlString.Append(".responsive-padding {");
                            htmlString.Append("padding: 0px 20px;");
                            htmlString.Append("}");
                            htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                            htmlString.Append("@media (max-width: 768px) {");
                            htmlString.Append(".responsive-padding {");
                            htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                            htmlString.Append("}");
                            htmlString.Append("}");
                            htmlString.Append("</style>");

                            htmlString.Append("</head>");

                            htmlString.Append("<body>");
                            htmlString.Append("<div class=\"email-container\">");


                            htmlString.Append("<div class=\"header\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"center\">");
                            htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td class=\"responsive-padding\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:22px 0 0 0\"><a  style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                            htmlString.Append("<img alt=\"sintra\" height=\"60\" src=\"https://mountfeasthotel.com/Content/Assets/img/logos/logo.png\" style=\"display:block\" width=\"164\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                            htmlString.Append("</a></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#000\">");
                            htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"24\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</div>");

                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td align=\"center\" width=\"100%\">");
                            htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"20\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"center\">");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                            htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                            htmlString.Append("Thanks <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                            htmlString.Append("Your Reservation in Mount Feast Hotel is confirmed.");
                            htmlString.Append("</h1>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"20\">");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Reservation details</td>");

                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td height=\"16\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding: 0 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                            htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                            htmlString.Append("<tbody>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Check-in");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                            //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                            //htmlString.Append("(from 15:00)");
                            //htmlString.Append("</span>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Check-out");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                            //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                            //htmlString.Append("(until 12:00)");
                            //htmlString.Append("</span>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Your reservation");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("You booked for");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + TotalGuests + " Adults" + "");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Booking Date");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<address style=\"font-style:normal\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToShortDateString() + "");
                            htmlString.Append("</address>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Phone");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                            htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Email");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                            htmlString.Append("" + _booking[0].PaxEmail + "");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Room");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + _booking[0].RoomCategory + "");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td height=\"32\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                            htmlString.Append("Price details");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"16\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:0 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                            htmlString.Append(_booking[0].RoomCategory);
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                            htmlString.Append("5% KPRA Tax.");
                            //htmlString.Append("" + _booking[0].RoomTax + " GST is included.");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                            htmlString.Append("To confirm your booking please make a payment of 50% of the total amount within 1 hour and share a screenshot via WhatsApp ");
                            htmlString.Append("<a href=\"https://wa.me/+923111786446? text=Hi! MountFeast Hotel.\" target=\"_blank\">");
                            htmlString.Append("<b>+923111786446</b>");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            //Conformation Booking

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Account Name");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("MOUNT FEAST HOTEL");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Bank Name");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("HABIB BANK LIMITED PAKISTAN (H.B.L)");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Account Number");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("23017902291403");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("IBAN");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("PK12 HABB0023017902291 403");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            //this is bank details

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                            htmlString.Append("Total price");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"32\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");




                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:0px 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                            htmlString.Append("Important Details");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                            htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is 2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                            htmlString.Append("</ul>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"32\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</div>");
                            htmlString.Append("</body>");

                            htmlString.Append("</html>");

                            MailMessage mm = new MailMessage();
                            mm.IsBodyHtml = true;
                            mm.To.Add(_booking[0].PaxEmail);

                            mm.From = new MailAddress("info@mountfeasthotel.com");
                            mm.Subject = "Order Confirmation: " + confirmationId;
                            mm.Body = htmlString.ToString();
                            SmtpClient smc = new SmtpClient();
                            smc.Host = "smtp.gmail.com";
                            smc.Port = 587;
                            smc.UseDefaultCredentials = false;
                            smc.EnableSsl = true;
                            smc.Credentials = new System.Net.NetworkCredential("info@mountfeasthotel.com", "xhhusxiqfemxcpan");
                            smc.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smc.Send(mm);


                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                        }

                    }

                    return View();
                }
                else
                {
                    return RedirectToAction("PaymentError", "SearchResult", new { @pp_ResponseCode = pp_ResponseCode, @pp_ResponseMessage = pp_ResponseMessage });
                }



            }
            catch (Exception ex)
            {

                return Json(new { status = "error", message = ex.ToString(), JsonRequestBehavior.AllowGet });
            }
        }

        [HttpPost]
        public ActionResult Thankyou(FormCollection frm, string billReference)
        {
            try
            {
                string confirmationId = "";
                string pp_ResponseCode = Request.Form["pp_ResponseCode"];
                string pp_ResponseMessage = Request.Form["pp_ResponseMessage"];

                if (Request.Form["pp_BillReference"] != null)
                {
                    confirmationId = Request.Form["pp_BillReference"];
                }
                else
                {
                    confirmationId = billReference;
                }

                if (pp_ResponseCode == "000" || pp_ResponseCode == null)
                {
                    var _booking = (from obj in HDE.Bookings
                                    join b in HDE.RoomCategories
                                     on obj.RoomCatCode equals b.Id
                                    where (obj.ConfirmationNo == confirmationId)
                                    select new FinalBooking
                                    {
                                        BookingID = obj.BookingID,
                                        PaxTitle = obj.PaxTitle,
                                        PaxFName = obj.PaxFName,
                                        PaxLName = obj.PaxLName,
                                        NoOfRooms = obj.NoOfRooms,
                                        RoomCode = obj.RoomCode,
                                        RoomCatCode = obj.RoomCatCode,
                                        RoomCategory = b.Category,
                                        RoomNum = obj.RoomNum,
                                        PaxEmail = obj.PaxEmail,
                                        PaxPhoneNumber = obj.PaxPhoneNumber,
                                        PaxDescription = obj.PaxDescription,
                                        Adults = obj.Adults,
                                        Children = obj.Children,
                                        CheckIn = obj.CheckIn,
                                        CheckOut = obj.CheckOut,
                                        RoomRate = obj.RoomRate,
                                        HotelCode = obj.HotelCode,
                                        ConfirmationNo = obj.ConfirmationNo,
                                        NoofDay = obj.NoofDay,
                                        TotalRoomRate = obj.TotalRoomRate
                                    }).ToList();

                    //RoomCates = RoomCates.Remove(0, 1);
                    //TempData["RoomCates"] = RoomCates;
                    //TempData["noOfDay"] = noOfDay;
                    Session["booking"] = _booking;
                    Session["confirmationId"] = confirmationId;

                    using (var db = new HotelDBEntities())
                    {
                        var myData = db.Bookings.Where(x => x.ConfirmationNo == confirmationId).ToList();
                        myData.ForEach(m => m.BookingStatus = "Booked");
                        db.SaveChanges();
                    }


                    TimeSpan t = Convert.ToDateTime(_booking[0].CheckOut) - Convert.ToDateTime(_booking[0].CheckIn);
                    int noOfDay = (int)t.TotalDays;

                    try
                    {
                        int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                        StringBuilder htmlString = new StringBuilder();
                        htmlString.Append("<html lang=\"en\">");
                        htmlString.Append("<head>");
                        htmlString.Append("<meta charset=\"UTF-8\">");
                        htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                        htmlString.Append("<title>Booking Confirmation</title>");
                        htmlString.Append("<style>");
                        htmlString.Append("body {");
                        htmlString.Append("font-family: Arial, sans-serif;");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("padding: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".email-container {");
                        htmlString.Append("width: 100%;");
                        htmlString.Append("max-width: 650px;");
                        htmlString.Append("margin: 0 auto;");
                        htmlString.Append("background-color: #ffffff;");
                        htmlString.Append("border-radius: 8px;");
                        htmlString.Append("overflow: hidden;");
                        htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                        htmlString.Append("}");
                        htmlString.Append(".header {");
                        htmlString.Append("background-color: #f5f5f5;");
                        htmlString.Append("color: white;");
                        htmlString.Append("}");
                        htmlString.Append(".header h1 {");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".content {");
                        htmlString.Append("padding: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".content h2 {");
                        htmlString.Append("color: #333;");
                        htmlString.Append("}");
                        htmlString.Append(".details {");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("background-color: #f9f9f9;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("}");
                        htmlString.Append(".details p {");
                        htmlString.Append("margin: 5px 0;");
                        htmlString.Append("}");
                        htmlString.Append(".footer {");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("text-align: center;");
                        htmlString.Append("font-size: 12px;");
                        htmlString.Append("color: #777;");
                        htmlString.Append("}");
                        htmlString.Append(".footer a {");
                        htmlString.Append("color: #0066cc;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("}");
                        htmlString.Append(".button {");
                        htmlString.Append("background-color: #0066cc;");
                        htmlString.Append("color: white;");
                        htmlString.Append("padding: 10px 20px;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("display: inline-block;");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 20px;");
                        htmlString.Append("}");
                        htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                        htmlString.Append("@media (max-width: 768px) {");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                        htmlString.Append("}");
                        htmlString.Append("}");
                        htmlString.Append("</style>");

                        htmlString.Append("</head>");

                        htmlString.Append("<body>");
                        htmlString.Append("<div class=\"email-container\">");


                        htmlString.Append("<div class=\"header\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td class=\"responsive-padding\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:22px 0 0 0\"><a  style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                        htmlString.Append("<img alt=\"sintra\" height=\"60\" src=\"https://mountfeasthotel.com/Content/Assets/img/logos/logo.png\" style=\"display:block\" width=\"144\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                        htmlString.Append("</a></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#000\">");
                        htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"24\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");

                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td align=\"center\" width=\"100%\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                        htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                        htmlString.Append("Thanks <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                        htmlString.Append("Your booking in Mount Feast Hotel is confirmed.");
                        htmlString.Append("</h1>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Reservation details</td>");

                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"16\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding: 0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-in");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(from 15:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-out");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(until 12:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Your reservation");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("You booked for");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + TotalGuests + " Adults" + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Booking Date");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<address style=\"font-style:normal\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToShortDateString() + "");
                        htmlString.Append("</address>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Phone");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Email");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxEmail + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Room");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].RoomCategory + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"32\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                        htmlString.Append("Price details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"16\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                        htmlString.Append(_booking[0].RoomCategory);
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("5% KPRA Tax.");
                        //htmlString.Append("" + _booking[0].RoomTax + " GST is included.");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("To confirm your booking please make a payment of 50% of the total amount within 1 hour and share a screenshot via WhatsApp ");
                        htmlString.Append("<a href=\"https://wa.me/+923111786446? text=Hi! MountFeast Hotel.\" target=\"_blank\">");
                        htmlString.Append("<b>+923111786446</b>");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        //Confirmation booking

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Account Name");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("MOUNT FEAST HOTEL");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Bank Name");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("HABIB BANK LIMITED PAKISTAN (H.B.L)");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Account Number");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("23017902291403");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("IBAN");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("PK12 HABB0023017902291 403");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        //this is bank details






                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("Total price");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");




                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                        htmlString.Append("Important Details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is 2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                        htmlString.Append("</ul>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");
                        htmlString.Append("</body>");

                        htmlString.Append("</html>");

                        MailMessage mm = new MailMessage();
                        mm.IsBodyHtml = true;
                        mm.To.Add("info@mountfeasthotel.com");

                        mm.From = new MailAddress("info@mountfeasthotel.com");
                        mm.Subject = "Order Confirmation: " + confirmationId;
                        mm.Body = htmlString.ToString();
                        SmtpClient smc = new SmtpClient();
                        smc.Host = "smtp.gmail.com";
                        smc.Port = 587;
                        smc.UseDefaultCredentials = false;
                        smc.EnableSsl = true;
                        smc.Credentials = new System.Net.NetworkCredential("info@mountfeasthotel.com", "xhhusxiqfemxcpan");
                        smc.DeliveryMethod = SmtpDeliveryMethod.Network;

                        smc.Send(mm);


                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                    }

                    if (_booking[0].PaxEmail != "")
                    {
                        try
                        {
                            int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                            StringBuilder htmlString = new StringBuilder();
                            htmlString.Append("<html lang=\"en\">");
                            htmlString.Append("<head>");
                            htmlString.Append("<meta charset=\"UTF-8\">");
                            htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                            htmlString.Append("<title>Booking Confirmation</title>");
                            htmlString.Append("<style>");
                            htmlString.Append("body {");
                            htmlString.Append("font-family: Arial, sans-serif;");
                            htmlString.Append("background-color: #f4f4f4;");
                            htmlString.Append("margin: 0;");
                            htmlString.Append("padding: 0;");
                            htmlString.Append("}");
                            htmlString.Append(".email-container {");
                            htmlString.Append("width: 100%;");
                            htmlString.Append("max-width: 650px;");
                            htmlString.Append("margin: 0 auto;");
                            htmlString.Append("background-color: #ffffff;");
                            htmlString.Append("border-radius: 8px;");
                            htmlString.Append("overflow: hidden;");
                            htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                            htmlString.Append("}");
                            htmlString.Append(".header {");
                            htmlString.Append("background-color: #f5f5f5;");
                            htmlString.Append("color: white;");
                            htmlString.Append("}");
                            htmlString.Append(".header h1 {");
                            htmlString.Append("margin: 0;");
                            htmlString.Append("}");
                            htmlString.Append(".content {");
                            htmlString.Append("padding: 20px;");
                            htmlString.Append("}");
                            htmlString.Append(".content h2 {");
                            htmlString.Append("color: #333;");
                            htmlString.Append("}");
                            htmlString.Append(".details {");
                            htmlString.Append("margin-top: 20px;");
                            htmlString.Append("background-color: #f9f9f9;");
                            htmlString.Append("padding: 15px;");
                            htmlString.Append("border-radius: 5px;");
                            htmlString.Append("}");
                            htmlString.Append(".details p {");
                            htmlString.Append("margin: 5px 0;");
                            htmlString.Append("}");
                            htmlString.Append(".footer {");
                            htmlString.Append("background-color: #f4f4f4;");
                            htmlString.Append("padding: 15px;");
                            htmlString.Append("text-align: center;");
                            htmlString.Append("font-size: 12px;");
                            htmlString.Append("color: #777;");
                            htmlString.Append("}");
                            htmlString.Append(".footer a {");
                            htmlString.Append("color: #0066cc;");
                            htmlString.Append("text-decoration: none;");
                            htmlString.Append("}");
                            htmlString.Append(".button {");
                            htmlString.Append("background-color: #0066cc;");
                            htmlString.Append("color: white;");
                            htmlString.Append("padding: 10px 20px;");
                            htmlString.Append("text-decoration: none;");
                            htmlString.Append("border-radius: 5px;");
                            htmlString.Append("display: inline-block;");
                            htmlString.Append("margin-top: 20px;");
                            htmlString.Append("}");
                            htmlString.Append(".responsive-padding {");
                            htmlString.Append("padding: 0px 20px;");
                            htmlString.Append("}");
                            htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                            htmlString.Append("@media (max-width: 768px) {");
                            htmlString.Append(".responsive-padding {");
                            htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                            htmlString.Append("}");
                            htmlString.Append("}");
                            htmlString.Append("</style>");

                            htmlString.Append("</head>");

                            htmlString.Append("<body>");
                            htmlString.Append("<div class=\"email-container\">");


                            htmlString.Append("<div class=\"header\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"center\">");
                            htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td class=\"responsive-padding\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:22px 0 0 0\"><a  style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                            htmlString.Append("<img alt=\"sintra\" height=\"60\" src=\"https://mountfeasthotel.com/Content/Assets/img/logos/logo.png\" style=\"display:block\" width=\"144\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                            htmlString.Append("</a></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#000\">");
                            htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"24\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</div>");

                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td align=\"center\" width=\"100%\">");
                            htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"20\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"center\">");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                            htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                            htmlString.Append("Thanks <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                            htmlString.Append("Your booking in Mount Feast Hotel is confirmed.");
                            htmlString.Append("</h1>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"20\">");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Reservation details</td>");

                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td height=\"16\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding: 0 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                            htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                            htmlString.Append("<tbody>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Check-in");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                            //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                            //htmlString.Append("(from 15:00)");
                            //htmlString.Append("</span>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Check-out");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                            //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                            //htmlString.Append("(until 12:00)");
                            //htmlString.Append("</span>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Your reservation");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("You booked for");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + TotalGuests + " Adults" + "");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Booking Date");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<address style=\"font-style:normal\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToShortDateString() + "");
                            htmlString.Append("</address>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Phone");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                            htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Email");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                            htmlString.Append("" + _booking[0].PaxEmail + "");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Room");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + _booking[0].RoomCategory + "");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td height=\"32\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                            htmlString.Append("Price details");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"16\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:0 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                            htmlString.Append(_booking[0].RoomCategory);
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                            htmlString.Append("5% KPRA Tax.");
                            //htmlString.Append("" + _booking[0].RoomTax + " GST is included.");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                            htmlString.Append("To confirm your booking please make a payment of 50% of the total amount within 1 hour and share a screenshot via WhatsApp ");
                            htmlString.Append("<a href=\"https://wa.me/+923111786446? text=Hi! MountFeast Hotel.\" target=\"_blank\">");
                            htmlString.Append("<b>+923111786446</b>");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            //Conformation Booking

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Account Name");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("MOUNT FEAST HOTEL");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Bank Name");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("HABIB BANK LIMITED PAKISTAN (H.B.L)");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Account Number");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("23017902291403");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("IBAN");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("PK12 HABB0023017902291 403");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            //this is bank deatils


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                            htmlString.Append("Total price");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"32\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");




                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:0px 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                            htmlString.Append("Important Details");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                            htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is 2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                            htmlString.Append("</ul>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"32\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</div>");
                            htmlString.Append("</body>");

                            htmlString.Append("</html>");

                            MailMessage mm = new MailMessage();
                            mm.IsBodyHtml = true;
                            mm.To.Add(_booking[0].PaxEmail);

                            mm.From = new MailAddress("info@mountfeasthotel.com");
                            mm.Subject = "Order Confirmation: " + confirmationId;
                            mm.Body = htmlString.ToString();
                            SmtpClient smc = new SmtpClient();
                            smc.Host = "smtp.gmail.com";
                            smc.Port = 587;
                            smc.UseDefaultCredentials = false;
                            smc.EnableSsl = true;
                            smc.Credentials = new System.Net.NetworkCredential("info@mountfeasthotel.com", "xhhusxiqfemxcpan");
                            smc.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smc.Send(mm);


                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                        }

                    }

                    return View();
                }
                else
                {
                    return RedirectToAction("PaymentError", "SearchResult", new { @pp_ResponseCode = pp_ResponseCode, @pp_ResponseMessage = pp_ResponseMessage });
                }



            }
            catch (Exception ex)
            {

                return Json(new { status = "error", message = ex.ToString() });
            }
        }

        public ActionResult Reservation(DateTime checkIn, DateTime checkOut, int Adults, int Children, int NoOfRooms, int RoomRate, int TotalAmount, string RoomCatCodes,
            string Quatntity, string RatePlanIDs, int RoomRates, int? TaxAmount, int PropertyId)
        {
            var db = new HotelDBEntities();
            int roomcategory = Convert.ToInt32(RoomCatCodes);
            TimeSpan tme = Convert.ToDateTime(checkOut) - Convert.ToDateTime(checkIn);
            int noOfDay = (int)tme.TotalDays;
            if (noOfDay < 1) noOfDay = 1;

            if (TaxAmount == null)
            {
                TaxAmount = 0;
            }

            // Get room pictures and category details
            ViewBag.RoomCategory = db.RoomPictures.FirstOrDefault(x => x.RoomCode == roomcategory && x.Active == true);
            ViewBag.Category = db.RoomCategories.FirstOrDefault(x => x.Id == roomcategory);

            // Get Room Rate from RateSlabs table
            var roomRate = db.RateSlabs
                .Where(x => x.RoomCategoryCode == roomcategory && x.Status == true)
                .Select(x => x.RoomRate)
                .FirstOrDefault();

            // Get Total Discount from RateSlabs
            //var discountAmount = RoomRates;
            var discountAmount = db.RateSlabs
                .Where(rs => rs.PropertyId == PropertyId && rs.RoomCategoryCode == roomcategory && rs.Status == true)
                .Select(rs => rs.Discount)
                .FirstOrDefault();

            // Get Tax Rate from Taxes table
            var taxRate = db.Taxes
                .Where(t => t.PropertyId == PropertyId && t.RoomCategory == roomcategory && t.Status == true)
                .Select(t => t.Amount)
                .FirstOrDefault();

            // Calculate Total Tax
            int TotalTax = 0;
            int GrantTotalTax = 0;
            int GrantTotalAmount = 0;
            int totalDiscountAmount = 0;
            totalDiscountAmount = (Convert.ToInt32(discountAmount) * NoOfRooms);
            if (TotalAmount > 0 && taxRate > 0)
            {
                TotalTax = (Convert.ToInt32(totalDiscountAmount) * Convert.ToInt32(taxRate)) / 100;
            }

            // ✅ Get Adults and NoOfChild allowed from Room table
            var roomInfo = db.Rooms
                .Where(r => r.propertyID == PropertyId && r.RoomCategory == roomcategory && r.Status == true)
                .Select(r => new { r.Adult, r.NoOfChild })
                .FirstOrDefault();
            // Convert RoomCatCodes to integer (Room Category ID)
            int roomCategoryId = Convert.ToInt32(RoomCatCodes);
            int adultsAllowed = roomInfo?.Adult ?? 0;
            int childrenAllowed = roomInfo?.NoOfChild ?? 0;

            // Get top 1 active image with PicNum == 1 for the selected room category
            var roomImageName = db.RoomPictures.Where(x => x.RoomCode == roomCategoryId && x.Active == true)
                                .OrderBy(x => x.PicNum) // You can change this to any relevant field for ordering
                                .Select(x => x.Picture)
                                .FirstOrDefault();

            // Convert image name to a web-accessible path
            string roomImagePath = roomImageName != null ? Url.Content("~/uploads/" + roomImageName) : Url.Content("~/uploads/no-image.jpg");

            // Store the image path in ViewBag (single image only)
            ViewBag.RoomImage = roomImagePath;

            // Optional: store list of image paths in session if needed for other purposes
            List<string> roomImagePathList = GetRoomImagePaths(roomCategoryId);
            Session["RoomImagePaths"] = roomImagePathList;

            // for calulation tax and totalAmount

            //  totalDiscountAmount = Convert.ToInt32(discountAmount);
            GrantTotalTax = TotalTax * noOfDay;
            if (NoOfRooms > 1)
            {
                GrantTotalAmount = TotalAmount;
            }
            else 
            { 
                GrantTotalAmount = RoomRates; 
            }

            Session["NoOfRooms"] = NoOfRooms;
            // ✅ Assign to ViewBag to use in view
            ViewBag.AdultsAllowed = adultsAllowed;
            ViewBag.ChildrenAllowed = childrenAllowed;
            ViewBag.discountAmount = totalDiscountAmount * noOfDay;
            // Other values
            ViewBag.checkIn = checkIn;
            ViewBag.checkOut = checkOut;
            ViewBag.Adults = Adults;
            ViewBag.Children = Children;
            ViewBag.NoOfRooms = NoOfRooms;
            ViewBag.RoomRate = TotalAmount;
            // ViewBag.TotalAmount = TotalAmount;
            ViewBag.RoomCatCodes = RoomCatCodes;
            ViewBag.RoomCateRoomNums = Quatntity;
            ViewBag.RatePlanIDs = RatePlanIDs;
            ViewBag.RoomRates = RoomRates.ToString();
            ViewBag.TotalTax = GrantTotalTax;
            ViewBag.TotalAmount = GrantTotalAmount;
            ViewBag.TaxAmount = GrantTotalTax;
            ViewBag.NoOfDays = noOfDay;

            return View();
        }

        [HttpPost]
        public ActionResult ReservationPost()
        {

            string checkIn = Convert.ToString(Request.Form["CheckIn"]);
            string checkOut = Convert.ToString(Request.Form["CheckOut"]);
            int RoomCatCode = 0;  //Convert.ToInt32(Request.Form["RoomCatCode"]);
            int Adults = Convert.ToInt32(Request.Form["Adults"]);
            int Children = Convert.ToInt32(Request.Form["Children"]);
            int NoOfRooms = Convert.ToInt32(Request.Form["hdTotalRooms"]);
            int RoomRate = 0; // Convert.ToInt32(Request.Form["RoomRate"]);
            int TotalAmount = Convert.ToInt32(Request.Form["TotalAmount"]);
            string RoomCatCodes = Request.Form["RoomCatCode"];
            string RoomCateRoomNums = Request.Form["RoomCateRoomNums"];
            string RatePlanIDs = Request.Form["RatePlanIDs"];
            string RoomRates = Request.Form["RoomRates"];

            string[] _CheckIn = checkIn.Split('-');
            string[] _CheckOut = checkOut.Split('-');

            checkIn = _CheckIn[1] + "/" + _CheckIn[0] + "/" + _CheckIn[2];
            checkOut = _CheckOut[1] + "/" + _CheckOut[0] + "/" + _CheckOut[2];

            DateTime CheckIn = Convert.ToDateTime(checkIn);
            DateTime CheckOut = Convert.ToDateTime(checkOut);
            return RedirectToAction("Reservation", "SearchResult", new { @checkIn = CheckIn, @checkOut = CheckOut, @Adults = Adults, @Children = Children, @NoOfRooms = NoOfRooms, @RoomRate = RoomRate, @TotalAmount = TotalAmount, @RoomCatCodes = RoomCatCodes, @RoomCateRoomNums = RoomCateRoomNums, @RatePlanIDs = RatePlanIDs, @RoomRates = RoomRates });
        }

        [HttpPost]
        public ActionResult ReservationPost2()
        {
            int propertyId = Convert.ToInt32(Request.Form["PropertyId"]);
            string checkIn = Convert.ToString(Request.Form["CheckIn"]);
            string checkOut = Convert.ToString(Request.Form["CheckOut"]);
            //  int RoomCatCode = 0;  //Convert.ToInt32(Request.Form["RoomCatCode"]);
            int Adults = Convert.ToInt32(Request.Form["Adults"]);
            int Children = Convert.ToInt32(Request.Form["Children"]);
            int NoOfRooms = Convert.ToInt32(Request.Form["hdTotalRooms"]);
            int RoomRate = 0; // Convert.ToInt32(Request.Form["RoomRate"]);
            int TotalAmount = Convert.ToInt32(Request.Form["hdTotalAmount"]);
            int TotAmount = Convert.ToInt32(Request.Form["RoomRates"]);
            string RoomCatCodes = Request.Form["RoomCate"];
            string txtQuatntity = Request.Form["txtQuatntity"];
            string RatePlanIDs = Request.Form["RatePlanIDs"];
            string RoomRates = Request.Form["RoomRates"];

            // Store TotalAmount in session
            Session["TotalAmount"] = TotAmount;

            string CheckinDate = checkIn;
            string CheckoutDate = checkOut;

            //string[] _CheckIn = checkIn.Split('-');
            //string[] _CheckOut = checkOut.Split('-');

            //CheckinDate = _CheckIn[1] + "/" + _CheckIn[0] + "/" + _CheckIn[2];
            //CheckoutDate = _CheckOut[1] + "/" + _CheckOut[0] + "/" + _CheckOut[2];

            string DateString = CheckinDate;
            IFormatProvider culture = new CultureInfo("en-US", true);
            DateTime datestart = DateTime.ParseExact(DateString, "MM/dd/yyyy", culture);

            string DateString2 = CheckoutDate;
            IFormatProvider culture2 = new CultureInfo("en-US", true);
            DateTime dateend = DateTime.ParseExact(DateString2, "MM/dd/yyyy", culture2);


            // Fetch image data here
            List<string> roomImages = RoomImagesCat(Convert.ToInt32(RoomCatCodes)); // Get the image list based on RoomCatCodes

            // Convert the list of images into a comma-separated string or pass as a list
            string roomImagesString = string.Join(",", roomImages); // or pass the List<string> if preferred

            return RedirectToAction("Reservation", "SearchResult", new { @checkIn = datestart, @checkOut = dateend, @Adults = Adults, @Children = Children, @NoOfRooms = NoOfRooms, @RoomRate = RoomRate, @TotalAmount = TotalAmount, @RoomCatCodes = RoomCatCodes, @Quatntity = txtQuatntity, @RatePlanIDs = RatePlanIDs, @RoomRates = RoomRates, PropertyId = propertyId, @RoomImages = roomImagesString });
        }


        public string SaveBooking(string PaxTitle, string PaxFName, string PaxLName, DateTime CheckIn, DateTime CheckOut, int RoomCatCode,
            string PaxPhoneNumber, string PaxEmail, string PaxDescription, int Adults, int Children, int NoOfRooms, int RoomRate, int TotalAmount,
            string RoomCatCodes, string RoomCateRoomNums, string RatePlanIDs, string RoomRates/*,string Taxes,string AmountWithoutTaxes*/)
        {
            try
            {
                using (var db = new HotelDBEntities())
                {
                    var rowsAffected = db.Bookings.Where(x => x.PaxFName == PaxFName && x.PaxLName == PaxLName &&
                                x.CheckIn == CheckIn).FirstOrDefault();

                    if (true == true)
                    {
                        int? bookingID = db.Bookings.DefaultIfEmpty().Max(p => p == null ? 0 : p.BookingID);
                        string confirmationId = "MHH-" + Convert.ToString(bookingID + 1);
                        int? PropertyId = db.Rooms.Where(p => p.RoomCategory == RoomCatCode).Select(p => p.propertyID).FirstOrDefault();
                        int? RoomCode = 0;
                        string RoomCate = "";
                        string RoomCates = "";
                        TimeSpan t = Convert.ToDateTime(CheckOut) - Convert.ToDateTime(CheckIn);
                        int noOfDay = (int)t.TotalDays;
                        string[] _RoomCatCodes;
                        string[] _RoomCateRoomNums;
                        string[] _RatePlanIDs;
                        string[] _RoomRates;
                        int _hotelCode = 1;

                        RoomRates = RoomRates.Remove(0, 1);
                        _RoomRates = RoomRates.Split(',');

                        RoomCatCodes = RoomCatCodes.Remove(0, 1);
                        _RoomCatCodes = RoomCatCodes.Split(',');

                        RatePlanIDs = RatePlanIDs.Remove(0, 1);
                        _RatePlanIDs = RatePlanIDs.Split(',');

                        RoomCateRoomNums = RoomCateRoomNums.Remove(0, 1);
                        _RoomCateRoomNums = RoomCateRoomNums.Split(',');

                        if (Session["PropertyId"] != null)
                        {
                            _hotelCode = (int)Session["PropertyId"];
                        }


                        //foreach (var itm in _RoomCatCodes)
                        for (int itm = 0; itm < _RoomCatCodes.Length; itm++)
                        {
                            int CateCode = Convert.ToInt32(_RoomCatCodes[itm]);
                            RoomCode = db.Rooms.Where(p => p.RoomCategory == CateCode).Select(p => p.RoomCode).FirstOrDefault();
                            RoomCate = db.RoomCategories.Where(p => p.Id == CateCode).Select(p => p.Category).FirstOrDefault();
                            RoomCates = RoomCates + "," + RoomCate;

                            //foreach (var itm1 in _RoomCateRoomNums)
                            {
                                int NoofRooms = Convert.ToInt32(_RoomCateRoomNums[itm]);

                                for (int i = 1; i <= NoofRooms; i++)
                                {
                                    var bookedRooms = db.Bookings.Where(x => x.RoomCode == RoomCode && x.CheckIn == CheckIn).Select(p => p.RoomNum).ToList();
                                    var RoomNum = (db.RoomNumbers.Where(p => p.RoomCode == RoomCode).Select(p => p.RoomNumber1).ToList());
                                    //var roomnum = db.RoomNumbers.Where(p => p.RoomNumber1 == RoomNum).FirstOrDefault();

                                    var roomnum = RoomNum.Except(bookedRooms).FirstOrDefault();

                                    if (roomnum != null)
                                    {
                                        //roomnum.Booked = true;

                                        //db.Entry(roomnum).State = System.Data.EntityState.Modified;
                                        //db.SaveChanges();

                                        Booking booking = new Booking();

                                        booking.PaxTitle = PaxTitle;
                                        booking.PaxFName = PaxFName;
                                        booking.PaxLName = PaxLName;
                                        booking.NoOfRooms = 1;
                                        booking.RoomCode = RoomCode;
                                        booking.RoomCatCode = CateCode;
                                        booking.RoomNum = roomnum;
                                        booking.PaxEmail = PaxEmail;
                                        booking.PaxPhoneNumber = PaxPhoneNumber;
                                        booking.PaxDescription = PaxDescription;
                                        booking.Adults = Adults;
                                        booking.Children = Children;
                                        booking.CheckIn = CheckIn;
                                        booking.CheckOut = CheckOut;
                                        booking.ReservationDate = DateTime.Now;
                                        booking.RoomRate = Convert.ToInt32(_RoomRates[itm]);
                                        // booking.TotalRoomRate = Convert.ToInt32(_RoomRates[itm]);
                                        booking.HotelCode = _hotelCode;
                                        booking.ConfirmationNo = confirmationId;
                                        booking.DateModified = DateTime.Now;
                                        booking.BookingStatus = "Booked";
                                        booking.NoofDay = noOfDay;
                                        booking.TotalRoomRate = TotalAmount;
                                        booking.ThemeColor = "red";
                                        booking.CountryCode = "PK";
                                        booking.RatePlanId = Convert.ToInt32(_RatePlanIDs[itm]);

                                        db.Entry(booking).State = EntityState.Added;
                                        db.SaveChanges();




                                    }
                                }
                            }
                        }

                        var _booking = (from obj in HDE.Bookings
                                        join b in HDE.RoomCategories
                                         on obj.RoomCatCode equals b.Id
                                        where (obj.ConfirmationNo == confirmationId)
                                        select new FinalBooking
                                        {
                                            BookingID = obj.BookingID,
                                            PaxTitle = obj.PaxTitle,
                                            PaxFName = obj.PaxFName,
                                            PaxLName = obj.PaxLName,
                                            NoOfRooms = obj.NoOfRooms,
                                            RoomCode = obj.RoomCode,
                                            RoomCatCode = obj.RoomCatCode,
                                            RoomCategory = b.Category,
                                            RoomNum = obj.RoomNum,
                                            PaxEmail = obj.PaxEmail,
                                            PaxPhoneNumber = obj.PaxPhoneNumber,
                                            PaxDescription = obj.PaxDescription,
                                            Adults = obj.Adults,
                                            Children = obj.Children,
                                            CheckIn = obj.CheckIn,
                                            CheckOut = obj.CheckOut,
                                            RoomRate = obj.RoomRate,
                                            HotelCode = obj.HotelCode,
                                            ConfirmationNo = obj.ConfirmationNo,
                                            NoofDay = obj.NoofDay,
                                            TotalRoomRate = obj.TotalRoomRate
                                        }).ToList();

                        RoomCates = RoomCates.Remove(0, 1);
                        TempData["RoomCates"] = RoomCates;
                        TempData["noOfDay"] = noOfDay;
                        Session["booking"] = _booking;
                        Session["confirmationId"] = confirmationId;

                        try
                        {
                            StringBuilder htmlString = new StringBuilder();
                            htmlString.Append(@"<html>");
                            htmlString.Append("<body><div>");
                            htmlString.Append("<div style='background-color:#b7b7b7;color:white;font-size:22px;line-height:44px;padding:0 10px'>Mount Feast Hotel</div>");
                            htmlString.Append("<p>Thank you for choosing MountFeast Hotel, for your stay. We have successfully confirmed your reservation and have issued you the following confirmation number: <b>" + confirmationId + "</b></p>");
                            htmlString.Append("<br /><p>If you need to make changes or require assistance, please call +923328571133 or email us at info@mountfeasthotel.com. We look forward to your stay.</p><br /><br />");
                            htmlString.Append("<table border='0'><tr>");
                            htmlString.Append("<table border='0'><tr>");
                            htmlString.Append("</tr></table>");
                            htmlString.Append("<table border='0'><tr>");
                            htmlString.Append("<th>Guest Name</th><th>Phone Num.</th><th>Room Category</th><th>CheckIn Date/Time</th><th>CheckOut</th></tr>");
                            htmlString.Append("<tr><td>" + PaxTitle + " " + PaxFName + " " + PaxLName + " </td><td> " + PaxPhoneNumber + "</td>");
                            htmlString.Append("<td>" + RoomCates + "</td>");
                            htmlString.Append("<td>" + CheckIn + "</td> ");
                            htmlString.Append("<td>" + CheckOut + "</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</table><br />");
                            htmlString.Append("<span><b>Adults:</b></span><span>" + Adults + "</span><br />");
                            htmlString.Append("<span><b>Children:</b></span><span>" + Children + "</span><br />");
                            htmlString.Append("<span><b>No. Of Rooms:</b></span><span>" + NoOfRooms + "</span><br />");
                            htmlString.Append("<span><b>No. Of Night:</b></span><span>" + noOfDay + "</span><br />");
                            htmlString.Append("<span><b>Total Amount:</b></span><span>" + TotalAmount + "</span><br />");

                            htmlString.Append("</div></body>");
                            htmlString.Append("</html>");

                            MailMessage mm = new MailMessage();
                            mm.IsBodyHtml = true;
                            mm.To.Add("info@mountfeasthotel.com");

                            mm.From = new MailAddress("info@mountfeasthotel.com");
                            mm.Subject = "Order Confirmation: " + confirmationId;
                            mm.Body = htmlString.ToString();
                            SmtpClient smc = new SmtpClient();
                            smc.Host = "smtp.gmail.com";
                            smc.Port = 587;
                            // smc.UseDefaultCredentials = false;
                            smc.EnableSsl = true;
                            smc.Credentials = new System.Net.NetworkCredential("info@mountfeasthotel.com", "xhhusxiqfemxcpan");
                            smc.DeliveryMethod = SmtpDeliveryMethod.Network;

                            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                                      System.Security.Cryptography.X509Certificates.X509Chain chain,
                                      System.Net.Security.SslPolicyErrors sslPolicyErrors)
                            {
                                return true;  // Bypasses the SSL certificate validation
                            };

                            smc.Send(mm);


                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                        }

                        if (PaxEmail != "")
                        {
                            try
                            {
                                StringBuilder htmlString = new StringBuilder();
                                htmlString.Append(@"<html>");
                                htmlString.Append("<body>");
                                htmlString.Append("<h3>Thanks</h3>");
                                htmlString.Append("<p> We have received your booking order and are working on a confirmation.</p>");
                                htmlString.Append("Your Confirmation Num.:  <b>" + confirmationId + "</b><br />");
                                htmlString.Append("<table border='1'><tr>");
                                htmlString.Append("<th>Guest Nmae</th><th>Phone Num.</th><th>Room Category</th><th>CheckIn Date/Time</th><th>CheckOut</th></tr>");
                                htmlString.Append("<tr><td>" + PaxTitle + " " + PaxFName + " " + PaxLName + " </td><td>" + PaxPhoneNumber + " </td>");
                                htmlString.Append("<td>" + RoomCates + "</td>");
                                htmlString.Append("<td>" + CheckIn + "</td> ");
                                htmlString.Append("<td>" + CheckOut + "</td>");
                                htmlString.Append("</tr>");
                                htmlString.Append("</table><br />");
                                htmlString.Append("<span><b>Adults:</b></span><span>" + Adults + "</span><br />");
                                htmlString.Append("<span><b>Children:</b></span><span>" + Children + "</span><br />");
                                htmlString.Append("<span><b>No. Of Rooms:</b></span><span>" + NoOfRooms + "</span><br />");
                                htmlString.Append("<span><b>No. Of Night:</b></span><span>" + noOfDay + "</span><br />");
                                htmlString.Append("<span><b>Total Amount:</b></span><span>" + TotalAmount + "</span><br />");
                                htmlString.Append("</body>");
                                htmlString.Append("</html>");

                                MailMessage mm = new MailMessage();
                                mm.IsBodyHtml = true;
                                mm.To.Add(PaxEmail);

                                mm.From = new MailAddress("info@mountfeasthotel.com");
                                mm.Subject = "Order Confirmation: " + confirmationId;
                                mm.Body = htmlString.ToString();
                                SmtpClient smc = new SmtpClient();
                                smc.Host = "smtp.gmail.com";
                                smc.Port = 587;
                                // smc.UseDefaultCredentials = false;
                                smc.EnableSsl = true;
                                smc.Credentials = new System.Net.NetworkCredential("info@mountfeasthotel.com", "xhhusxiqfemxcpan");
                                smc.DeliveryMethod = SmtpDeliveryMethod.Network;
                                smc.Send(mm);


                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                            }

                        }

                        return ("Booking done succesfully");
                        //jsonString = JsonSerializer.Serialize(confirmationId);

                    }
                    else
                    {
                        // responseBasicProfile.MessageList.Add("Not Record Found.");
                        return ("Something went wrong.");
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                return ("Something went wrong.");
            }
        }

        [HttpPost]
        public ActionResult SaveBooking(FormCollection frm)
        {
            try
            {
                string PaymentType = Request.Form["PaymentType"];
                string PaxTitle = Request.Form["PaxTitle"];
                string PaxFName = Request.Form["PaxFName"];
                string PaxLName = Request.Form["PaxLName"];
                string checkIn = Convert.ToString(Request.Form["CheckIn"]);
                string checkOut = Convert.ToString(Request.Form["CheckOut"]);
                int RoomCatCode = 0;  //Convert.ToInt32(Request.Form["RoomCatCode"]);
                string PaxPhoneNumber = Request.Form["PaxPhoneNumber"];
                string PaxEmail = Request.Form["PaxEmail"];
                string PaxDescription = Request.Form["PaxDescription"];
                int Adults = Convert.ToInt32(Request.Form["Adults"]);
                int Children = Convert.ToInt32(Request.Form["Children"]);
                int NoOfRooms = Convert.ToInt32(Request.Form["hdTotalRooms"]);
                int RoomRate = 0;
                int TotalAmount = Session["TotalAmount"] != null ? Convert.ToInt32(Session["TotalAmount"]) : 0;
                string RoomCatCodes = Request.Form["RoomCatCode"];
                string RoomCateRoomNums = Request.Form["RoomCateRoomNums"];
                string RatePlanIDs = Request.Form["RatePlanIDs"];
                string RoomRates = Request.Form["RoomRates"];
                int DiscountedAmount = Convert.ToInt32(Request.Form["DiscountedAmount"]);

                int hdnTotalAmount = Convert.ToInt32(Request.Form["hdnTotalAmount"]);
                int hdnDiscountAmount = Convert.ToInt32(Request.Form["hdnDiscountAmount"]);
                int hdnTotalTax = Convert.ToInt32(Request.Form["hdnTotalTax"]);

                string VoucherCode = Request.Form["VoucherCode"];

                DateTime CheckIn = Convert.ToDateTime(checkIn);
                DateTime CheckOut = Convert.ToDateTime(checkOut);

                if (Adults == 0)
                {
                    Adults = 1;
                }

                using (var db = new HotelDBEntities())
                {
                    var rowsAffected = db.Bookings.Where(x => x.PaxFName == PaxFName && x.PaxLName == PaxLName &&
                                x.CheckIn == CheckIn).FirstOrDefault();

                    if (true == true)
                    {
                        int? bookingID = db.Bookings.DefaultIfEmpty().Max(p => p == null ? 0 : p.BookingID);
                        string confirmationId = "MFH-" + Convert.ToString(bookingID + 1);
                        int? PropertyId = db.Rooms.Where(p => p.RoomCategory == RoomCatCode).Select(p => p.propertyID).FirstOrDefault();
                        int? RoomCode = 0;
                        string RoomCate = "";
                        string RoomCates = "";
                        TimeSpan t = Convert.ToDateTime(CheckOut) - Convert.ToDateTime(CheckIn);
                        int noOfDay = (int)t.TotalDays;
                        double GrandTotal = 0;
                        {
                            int CateCode = Convert.ToInt32(RoomCatCodes);
                            RoomCode = db.Rooms.Where(p => p.RoomCategory == CateCode).Select(p => p.RoomCode).FirstOrDefault();
                            RoomCate = db.RoomCategories.Where(p => p.Id == CateCode).Select(p => p.Category).FirstOrDefault();
                            RoomCates = RoomCates + "," + RoomCate;

                            int _hotelCode = 1;

                            if (Session["PropertyId"] != null)
                            {
                                _hotelCode = (int)Session["PropertyId"];
                            }

                            {
                                int NoofRooms = Convert.ToInt32(RoomCateRoomNums);

                                {
                                    var bookedRooms = db.Bookings.Where(x => x.RoomCode == RoomCode && x.CheckIn == CheckIn).Select(p => p.RoomNum).ToList();
                                    var RoomNum = (db.RoomNumbers.Where(p => p.RoomCode == RoomCode).Select(p => p.RoomNumber1).ToList());
                                    var roomnum = RoomNum.Except(bookedRooms).FirstOrDefault();

                                    if (roomnum != null)
                                    {
                                        GrandTotal = GrandTotal + hdnTotalAmount;

                                        Booking booking = new Booking();

                                        booking.PaxTitle = PaxTitle;
                                        booking.PaxFName = PaxFName;
                                        booking.PaxLName = PaxLName;
                                        booking.NoOfRooms = Convert.ToInt32(Session["NoOfRooms"]);
                                        booking.RoomCode = RoomCode;
                                        booking.RoomCatCode = CateCode;
                                        booking.RoomNum = roomnum;
                                        booking.PaxEmail = PaxEmail;
                                        booking.PaxPhoneNumber = PaxPhoneNumber;
                                        booking.PaxDescription = PaxDescription;
                                        booking.Adults = Adults;
                                        booking.Children = Children;
                                        booking.CheckIn = CheckIn;
                                        booking.CheckOut = CheckOut;
                                        booking.RoomRate = Convert.ToInt32(hdnDiscountAmount);
                                        //booking.TotalRoomRate = TotalAmount;
                                        booking.HotelCode = _hotelCode;
                                        booking.ConfirmationNo = confirmationId;
                                        booking.DateModified = DateTime.Now;
                                        booking.ReservationDate = DateTime.Now;
                                        booking.BookingStatus = "Pending";
                                        booking.NoofDay = noOfDay;
                                        booking.RoomTax = hdnTotalTax;
                                        booking.CountryCode = "PK";
                                        //booking.TotalRoomRate = TotalAmount;
                                        decimal discountAmount = GetDiscountAmountDecimal(VoucherCode);
                                        if (discountAmount > 0)
                                        {
                                            booking.VoucherAmount = Convert.ToInt32(discountAmount);
                                        }
                                        else
                                        {
                                            booking.VoucherAmount = 0;
                                        }

                                        GrandTotal -= (double)discountAmount;
                                        DiscountedAmount -= (int)discountAmount;

                                        // Insert logic to set TotalRoomRate
                                        if (discountAmount > 0)
                                        {
                                            booking.TotalRoomRate = Convert.ToInt32(hdnTotalAmount) - Convert.ToInt32(discountAmount);
                                        }
                                        else
                                        {
                                            booking.TotalRoomRate = Convert.ToInt32(hdnTotalAmount);
                                        }

                                        booking.ThemeColor = "red";
                                        booking.RatePlanId = Convert.ToInt32(RatePlanIDs);


                                        db.Entry(booking).State = EntityState.Added;
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }

                        if (PaymentType == "1" || PaymentType == null)
                        {
                            return RedirectToAction("ThankyouReservation", "SearchResult", new { @basket_id = confirmationId, @Total = GrandTotal, @err_code = "00", @err_msg = "" });
                        }
                        else
                        {
                            return RedirectToAction("GetAccessToken", "Payfast", new { @billReference = confirmationId, @GrandTotal = GrandTotal });
                        }

                    }
                    else
                    {
                        // responseBasicProfile.MessageList.Add("Not Record Found.");
                        return RedirectToAction("Index", "Home");

                        //return null;
                    }
                }

            }
            catch (Exception ex)
            {
                //System.Diagnostics.Trace.TraceError("Reservation confirmation email failed: {0}", ex);
                return RedirectToAction("Index", "Home");
                // return null;
            }
        }

        public JsonResult GetDiscountAmount(string code)
        {
            var voucher = HDE.Vouchers.FirstOrDefault(v => v.Code == code);

            if (voucher != null)
            {
                return Json(new { success = true, discountAmount = voucher.DiscountAmount }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, discountAmount = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        private decimal GetDiscountAmountDecimal(string code)
        {
            var voucher = HDE.Vouchers.FirstOrDefault(v => v.Code == code);
            return voucher?.DiscountAmount ?? 0;
        }

        private List<string> GetRoomImagePaths(int roomCategoryId)
        {
            var db = new HotelDBEntities();
            var roomImages = db.RoomPictures
                .Where(x => x.RoomCode == roomCategoryId && x.Active == true)
                .Select(x => x.Picture)
                .ToList();

            string imagePathPrefix = "~/uploads/";
            return roomImages.Select(image => imagePathPrefix + image).ToList();
        }

        public List<string> RoomImagesCat(int roomCategoryId)
        {
            try
            {
                using (var db = new HotelDBEntities())
                {
                    var roomPictures = db.RoomPictures
                        .Where(a => a.RoomCode == roomCategoryId && a.Active == true)
                        .Select(a => a.Picture)
                        .ToList();

                    return roomPictures;
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as necessary
                return new List<string>(); // Return an empty list if an exception occurs
            }
        }
    }

    public class BookedRooms
    {
        public string RoomCate { get; set; }
        public int? Quantity { get; set; }
    }

    public class RoomImages
    {
        public int? Category { get; set; }
        public string ImageName { get; set; }
    }

    public class FinalBooking
    {
        public int? BookingID { get; set; }
        public string PaxTitle { get; set; }
        public string PaxFName { get; set; }
        public string PaxLName { get; set; }
        public int? NoOfRooms { get; set; }
        public int? RoomCode { get; set; }
        public int? RoomCatCode { get; set; }
        public string RoomCategory { get; set; }
        public string RoomNum { get; set; }
        public string PaxEmail { get; set; }
        public string PaxPhoneNumber { get; set; }
        public string PaxDescription { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public DateTime? ReservationDate { get; set; }
        public int? RoomRate { get; set; }
        public int? HotelCode { get; set; }
        public string ConfirmationNo { get; set; }
        public int? NoofDay { get; set; }
        public int? TotalRoomRate { get; set; }
        public int? VoucherAmount { get; set; }
        public int? PayableAmount { get; set; }

        public double? RoomTax { get; set; }
    }
    public class RoomSearchResult
    {
        public int RoomCode { get; set; } // int in the stored procedure
        public int RoomCateCode { get; set; } // int in the stored procedure
        public string RoomCategory { get; set; } // varchar(30) in the stored procedure
        public string CheckIn { get; set; } // varchar(5) for time in string format
        public string CheckOut { get; set; } // varchar(5) for time in string format
        public int? NormalPrice { get; set; } // int, but might be null
        public int? AdultAllowed { get; set; } // int, but might be null
        public int? AdultPaxs { get; set; } // int, but might be null
        public int? ExtraAdultPrice { get; set; } // int, but might be null
        public int? NoOfChildAllowed { get; set; } // int, but might be null
        public int? ChildPaxs { get; set; } // int, but might be null
        public int? ExtraChildPrice { get; set; } // int, but might be null
        public int? ChildAge { get; set; } // int, but might be null
        public int? DiscountedPrice { get; set; } // int, but might be null
        public int? NoOfRooms { get; set; } // int, but might be null
        public int? Capacity { get; set; } // int, but might be null
        public DateTime? DateCreated { get; set; } // date, but might be null
        public int? NoOfDays { get; set; } // int, but might be null
        public string ImgURL { get; set; } // varchar(50) in the stored procedure
        public string BedType { get; set; } // varchar(30) in the stored procedure
        public int? TaxAmount { get; set; } // int, but might be null
        public int? TaxPercent { get; set; } // int, but might be null
        public int? TotalAmount { get; set; } // int, but might be null
        public int? DiscountedAmount { get; set; } // int, but might be null
        public int? NoOfRoomsRequired { get; set; } // int, but might be null
        public string Features { get; set; } // varchar(MAX) in the stored procedure
        public string VideoLink { get; set; } // varchar(200) in the stored procedure


    }



}