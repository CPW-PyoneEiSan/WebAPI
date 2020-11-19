using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.UI.WebControls;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class TagNameController : ApiController
    {
        public HttpResponseMessage Get()
        {
            string query = @"
                select * from tblTagName where TagName not like '%to be%' order by tagName          
            ";

            DataTable dt = new DataTable();

            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["Tags"].ConnectionString))
                using (var cmd=new SqlCommand(query,con))
                using (var da=new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                da.Fill(dt);
            }

            return Request.CreateResponse(HttpStatusCode.OK, dt);
        }
        public string Post(TagName tn)
        {
            try
            {
                string query = @"
                insert into   dbo.tblTagName (TagName) values
                ('"+tn.tagName+@"')
            ";
                DataTable dt = new DataTable();

                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["Tags"].ConnectionString))
                using (var cmd = new SqlCommand(query, con))
                using (var da = new SqlDataAdapter(cmd))
                {
                    cmd.CommandType = CommandType.Text;
                    da.Fill(dt);
                }

                return "Added Successfully.";
            }
            catch(Exception)
            {
                return "Added Failed.";
            }
        }
    }
   
}
