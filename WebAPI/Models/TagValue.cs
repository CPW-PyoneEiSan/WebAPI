using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPI.Models
{
    public class TagValue
    {
        public DateTime DateTime { get; set; }
        public decimal tagValue { get; set; }
        public string TagName { get; set; }
    }
}