using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Tools
{
    public class MailUtils
    {
        public static bool SendEmail(string mailSubject, string mailBody, string filePath)
        {
            try
            {
                MailAddress to = new MailAddress(ReportSetting.getInstance().MailTo);
                MailAddress from = new MailAddress(ReportSetting.getInstance().MailFrom);
                MailMessage message = new MailMessage(from, to);
                message.Subject = mailSubject;
                message.Body = mailBody;

                if (!String.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    Attachment att = new Attachment(filePath);
                    message.Attachments.Add(att);
                }
                SmtpClient smtp = new SmtpClient();
                smtp.Host = ReportSetting.getInstance().SmtpHost;
                if (!string.IsNullOrEmpty(ReportSetting.getInstance().MailUser)
                    && !string.IsNullOrEmpty(ReportSetting.getInstance().MailPwd))
                {
                    smtp.Credentials = new NetworkCredential(ReportSetting.getInstance().MailUser,
                        ReportSetting.getInstance().MailPwd);
                }

                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
                return true;
            }
            catch (SmtpFailedRecipientsException ex)
            {
                Logger.Logger.GetLogger(typeof(MailUtils).Name).Error(string.Format("发送附件错误，错误原因：{0}", ex.Message));
            }
            catch (SmtpException ex)
            {
                Logger.Logger.GetLogger(typeof(MailUtils).Name).Error(string.Format("发送邮件错误，错误代码：{0}，错误描述：{1}"
                    , ex.StatusCode.ToString(), ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Logger.Logger.GetLogger(typeof(MailUtils).Name).Error(string.Format("发送邮件错误，错误代码：{0}，错误描述：{1}"
    , ex.Data, ex.Message));
            }
            catch (IndexOutOfRangeException ex)
            {
                Logger.Logger.GetLogger(typeof(MailUtils).Name).Error(string.Format("发送邮件错误，错误代码：{0}，错误描述：{1}"
    , ex.Data, ex.Message));
            }
            catch (System.Exception ex)
            {
                Logger.Logger.GetLogger(typeof(MailUtils).Name).Error(string.Format("未知错误，错误描述：{0}", ex.Message));
            }
            return false;
        }
    }
}
