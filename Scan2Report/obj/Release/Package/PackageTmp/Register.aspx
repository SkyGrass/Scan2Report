<%@ Page Title="用户绑定" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="Scan2Report.Register" Async="true" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <h2><%: Title %></h2>

    <div class="row">
        <div class="col-md-8">
            <section id="loginForm">
                <div class="form-horizontal">
                    <h4>请先核实信息，确认后再绑定。</h4>
                    <hr />

                    <div class="form-group">
                        <label for="txtPhone" class="col-md-2 control-label">手机号</label>
                        <div class="col-md-10">
                            <input type='text' name="mobile" id="txtPhone" class="form-control" readonly />
                        </div>
                    </div>
                    <div class="form-group">
                        <label for="txtName" class="col-md-2 control-label">姓名</label>
                        <div class="col-md-10">
                            <input type='text' name="name" id="txtName" class="form-control" readonly />
                        </div>
                    </div>
                    <div class="form-group" style="display: none">
                        <label for="txtWcId" class="col-md-2 control-label">微信ID</label>
                        <div class="col-md-10">
                            <input type='text' name="userId" id="txtWcId" class="form-control" readonly />
                        </div>
                    </div>

                    <div class="form-group">
                        <div class="col-md-offset-2 col-md-10">
                            <input type='button' id="submit" value="绑定" class="btn btn-primary btn-block" />
                            <input type='button' id="close" value="关闭" class="btn btn-info btn-block" style="display: none" />
                        </div>
                    </div>
                </div>
            </section>

            <p id="info" style="color: red"></p>
        </div>
    </div>

    <script>
        function showError(errmsg, info) {
            ZENG.msgbox.show(errmsg, 5);
            if (info) {
                $('#info').html(info);
                $('#info').show();
                $('#info').css('color', 'red')
            } else {
                $('#info').html('');
                $('#info').hide();
            }
            $('#submit').attr('disabled', true)
        }

        function setUserInfo(userinfo) {
            userinfo = JSON.parse(userinfo);
            $('#txtPhone').val(userinfo.mobile);
            $('#txtName').val(userinfo.name);
            $('#txtWcId').val(userinfo.userId);
            if (userinfo.bind == "0") {
                $('#submit').attr('disabled', false);
                $('#info').css('color', 'red');
                $('#info').hide();
                $('#close').hide();
            } else {
                $('#info').html("当前微信已绑定!");
                $('#info').show();
                $('#info').css('color', 'green')
                $('#submit').hide();
                $('#close').show();
            }
        }

        $('#close').click(function () {
            wx.closeWindow();
        });

        $('#submit').click(function () {
            var formStr = $(document.forms).serialize();
            var form = {};
            formStr.split("&").forEach(function (ele) {
                var arr = ele.split("=");
                if (arr[0].indexOf("__") < 0) {
                    form[arr[0]] = decodeURIComponent(arr[1])
                }
            })
            if (Object.keys(form) <= 0) {
                return ZENG.msgbox.show('绑定数据不完整，请重试...', 5);
            }
            var arr = [];
            for (var v in form) {
                arr.push(form[v]);
            }
            if (arr.some(function (ele) { return ele == "" })) {
                return ZENG.msgbox.show('绑定数据不完整，请重试...', 5);
            }
            ZENG.msgbox.show('正在提交，请稍后...', 6);
            $.post('./proxy?action=binding', form, function (res) {
                res = JSON.parse(res);
                ZENG.msgbox._hide();
                ZENG.msgbox.show(res.msg, res.state == "success" ? 4 : 5);
                if (res.state == "success") {
                    //$(document.forms)[0].reset(); 
                    $('#info').html(res.msg);
                    $('#info').show();
                    $('#info').css('color', 'green')
                    $('#submit').hide();
                    $('#close').show();
                }
            })
        });
    </script>
</asp:Content>
