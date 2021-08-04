<%@ Page Title="采集" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="Scan2Report.Report" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <%--<h2><%: Title %></h2>--%>

    <div class="form-horizontal"> 
        <div class="form-group">
            <label for="txtMachine" class="col-md-2 control-label">机台号</label>
            <div class="col-md-10">
                <div style="display: inline-flex; width: 100%">
                    <input name="machine" type="text" id="txtMachine" class="form-control" readonly />
                    <input type="button" id="scan-machine" value="扫描" class="btn btn-sm btn-info btn-scan" />
                </div>
            </div>
        </div>

        <div class="form-group">
            <label for="txtMould" class="col-md-2 control-label">模具号</label>
            <div class="col-md-10">
                <div style="display: inline-flex; width: 100%">
                    <input type="text" name="mould" id="txtMould" class="form-control" readonly />
                    <input type="button" id="scan-mould" value="扫描" class="btn btn-sm btn-info btn-scan" />
                </div>
            </div>
        </div>

        <div class="form-group">
            <label for="txtCurProcess" class="col-md-2 control-label">当前阶段</label>
            <div class="col-md-10">
                <input type="text" id="txtCurProcess" class="form-control" readonly />
            </div>
        </div>

        <div class="form-group">
            <label for="txtBegin" class="col-md-2 control-label">开始时间</label>
            <div class="col-md-10">
                <input type="text" id="txtBegin" class="form-control" readonly />
            </div>
        </div>

        <input type="text" name="date" id="txtDate" readonly hidden="hidden" />
        <input type="text" name="status" id="txtNextStatus" readonly hidden="hidden" />
        <input type="text" name="type" id="txtNextType" readonly hidden="hidden" />

        <div class="form-group" id="end-group" style="display: none">
            <label for="Txt_End" class="col-md-2 control-label">结束时间</label>
            <div class="col-md-10">
                <input type="text" id="txtEnd" class="form-control" readonly />
            </div>
        </div>

        <div class="form-group">
            <label for="txtUserName" class="col-md-2 control-label">当前登录人</label>
            <div class="col-md-10">
                <input type="text" id="txtUserName" class="form-control" readonly />
            </div>
        </div>

        <div class="form-group col-sm-8" style="padding-top: 7px; display: none" id="check-group">
            <div class="checkbox-custom checkbox-default">
                <input type="checkbox" name="ispause" id="txtIsPause">
                <label for="txtIsPause" style="font-weight: bold;">是否暂停</label>
            </div>
        </div>


        <div class="form-group">
            <div class="col-md-offset-2 col-md-10">
                <input type="button" id="submit" value="提交" class="btn btn-warning btn-block btn-huge" />
            </div>
        </div>

        <fieldset>
            <legend>扫描记录</legend>
            <table>
                <thead>
                    <tr>
                        <th>序号</th>
                        <th>阶段</th>
                        <th>时间</th>
                        <th>记录</th>
                    </tr>
                </thead>
                <tbody id="tbody">
                    <tr>
                        <td colspan="4">暂无记录</td>
                    </tr>
                </tbody>
            </table>
        </fieldset>
    </div>

    <script>
        $('.btn-scan').unbind().bind("click", function (e) {
            const dom = e.currentTarget.id;
            wx.scanQRCode({
                desc: 'scanQRCode desc',
                needResult: 1,
                scanType: ["qrCode", "barCode"],
                success: function (res) {
                    if (res.errMsg.indexOf("ok") > -1) {
                        scanCallBack(res.resultStr, dom);
                    } else {
                        showError(res.errMsg);
                    }
                },
                error: function (res) {
                    if (res.errMsg.indexOf('function_not_exist') > 0) {
                        showError('版本过低请升级','forbidden');
                    }
                }
            });
        });

        function scanCallBack(res, dom) {
            if (dom.indexOf('mould') > -1) {
                $('#txtMould').val(res)
                showSuccess("扫描成功");
            } else if (dom.indexOf('machine') > -1) {
                $('#txtMachine').val(res)
                ZENG.msgbox.show('正在检索，请稍后...', 6);
                $.get("./proxy", {
                    machine: res,
                    action: "query"
                }, function (res) {
                    ZENG.msgbox._hide();
                    res = JSON.parse(res);
                    if (res.state == "success") {
                        var data = JSON.parse(res.data);
                        var info = data.a;
                        var list = data.b;
                        if (info.length > 0) {
                            info = info.pop();
                            setform(info)
                            showSuccess("扫描成功");
                            $('#tbody').empty();
                            if (list.length > 0) {
                                var str = buildTableBody(list);
                                $('#tbody').append(str);
                            }
                        } else {
                            showError(res.msg);
                        }
                    } else {
                        showError(res.msg);
                    }
                })
            }
        };

        function setform(res) {
            $('#txtCurProcess').val(res.FName);
            $('#txtNextStatus').val(res.FNextStatus);
            $('#txtNextType').val(res.FNextType);
            $('#txtDate').val(res.FDate);
            $('#txtMould').val(res.FMould);

            if (res.FNextType == "2") {  //结束采集
                $('#end-group').show();
                $('#txtBegin').val(res.FBeginDate); //本道工序开始时间
                $('#txtEnd').val(res.FDate);//本道工序结束时间（当前时间）
            } else {  //开始采集
                $('#end-group').hide();
                $('#txtBegin').val(res.FDate);//本道工序开始时间（当前时间） 
            }

            if (res.FMould != "") {
                $('#scan-mould').hide()
            } else {
                $('#scan-mould').show()
            }

            if (res.FPermitPause == "False") {
                $('#txtIsPause').hide()
                $('#check-group').hide()
            } else {
                $('#check-group').show()
                $('#txtIsPause').show()
                $('#txtIsPause').attr("checked", false)
            }
        }

        function buildTableBody(rows) {
            var str = ""
            rows.forEach(function (row, index) {
                str += ("<tr>" +
                    "    <td rowspan='2'>" + (index + 1) + "</td>" +
                    "    <td rowspan='2'>" + row.FStep + "</td>" +
                    "    <td>" + row.FBeginDate + "</td>" +
                    "    <td>" + row.FMould + "</td>" +
                    "</tr>" +
                    "<tr>" +
                    "    <td>" + row.FEndDate + "</td>" +
                    "    <td>" + row.FOperator + "</td>" +
                    "</tr>")
            })
            return str;
        }

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
                return showError('采集数据不完整，请重试...');
            }
            form["ispause"] = $('#txtIsPause').is(':checked') ? "1" : "0"
            var arr = [];
            for (var v in form) {
                arr.push(form[v]);
            }
            if (arr.some(function (ele) { return ele == "" })) {
                return showError('采集数据不完整，请重试...');
            }
            ZENG.msgbox.show('正在提交，请稍后...', 6);
            $.post('./proxy?action=save', form, function (res) {
                res = JSON.parse(res);
                ZENG.msgbox._hide();
                if (res.state == "success") {
                    showSuccess(res.msg, function () {
                        $(document.forms)[0].reset()
                        $('#tbody').empty();
                        $("#txtUserName").val("<%=CurUserName%>");
                        var list = res.data
                        if (list.length > 0) {
                            list = JSON.parse(res.data)
                            var str = buildTableBody(list);
                            $('#tbody').append(str);
                        }
                    });
                } else {
                    showError(res.msg);
                }
            })
        });

        function forbidden() {
            $('#submit').hide();
            $('.btn-scan').hide();
        }

        $(function () {
            var name = "<%=CurUserName%>";
            if (name == "") {
                name = "未获取";
                showError('您可能还未绑定用户，请先绑定...详情:请到绑定用户模块核实!', 'forbidden');
            }
            $("#txtUserName").val(name);
        });
    </script>
    <style>
        table, table tr th, table tr td {
            border: 1px solid #333;
            text-align: center;
        }

        table {
            width: 100%;
            min-height: 25px;
            line-height: 25px;
            border-collapse: collapse;
            padding: 2px;
        }
    </style>
</asp:Content>
