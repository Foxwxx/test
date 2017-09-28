using System;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Linq;
using System.Data;
using DIYGENS.COM.DBLL.Mssql;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;

namespace HubBill
{
    /// <summary>
    /// 此項目在lbs.foxconn.com上部署.
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class HUBBill : System.Web.Services.WebService
    {
        private static string strCnnLBS = ConfigurationManager.ConnectionStrings["LBSUse"].ConnectionString;

        //[WebMethod(Description = "Test")]
        //public DataTable TestTransHUB()
        //{
        //    DataSet ds = new DataSet();
        //    ds.ReadXml(@"D:\XML\VMI\161222-636179951928035666.xml");
        //    DataTable bk = TransHUB(ds);
        //    return bk;
        //}

        public bool ValidCode(string code)
        {
            string strsql = "exec usp_TransVMIData_Single @ActionType='CheckTransData',@code='" + code + "'";
            string CheckResult = new MssqlDAL(strCnnLBS, strsql).Rows[0].ToString();
            return CheckResult == "Y" ? true : false;
        }
        
        public DataTable IsMax(string code)
        {
            string strsql = "exec usp_TransVMIData_Single @ActionType='GetDataType',@code='" + code + "'";
            DataTable CheckResult = new MssqlDAL(strCnnLBS, strsql).GetDataTable();
            return CheckResult;
        }

        [WebMethod(Description = "return datatable")]
        public DataTable TransHUB(DataSet ds)
        {
            string ARAPFlag = "", MaxFlag = "";
            int header = 0, body = 0;
            
            DataTable DT_R = new DataTable();
            DT_R.TableName = "Result";
            DT_R.Columns.Add("FeeBillNo", typeof(string));
            DT_R.Columns.Add("Result", typeof(string));
            DT_R.Columns.Add("Msg", typeof(string));
            if (ds.Tables.Count < 2)
            {
                DataRow dr = DT_R.NewRow();
                dr[0] = "Error"; dr[1] = "N";
                dr[2] = "缺少對帳單或台賬信息";
                DT_R.Rows.Add(dr);
                return DT_R;
            }
            else if (ds.Tables[0] == null || ds.Tables[1] == null || ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0)
            {
                DataRow dr = DT_R.NewRow();
                dr[0] = "Error"; dr[1] = "N";
                dr[2] = "對帳單或台賬信息為空";
                DT_R.Rows.Add(dr);
                return DT_R;
            }
            else
            {
                string xml = @"D:\XML\" + DateTime.Today.ToString("yyMMdd") + "-" + DateTime.Now.Ticks.ToString() + ".xml";
                ds.WriteXml(xml);

                DataTable TF = ds.Tables[0];
                DataTable TA = ds.Tables[1];
                //檢查對帳單是否有已拋轉數據
                foreach (DataRow df in TF.Rows)                
                {
                    DataRow dr = DT_R.NewRow();
                    string code = df["BillNo"].ToString().Trim();
                    ARAPFlag = df["ARAPFlag"].ToString().Trim();
                    
                    if (ValidCode(code))
                    {
                        dr[0] = code;
                        dr[1] = "N";
                        dr[2] = "資料已拋轉";
                        DT_R.Rows.Add(dr);
                    }
                    else
                    {
                        //分離正常數據
                        //drCheck = IsMax(code).Rows[0];
                        MaxFlag = "N"; //drCheck[1].ToString();

                        if (MaxFlag == "N")
                        {
                            string strSQL = @"SELECT code,totalfee,currency,handledate,ARAPFLAG,DataFrom,description,creator,payer_legal_entity_code,payer_legal_entity_name,
                                            receiver_legal_entity_code,receiver_legal_entity_name,receiver_charge_no,MyPayerCode,MyReceiverCode,Status FROM dbo.ID_ReconciliationHeader where 1=0";
                            string strSQLA = @" select Fee_Bill_Code,code,quantity,price, unit,amount,CHARGE_ITEM_Code,CHARGE_ITEM_Name,DESCRIPTION,occur_date,tax_point,iftax,service_code,AccountType  FROM dbo.ID_ReconciliationDetail where 1=0";

                            DataTable MDA = new MssqlDAL(strCnnLBS, strSQL).GetDataTable();
                            DataTable MDB = new MssqlDAL(strCnnLBS, strSQLA).GetDataTable();

                            DataRow drFeeBill = MDA.NewRow();
                            #region 正常對帳單資料插入 
                            //BillNo	totalfee	currency	handledate	ARAPFLAG	DataFrom	description	creator	createdtime	PayerCode	PayerName	ReceiverCode	ReceiverName	ReceiverChargeNo
                            drFeeBill.ItemArray = new string[] 
                                                        { df["BillNo"].ToString().Trim(),
                                                            df["totalfee"].ToString().Trim(),
                                                            df["currency"].ToString().Trim(),
                                                            df["handledate"].ToString().Trim(),
                                                            df["ARAPFLAG"].ToString().Trim(),
                                                            df["DataFrom"].ToString().Trim(),
                                                            df["description"].ToString().Trim(),
                                                            df["creator"].ToString().Trim(),
                                                            //df["createdtime"].ToString().Trim(),
                                                            df["PayerCode"].ToString().Trim(),
                                                            df["PayerName"].ToString().Trim(),
                                                            df["ReceiverCode"].ToString().Trim(),
                                                            df["ReceiverName"].ToString().Trim(),
                                                            df["ReceiverChargeNo"].ToString().Trim(),
                                                            df["PayerCode"].ToString().Trim(),
                                                            df["ReceiverCode"].ToString().Trim(),"RECONCILIATION"
                                                        };
                            MDA.Rows.Add(drFeeBill);

                            //BillNo	code	amount	itemCode	itemName	DESCRIPTION	occur_date	tax_point	iftax	service_code
                            DataRow[] TARows = TA.Select("BillNo='" + code.Trim()+"'");
                            foreach (DataRow da in TARows)
                            {
                                DataRow drAccount = MDB.NewRow();
                                drAccount.ItemArray = new object[] { 
                                                                code,       //注意單身必須插入此關聯欄位
                                                                da["code"].ToString().Trim(),
                                                                1,
                                                                da["amount"].ToString().Trim(),
                                                                "項",
                                                                da["amount"].ToString().Trim(),
                                                                da["itemCode"].ToString().Trim(),
                                                                da["itemName"].ToString().Trim(),
                                                                da["description"].ToString().Trim(),
                                                                da["occur_date"].ToString().Trim(),
                                                                da["tax_point"].ToString().Trim(),
                                                                da["iftax"].ToString().Trim()=="1"?true:false,
                                                                da["service_code"].ToString().Trim(),da["service_code"].ToString().Trim()
                                                                };
                                MDB.Rows.Add(drAccount);
                            }

                            try
                            {
                                SqlDataAdapter SDA = new SqlDataAdapter(strSQL, strCnnLBS);
                                SqlCommandBuilder SCA = new SqlCommandBuilder(SDA);
                                if (SDA.Update(MDA) > 0)
                                {
                                    SqlDataAdapter SDB = new SqlDataAdapter(strSQLA, strCnnLBS);
                                    SqlCommandBuilder SCB = new SqlCommandBuilder(SDB);
                                    body = SDB.Update(MDB);
                                    if (body > 0 && body == TARows.Length)
                                    {
                                        dr[0] = code;
                                        dr[1] = "Y";
                                        DT_R.Rows.Add(dr);
                                    }
                                    else
                                    {
                                        //台賬數據插入失敗，刪除已插入的對帳單                                       
                                        string strdel = "exec usp_TransVMIData_Single @ActionType='DeleteTransData',@code='" + code + "'";
                                        bool bk = new MssqlDAL(strCnnLBS, strdel).UpdateSingle();
                                        dr[0] = code;
                                        dr[1] = "N";
                                        dr[2] = "台賬數據插入失敗";
                                        DT_R.Rows.Add(dr);
                                    }
                                }
                                else
                                {
                                    dr[0] = code;
                                    dr[1] = "N";
                                    dr[2] = "對帳單插入失敗";
                                    DT_R.Rows.Add(dr);
                                }
                            }
                            catch (Exception ex)
                            {
                                dr[0] = code;
                                dr[1] = "N";
                                dr[2] = ex.Message;
                                DT_R.Rows.Add(dr);
                            }

                            #endregion
                        }
                        //分離超額數據,暫不啟用
                        else
                        {
                            string strSQLB = @"SELECT IdeasID as id,code,totalfee,currency,handledate,status,ARAPFLAG,DataFrom,description,creator,createdtime,lastoperator,	
				                     updatetime,	payer_legal_entity_code,payer_legal_entity_name,receiver_legal_entity_code,	receiver_legal_entity_name,	receiver_charge_no  FROM dbo.ID_BillHeader  where 1=0";
                            string strSQLC = @"select id, code,  Fee_Bill_Code, bill_type, quantity,price, unit, amount, DESCRIPTION, occur_date, arap_date, settle_date, settle_type,fee_bill_id,
                                     tax_point,iftax,service_code, status  FROM dbo.ID_BillDetail  where 1=0";

                            DataTable MDC = new MssqlDAL(strCnnLBS, strSQLB).GetDataTable();
                            DataTable MDD = new MssqlDAL(strCnnLBS, strSQLC).GetDataTable();
                            #region    超限對帳單數據
                            DataRow drBillHeader = MDC.NewRow();
                            drBillHeader.ItemArray = new string[] 
                                                        {   df["id"].ToString().Trim(),
                                                            df["code"].ToString().Trim(),
                                                            df["total_fee"].ToString().Trim(),
                                                            df["currency"].ToString().Trim(),
                                                            df["handle_date"].ToString().Trim(),
                                                            df["status"].ToString().Trim(),
                                                            ARAPFlag,
                                                            "HUB",
                                                            df["description"].ToString().Trim(),
                                                            df["creator"].ToString().Trim(),
                                                            df["created_time"].ToString().Trim(),
                                                            df["last_operator"].ToString().Trim(),
                                                            df["update_time"].ToString().Trim(),
                                                            df["payer_legal_entity_code"].ToString().Trim(),
                                                            df["payer_legal_entity_name"].ToString().Trim(),
                                                            df["receiver_legal_entity_code"].ToString().Trim(),
                                                            df["receiver_legal_entity_name"].ToString().Trim(),
                                                            df["receiver_charge_no"].ToString().Trim() };
                            MDC.Rows.Add(drBillHeader);
                            DataRow[] TARows = TA.Select("fee_bill_id=" + df["id"].ToString().Trim());
                            foreach (DataRow da in TARows)
                            {
                                DataRow drBillDetail = MDD.NewRow();
                                drBillDetail.ItemArray = new object[] { 
                                                                da["id"].ToString().Trim(),
                                                                da["code"].ToString().Trim(),
                                                                code,       //注意單身必須插入此關聯欄位
                                                                da["bill_type"].ToString().Trim(),
                                                                da["quantity"].ToString().Trim(),
                                                                da["price"].ToString().Trim(),
                                                                da["unit"].ToString().Trim(),
                                                                da["amount"].ToString().Trim(),
                                                                da["description"].ToString().Trim(),
                                                                da["occur_date"].ToString().Trim(),
                                                                da["arap_date"].ToString(),
                                                                da["settle_date"].ToString(),
                                                                da["settle_type"].ToString().Trim(),
                                                                da["fee_bill_id"].ToString().Trim(),
                                                                da["tax_point"].ToString().Trim(),
                                                                da["iftax"].ToString().Trim()=="1"?true:false,
                                                                da["service_code"].ToString().Trim(),
                                                                da["status"].ToString().Trim()
                                                                };
                                MDD.Rows.Add(drBillDetail);
                            }
                            try
                            {
                                SqlDataAdapter SDC = new SqlDataAdapter(strSQLB, strCnnLBS);
                                SqlCommandBuilder SCC = new SqlCommandBuilder(SDC);
                                if (SDC.Update(MDC) > 0)
                                {
                                    SqlDataAdapter SDD = new SqlDataAdapter(strSQLC, strCnnLBS);
                                    SqlCommandBuilder SCD = new SqlCommandBuilder(SDD);
                                    body = SDD.Update(MDD);
                                    if (body > 0 && body == TARows.Length)
                                    {
                                        dr[0] = code;
                                        dr[1] = "Y";
                                        DT_R.Rows.Add(dr);
                                    }
                                    else
                                    {
                                        //台賬數據插入失敗，刪除已插入的對帳單                                       
                                        string strdel = "exec usp_TransVMIData_Single @ActionType='DeleteBillData',@code='" + code + "'";
                                        bool bk = new MssqlDAL(strCnnLBS, strdel).UpdateSingle();
                                        dr[0] = code;
                                        dr[1] = "N";
                                        dr[2] = "台賬數據插入失敗";
                                        DT_R.Rows.Add(dr);
                                    }
                                }
                                else
                                {
                                    dr[0] = code;
                                    dr[1] = "N";
                                    dr[2] = "對帳單插入失敗";
                                    DT_R.Rows.Add(dr);
                                }
                            }
                            catch (Exception ex)
                            {
                                dr[0] = code;
                                dr[1] = "N";
                                dr[2] = ex.Message;
                                DT_R.Rows.Add(dr);
                            }

                            #endregion
                        }

                    }

                }
                return DT_R;
            }


        }

        [WebMethod(Description = "return base data list,type=customer/supplier")]
        public DataTable TransBasicData(string type)
        {
            DataTable dt = new DataTable();
            dt.TableName = type;

            string strsql = "exec usp_TransVMIData_Single @ActionType='getBasicData',@code='" + type + "'";
            dt = new MssqlDAL(strCnnLBS, strsql).GetDataTable();
            return dt;
        }

        [WebMethod(Description = "Test HKHub TransData")]
        public DataTable TestTransHUB()
        {
            string strXMLFile = @"D:\170407-636271843917553184.xml";
            DataSet ds = new DataSet();
            ds.ReadXml(strXMLFile);
            DataTable dt = TransHUB(ds);
            dt.TableName = "TransHUB";
            return dt;
        }
    }
}
