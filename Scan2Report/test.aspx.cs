using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Scan2Report
{
    public partial class Test : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            ClientScriptManager cs = Page.ClientScript;
            if (!Page.IsPostBack)
            {
                Context.GetOwinContext().Response.Cookies.Append("test_server", "true");
                cs.RegisterStartupScript(typeof(string), "", "<script>showSuccess('server is running')</script>");
            }
        }
    }
}