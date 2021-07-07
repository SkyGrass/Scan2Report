<%@ Page Title="报工" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="Scan2Report.Report" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <h2><%: Title %></h2>

    <div class="form-horizontal">
        <hr />

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
            <div class="col-md-offset-2 col-md-10">
                <input type="button" id="submit" value="提交" class="btn btn-warning btn-block" />
            </div>
        </div>

    </div>

    <script>
        $('.btn-scan').unbind().bind("click", function (e) {
            const dom = e.currentTarget.id;
            wx.scanQRCode({
                desc: 'scanQRCode desc',
                needResult: 1,
                scanType: ["qrCode", "barCode"],
                success: function (res) {
                    if (res.err_Info == "success") {
                        scanCallBack(res.resultStr, dom);
                    } else {
                        ZENG.msgbox.show(res.errMsg, 5);
                    }
                },
                error: function (res) {
                    if (res.errMsg.indexOf('function_not_exist') > 0) {
                        ZENG.msgbox.show('版本过低请升级', 5);
                    }
                }
            });
        });

        function scanCallBack(res, dom) {
            if (dom.indexOf('mould') > -1) {
                $('#txtMould').val(res)
                ZENG.msgbox.show('正在查询模具号，请稍后...', 6);
                $.get("./proxy", {
                    mould: $('#txtMould').val(),
                    action: "query"
                }, function (res) {
                    ZENG.msgbox._hide();
                    res = JSON.parse(res);
                    if (res.state == "success") {
                        res = res.data;
                        if (res.length > 0) {
                            res = res.pop();
                            $('#txtCurProcess').val(res.FName);
                            $('#txtNextStatus').val(res.FNextStatus);
                            $('#txtNextType').val(res.FNextType);
                            $('#txtDate').val(res.FDate);
                            if (res.FNextType == "2") {  //结束报工
                                $('#end-group').show();
                                $('#txtBegin').val(res.FBeginDate); //本道工序开始时间
                                $('#txtEnd').val(res.FDate);//本道工序结束时间（当前时间）
                            } else {  //开始报工
                                $('#end-group').hide();
                                $('#txtBegin').val(res.FDate);//本道工序开始时间（当前时间） 
                            }
                        } else {
                            ZENG.msgbox.show(res.msg, 1);
                        }
                    } else {
                        ZENG.msgbox.show(res.msg, 1);
                    }
                })
            } else if (dom.indexOf('machine') > -1) {
                $('#txtMachine').val(res)
            }
        };

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
                return ZENG.msgbox.show('报工数据不完整，请重试...', 5);
            }
            var arr = [];
            for (var v in form) {
                arr.push(form[v]);
            }
            if (arr.some(function (ele) { return ele == "" })) {
                return ZENG.msgbox.show('报工数据不完整，请重试...', 5);
            }
            ZENG.msgbox.show('正在提交，请稍后...', 6);
            $.post('./proxy?action=save', form, function (res) {
                res = JSON.parse(res);
                ZENG.msgbox._hide();
                ZENG.msgbox.show(res.msg, res.state == "success" ? 4 : 5);
                if (res.state == "success") {
                    $(document.forms)[0].reset();
                }
            })
        });
    </script>
</asp:Content>
