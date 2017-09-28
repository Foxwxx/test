using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Util;
using System.Data;
using System.IO;
using System.Configuration;


namespace LBSSendMail
{
    class MailMethod
    {
        public static string subjectTemplate = "物流費用結算平臺--AR立賬逾期提醒";


        private static string subject = "Logistics AR/AP PlatForm Report to";

        private const string bodyStyle = @"<html><head><title></title>
                                            <style>
                                                body {background-color:#D4E8FC;} 
                                                td, th, h3, h6 {color: #330000;}
                                                body,h1, h2, h4, h5 {color: #660000;}
                                                a {color: #003366;}
                                                p {font-size: 12px;}
                                                .name {font-family:新細明體;font-size:smaller;color:Black;}
                                            </style></head><body>";

        private const string bodyEnd = @"<p><br /><br /><br /><br /><br /><br /><a href='http://lbs.foxconn.com'><font size='3' color='blue'>Logistics AR/AP PlatForm</font></a></p>
                                            <p>Foxconn Group CFAG-IT-<span style='font-size:10px;color:#37605e;'>王星星 ，Email：CFA-IT-WEB/CEN/FOXCONN</span></p>
                                            </body></html>";


        public static bool SendMail(IList<ARAccountInfo> table, string mailto)
        {
            string body = GetContent(table);
            string content = bodyStyle + body + bodyEnd;
            MailSvc.Service1 mail = new MailSvc.Service1();
            bool bk = mail.SendMail(mailto, "", "", "LBS_system@foxconn.com", subjectTemplate, content);
            return bk;
            
        }

        private static string GetContent(IList<ARAccountInfo> table)
        {
            if (table.Count<=0)
            {
                return "There have no data to send today.";
            }
            string[] header = { "序號", "預立賬單號", "預立賬日期", "正式立賬單號", "到期日", "付款法人", "收款法人", "幣別", "總金額", "實收金額" };
            StringBuilder builder = new StringBuilder("<table style='border:1px solid #aaa' width='1200px'>");
            builder.Append("<tr><td align='center' colspan='10'><font color='blue'>即將到期立賬清單</font></td></tr>");
            builder.Append("<tr>");
            for (int i = 0; i < header.Length; i++)
            {
                builder.Append("<td>" + header[i] + "</td>");
            }
            builder.Append("</tr>");
            int j = 1;
            foreach (ARAccountInfo ario in table)
            {
                builder.Append("<tr>");
                builder.Append(string.Format("<td>{0}</td>", j.ToString()));
                builder.Append(string.Format("<td>{0}</td>", ario.oma01));
                builder.Append(string.Format("<td>{0}</td>", ario.oma02));
                builder.Append(string.Format("<td>{0}</td>", ario.oma01Tiptop));
                builder.Append(string.Format("<td>{0}</td>", ario.oma12));
                builder.Append(string.Format("<td>{0}</td>", ario.oma032));
                builder.Append(string.Format("<td>{0}</td>", ario.Collectee));
                builder.Append(string.Format("<td>{0}</td>", ario.oma23));
                builder.Append(string.Format("<td width='100px'>{0}</td>", ario.oma54t));
                builder.Append(string.Format("<td width='100px'>{0}</td>", ario.RcvAmount));
                builder.Append("</tr>");

                j++;
            }
            builder.Append("</table>");
            string strFilePath = @"D:\work\" + DateTime.Today.ToString("yyyyMMdd") + "_MailText.txt";
            File.AppendAllText(strFilePath, builder.ToString());
            return builder.ToString();
        }

    }
}
