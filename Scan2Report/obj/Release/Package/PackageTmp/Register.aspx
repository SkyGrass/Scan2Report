<%@ Page Title="用户绑定" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="Scan2Report.Register" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
<%--    <h2><%: Title %></h2>--%>

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
                            <input type='button' id="submit" value="绑定" class="btn btn-primary btn-block btn-huge"/>
                            <input type='button' id="close" value="关闭" class="btn btn-info btn-block btn-huge" style="display: none" />
                        </div>
                    </div>
                </div>
            </section>
        </div>
    </div>

    <script>
        function forbidden() {
            $('#submit').attr('disabled', true)
        }

        function setUserInfo(userinfo) {
            userinfo = JSON.parse(userinfo);
            $('#txtPhone').val(userinfo.mobile);
            $('#txtName').val(userinfo.name);
            $('#txtWcId').val(userinfo.userId);
            if (userinfo.bind == "0") {
                $('#submit').attr('disabled', false);
                $('#close').hide();
            } else {
                showSuccess("当前微信已绑定!", function () {
                    $('#submit').hide();
                    $('#close').show();
                })
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
                return showError('绑定数据不完整，请重试...');
            }
            var arr = [];
            for (var v in form) {
                arr.push(form[v]);
            }
            if (arr.some(function (ele) { return ele == "" })) {
                return showError('绑定数据不完整，请重试...');
            }
            ZENG.msgbox.show('正在提交，请稍后...', 6);
            $.post('./proxy?action=binding', form, function (res) {
                res = JSON.parse(res);
                ZENG.msgbox._hide();
                if (res.state == "success") {
                    showSuccess(res.msg, function () {
                        $('#submit').hide();
                        $('#close').show();
                    });
                } else {
                    showError(res.msg)
                }
            })
        });
    </script>
</asp:Content>
