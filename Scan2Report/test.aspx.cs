using Scan2Report.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Scan2Report
{
    public partial class test : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            ClientScriptManager cs = Page.ClientScript;
            string errMsg = "";
            if (!Page.IsPostBack)
            {
                string config = AppHelper.InitWx("http://auth.skygrass.xyz:801/test", ref errMsg);
                cs.RegisterStartupScript(typeof(string), "", "<script>initWxConfig('" + config + "')</script>");


                Context.GetOwinContext().Response.Redirect("Auth.aspx");

            }
        }
    }
}