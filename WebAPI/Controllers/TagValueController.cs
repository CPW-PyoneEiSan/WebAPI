using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class TagValueController : ApiController
    {
        public HttpResponseMessage Get(string StartDate, string EndDate)
        {
            string query = @"
                DECLARE @cols AS NVARCHAR(MAX),    @query  AS NVARCHAR(MAX)
                select @cols = STUFF((SELECT ',[' + convert(varchar(50),TagName)+']'
                    from tblTagName					
                    order by TagName
                FOR XML PATH(''), TYPE
                ).value('.', 'NVARCHAR(MAX)') 
                ,1,1,'')

		        set @query='

		        select format(DateTime,''dd-MMM-yyyy HH:mm'') as DateTime1, * from tblTagValue
		        pivot 
		        (
		        min(TagValue) for TagName in ('+@cols+')
		        )
		        pv1
                where datetime between ''"+StartDate+"'' and ''"+EndDate+@"''
		        order by datetime
		        '
		        exec (@query)        
                ";

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
        [Route("File/Post")]
        public IHttpActionResult UploadFile()
        {
            try
            {
                string message = "";
                var httpRequest = HttpContext.Current.Request;
                var postedFile = httpRequest.Files.Count;

                if (httpRequest.Files.Count > 0)
                {
                    var file = httpRequest.Files[0].FileName;

                    using (var sreader = new StreamReader(httpRequest.Files[0].InputStream))
                    {
                        //First line is header. If header is not passed in csv then we can neglect the below line.
                        string[] headers = sreader.ReadLine().Split(',');

                        for (int i = 1; i < headers.Length; i++)
                        {
                            string query = @"
                                IF NOT EXISTS (select * from tblTagName where TagName='" + headers[i].Trim() + @"')
                                BEGIN
                                insert into   dbo.tblTagName (TagName) values
                                ('" + headers[i].Trim() + @"')
                                END
                                                                
                            ";
                            DataTable dt = new DataTable();

                            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["Tags"].ConnectionString))
                            using (var cmd = new SqlCommand(query, con))
                            using (var da = new SqlDataAdapter(cmd))
                            {
                                cmd.CommandType = CommandType.Text;
                                da.Fill(dt);
                            }
                        }

                        //Loop through the records
                        while (!sreader.EndOfStream)
                        {
                            string[] rows = sreader.ReadLine().Split(',');
                            for (int j = 1; j < rows.Length; j++)
                            {
                                string query = @"
                            insert into   dbo.tbltagValue  values
                            ('" + rows[0] + "','" + rows[j] + "','" + headers[j].Trim() + @"')
                            ";
                                DataTable dt = new DataTable();

                                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["Tags"].ConnectionString))
                                using (var cmd = new SqlCommand(query, con))
                                using (var da = new SqlDataAdapter(cmd))
                                {
                                    cmd.CommandType = CommandType.Text;
                                    da.Fill(dt);
                                }
                            }
                        }
                    }
                    return Ok("Upload Succesful.");
                }
                else
                {
                    return Ok("b");
                }
            }
            catch (System.Exception ex)
            {
                return Ok("Upload Failed: " + ex.Message);
            }


        }
        [Route("File/PostSpec")]
        [HttpPost]
        public IHttpActionResult ExcelUpload()
        {
            string message = "";
            HttpResponseMessage result = null;
            var httpRequest = HttpContext.Current.Request;
            var fileName = httpRequest.Files[0].FileName;
            try
            {
                if (httpRequest.Files.Count > 0)
                {
                    HttpPostedFile file = httpRequest.Files[0];
                    Stream stream = file.InputStream;

                    IExcelDataReader reader = null;

                    if (file.FileName.EndsWith(".xls"))
                    {
                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                    }
                    else if (file.FileName.EndsWith(".xlsx"))
                    {
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                    }
                    else
                    {
                        message = "This file format is not supported";
                    }

                    DataSet excelRecords = reader.AsDataSet();
                    reader.Close();

                    var finalRecords = excelRecords.Tables[0];
                    for (int i = 5; i < finalRecords.Rows.Count; i++)
                    {
                        if (finalRecords.Rows[i][3].ToString() != string.Empty)
                        {
                            string query = @"
                           
                        IF EXISTS (select * from tblTagName where TagName='" + finalRecords.Rows[i][3].ToString().Trim() + @"')
                        BEGIN
                        UPDATE tblTagName SET Unit='" + finalRecords.Rows[i][9].ToString() + "' , TagDesc='" + finalRecords.Rows[i][2].ToString() +
                       "' , TagAbbr='" + finalRecords.Rows[i][1].ToString() + @"' 
                        WHERE TagName= '" + finalRecords.Rows[i][3].ToString().Trim() + @"'
                        END
                        ELSE
                        BEGIN

                        INSERT INTO tblTagName (TagName,Unit,TagDesc,TagAbbr)
                        VALUES ('" + finalRecords.Rows[i][3].ToString().Trim() + "','" + finalRecords.Rows[i][9].ToString() + "','" + finalRecords.Rows[i][2].ToString() + "','" + finalRecords.Rows[i][1].ToString() + @"')

                        END
                            ";

                            DataTable dt = new DataTable();

                            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["Tags"].ConnectionString))
                            using (var cmd = new SqlCommand(query, con))
                            using (var da = new SqlDataAdapter(cmd))
                            {
                                cmd.CommandType = CommandType.Text;
                                da.Fill(dt);
                            }

                        }
                    }
                }
                return Ok("Upload Succesful.");
            }
            catch (Exception ex)
            {
                return Ok("Upload Failed Excel Specification : " + ex.Message);
            }
        }

        [Route("GetTagNameList")]
        [HttpGet]
        public HttpResponseMessage GetTagNameList()
        {
            string query = @"
            select * from tblTagName where TagName not like '%to be%' order by TagName
            ";

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
      
        [Route("GetSelectedTagName")]
        [HttpGet]
        public HttpResponseMessage GetSelectedTagName(string TagName)
        {
            string query = @"
            select * from tblTagName  where TagName='" + TagName + "'";

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
        [Route("GetSelectedTagValue")]
        [HttpGet]
        public HttpResponseMessage GetSelectedTagValue(string TagName)
        {
            string query = @"
            select TagValue from tblTagValue  where TagName='" + TagName + "'";

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
        [Route("GetSelectedTagValueAndDate")]
        [HttpGet]
        public HttpResponseMessage GetSelectedTagValueAndDate(string TagName)
        {
            string query = @"
            select  format(DateTime,'dd-MMM-yyyy HH:mm') as DateTime, TagValue from tblTagValue  where TagName='" + TagName + "'";

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
        [Route("SaveNewTag")]
        [HttpGet]
        public HttpResponseMessage SaveNewTag(string TagName,string Formula)
        {
            string query = @"
             DECLARE @cols AS NVARCHAR(MAX),    @query  AS NVARCHAR(MAX)
                select @cols = STUFF((SELECT ',[' + convert(varchar(50),TagName)+']'
                    from tblTagName					
                    order by TagName
                FOR XML PATH(''), TYPE
                ).value('.', 'NVARCHAR(MAX)') 
                ,1,1,'')

		        set @query='
				insert into tblTagValue 
		        select DateTime, "+ Formula + ",''" + TagName + @" '' 
                from tblTagValue
		        pivot 
		        (
		        min(TagValue) for TagName in ('+@cols+')
		        )
		        pv1
     
		        order by datetime
		        '
		        exec (@query) 
                 ";
            DataTable dt = new DataTable();

            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["Tags"].ConnectionString))
            using (var cmd = new SqlCommand(query, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;
                da.Fill(dt);
            }

            return Request.CreateResponse(HttpStatusCode.OK, dt);
        }
        [Route("SaveNewTagName")]
        [HttpGet]
        public HttpResponseMessage SaveNewTagName(string NewTagName, string NewTagAbbr,string NewTagDesc,string NewTagUnit)
        {
            string query = @"
                insert into tblTagName (TagName,Unit,TagDesc,TagAbbr)
                values('"+NewTagName+"','"+NewTagUnit+"','"+NewTagDesc+"','"+NewTagAbbr+@"') 
                 ";
            DataTable dt = new DataTable();

            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["Tags"].ConnectionString))
            using (var cmd = new SqlCommand(query, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;
                da.Fill(dt);
            }

            return Request.CreateResponse(HttpStatusCode.OK, dt);
        }
    }
}
