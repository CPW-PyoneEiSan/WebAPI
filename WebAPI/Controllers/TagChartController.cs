using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class TagChartController : ApiController
    {
        public HttpResponseMessage Get(string TagName,string StartDate,string EndDate)
        {
            string query = @"
               select  format(DateTime,'dd-MMM-yyyy HH:mm') as DateTime1, * from tblTagValue where TagName='" + TagName + "' and DateTime between '"+ StartDate+"' and '"+ EndDate + "'  order by DateTime ";

            DataTable dt = new DataTable();

            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["Tags"].ConnectionString))
            using (var cmd = new SqlCommand(query, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                da.Fill(dt);
            }

            return Request.CreateResponse(HttpStatusCode.OK, dt);
        }

        [HttpPost]
        public string Sample(string email, string password) 
        {
            string message = email+password;
            return message;

        }
    }
}
