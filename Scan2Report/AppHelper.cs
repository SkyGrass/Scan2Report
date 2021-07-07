using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;

namespace Scan2Report
{
    public static class AppHelper
    {

        private static string AppId = System.Configuration.ConfigurationManager.AppSettings["AppId"];
        private static string AppSecret = System.Configuration.ConfigurationManager.AppSettings["AppSecret"];

        public static bool SetCache(string cache_key, string cache_content, int expires_in, ref string err_msg)
        {
            bool result = false;
            try
            {
                string cmdText = "";
                if (!ZYSoft.DB.BLL.Common.Exist(string.Format(@"SELECT 1 FROM t_Cache WHERE FCacheType ='{0}'", cache_key)))
                {
                    cmdText = (string.Format(@"INSERT INTO dbo.t_Cache
	                                ( FCacheType ,FCacheContent ,FSaveTime ,FExpiresTime) values('{0}','{1}','{2}','{3}')",
                                   cache_key, cache_content, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.AddSeconds(expires_in).ToString("yyyy-MM-dd HH:mm:ss")));
                }
                else
                {
                    if (!ZYSoft.DB.BLL.Common.Exist(string.Format(@"SELECT 1 FROM t_Cache WHERE FCacheType ='{0}' and FExpiresTime>Getdate()", cache_key)))
                    {
                        cmdText = (string.Format(@"update t_Cache set FCacheContent ='{1}',FSaveTime = '{2}' ,FExpiresTime ='{3}' where FCacheType='{0}'",
                                                         cache_key, cache_content, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.AddSeconds(expires_in).ToString("yyyy-MM-dd HH:mm:ss")));
                    }
                }


                int effectRow = ZYSoft.DB.BLL.Common.ExecuteNonQuery(cmdText);

                result = effectRow > 0;
            }
            catch (Exception e)
            {
                err_msg = e.Message;
                WriteLog(err_msg);
            }
            return result;
        }

        public static string GetCache(string cache_key, ref string err_msg)
        {
            string cache_content = "";
            try
            {
                DataTable dt = ZYSoft.DB.BLL.Common.ExecuteDataTable(string.Format(@"SELECT FCacheContent FROM t_Cache WHERE FCacheType ='{0}' and FExpiresTime>Getdate()", cache_key));
                if (dt != null && dt.Rows.Count > 0)
                {
                    cache_content = dt.Rows[0]["FCacheContent"].ToString();
                }
            }
            catch (Exception e)
            {
                err_msg = e.Message;
                WriteLog(err_msg);
            }
            return cache_content;
        }


        public static string GetAccessToken(ref string errMsg)
        {
            string access_token = "";
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add("corpid", AppId);
            form.Add("corpsecret", AppSecret);
            string resp = GetRequestApi("https://qyapi.weixin.qq.com/cgi-bin/gettoken", ref errMsg, form);
            if (!string.IsNullOrEmpty(resp))
            {
                Dictionary<string, string> res = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp);
                if (res.ContainsKey("errcode") && res["errcode"].Equals("0"))
                {
                    access_token = res["access_token"] ?? "";
                    int expires_in = int.Parse(res["expires_in"] ?? "0");
                    SetCache("access_token", access_token, expires_in, ref errMsg);
                }
                else
                {
                    errMsg = res["errmsg"];
                }
            }
            return access_token;
        }

        public static string GetUserId(string code, ref string errMsg)
        {
            string userId = "";
            string access_token = GetCache("access_token", ref errMsg);
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add("access_token", access_token);
            form.Add("code", code);
            string resp = GetRequestApi("https://qyapi.weixin.qq.com/cgi-bin/user/getuserinfo", ref errMsg, form);
            if (!string.IsNullOrEmpty(resp))
            {
                Dictionary<string, string> res = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp);
                if (res.ContainsKey("errcode") && res["errcode"].Equals("0"))
                {
                    userId = res["UserId"] ?? "";
                }
                else
                {
                    errMsg = res["errmsg"];
                }
            }
            return userId;
        }

        public static Dictionary<string, string> GetUserInfoByUserId(string userId, ref string errMsg)
        {
            Dictionary<string, string> userInfo = new Dictionary<string, string>();
            DataTable dt = ZYSoft.DB.BLL.Common.ExecuteDataTable(string.Format(@"SELECT FUserName,ISNULL(FMobile,'')FMobile,FWeChatID FROM dbo.t_User WHERE FWeChatID = '{0}'", userId));
            if (dt != null && dt.Rows.Count > 0)
            {
                userInfo.Add("userId", dt.Rows[0]["FWeChatID"].ToString());
                userInfo.Add("name", dt.Rows[0]["FUserName"].ToString());
                userInfo.Add("mobile", dt.Rows[0]["FMobile"].ToString());
                userInfo.Add("bind", "1");  
            }
            else
            {
                string access_token = GetCache("access_token", ref errMsg);
                Dictionary<string, string> form = new Dictionary<string, string>();
                form.Add("access_token", access_token);
                form.Add("userid", userId);
                string resp = GetRequestApi("https://qyapi.weixin.qq.com/cgi-bin/user/get", ref errMsg, form);
                if (!string.IsNullOrEmpty(resp))
                {
                    Dictionary<string, object> res = JsonConvert.DeserializeObject<Dictionary<string, object>>(resp);
                    if (res.ContainsKey("errcode") && res["errcode"].ToString().Equals("0"))
                    {
                        userInfo.Add("userId", res["userid"].ToString());
                        userInfo.Add("name", res["name"].ToString());
                        userInfo.Add("mobile", res["mobile"].ToString());
                        userInfo.Add("bind", "0");
                    }
                    else
                    {
                        errMsg = res["errmsg"].ToString();
                    }
                }
            }
            return userInfo;
        }

        public static string GetRequestApi(string url, ref string errMsg, Dictionary<string, string> dic = null)
        {
            string resp = "";
            try
            {
                string query = "";
                if (dic != null && dic.Count > 0)
                {
                    foreach (var ele in dic)
                    {
                        query += string.Format(@"{0}={1}&", ele.Key, ele.Value);
                    }
                    query = query.Substring(0, query.Length - 1);
                }
                using (HttpWebResponse response = HttpHelper.HttpRequest("get", string.Format(@"{0}?{1}", url, query),
                                                    null, null, Encoding.UTF8, ""))
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        resp = reader.ReadToEnd();  //得到响应结果
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }

            return resp;
        }

        public static string GetTicket(ref string errMsg)
        {
            string ticket = "";
            string access_token = GetCache("access_token", ref errMsg);
            if (string.IsNullOrEmpty(access_token))
            {
                access_token = GetAccessToken(ref errMsg);
            }
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add("access_token", access_token);
            string resp = GetRequestApi("https://qyapi.weixin.qq.com/cgi-bin/get_jsapi_ticket", ref errMsg, form);
            if (!string.IsNullOrEmpty(resp))
            {
                Dictionary<string, string> res = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp);
                if (res.ContainsKey("errcode") && res["errcode"].Equals("0"))
                {
                    ticket = res["ticket"] ?? "";
                    int expires_in = int.Parse(res["expires_in"] ?? "0");
                    SetCache("ticket", ticket, expires_in, ref errMsg);
                }
                else
                {
                    errMsg = res["errmsg"];
                }
            }
            return ticket;
        }

        public static string InitWx(string url, ref string errMsg)
        {
            string ticket_from_session = GetCache("ticket", ref errMsg);
            if (string.IsNullOrEmpty(ticket_from_session))
            {
                ticket_from_session = GetTicket(ref errMsg);
            }
            string noncestr = Guid.NewGuid().ToString().Replace("-", "").ToLower().Substring(0, 16);
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string timestamp = ((int)(DateTime.Now - startTime).TotalSeconds).ToString();

            string str = string.Format(@"jsapi_ticket={0}&noncestr={1}&timestamp={2}&url={3}", ticket_from_session, noncestr, timestamp, url);
            string sign = Sign(str);
            Dictionary<string, string> config = new Dictionary<string, string>();
            config.Add("appId", AppId);
            config.Add("timestamp", timestamp);
            config.Add("nonceStr", noncestr);
            config.Add("signature", sign);
            config.Add("url", url);
            config.Add("ticket", ticket_from_session);
            return JsonConvert.SerializeObject(config);
        }

        public static string Sign(string stringSor)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] bytes_sha1_in = UTF8Encoding.Default.GetBytes(stringSor);
            byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
            return BitConverter.ToString(bytes_sha1_out).Replace("-", "").ToLower();
        }

        public static void WriteLog(string content)
        {
            try
            {
                string tracingFile = HttpContext.Current.Server.MapPath("log/");
                if (!Directory.Exists(tracingFile))
                    Directory.CreateDirectory(tracingFile);
                string fileName = DateTime.Now.ToString("yyyyMMdd") + ".txt";
                tracingFile += fileName;
                if (tracingFile != String.Empty)
                {
                    FileInfo file = new FileInfo(tracingFile);
                    StreamWriter debugWriter = new StreamWriter(file.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                    debugWriter.WriteLine(DateTime.Now.ToString());
                    debugWriter.WriteLine(content);
                    debugWriter.WriteLine();
                    debugWriter.Flush();
                    debugWriter.Close();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}