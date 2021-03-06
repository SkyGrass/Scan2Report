using System;
using System.Web;
using System.Web.UI;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Scan2Report
{
    public partial class Register : Page
    {
        private static string AppId = System.Configuration.ConfigurationManager.AppSettings["AppId"];
        private static string AppSecret = System.Configuration.ConfigurationManager.AppSettings["AppSecret"];
        protected void Page_Load(object sender, EventArgs e)
        {
            string errMsg = "";
            ClientScriptManager cs = Page.ClientScript;
            if (!Page.IsPostBack)
            {
                string code = Request.QueryString["code"] ?? "";
                if (!string.IsNullOrEmpty(code))
                {
                    string access_token_from_session = AppHelper.GetCache("access_token", ref errMsg);
                    if (string.IsNullOrEmpty(access_token_from_session))
                    {
                        access_token_from_session = AppHelper.GetAccessToken(ref errMsg);
                    }
                    if (!string.IsNullOrEmpty(access_token_from_session))
                    {
                        string userId = AppHelper.GetUserId(code, ref errMsg);
                        if (!string.IsNullOrEmpty(userId))
                        {
                            Dictionary<string, string> userInfo = AppHelper.GetUserInfoByUserId(userId, ref errMsg);
                            if (userInfo.Count > 0)
                            {
                                if (userInfo["bind"].Equals("1"))
                                {
                                    Context.GetOwinContext().Response.Cookies.Append("UserId", userId, new Microsoft.Owin.CookieOptions()
                                    {
                                        HttpOnly = false,
                                        Expires = DateTime.MaxValue
                                    });
                                    Context.GetOwinContext().Response.Cookies.Append("UserName", userInfo["name"], new Microsoft.Owin.CookieOptions()
                                    {
                                        HttpOnly = false,
                                        Expires = DateTime.MaxValue
                                    });
                                }
                                string config = AppHelper.InitWx(Request.Url.AbsoluteUri, ref errMsg);
                                cs.RegisterStartupScript(typeof(string), "", "<script>setUserInfo('" + JsonConvert.SerializeObject(userInfo) + "')</script>");
                            }
                            else
                            {
                                cs.RegisterStartupScript(typeof(string), "", string.Format(@"<script>showError('未能获取到用户信息，请稍后再试...\r\n详情:{0}','forbidden')</script>", errMsg));
                            }
                        }
                        else
                        {
                            cs.RegisterStartupScript(typeof(string), "", string.Format(@"<script>showError('未能获取到用户信息失败，请稍后再试...\r\n详情:{0}','forbidden')</script>", errMsg));
                        }
                    }
                    else
                    {
                        cs.RegisterStartupScript(typeof(string), "", string.Format(@"<script>showError('获取企业微信授权失败，请稍后再试...\r\n详情:{0}','forbidden')</script>", errMsg));
                    }
                }
                else
                {
                    errMsg = "您的访问请求不合法";
                    cs.RegisterStartupScript(typeof(string), "", string.Format(@"<script>showError('未能获取到授权Code，请关闭重新打开...\r\n详情:{0}','forbidden')</script>", errMsg));
                }
            }
        }



    }
}