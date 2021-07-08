using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

namespace Scan2Report
{
    public partial class Proxy : System.Web.UI.Page
    {
        private static bool VerifyMachine = (System.Configuration.ConfigurationManager.AppSettings["VerifyMachine"] ?? "0").Equals("1");
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                string action = Request.QueryString["action"] ?? "";
                if (string.IsNullOrEmpty(action)) return;
                switch (action)
                {
                    case "query":
                        if (Request.HttpMethod.ToLower().Equals("get"))
                        {
                            string machine = Request.QueryString["machine"] ?? "";
                            string msg = ""; bool isSuccess = false;
                            if (!string.IsNullOrEmpty(machine))
                            {
                                string cmdText = string.Format(@"EXEC P_GetData '{0}'", machine);
                                DataSet ds = null; DataTable dtInfo = null; DataTable dtList = null;
                                try
                                {
                                    ds = ZYSoft.DB.BLL.Common.ExecuteDataSet(cmdText);
                                    dtInfo = ds.Tables[0];
                                    dtList = null;
                                    if (dtInfo.Rows[0]["IsSuccess"].Equals("0"))
                                    {
                                        msg = dtInfo.Rows[0]["Msg"].ToString();
                                        dtList = new DataTable();
                                    }
                                    else
                                    {
                                        dtList = ds.Tables[1];
                                        isSuccess = dtInfo.Rows.Count > 0;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    msg = ex.Message;
                                }

                                ResponseStr(Serialize(new
                                {
                                    state = isSuccess ? "success" : "error",
                                    msg = msg,
                                    data = isSuccess ? JsonConvert.SerializeObject(new
                                    {
                                        a = DataTable2Dic(dtInfo),
                                        b = DataTable2Dic(dtList)
                                    }) : JsonConvert.SerializeObject(new
                                    {
                                        a = new string[] { },
                                        b = new string[] { }
                                    })
                                }));
                            }
                            else
                            {
                                ResponseStr(Serialize(new
                                {
                                    state = "error",
                                    msg = "没有指定要查询的机台号!"
                                }));
                            }
                        }
                        else
                        {
                            ResponseStr(Serialize(new
                            {
                                state = "error",
                                msg = "请求方式不合法!"
                            }));
                        }
                        break;
                    case "save":
                        if (Request.HttpMethod.ToLower().Equals("post"))
                        {
                            string machine = Request.Form["machine"] ?? "";
                            string mould = Request.Form["mould"] ?? "";
                            string date = Request.Form["date"] ?? "";
                            string status = Request.Form["status"] ?? "";
                            string type = Request.Form["type"] ?? "";
                            string ispause = Request.Form["ispause"] ?? "0";
                            date = date.Replace("+", " ");
                            string userName = Context.GetOwinContext().Request.Cookies["UserName"] ?? "";
                            string UserId = Context.GetOwinContext().Request.Cookies["UserId"] ?? "";
                            string errMsg = "";
                            if (BeforeSave(machine, mould, date, status, type, UserId, userName, ref errMsg))
                            {
                                bool isSuccess = false;
                                if (ZYSoft.DB.BLL.Common.Exist(string.Format(@"SELECT 1 FROM dbo.t_User WHERE FUserName ='{0}' AND FWeChatID ='{1}'", userName, UserId)))
                                {
                                    string cmdText = string.Format(@"EXEC dbo.P_SaveRecord @FMachine = '{0}',
                                                                    @FMould = '{1}', 
                                                                    @FUserName = '{2}', 
                                                                    @FDate = '{3}', 
                                                                    @FStatus = {4},
                                                                    @FType = {5},
                                                                    @FIsPause= {6}", machine, mould, userName, date, status, type, ispause.Equals("1"));


                                    AppHelper.WriteLog(cmdText);
                                    DataTable dt = ZYSoft.DB.BLL.Common.ExecuteDataTable(cmdText);

                                    if (dt != null && dt.Rows.Count > 0)
                                    {
                                        isSuccess = dt.Rows[0]["IsSuccess"].ToString().Equals("1");
                                        errMsg = dt.Rows[0]["Msg"].ToString();
                                    }
                                }
                                else
                                {
                                    errMsg = "没有查询到采集人的绑定信息,您的操作可能不合法!";
                                }
                                ResponseStr(Serialize(new
                                {
                                    state = isSuccess ? "success" : "error",
                                    msg = errMsg
                                }));
                            }
                            else
                            {
                                ResponseStr(Serialize(new
                                {
                                    state = "error",
                                    msg = errMsg
                                }));
                            }
                        }
                        else
                        {
                            ResponseStr(Serialize(new
                            {
                                state = "error",
                                msg = "请求方式不合法!"
                            }));
                        }
                        break;
                    case "binding":
                        if (Request.HttpMethod.ToLower().Equals("post"))
                        {
                            string name = Request.Form["name"] ?? "";
                            string userId = Request.Form["userId"] ?? "";
                            string mobile = Request.Form["mobile"] ?? "";
                            string errMsg = "";
                            if (BeforeBinding(name, userId, ref errMsg))
                            {
                                SqlParameter[] parm = new SqlParameter[] {
                                    new SqlParameter("@FUserName", name),
                                    new SqlParameter("@FUserId", userId),
                                    new SqlParameter("@FMobile", mobile)
                                };
                                bool isSuccess = false;
                                if (!ZYSoft.DB.BLL.Common.Exist(string.Format(@"SELECT 1 FROM dbo.t_User WHERE FWeChatID = '{0}'", userId)))
                                {
                                    int effectRow = ZYSoft.DB.BLL.Common.ExecuteNonQuery(string.Format(@"INSERT INTO dbo.t_User(FUserName, FWeChatID,FMobile )
		                                        SELECT @FUserName,@FUserId,@FMobile"), parm);
                                    isSuccess = effectRow > 0;
                                    errMsg = isSuccess ? "绑定成功!" : "绑定失败!";

                                    if (isSuccess)
                                    {
                                        Context.GetOwinContext().Response.Cookies.Append("UserId", userId, new Microsoft.Owin.CookieOptions()
                                        {
                                            HttpOnly = false,
                                            Expires = DateTime.MaxValue
                                        });
                                        Context.GetOwinContext().Response.Cookies.Append("UserName", name, new Microsoft.Owin.CookieOptions()
                                        {
                                            HttpOnly = false,
                                            Expires = DateTime.MaxValue
                                        });
                                    }
                                }
                                else
                                {
                                    errMsg = "当前微信身份已被绑定，请核实!";
                                }
                                ResponseStr(Serialize(new
                                {
                                    state = isSuccess ? "success" : "error",
                                    msg = errMsg
                                }));
                            }
                            else
                            {
                                ResponseStr(Serialize(new
                                {
                                    state = "error",
                                    msg = errMsg
                                }));
                            }
                        }
                        else
                        {
                            ResponseStr(Serialize(new
                            {
                                state = "error",
                                msg = "请求方式不合法!"
                            }));
                        }
                        break;
                    default:
                        ResponseStr(Serialize(new
                        {
                            state = "error",
                            msg = "未指定业务类型!"
                        }));
                        break;
                }
            }
        }

        private bool BeforeSave(string machine, string mould, string date, string status, string type, string userId, string userName, ref string errMsg)
        {
            errMsg = "采集信息必录项不完整,请核实!";
            return !string.IsNullOrEmpty(machine) &&
                 !string.IsNullOrEmpty(mould) &&
                 !string.IsNullOrEmpty(date) &&
                 !string.IsNullOrEmpty(status) &&
                 !string.IsNullOrEmpty(type) &&
                 !string.IsNullOrEmpty(userId) &&
                 !string.IsNullOrEmpty(userName);
        }

        private bool BeforeBinding(string name, string userId, ref string errMsg)
        {
            errMsg = "绑定信息必录项不完整,请核实!";
            return !string.IsNullOrEmpty(name) &&
                 !string.IsNullOrEmpty(userId);
        }

        public void ResponseStr(string result)
        {
            Response.Write(result);
        }

        public string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public List<Dictionary<string, string>> DataTable2Dic(DataTable dt)
        {
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            foreach (DataRow dr in dt.Rows)
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                foreach (DataColumn column in dt.Columns)
                {
                    dic[column.ColumnName] =
                        dr[column.ColumnName] == DBNull.Value ? "" : dr[column.ColumnName].ToString();
                }
                list.Add(dic);
            }
            return list;
        }
    }
}