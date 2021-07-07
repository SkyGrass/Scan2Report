using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Scan2Report
{
    public partial class Auth : System.Web.UI.Page
    {
        private static string AppId = System.Configuration.ConfigurationManager.AppSettings["AppId"];
        private static string RedirectUrl = System.Configuration.ConfigurationManager.AppSettings["RedirectUrl"];
        private static string AgentId = System.Configuration.ConfigurationManager.AppSettings["AgentId"];
        protected void Page_Load(object sender, EventArgs e)
        {
            /*
             https://open.weixin.qq.com/connect/oauth2/authorize?appid=wx0c0a73fc9bbce7c2&redirect_uri=https%3A%2F%2Fskygrass.xyz&response_type=code&scope=snsapi_base&agentid=1000008#wechat_redirect
             */
            if (!Page.IsPostBack)
            {
                try
                { 
                    Response.Redirect(string.Format(@"https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_base&agentid={2}#wechat_redirect",
                        AppId, HttpUtility.HtmlEncode(RedirectUrl), AgentId), false);
                }
                catch (Exception)
                {
                     
                }
            }
        }
    }
}