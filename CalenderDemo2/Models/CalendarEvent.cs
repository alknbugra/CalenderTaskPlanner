using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CalenderDemo2.Models
{
    public class CalendarEvent
    {
        public int? id { get; set; }
        public int? taskid { get; set; }
        public int? statusId { get; set; }
        public string status { get; set; }
        public string title { get; set; }
        public string shorttitle { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public bool? allDay { get; set; }
        public string backgroundColor { get; set; }
        public string textColor { get; set; }
        public string borderColor { get; set; }
        public string url { get; set; }

    }

}