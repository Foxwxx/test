using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Util;
using System.Linq;
using System.Text;
using System.Data;
using DIYGENS.COM.DBLL.Mssql;
using System.IO;
using System.Windows.Forms;

namespace LBSSendMail
{
    class Program
    {
        static string strCnn = "server=10.134.154.176,3000;user id=jusda;password=jusda@Foxconn.com;database=JusdaRevenue;";
        static string strPro = "usp_ARAccount";
        static string strPath = Directory.GetParent(Application.StartupPath) + @"\sendlog\";
        static string strLogPath = strPath + @"\SendLog_" + DateTime.Today.ToString("yyyyMMdd") + ".txt";


        static void Main(string[] args)
        {
            File.AppendAllText(strLogPath, DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "開始準備資料...\r\n\r\n");
            GetAccountDataBF10();
            File.AppendAllText(strLogPath, DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "逾期數據郵件發送完畢...\r\n");

        }

        private static void GetAccountDataBF10()
        {
            IList<ARAccountInfo> ARIO = new List<ARAccountInfo>();
            try
            {
                string strSQL = "exec usp_ARAccount  @ActionType='GetAccountDataBF10'";
                MssqlDAL MD = new MssqlDAL(strCnn, strSQL);
                DataSet ds = MD.GetList();
                DataTable dtdata = ds.Tables[0];
                DataTable dtPerson = ds.Tables[1];
                foreach (DataRow dr in dtPerson.Rows)
                {
                    string UserID = dr["oma14"].ToString();
                    string UserName = dr["UserName"].ToString();
                    string Email = dr["Email"].ToString();
                    foreach(DataRow dra in dtdata.Rows)
                    {
                        string oma01 = dra["oma01"].ToString();
                        string oma02 = dra["oma02"].ToString();
                        string oma032 = dra["oma032"].ToString();
                        string Collectee = dra["Collectee"].ToString();
                        string oma14 = dra["oma14"].ToString();
                        string oma23 = dra["oma23"].ToString();
                        string oma54t = dra["oma54t"].ToString();
                        string RcvAmount = dra["RcvAmount"].ToString();
                        string oma01Tiptop = dra["oma01Tiptop"].ToString();
                        string oma12 = dra["oma12"].ToString();

                        if (oma14==UserID)
                        {
                            ARIO.Add(new ARAccountInfo() { 
                                oma01= oma01,
                                oma02=oma02,
                                oma032=oma032,
                                Collectee=Collectee,
                                oma14=oma14,
                                oma23=oma23 ,
                                oma54t=oma54t,
                                RcvAmount=RcvAmount,
                                oma01Tiptop=oma01Tiptop,
                                oma12=oma12
                            });
                            File.AppendAllText(strLogPath, oma01 + "準備發送...\r\n");
                        }
                    }
                    bool bk =MailMethod.SendMail(ARIO, Email);
                    if (bk)
                    {
                        File.AppendAllText(strLogPath, "以上數據發送成功...\r\n");
                    }
                    else
                    {
                        File.AppendAllText(strLogPath, "以上數據發送失敗...\r\n");
                    }
                    ARIO.Clear();
                }

            }
            catch (Exception ex)
            {
                File.AppendAllText(strLogPath, DateTime.Now.ToString("yyyy/MM/dd HH:mm") + "\r\n逾期數據郵件發送出現錯誤..." + ex.Message);
            }
        }

        
    }
}
