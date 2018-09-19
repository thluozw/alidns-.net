using Aliyun.Acs.Alidns.Model.V20150109;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using LitJson;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Aliyun.Acs.Alidns.Model.V20150109.DescribeDomainRecordsResponse;

namespace AlidnsApp
{
    public partial class MainForm : Form
    {
        private string _recordId;
        private string Domain = "YourDomain.com";
        private string Rr = "www";
        private string AccessKeyId = "YourAccessKeyId";
        private string AccessKeySecret = "YourAccessKeySecret";
        private IClientProfile ClientProfile;
        private IAcsClient Client;
        private System.Threading.Timer timer;
        private bool isRuning = false;
        private long period = 0;
        private int day = 0;

        public MainForm()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            notifyIcon1.Visible = false;
            if (Properties.Settings.Default.accessKeyId != string.Empty)
                textBox1.Text = Properties.Settings.Default.accessKeyId;
            if (Properties.Settings.Default.accessKeySecret != string.Empty)
                textBox2.Text = Properties.Settings.Default.accessKeySecret;
            if (Properties.Settings.Default.RRKeyWord != string.Empty)
                textBox3.Text = Properties.Settings.Default.RRKeyWord;
            if (Properties.Settings.Default.domain != string.Empty)
                textBox4.Text = Properties.Settings.Default.domain;
            if (Properties.Settings.Default.period != string.Empty)
                textBox5.Text = Properties.Settings.Default.period;
            day = DateTime.Now.Day;

            if (textBox1.Text != string.Empty && textBox2.Text != string.Empty && textBox3.Text != string.Empty && textBox4.Text != string.Empty && textBox5.Text != string.Empty)
            {
                button1_Click(null, null);
            }
        }

        private async void Init()
        {
            try
            {
                _recordId = Properties.Settings.Default.recordId;


                ClientProfile = DefaultProfile.GetProfile(_recordId, AccessKeyId, AccessKeySecret);
                Client = new DefaultAcsClient(ClientProfile);
                //string r = GetRecordId(Domain, Rr);
                if (string.IsNullOrWhiteSpace(_recordId) || Properties.Settings.Default.accessKeyId != AccessKeyId || Rr != Properties.Settings.Default.RRKeyWord || Domain != Properties.Settings.Default.domain)
                {
                    _recordId = await GetRecordId(Domain, Rr);
                    Properties.Settings.Default.recordId = _recordId;
                    TextAdd(string.Format("[{0}]更新RecordId:{1}", DateTime.Now.ToLocalTime(), _recordId));
                    //Init();
                    //return;
                }
                timer = new System.Threading.Timer(async (obj) =>
                {
                    string lastIp = Properties.Settings.Default.lastIP;
                    string curIp = await GetIpAsync();
                    if (!curIp.Equals("") && !lastIp.Equals(curIp))
                    {
                        var request = new UpdateDomainRecordRequest
                        {
                            RecordId = _recordId,
                            RR = Rr,
                            Type = "A",
                            Value = curIp,
                            TTL = 600,
                            Priority = 10
                        };
                        Client.DoAction(request);
                        Properties.Settings.Default.lastIP = curIp;
                        TextAdd(string.Format("[{0}]IP变更为:{1}", DateTime.Now.ToLocalTime(), curIp));
                        //刷新DNS
                        StartCmd();
                    }
                    else
                        TextAdd(string.Format("[{0}]IP没有改变......", DateTime.Now.ToLocalTime()));
                }, null, 0, period);
                Properties.Settings.Default.accessKeyId = AccessKeyId;
                Properties.Settings.Default.accessKeySecret = AccessKeySecret;
                Properties.Settings.Default.RRKeyWord = Rr;
                Properties.Settings.Default.domain = Domain;
                Properties.Settings.Default.period = period.ToString(); ;
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                TextAdd(string.Format("[{1}]Error:{0}", e.Message, DateTime.Now.ToLocalTime()));
                return;
            }
        }

        #region 调用阿里云接口获取域名的唯一记录ID
        /// <summary>
        /// 调用阿里云接口获取域名的唯一记录ID
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="rr">子域名</param>
        /// <returns></returns>
        private async Task<string> GetRecordId(string domain, string rr)
        {
            try
            {
                DescribeDomainRecordsRequest reqq = new DescribeDomainRecordsRequest
                {
                    DomainName = domain,
                    RRKeyWord = rr
                };
                DescribeDomainRecordsResponse rss = Client.GetAcsResponse(reqq);
                var record = rss.DomainRecords.Find(re => { return re.RR == rr && re.DomainName == domain; });//从解析列表中查找对应记录
                if (record == null)
                {
                    AddDomainRecord(new Record
                    {
                        DomainName = domain,
                        RR = rr,
                        Type = "A",
                        Value = await GetIpAsync(),
                    });
                    await GetRecordId(domain, rr);
                    TextAdd(string.Format("[{0}]未找到[{1}.{2}]的解析记录,将其添加为新的解析记录。", DateTime.Now.ToLocalTime(), rr, domain));
                }
                else
                    return record.RecordId;
            }
            catch (Exception e)
            {
                TextAdd(string.Format("[{1}]Error:{0}", e.Message, DateTime.Now.ToLocalTime()));
                //MessageBox.Show(e.Message);
            }
            return "";
        }
        #endregion

        #region 调用API添加一条解析记录
        /// <summary>
        /// 添加一条记录
        /// </summary>
        /// <param name="record"></param>
        private void AddDomainRecord(Record record)
        {
            try
            {
                AddDomainRecordRequest request = new AddDomainRecordRequest();
                request.DomainName = record.DomainName;
                request.RR = record.RR;
                request.Type = record.Type;
                request.Value = record.Value;
                AddDomainRecordResponse response = Client.GetAcsResponse(request);
            }
            catch (Exception e)
            {
                TextAdd(string.Format("[{1}]Error:{0}", e.Message, DateTime.Now.ToLocalTime()));
            }
        }
        #endregion

        #region 获取公网IP
        /// <summary>
        /// 获取当前电脑的真实外网IP地址，可以自行修改成稳定的
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetIpAsync()
        {
            string ip = "";
            try
            {
                WebClient myWebClient = new WebClient { Credentials = CredentialCache.DefaultCredentials };
                //获取或设置用于向Internet资源的请求进行身份验证的网络凭据
                //Byte[] pageData = myWebClient.DownloadData("http://www.yxxrui.cn/yxxrui_cabangs_api/myip.ashx");//从指定网站下载数据
                Byte[] pageData = await myWebClient.DownloadDataTaskAsync("http://ip.taobao.com/service/getIpInfo.php?ip=myip");//从指定网站下载数据
                //string pageHtml = Encoding.Default.GetString(pageData); //如果获取网站页面采用的是GB2312，则使用这句
                string pageHtml = Encoding.UTF8.GetString(pageData); //如果获取网站页面采用的是UTF-8，则使用这句
                pageHtml = GetIPData(pageHtml).ip;
                return pageHtml;
            }
            catch (WebException e)
            {

                TextAdd(string.Format("[{1}]Error:{0}", e.Message, DateTime.Now.ToLocalTime()));
                //MessageBox.Show(webEx.Message);
            }
            return ip;
        }

        /// <summary>
        /// 从Json中获取IP信息
        /// </summary>
        /// <param name="str">Josn</param>
        /// <returns></returns>
        private IPData GetIPData(string str)
        {
            IPData data = null;
            try
            {
                Root jsonData = JsonMapper.ToObject<Root>(str);
                data = jsonData.data;
            }
            catch (Exception e)
            {
                TextAdd(string.Format("[{1}]Error:{0}", e.Message, DateTime.Now.ToLocalTime()));
            }
            return data;
        }

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            if (!isRuning)
            {
                if (!long.TryParse(textBox5.Text, out period))
                {
                    MessageBox.Show("请输入一个合法的时长（纯数字，单位为毫秒）！");
                    return;
                }
                AccessKeyId = textBox1.Text;
                AccessKeySecret = textBox2.Text;
                Rr = textBox3.Text;
                Domain = textBox4.Text;
                Init();
                isRuning = true;
                button1.Text = "停止";
            }
            else
            {
                timer.Dispose();
                isRuning = false;
                button1.Text = "开始";
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                //还原窗体显示    
                WindowState = FormWindowState.Normal;
                //激活窗体并给予它焦点
                this.Activate();
                //任务栏区显示图标
                this.ShowInTaskbar = true;
                //托盘区图标隐藏
                notifyIcon1.Visible = false;
            }
        }

        /// <summary>
        /// 点击关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {// 注意判断关闭事件reason来源于窗体按钮，否则用菜单退出时无法退出!
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // 取消关闭窗体 
                this.WindowState = FormWindowState.Minimized;//最小化
                //Main_SizeChanged(sender, null);
            }
        }

        /// <summary>
        /// 判断是否最小化,然后显示托盘
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Main_SizeChanged(object sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮
            if (WindowState == FormWindowState.Minimized)
            {
                //隐藏任务栏区图标
                this.ShowInTaskbar = false;
                //图标显示在托盘区
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(3000, "程序最小化提示", "图标已经缩小到托盘，打开窗口请双击图标即可。", ToolTipIcon.Info);
            }
        }

        private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                //还原窗体显示    
                WindowState = FormWindowState.Normal;
                //激活窗体并给予它焦点
                this.Activate();
                //任务栏区显示图标
                this.ShowInTaskbar = true;
                //托盘区图标隐藏
                notifyIcon1.Visible = false;
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dispose();
            timer.Dispose();
            Application.Exit();
        }

        private void TextAdd(string str)
        {
            textBox6.Text = str + "\r\n" + textBox6.Text;
            SaveLog();
        }

        private void SaveLog()
        {
            if (day != DateTime.Now.Day)
            {
                string path = Application.StartupPath + "\\log\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path += DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(textBox6.Text);
                sw.Close();
                fs.Close();
                //textBox6.Text = string.Format("[{0}]日志已保存为文件。", DateTime.Now.ToLocalTime());
                day = DateTime.Now.Day;
            }
        }

        public static void StartCmd(String command = "ipconfig /flushdns")
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe"; //命令
            p.StartInfo.UseShellExecute = false; //不启用shell启动进程
            p.StartInfo.RedirectStandardInput = true; // 重定向输入
            p.StartInfo.RedirectStandardOutput = true; // 重定向标准输出
            p.StartInfo.RedirectStandardError = true; // 重定向错误输出 
            p.StartInfo.CreateNoWindow = true; // 不创建新窗口
            p.Start();
            p.StandardInput.WriteLine(command); //cmd执行的语句
                                                //p.StandardOutput.ReadToEnd(); //读取命令执行信息
            p.StandardInput.WriteLine("exit"); //退出
        }
    }


}
