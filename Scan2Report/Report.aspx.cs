using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Owin;
using Scan2Report.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Scan2Report
{
    public partial class Report : Page
    {
        public static string CurUserName = "";
        public static string VerifyMachine = (System.Configuration.ConfigurationManager.AppSettings["VerifyMachine"] ?? "0").Equals("1") ? "true" : "false";
        protected void Page_Load(object sender, EventArgs e)
        {
            ClientScriptManager cs = Page.ClientScript;
            string errMsg = "";
            if (!Page.IsPostBack)
            {
                CurUserName = Context.Response.Cookies["UserName"].Value ?? "";
                string config = AppHelper.InitWx("http://auth.skygrass.xyz:801/report", ref errMsg);
                cs.RegisterStartupScript(typeof(string), "", "<script>initWxConfig('" + config + "')</script>");
            }
        }
    }
}