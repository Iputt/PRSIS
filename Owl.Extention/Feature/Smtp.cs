using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Text.RegularExpressions;
using System.Configuration;

namespace Owl.Feature
{
    /// <summary>
    /// 邮箱类型
    /// </summary>
    public enum EmailType
    {
        /// <summary>
        /// Google 的网络邮件服务
        /// </summary>
        Gmail,
        /// <summary>
        /// HotMail/Live
        /// </summary>
        HotMail,
        /// <summary>
        /// QQ/FoxMail（Foxmail被腾讯收购）
        /// </summary>
        QQ_FoxMail,

        /// <summary>
        /// 腾讯企业油箱
        /// </summary>
        QQ_ExMail,
        /// <summary>
        /// 网易126
        /// </summary>
        Mail_126,
        /// <summary>
        /// 网易163
        /// </summary>
        Mail_163,
        /// <summary>
        /// 新浪邮箱
        /// </summary>
        Sina,
        /// <summary>
        /// Tom
        /// </summary>
        Tom,
        /// <summary>
        /// 搜狐邮箱
        /// </summary>
        SoHu,
        /// <summary>
        /// 雅虎邮箱
        /// </summary>
        Yahoo
    }

    public class SmtpAttach
    {
        public string Name { get; set; }

        public Stream Content { get; set; }

        public SmtpAttach(string name, Stream content)
        {
            Name = name;
            Content = content;
        }
    }

    public class SmtpSection : Impl.Config.Section
    {

        /// <summary>
        /// 主机地址
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// 服务端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 是否启用ssl
        /// </summary>
        public bool EnableSsl { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 环境标记：开发环境、测试环境、生产环境 DEV QAS PRD
        /// </summary>
        public string EnvironmentTag { get; set; }

        Smtp _smtp;
        public Smtp Smtp
        {
            get
            {
                if (_smtp == null)
                {
                    _smtp = Smtp.Create(Host, Port, EnableSsl, UserName, Password, EnvironmentTag);
                }
                return _smtp;
            }
        }
    }

    public enum SmtpFailedCode
    {
        TimeOut,
        Error
    }

    public class SmtpFailedRecipient
    {
        /// <summary>
        /// 收件人
        /// </summary>
        public string Recipient { get; private set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public SmtpFailedCode Code { get; private set; }

        public SmtpFailedRecipient(string recipient, SmtpFailedCode code)
        {
            Recipient = recipient;
            Code = code;
        }
    }

    /// <summary>
    /// smtp 帮助类
    /// </summary>
    public class Smtp
    {
        #region
        string m_host;
        int m_port;
        bool m_ssl;
        string m_username;
        string m_pwd;

        string m_from;
        //环境标记：开发环境、测试环境、生产环境 DEV QAS PRD
        string m_tag;
        /// <summary>
        /// 发送邮件地址
        /// </summary>
        public string FromAddress
        {
            get
            {
                if (string.IsNullOrEmpty(m_from) && !string.IsNullOrEmpty(m_username))
                {
                    if (m_username.Contains("@"))
                        m_from = m_username;
                    else
                        m_from = string.Format("{0}@{1}", m_username, m_host.Replace("smtp.", ""));
                }
                return m_from;
            }
            set { m_from = value; }
        }

        /// <summary>
        /// 发送邮件显示名称
        /// </summary>
        public string DisplayName { get; set; }
        #endregion

        static Smtp()
        {
            ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
        }

        private Smtp()
        {

        }

        /// <summary>
        /// 默认的smtp客户端
        /// </summary>
        public static Smtp Default
        {
            get
            {
                return Config.Section<SmtpSection>().Smtp;
            }
        }

        /// <summary>
        /// 创建smtp
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="enablessl"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Smtp Create(string host, int port, bool enablessl, string username, string password, string environmenttag)
        {
            return new Smtp() { m_host = host, m_port = port, m_ssl = enablessl, m_username = username, m_pwd = password, m_tag = environmenttag };
        }

        SmtpClient GetClient()
        {
            var client = new SmtpClient(m_host, m_port);
            client.EnableSsl = m_ssl;
            if (!string.IsNullOrEmpty(m_username))
            {
                client.Credentials = new NetworkCredential(m_username, m_pwd);
            }
            return client;
        }
        private static readonly Regex pattern = new Regex(@"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$");

        //增加参数 translations rest请求必要时传值
        MailMessage CreateMsg(string recipients, string subject, string body, IEnumerable<SmtpAttach> attach, Dictionary<string, string> translations = null)
        {
            if (string.IsNullOrEmpty(recipients))
                throw new ArgumentNullException("recipients");
            //环境标记：开发环境、测试环境、生产环境 DEV QAS PRD
            string environmentalSaid = string.Empty;
            string textReplay = string.Empty;
            string textRegards = string.Empty;
            //rest请求translations不为空
            //修改key值以及翻译内容，请修改对应的API代码
            if (translations != null)
            {
                if (m_tag == "QAS")
                {
                    environmentalSaid = translations.ContainsKey("owl.extention.feature.qas") ? translations["owl.extention.feature.qas"] : "[开发环境]";
                }
                else if (m_tag == "DEV")
                {
                    environmentalSaid = translations.ContainsKey("owl.extention.feature.dev") ? translations["owl.extention.feature.dev"] : "[测试环境]";
                }

                textReplay = translations.ContainsKey("owl.extention.feature.emailreplay") ? translations["owl.extention.feature.emailreplay"] : "此邮件为系统邮件，请勿回复。";
                
                //20200605 取消系统邮件祝福语
                //textRegards = translations.ContainsKey("owl.extention.feature.emailbestregards") ? translations["owl.extention.feature.emailbestregards"] : "顺颂商祺！";
                textRegards = "";
            }
            else
            {
                if (m_tag == "QAS")
                {
                    environmentalSaid = Translation.Get("owl.extention.feature.qas", "[测试环境]", true);
                }
                else if (m_tag == "DEV")
                {
                    environmentalSaid = Translation.Get("owl.extention.feature.dev", "[开发环境]", true);
                }
                //底层发送邮件模板，已翻译
                textReplay = Translation.Get("owl.extention.feature.emailreplay", "此邮件为系统邮件，请勿回复。", true);

                //20200605 取消系统邮件祝福语
                //textRegards = Translation.Get("owl.extention.feature.emailbestregards", "顺颂商祺！", true);
                textRegards = "";
            }
            subject = environmentalSaid + subject;
            MailMessage msg = new MailMessage()
            {
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body + "<br/><br/>&nbsp;&nbsp;&nbsp;&nbsp;" + textReplay + "<br/><br/>&nbsp;&nbsp;&nbsp;&nbsp;" + textRegards,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
            };
            msg.Headers.Add("X-Priority", "3");
            msg.Headers.Add("X-MSMail-Priority", "Normal");
            msg.Headers.Add("X-Mailer", "Microsoft Outlook Express 6.00.2900.2869");
            msg.Headers.Add("X-MimeOLE", "Produced By Microsoft MimeOLE V6.00.2900.2869");
            msg.Headers.Add("ReturnReceipt", "1");
            msg.From = new MailAddress(FromAddress, DisplayName, Encoding.UTF8);
            var address = recipients.Split('$');
            if (address.Length > 0)
            {
                foreach (var addr in address[0].Split(';'))
                {
                    if (!string.IsNullOrEmpty(addr) && pattern.IsMatch(addr))
                        msg.To.Add(new MailAddress(addr));
                }
            }
            if (address.Length > 1)
            {
                foreach (var addr in address[1].Split(';'))
                {
                    if (!string.IsNullOrEmpty(addr) && pattern.IsMatch(addr))
                        msg.CC.Add(new MailAddress(addr));
                }
            }
            if (attach != null)
            {
                foreach (var item in attach)
                {
                    msg.Attachments.Add(new Attachment(item.Content, item.Name));
                }
            }
            return msg;
        }
        IEnumerable<SmtpFailedRecipient> Convert(SmtpFailedRecipientsException ex)
        {
            List<SmtpFailedRecipient> recipients = new List<SmtpFailedRecipient>();
            for (int i = 0; i < ex.InnerExceptions.Length; i++)
            {
                SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;
                if (status == SmtpStatusCode.MailboxBusy ||
                 status == SmtpStatusCode.MailboxUnavailable)
                {
                    recipients.Add(new SmtpFailedRecipient(ex.InnerExceptions[i].FailedRecipient, SmtpFailedCode.TimeOut));
                }
                else
                {
                    recipients.Add(new SmtpFailedRecipient(ex.InnerExceptions[i].FailedRecipient, SmtpFailedCode.Error));
                }
            }
            return recipients;
        }
        IEnumerable<SmtpFailedRecipient> Send(MailMessage msg)
        {
            using (var client = GetClient())
            {
                try
                {
                    client.Send(msg);
                }
                catch (SmtpFailedRecipientsException ex)
                {
                    return Convert(ex);
                }
                finally
                {
                    msg.Dispose();
                }
            }
            return new SmtpFailedRecipient[0];
        }
        void client_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            (sender as SmtpClient).Dispose();
            (e.UserState as MailMessage).Dispose();
        }
        void SendAsync(MailMessage msg)
        {
            TaskMgr.StartTask(s => Send(s), msg);
            //var client = GetClient();
            //client.SendCompleted += new SendCompletedEventHandler(client_SendCompleted);
            //client.SendMailAsync(msg);
        }


        /// <summary>
        /// 同步发送电子邮件
        /// 增加参数 translations rest请求必要时传值
        /// </summary>
        /// <param name="recipients">收件人，多个收件人间用 ';' 分隔,$后面是抄送</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param> 
        /// <param name="translations">翻译内容，rest请求使用</param>
        /// <param name="attach">附件</param>
        public IEnumerable<SmtpFailedRecipient> Send(string recipients, string subject, string body, Dictionary<string, string> translations = null, params SmtpAttach[] attach)
        {
            return Send(CreateMsg(recipients, subject, body, attach, translations));
        }

        /// <summary>
        /// 异步发送包含附件的电子邮件
        /// </summary>
        /// <param name="recipients">收件人，多个收件人间用 ';' 分隔</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="body">邮件内容</param>
        /// <param name="attach">附件</param>
        public void SendAsync(string recipients, string subject, string body, params SmtpAttach[] attach)
        {
            SendAsync(CreateMsg(recipients, subject, body, attach));
        }
        /// <summary>
        /// smtp配置是否有效
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(m_host) && m_port != 0 && !string.IsNullOrEmpty(FromAddress);
        }
    }
}
