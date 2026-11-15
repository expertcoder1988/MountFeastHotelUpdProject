using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MountFeastMVC.Models
{
    public class Location
    {
        public string Name { get; set; }
        public string GoogleMapUrl { get; set; }  // Embed URL for iframe
        public string ViewUrl { get; set; }       // Direct Google Maps link
    }
}