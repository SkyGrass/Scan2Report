using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using Owin;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Scan2Report
{
    public partial class Report : Page
    {
        public static string CurUserName = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            ClientScriptManager cs = Page.ClientScript;
            string errMsg = "";
            if (!Page.IsPostBack)
            {
                CurUserName = HttpUtility.UrlDecode(Context.Request.Cookies["UserName"] == null ? "" : Context.Request.Cookies["UserName"].Value);
                string config = AppHelper.InitWx(Request.Url.AbsoluteUri, ref errMsg);
                cs.RegisterStartupScript(typeof(string), "", "<script>initWxConfig('" + config + "')</script>");
            }
        }
    }
}