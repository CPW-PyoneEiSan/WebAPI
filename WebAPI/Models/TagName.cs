using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPI.Models
{
    public class TagName
    {
        public int AutoID { get; set; }
        public string tagName { get; set; }
        public string Unit { get; set; }
        public string TagDesc { get; set; }
        public string TagAbbr { get; set; }

    }
}