using System;
using System.Collections.Generic;
using System.Drawing;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HaoMaster
{
    public class Section
    {
        public string Title;
        public List<KeyValuePair<string, string>> Items = new List<KeyValuePair<string, string>>();

        public Section(string title) { Title = title; }

        public void Add(string key, string value)
        {
            Items.Add(new KeyValuePair<string, string>(key, string.IsNullOrEmpty(value) ? "未知" : value));
        }
    }

    public class MainForm : Form
    {
        private TreeView tree;
        private ListView list;
        private Button btnRefresh;
        private Button btnExport;
        private Label lblStatus;
        private List<Section> allSections;

        public MainForm()
        {
            InitUI();
            StartScan();
        }

        private void InitUI()
        {
            Text = "郝大师 - 硬件检测";
            Font = new Font("Microsoft YaHei UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1060, 680);
            MinimumSize = new Size(860, 540);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 64;
            header.BackColor = Color.FromArgb(250, 246, 235);

            Label title = new Label();
            title.Text = "郝大师";
            title.Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(146, 92, 46);
            title.AutoSize = true;
            title.Location = new Point(18, 12);

            Label sub = new Label();
            sub.Text = "离线硬件检测 · 单文件绿色版 v1.0";
            sub.ForeColor = Color.Gray;
            sub.AutoSize = true;
            sub.Location = new Point(130, 26);

            btnRefresh = new Button();
            btnRefresh.Text = "重新检测";
            btnRefresh.Size = new Size(96, 30);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefresh.Location = new Point(1060 - 96 - 18 - 106, 17);
            btnRefresh.Click += delegate { StartScan(); };

            btnExport = new Button();
            btnExport.Text = "导出报告";
            btnExport.Size = new Size(96, 30);
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExport.Location = new Point(1060 - 96 - 18, 17);
            btnExport.Click += delegate { ExportReport(); };

            header.Controls.Add(title);
            header.Controls.Add(sub);
            header.Controls.Add(btnRefresh);
            header.Controls.Add(btnExport);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 230;
            split.FixedPanel = FixedPanel.Panel1;

            tree = new TreeView();
            tree.Dock = DockStyle.Fill;
            tree.HideSelection = false;
            tree.FullRowSelect = true;
            tree.ShowLines = false;
            tree.BorderStyle = BorderStyle.None;
            tree.Font = new Font("Microsoft YaHei UI", 9.5F);
            tree.AfterSelect += delegate
            {
                if (tree.SelectedNode != null) ShowSection(tree.SelectedNode.Text);
            };

            list = new ListView();
            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.MultiSelect = false;
            list.BorderStyle = BorderStyle.None;
            list.Columns.Add("项目", 240);
            list.Columns.Add("信息", 740);

            split.Panel1.Controls.Add(tree);
            split.Panel2.Controls.Add(list);

            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 28;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Padding = new Padding(10, 0, 0, 0);
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Text = "准备就绪";

            Controls.Add(split);
            Controls.Add(lblStatus);
            Controls.Add(header);
        }

        private void StartScan()
        {
            btnRefresh.Enabled = false;
            btnExport.Enabled = false;
            lblStatus.Text = "正在检测硬件信息，请稍候……";
            tree.Nodes.Clear();
            list.Items.Clear();

            Thread t = new Thread(delegate()
            {
                DateTime start = DateTime.Now;
                List<Section> sections = CollectAll();
                TimeSpan cost = DateTime.Now - start;
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        allSections = sections;
                        tree.BeginUpdate();
                        foreach (Section s in sections) tree.Nodes.Add(s.Title);
                        tree.EndUpdate();
                        if (tree.Nodes.Count > 0)
                        {
                            tree.SelectedNode = tree.Nodes[0];
                            ShowSection(tree.Nodes[0].Text);
                        }
                        int n = 0;
                        foreach (Section s in sections) n += s.Items.Count;
                        lblStatus.Text = string.Format(
                            "检测完成：{0} 个分类，共 {1} 项信息，用时 {2:F1} 秒",
                            sections.Count, n, cost.TotalSeconds);
                        btnRefresh.Enabled = true;
                        btnExport.Enabled = true;
                    });
                }
                catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void ShowSection(string title)
        {
            if (allSections == null) return;
            Section target = null;
            foreach (Section s in allSections)
                if (s.Title == title) { target = s; break; }
            if (target == null) return;

            list.BeginUpdate();
            list.Items.Clear();
            foreach (KeyValuePair<string, string> kv in target.Items)
            {
                ListViewItem it = new ListViewItem(kv.Key);
                it.SubItems.Add(kv.Value);
                list.Items.Add(it);
            }
            list.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            if (list.Columns[1].Width < 300) list.Columns[1].Width = 300;
            list.EndUpdate();
        }

        private void ExportReport()
        {
            if (allSections == null || allSections.Count == 0)
            {
                MessageBox.Show("还没有检测结果，请先执行检测。", "郝大师",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "导出硬件检测报告";
                dlg.Filter = "文本文件 (*.txt)|*.txt";
                dlg.FileName = "郝大师硬件报告.txt";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("====================================");
                sb.AppendLine("  郝大师 硬件检测报告");
                sb.AppendLine("  生成时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("  本机名：" + Environment.MachineName);
                sb.AppendLine("====================================");
                foreach (Section s in allSections)
                {
                    sb.AppendLine();
                    sb.AppendLine("【" + s.Title + "】");
                    foreach (KeyValuePair<string, string> kv in s.Items)
                        sb.AppendLine("  " + kv.Key.PadRight(16) + " : " + kv.Value);
                }
                System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                lblStatus.Text = "报告已导出：" + dlg.FileName;
                MessageBox.Show("报告已成功导出！", "郝大师",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }

        // ===================== 硬件采集 =====================

        private static string Get(ManagementBaseObject o, string prop)
        {
            try
            {
                object v = o[prop];
                if (v == null) return "";
                string s = v.ToString().Trim();
                return s;
            }
            catch { return ""; }
        }

        private static List<ManagementBaseObject> Query(string wql, string scope)
        {
            List<ManagementBaseObject> result = new List<ManagementBaseObject>();
            try
            {
                ManagementObjectSearcher searcher;
                if (string.IsNullOrEmpty(scope))
                    searcher = new ManagementObjectSearcher(wql);
                else
                    searcher = new ManagementObjectSearcher(new ManagementScope(scope), new ObjectQuery(wql));
                using (searcher)
                {
                    foreach (ManagementBaseObject o in searcher.Get()) result.Add(o);
                }
            }
            catch { }
            return result;
        }

        private static string FGB(object val)
        {
            ulong n;
            if (val == null || !ulong.TryParse(val.ToString(), out n)) return "";
            return string.Format("{0:F1} GB ({1:N0} 字节)", n / 1073741824.0, n);
        }

        private static string FDate(string wmiDate)
        {
            try
            {
                DateTime d = ManagementDateTimeConverter.ToDateTime(wmiDate);
                return d.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch { return wmiDate; }
        }

        private static string CpuArch(object v)
        {
            int n;
            if (v == null || !int.TryParse(v.ToString(), out n)) return "";
            if (n == 0) return "x86";
            if (n == 9) return "x64 (64位)";
            if (n == 5) return "ARM";
            if (n == 6) return "安腾 Itanium";
            return "未知 (" + n + ")";
        }

        private static string MemType(object v)
        {
            int n;
            if (v == null || !int.TryParse(v.ToString(), out n)) return "";
            if (n == 20) return "DDR";
            if (n == 21) return "DDR2";
            if (n == 24) return "DDR3";
            if (n == 26) return "DDR4";
            if (n == 34) return "DDR5";
            if (n == 0) return "未知";
            return "类型代码 " + n;
        }

        private static string MemForm(object v)
        {
            int n;
            if (v == null || !int.TryParse(v.ToString(), out n)) return "";
            if (n == 8) return "DIMM (台式机)";
            if (n == 12) return "SODIMM (笔记本)";
            return "形式代码 " + n;
        }

        private static string SpeedText(object val)
        {
            ulong bps;
            if (val == null || !ulong.TryParse(val.ToString(), out bps)) return "";
            double mbps = bps / 1000000.0;
            if (mbps >= 1000) return string.Format("{0:F2} Gbps", mbps / 1000.0);
            return string.Format("{0:F0} Mbps", mbps);
        }

        private List<Section> CollectAll()
        {
            List<Section> sections = new List<Section>();

            // --- 基础数据 ---
            ManagementBaseObject os = null, cs = null, bios = null, board = null;
            List<ManagementBaseObject> osList = Query("SELECT * FROM Win32_OperatingSystem", null);
            if (osList.Count > 0) os = osList[0];
            List<ManagementBaseObject> csList = Query("SELECT * FROM Win32_ComputerSystem", null);
            if (csList.Count > 0) cs = csList[0];
            List<ManagementBaseObject> biosList = Query("SELECT * FROM Win32_BIOS", null);
            if (biosList.Count > 0) bios = biosList[0];
            List<ManagementBaseObject> boardList = Query("SELECT * FROM Win32_BaseBoard", null);
            if (boardList.Count > 0) board = boardList[0];

            List<ManagementBaseObject> cpus = Query("SELECT * FROM Win32_Processor", null);
            List<ManagementBaseObject> mems = Query("SELECT * FROM Win32_PhysicalMemory", null);
            List<ManagementBaseObject> gpus = Query("SELECT * FROM Win32_VideoController", null);
            List<ManagementBaseObject> drives = Query("SELECT * FROM Win32_DiskDrive", null);
            List<ManagementBaseObject> logicals = Query("SELECT * FROM Win32_LogicalDisk", null);
            List<ManagementBaseObject> nics = Query("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True", null);
            List<ManagementBaseObject> niccfg = Query("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True", null);
            List<ManagementBaseObject> sounds = Query("SELECT * FROM Win32_SoundDevice", null);
            List<ManagementBaseObject> monitors = Query("SELECT * FROM WmiMonitorID", "root\\wmi");
            List<ManagementBaseObject> oldMonitors = Query("SELECT * FROM Win32_DesktopMonitor", null);
            List<ManagementBaseObject> keyboards = Query("SELECT * FROM Win32_Keyboard", null);
            List<ManagementBaseObject> pointers = Query("SELECT * FROM Win32_PointingDevice", null);
            List<ManagementBaseObject> batteries = Query("SELECT * FROM Win32_Battery", null);

            // --- 概览 ---
            Section sum = new Section("概览");
            if (cs != null)
                sum.Add("电脑型号", Get(cs, "Manufacturer") + " " + Get(cs, "Model"));
            if (os != null)
                sum.Add("操作系统", Get(os, "Caption") + " " + Get(os, "OSArchitecture"));
            if (cpus.Count > 0)
                sum.Add("处理器", Get(cpus[0], "Name"));
            ulong totalMem = 0;
            foreach (ManagementBaseObject m in mems)
            {
                ulong c;
                if (ulong.TryParse(Get(m, "Capacity"), out c)) totalMem += c;
            }
            if (totalMem > 0) sum.Add("内存总量", string.Format("{0:F1} GB", totalMem / 1073741824.0));
            if (gpus.Count > 0)
            {
                StringBuilder gs = new StringBuilder();
                foreach (ManagementBaseObject g in gpus)
                {
                    if (gs.Length > 0) gs.Append(" / ");
                    gs.Append(Get(g, "Name"));
                }
                sum.Add("显卡", gs.ToString());
            }
            ulong totalDisk = 0;
            foreach (ManagementBaseObject d in drives)
            {
                ulong c;
                if (ulong.TryParse(Get(d, "Size"), out c)) totalDisk += c;
            }
            if (totalDisk > 0) sum.Add("硬盘总量", string.Format("{0:F2} TB", totalDisk / 1099511627776.0));
            if (bios != null)
                sum.Add("BIOS", Get(bios, "Manufacturer") + " " + Get(bios, "SMBIOSBIOSVersion"));
            if (os != null)
            {
                sum.Add("最近启动", FDate(Get(os, "LastBootUpTime")));
                sum.Add("系统运行时长", FormatUptime(Get(os, "LastBootUpTime")));
            }
            if (cs != null) sum.Add("当前用户", Get(cs, "UserName"));
            sum.Add("管理员权限", "是 (以管理员身份运行)");
            sections.Add(sum);

            // --- 处理器 ---
            Section cpuSec = new Section("处理器");
            for (int i = 0; i < cpus.Count; i++)
            {
                ManagementBaseObject c = cpus[i];
                string pfx = cpus.Count > 1 ? "CPU" + (i + 1) + " " : "";
                cpuSec.Add(pfx + "名称", Get(c, "Name"));
                cpuSec.Add(pfx + "制造商", Get(c, "Manufacturer"));
                cpuSec.Add(pfx + "架构", CpuArch(Get(c, "Architecture")));
                cpuSec.Add(pfx + "物理核心", Get(c, "NumberOfCores"));
                cpuSec.Add(pfx + "逻辑线程", Get(c, "NumberOfLogicalProcessors"));
                ulong mhz;
                if (ulong.TryParse(Get(c, "MaxClockSpeed"), out mhz))
                    cpuSec.Add(pfx + "标称频率", string.Format("{0:F2} GHz ({1} MHz)", mhz / 1000.0, mhz));
                if (ulong.TryParse(Get(c, "CurrentClockSpeed"), out mhz))
                    cpuSec.Add(pfx + "当前频率", string.Format("{0:F2} GHz ({1} MHz)", mhz / 1000.0, mhz));
                ulong kb;
                if (ulong.TryParse(Get(c, "L2CacheSize"), out kb))
                    cpuSec.Add(pfx + "二级缓存", kb / 1024 + " MB");
                if (ulong.TryParse(Get(c, "L3CacheSize"), out kb))
                    cpuSec.Add(pfx + "三级缓存", kb / 1024 + " MB");
                cpuSec.Add(pfx + "插槽", Get(c, "SocketDesignation"));
                cpuSec.Add(pfx + "CPU ID", Get(c, "ProcessorId"));
                cpuSec.Add(pfx + "电压", Get(c, "CurrentVoltage"));
                cpuSec.Add(pfx + "状态", Get(c, "Status"));
            }
            if (cpuSec.Items.Count == 0) cpuSec.Add("信息", "未检测到处理器");
            sections.Add(cpuSec);

            // --- 主板 ---
            Section boardSec = new Section("主板");
            if (board != null)
            {
                boardSec.Add("制造商", Get(board, "Manufacturer"));
                boardSec.Add("型号", Get(board, "Product"));
                boardSec.Add("版本", Get(board, "Version"));
                boardSec.Add("序列号", Get(board, "SerialNumber"));
            }
            else boardSec.Add("信息", "未检测到主板信息");
            sections.Add(boardSec);

            // --- BIOS ---
            Section biosSec = new Section("BIOS");
            if (bios != null)
            {
                biosSec.Add("制造商", Get(bios, "Manufacturer"));
                biosSec.Add("版本", Get(bios, "SMBIOSBIOSVersion"));
                biosSec.Add("发布日期", FDate(Get(bios, "ReleaseDate")));
                biosSec.Add("序列号", Get(bios, "SerialNumber"));
                biosSec.Add("SMBIOS 版本", Get(bios, "SMBIOSMajorVersion") + "." + Get(bios, "SMBIOSMinorVersion"));
                string mode = Get(bios, "SecureBoot") == "" ? "" :
                    (Get(bios, "SecureBoot") == "True" ? "已开启" : "已关闭");
                if (mode != "") biosSec.Add("安全启动", mode);
            }
            else biosSec.Add("信息", "未检测到 BIOS 信息");
            sections.Add(biosSec);

            // --- 内存 ---
            Section memSec = new Section("内存");
            for (int i = 0; i < mems.Count; i++)
            {
                ManagementBaseObject m = mems[i];
                string pfx = mems.Count > 1 ? Get(m, "DeviceLocator") + " " : "";
                memSec.Add(pfx + "容量", FGB(Get(m, "Capacity")));
                memSec.Add(pfx + "制造商", Get(m, "Manufacturer"));
                memSec.Add(pfx + "型号", Get(m, "PartNumber"));
                memSec.Add(pfx + "内存类型", MemType(Get(m, "SMBIOSMemoryType")));
                memSec.Add(pfx + "额定频率", Get(m, "Speed") + " MHz");
                if (Get(m, "ConfiguredClockSpeed") != "")
                    memSec.Add(pfx + "实际频率", Get(m, "ConfiguredClockSpeed") + " MHz");
                memSec.Add(pfx + "规格", MemForm(Get(m, "FormFactor")));
                memSec.Add(pfx + "序列号", Get(m, "SerialNumber"));
            }
            if (mems.Count == 0) memSec.Add("信息", "未检测到内存条");
            if (cs != null)
            {
                ulong phys;
                if (ulong.TryParse(Get(cs, "TotalPhysicalMemory"), out phys))
                    memSec.Add("系统可用识别内存", FGB(phys));
            }
            sections.Add(memSec);

            // --- 显卡 ---
            Section gpuSec = new Section("显卡");
            for (int i = 0; i < gpus.Count; i++)
            {
                ManagementBaseObject g = gpus[i];
                string pfx = gpus.Count > 1 ? "显卡" + (i + 1) + " " : "";
                gpuSec.Add(pfx + "名称", Get(g, "Name"));
                ulong ram;
                if (ulong.TryParse(Get(g, "AdapterRAM"), out ram))
                    gpuSec.Add(pfx + "显存", FGB(ram));
                gpuSec.Add(pfx + "图形处理器", Get(g, "VideoProcessor"));
                gpuSec.Add(pfx + "驱动版本", Get(g, "DriverVersion"));
                gpuSec.Add(pfx + "驱动日期", FDate(Get(g, "DriverDate")));
                if (Get(g, "CurrentHorizontalResolution") != "")
                    gpuSec.Add(pfx + "当前分辨率",
                        Get(g, "CurrentHorizontalResolution") + " x " +
                        Get(g, "CurrentVerticalResolution") + " @" +
                        Get(g, "CurrentRefreshRate") + "Hz");
                gpuSec.Add(pfx + "色深", Get(g, "CurrentBitsPerPixel") + " 位");
                gpuSec.Add(pfx + "状态", Get(g, "Status"));
                gpuSec.Add(pfx + "PNP ID", Get(g, "PNPDeviceID"));
            }
            if (gpuSec.Items.Count == 0) gpuSec.Add("信息", "未检测到显卡");
            sections.Add(gpuSec);

            // --- 磁盘 ---
            Section diskSec = new Section("磁盘");
            for (int i = 0; i < drives.Count; i++)
            {
                ManagementBaseObject d = drives[i];
                string pfx = "硬盘" + (i + 1) + " ";
                diskSec.Add(pfx + "型号", Get(d, "Model"));
                diskSec.Add(pfx + "容量", FGB(Get(d, "Size")));
                diskSec.Add(pfx + "接口", Get(d, "InterfaceType"));
                diskSec.Add(pfx + "固件", Get(d, "FirmwareRevision"));
                diskSec.Add(pfx + "序列号", Get(d, "SerialNumber"));
                diskSec.Add(pfx + "分区数", Get(d, "Partitions"));
            }
            foreach (ManagementBaseObject l in logicals)
            {
                string dt = Get(l, "DriveType");
                if (dt != "3" && dt != "2") continue; // 只显示本地盘和可移动盘
                string drive = Get(l, "DeviceID");
                string label = Get(l, "VolumeName");
                ulong size, free;
                ulong.TryParse(Get(l, "Size"), out size);
                ulong.TryParse(Get(l, "FreeSpace"), out free);
                if (size == 0) continue;
                diskSec.Add("分区 " + drive, label + "  总计 " + FGB(size) +
                    "，可用 " + FGB(free) + "，文件系统 " + Get(l, "FileSystem"));
            }
            if (diskSec.Items.Count == 0) diskSec.Add("信息", "未检测到磁盘");
            sections.Add(diskSec);

            // --- 网络 ---
            Section netSec = new Section("网络");
            for (int i = 0; i < nics.Count; i++)
            {
                ManagementBaseObject n = nics[i];
                string pfx = nics.Count > 1 ? "网卡" + (i + 1) + " " : "";
                netSec.Add(pfx + "连接名", Get(n, "NetConnectionID"));
                netSec.Add(pfx + "名称", Get(n, "Name"));
                netSec.Add(pfx + "MAC 地址", Get(n, "MACAddress"));
                netSec.Add(pfx + "链路速度", SpeedText(Get(n, "Speed")));
                netSec.Add(pfx + "已连接", Get(n, "NetEnabled") == "True" ? "是" : "否");

                string mac = Get(n, "MACAddress");
                foreach (ManagementBaseObject cfg in niccfg)
                {
                    if (Get(cfg, "MACAddress") != mac) continue;
                    try
                    {
                        string[] ips = (string[])cfg["IPAddress"];
                        if (ips != null)
                        {
                            StringBuilder ipAll = new StringBuilder();
                            foreach (string ip in ips)
                            {
                                if (ipAll.Length > 0) ipAll.Append("  /  ");
                                ipAll.Append(ip);
                            }
                            netSec.Add(pfx + "IP 地址", ipAll.ToString());
                        }
                    }
                    catch { }
                    try
                    {
                        string[] gw = (string[])cfg["DefaultIPGateway"];
                        if (gw != null && gw.Length > 0)
                            netSec.Add(pfx + "默认网关", string.Join("  /  ", gw));
                    }
                    catch { }
                    try
                    {
                        string[] dns = (string[])cfg["DNSServerSearchOrder"];
                        if (dns != null && dns.Length > 0)
                            netSec.Add(pfx + "DNS 服务器", string.Join("  /  ", dns));
                    }
                    catch { }
                    break;
                }
            }
            if (netSec.Items.Count == 0) netSec.Add("信息", "未检测到物理网卡");
            sections.Add(netSec);

            // --- 音频 ---
            Section sndSec = new Section("音频设备");
            for (int i = 0; i < sounds.Count; i++)
            {
                ManagementBaseObject s = sounds[i];
                string pfx = sounds.Count > 1 ? "设备" + (i + 1) + " " : "";
                sndSec.Add(pfx + "名称", Get(s, "Name"));
                sndSec.Add(pfx + "制造商", Get(s, "Manufacturer"));
                sndSec.Add(pfx + "状态", Get(s, "Status"));
            }
            if (sndSec.Items.Count == 0) sndSec.Add("信息", "未检测到音频设备");
            sections.Add(sndSec);

            // --- 显示器 ---
            Section monSec = new Section("显示器");
            int monIdx = 0;
            foreach (ManagementBaseObject m in monitors)
            {
                monIdx++;
                string pfx = monitors.Count > 1 ? "显示器" + monIdx + " " : "";
                try
                {
                    byte[] name = (byte[])m["UserFriendlyName"];
                    byte[] mfr = (byte[])m["ManufacturerName"];
                    monSec.Add(pfx + "制造商", DecodeAscii(mfr));
                    monSec.Add(pfx + "型号", DecodeAscii(name));
                }
                catch { }
                try
                {
                    byte[] sn = (byte[])m["SerialNumberID"];
                    monSec.Add(pfx + "序列号", DecodeAscii(sn));
                }
                catch { }
                monSec.Add(pfx + "状态", Get(m, "Active") == "True" ? "激活" : "未激活");
            }
            if (monIdx == 0)
            {
                foreach (ManagementBaseObject om in oldMonitors)
                {
                    monIdx++;
                    monSec.Add("名称", Get(om, "Name"));
                    monSec.Add("描述", Get(om, "MonitorType"));
                    if (Get(om, "ScreenWidth") != "")
                        monSec.Add("分辨率", Get(om, "ScreenWidth") + " x " + Get(om, "ScreenHeight"));
                }
            }
            if (monIdx == 0) monSec.Add("信息", "未检测到显示器信息");
            sections.Add(monSec);

            // --- 输入设备 ---
            Section inSec = new Section("输入设备");
            foreach (ManagementBaseObject k in keyboards)
            {
                inSec.Add("键盘", Get(k, "Name"));
                inSec.Add("键盘布局", Get(k, "Layout"));
                inSec.Add("键盘描述", Get(k, "Description"));
            }
            foreach (ManagementBaseObject p in pointers)
            {
                string pfx = pointers.Count > 1 ? "指针设备" + (inSec.Items.Count + 1) + " " : "鼠标 ";
                inSec.Add(pfx + "名称", Get(p, "Name"));
                inSec.Add(pfx + "制造商", Get(p, "Manufacturer"));
                if (Get(p, "NumberOfButtons") != "")
                    inSec.Add(pfx + "按键数", Get(p, "NumberOfButtons"));
            }
            if (inSec.Items.Count == 0) inSec.Add("信息", "未检测到输入设备");
            sections.Add(inSec);

            // --- 电池（笔记本才有） ---
            if (batteries.Count > 0)
            {
                Section batSec = new Section("电池");
                for (int i = 0; i < batteries.Count; i++)
                {
                    ManagementBaseObject b = batteries[i];
                    string pfx = batteries.Count > 1 ? "电池" + (i + 1) + " " : "";
                    batSec.Add(pfx + "名称", Get(b, "Name"));
                    batSec.Add(pfx + "剩余电量", Get(b, "EstimatedChargeRemaining") + " %");
                    batSec.Add(pfx + "状态", Get(b, "BatteryStatus"));
                    batSec.Add(pfx + "电压", Get(b, "DesignVoltage") + " mV");
                }
                sections.Add(batSec);
            }

            return sections;
        }

        private static string DecodeAscii(byte[] bytes)
        {
            if (bytes == null) return "";
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                if (b == 0) break;
                sb.Append((char)b);
            }
            return sb.ToString().Trim();
        }

        private static string FormatUptime(string wmiBootTime)
        {
            try
            {
                DateTime boot = ManagementDateTimeConverter.ToDateTime(wmiBootTime);
                TimeSpan up = DateTime.Now - boot;
                if (up.TotalMinutes < 1) return "不足 1 分钟";
                if (up.TotalHours < 1) return string.Format("{0} 分钟", (int)up.TotalMinutes);
                if (up.TotalDays < 1)
                    return string.Format("{0} 小时 {1} 分钟", (int)up.TotalHours, up.Minutes);
                return string.Format("{0} 天 {1} 小时 {2} 分钟",
                    (int)up.TotalDays, up.Hours, up.Minutes);
            }
            catch { return ""; }
        }
    }
}
