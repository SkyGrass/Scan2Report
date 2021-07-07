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
                            string mould = Request.QueryString["mould"] ?? "";
                            string msg = ""; bool isSuccess = false;
                            if (!string.IsNullOrEmpty(mould))
                            {
                                string cmdText = string.Format(@"EXEC P_GetData '{0}'", mould);
                                DataTable dt = null;
                                try
                                {
                                    dt = ZYSoft.DB.BLL.Common.ExecuteDataTable(cmdText);
                                    if (dt.Columns.Contains("IsSuccess"))
                                    {
                                        msg = dt.Rows[0]["Msg"].ToString();
                                    }
                                    else
                                    {
                                        isSuccess = dt.Rows.Count > 0;
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
                                    data = isSuccess ? DataTable2Dic(dt) : new List<Dictionary<string, string>>()
                                }));
                            }
                            else
                            {
                                ResponseStr(Serialize(new
                                {
                                    state = "error",
                                    msg = "没有指定要查询的模具号!"
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
                            date = date.Replace("+", " ");
                            string userName = Context.GetOwinContext().Request.Cookies["UserName"] ?? "";
                            string errMsg = "";
                            if (BeforeSave(machine, mould, date, status, type, userName, ref errMsg))
                            {
                                string cmdText = string.Format(@"EXEC dbo.P_SaveRecord @FMachine = '{0}',
                                                                    @FMould = '{1}', 
                                                                    @FUserName = '{2}', 
                                                                    @FDate = '{3}', 
                                                                    @FStatus = {4},
                                                                    @FType = {5}", machine, mould, userName, date, status, type);
                                DataTable dt = ZYSoft.DB.BLL.Common.ExecuteDataTable(cmdText);
                                bool isSuccess = false;
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    isSuccess = dt.Rows[0]["IsSuccess"].ToString().Equals("1");
                                    errMsg = dt.Rows[0]["Msg"].ToString();
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

        private bool BeforeSave(string machine, string mould, string date, string status, string type, string userName, ref string errMsg)
        {
            errMsg = "必录项缺损!";
            return !string.IsNullOrEmpty(machine) &&
                 !string.IsNullOrEmpty(mould) &&
                 !string.IsNullOrEmpty(date) &&
                 !string.IsNullOrEmpty(status) &&
                 !string.IsNullOrEmpty(type) &&
                 !string.IsNullOrEmpty(userName);
        }

        private bool BeforeBinding(string name, string userId, ref string errMsg)
        {
            errMsg = "必录项缺损!";
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