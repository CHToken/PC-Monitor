using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;

#nullable disable

namespace PCMonitor;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

class ProcessInfo
{
    public string Category { get; set; } = "";
    public string Risk { get; set; } = "";
    public string Description { get; set; } = "";
    public string Note { get; set; } = "";
}

class MainForm : Form
{
    private Label cpuLabel, ramLabel, diskLabel, uptimeLabel, catSummary, riskSummary, countLabel, statusLabel, detailLabel;
    private ComboBox catCombo, riskCombo;
    private TextBox searchBox;
    private DataGridView grid;
    private Button btnKill, btnCpu, btnRam, btnName, btnRefresh;
    private Timer autoTimer, searchTimer;
    private int? selectedPid;
    private string sortMode = "CPU";

    private static readonly Dictionary<string, ProcessInfo> ProcessDB = new()
    {
        ["chrome"]        = new(){ Category="Browser",  Risk="Safe",     Description="Google Chrome web browser",                     Note="Multiple instances per tab normal." },
        ["msedge"]        = new(){ Category="Browser",  Risk="Safe",     Description="Microsoft Edge web browser",                    Note="" },
        ["firefox"]       = new(){ Category="Browser",  Risk="Safe",     Description="Mozilla Firefox web browser",                   Note="" },
        ["brave"]         = new(){ Category="Browser",  Risk="Safe",     Description="Brave web browser",                             Note="" },
        ["opera"]         = new(){ Category="Browser",  Risk="Safe",     Description="Opera web browser",                             Note="" },
        ["code"]          = new(){ Category="IDE",      Risk="Safe",     Description="Visual Studio Code editor",                     Note="Unsaved work will be lost." },
        ["cursor"]        = new(){ Category="IDE",      Risk="Safe",     Description="Cursor AI-powered code editor",                 Note="Unsaved work will be lost." },
        ["devenv"]        = new(){ Category="IDE",      Risk="Safe",     Description="Visual Studio 2022",                            Note="" },
        ["antigravity ide"]=new(){ Category="IDE",      Risk="Safe",     Description="VS Code/Cursor rendering helper process",       Note="Restarts with editor." },
        ["language_server_windows_x64"]=new(){ Category="DevTool", Risk="Safe", Description="C# Language Server (IntelliSense)",         Note="Restarts automatically when needed." },
        ["opencode"]      = new(){ Category="DevTool",  Risk="Safe",     Description="OpenCode AI coding assistant CLI",              Note="Your AI pair programmer." },
        ["node"]          = new(){ Category="DevTool",  Risk="Caution",  Description="Node.js JavaScript runtime",                    Note="May be running a dev server." },
        ["dotnet"]        = new(){ Category="DevTool",  Risk="Caution",  Description=".NET SDK/runtime (build, restore, run)",        Note="Let active builds finish." },
        ["git"]           = new(){ Category="DevTool",  Risk="Caution",  Description="Git version control operation",                  Note="" },
        ["explorer"]      = new(){ Category="System",   Risk="Critical", Description="Windows Explorer (desktop, taskbar, files)",     Note="DO NOT KILL. Desktop disappears." },
        ["svchost"]       = new(){ Category="System",   Risk="Critical", Description="Windows Service Host",                           Note="Multiple instances normal." },
        ["csrss"]         = new(){ Category="System",   Risk="Critical", Description="Client Server Runtime Subsystem",                Note="Core Windows process." },
        ["winlogon"]      = new(){ Category="System",   Risk="Critical", Description="Windows Logon process",                         Note="You will be logged out." },
        ["lsass"]         = new(){ Category="System",   Risk="Critical", Description="Local Security Authority",                      Note="Handles authentication." },
        ["system"]        = new(){ Category="System",   Risk="Critical", Description="Windows NT Kernel",                              Note="Cannot be killed." },
        ["idle"]          = new(){ Category="System",   Risk="Critical", Description="System Idle Process (CPU counter)",              Note="" },
        ["registry"]      = new(){ Category="System",   Risk="Critical", Description="Windows Registry kernel component",              Note="" },
        ["smss"]          = new(){ Category="System",   Risk="Critical", Description="Session Manager Subsystem",                      Note="" },
        ["services"]      = new(){ Category="System",   Risk="Critical", Description="Service Control Manager",                       Note="Manages all services." },
        ["dwm"]           = new(){ Category="System",   Risk="Critical", Description="Desktop Window Manager (visual effects)",        Note="Restarts if killed but may flicker." },
        ["spoolsv"]       = new(){ Category="System",   Risk="Caution",  Description="Print Spooler service",                           Note="" },
        ["sihost"]        = new(){ Category="System",   Risk="Caution",  Description="Shell Infrastructure Host (Start menu)",          Note="" },
        ["fontdrvhost"]   = new(){ Category="System",   Risk="Critical", Description="Font driver host",                                Note="" },
        ["ctfmon"]        = new(){ Category="System",   Risk="Caution",  Description="CTF Loader (text input, language)",               Note="" },
        ["windowsterminal"]=new(){ Category="Terminal", Risk="Safe",     Description="Windows Terminal app",                           Note="All terminal tabs close." },
        ["powershell"]    = new(){ Category="Terminal", Risk="Safe",     Description="PowerShell session",                             Note="" },
        ["cmd"]           = new(){ Category="Terminal", Risk="Safe",     Description="Command Prompt (cmd.exe)",                       Note="" },
        ["pwsh"]          = new(){ Category="Terminal", Risk="Safe",     Description="PowerShell Core",                                Note="" },
        ["conhost"]       = new(){ Category="Terminal", Risk="Caution",  Description="Console Window Host",                            Note="" },
        ["teams"]         = new(){ Category="Comm",     Risk="Safe",     Description="Microsoft Teams",                                Note="You will miss messages." },
        ["slack"]         = new(){ Category="Comm",     Risk="Safe",     Description="Slack",                                          Note="" },
        ["discord"]       = new(){ Category="Comm",     Risk="Safe",     Description="Discord",                                        Note="" },
        ["zoom"]          = new(){ Category="Comm",     Risk="Safe",     Description="Zoom",                                           Note="Active call will disconnect." },
        ["outlook"]       = new(){ Category="Comm",     Risk="Safe",     Description="Microsoft Outlook",                              Note="" },
        ["msmpeng"]       = new(){ Category="Security", Risk="Critical", Description="Microsoft Defender Antivirus engine",             Note="High CPU during scans is normal." },
        ["spotify"]       = new(){ Category="Media",    Risk="Safe",     Description="Spotify music streaming",                        Note="" },
        ["steam"]         = new(){ Category="Media",    Risk="Safe",     Description="Steam gaming platform",                          Note="Downloads will stop." },
        ["onedrive"]      = new(){ Category="Cloud",    Risk="Safe",     Description="Microsoft OneDrive sync client",                 Note="File sync pauses." },
        ["postgres"]      = new(){ Category="Database", Risk="Critical", Description="PostgreSQL server",                               Note="Your app database." },
        ["redis-server"]  = new(){ Category="Database", Risk="Critical", Description="Redis cache server",                              Note="Your app cache/session store." },
        ["nginx"]         = new(){ Category="WebServer",Risk="Critical", Description="Nginx web server",                                Note="Your production web server." },
        ["searchindexer"] = new(){ Category="Service",  Risk="Caution",  Description="Windows Search Indexer",                         Note="High CPU during active indexing." },
        ["wmiprvse"]      = new(){ Category="Service",  Risk="Caution",  Description="WMI Provider Host",                              Note="" },
        ["runtimebroker"] = new(){ Category="Service",  Risk="Caution",  Description="Runtime Broker (UWP apps)",                      Note="" },
        ["audiodg"]       = new(){ Category="Service",  Risk="Caution",  Description="Windows Audio Device Graph",                     Note="Audio stops if killed." },
        ["tiworker"]      = new(){ Category="Service",  Risk="Caution",  Description="Windows Update worker",                          Note="Active update in progress." },
        ["msiexec"]       = new(){ Category="Service",  Risk="Caution",  Description="Windows Installer",                              Note="Active installation." },
    };

    private static ProcessInfo GetProcessInfo(string name)
    {
        var lower = name.ToLowerInvariant();
        if (ProcessDB.TryGetValue(lower, out var info)) return info;
        foreach (var kv in ProcessDB)
            if (lower.Contains(kv.Key)) return kv.Value;
        // Heuristic fallback for system processes
        string[] sysPatterns = { "system", "idle", "registry", "smss", "csrss", "win", "login", "services", "lsass", "svchost", "spoolsv", "dwm", "sihost", "fontdrvhost", "ctfmon" };
        foreach (var p in sysPatterns)
            if (lower == p || (lower.Length > 3 && lower.Contains(p)))
                return new ProcessInfo { Category="System", Risk="Critical", Description="Windows system process" };
        return new ProcessInfo { Category="Unknown", Risk="Caution", Description="Unrecognized process - investigate before terminating" };
    }

    public MainForm()
    {
        Text = "Eatly PC Monitor";
        Size = new Size(1200, 750);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 30);
        MinimumSize = new Size(900, 550);
        FormBorderStyle = FormBorderStyle.Sizable;

        // Top stats panel
        var topPanel = new Panel { Size = new Size(1170, 100), Location = new Point(12, 12), BackColor = Color.FromArgb(42, 42, 42), BorderStyle = BorderStyle.FixedSingle };
        cpuLabel = MakeLabel("Loading...", new Point(15, 12), new Size(370, 22), 12, true);
        ramLabel = MakeLabel("", new Point(15, 38), new Size(370, 22), 12, true);
        diskLabel = MakeLabel("", new Point(15, 64), new Size(370, 22), 10);
        uptimeLabel = MakeLabel("", new Point(410, 12), new Size(360, 22), 11);
        catSummary = MakeLabel("", new Point(410, 40), new Size(360, 50), 9);
        riskSummary = MakeLabel("", new Point(810, 12), new Size(340, 80), 9);
        topPanel.Controls.AddRange(new Control[]{ cpuLabel, ramLabel, diskLabel, uptimeLabel, catSummary, riskSummary });
        Controls.Add(topPanel);

        // Control bar
        var ctrl = new Panel { Size = new Size(1170, 38), Location = new Point(12, 118), BackColor = Color.FromArgb(38, 38, 38) };
        btnCpu = MakeButton("By CPU", 5, 4, 78, Color.FromArgb(0, 122, 204));
        btnRam = MakeButton("By RAM", 88, 4, 78, Color.FromArgb(0, 122, 204));
        btnName = MakeButton("By Name", 171, 4, 78, Color.FromArgb(0, 122, 204));
        btnRefresh = MakeButton("Refresh", 730, 4, 80, Color.FromArgb(56, 142, 60));
        var l1 = new Label { Text = "Category:", Location = new Point(265, 9), Size = new Size(62, 22), Font = new Font("Segoe UI", 9) };
        catCombo = new ComboBox { Location = new Point(327, 6), Size = new Size(120, 22), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9) };
        var l2 = new Label { Text = "Risk:", Location = new Point(460, 9), Size = new Size(35, 22), Font = new Font("Segoe UI", 9) };
        riskCombo = new ComboBox { Location = new Point(495, 6), Size = new Size(110, 22), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9) };
        var l3 = new Label { Text = "Search:", Location = new Point(845, 10), Size = new Size(50, 18), Font = new Font("Segoe UI", 9) };
        searchBox = new TextBox { Location = new Point(900, 7), Size = new Size(155, 22), Font = new Font("Segoe UI", 9), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        countLabel = MakeLabel("", new Point(1065, 10), new Size(95, 18), 9);
        catCombo.Items.AddRange(new[]{"All","Browser","IDE","DevTool","System","Terminal","Comm","Security","Media","Cloud","Database","Service","WebServer","Unknown"});
        riskCombo.Items.AddRange(new[]{"All","Safe","Caution","Critical"});
        catCombo.SelectedIndex = 0;
        riskCombo.SelectedIndex = 0;
        ctrl.Controls.AddRange(new Control[]{ btnCpu, btnRam, btnName, btnRefresh, l1, catCombo, l2, riskCombo, l3, searchBox, countLabel });
        Controls.Add(ctrl);

        // Grid
        grid = new DataGridView { Location = new Point(12, 162), Size = new Size(1165, 420), BackgroundColor = Color.FromArgb(32, 32, 32), GridColor = Color.FromArgb(55, 55, 55),
            RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, EnableHeadersVisualStyles = false };
        grid.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
        grid.DefaultCellStyle.ForeColor = Color.White;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 105, 180);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(44, 44, 44);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        grid.ColumnHeadersHeight = 28;
        Controls.Add(grid);

        // Detail panel
        var dp = new Panel { Size = new Size(1165, 70), Location = new Point(12, 590), BackColor = Color.FromArgb(42, 42, 42), BorderStyle = BorderStyle.FixedSingle };
        detailLabel = MakeLabel("Select a process to view details. Filter by 'Safe' risk to see processes you can safely close.", new Point(15, 8), new Size(1000, 55), 9);
        btnKill = new Button { Text = "Kill Process", Location = new Point(1020, 18), Size = new Size(125, 35), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 100, 100), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Enabled = false };
        dp.Controls.Add(detailLabel);
        dp.Controls.Add(btnKill);
        Controls.Add(dp);

        // Legend
        Controls.Add(MakeLegendBox("  SAFE  ", 12, Color.FromArgb(56, 142, 60)));
        Controls.Add(MakeLegendBox("CAUTION", 72, Color.FromArgb(237, 140, 0)));
        Controls.Add(MakeLegendBox("CRITICAL", 132, Color.FromArgb(198, 40, 40)));
        var lnote = new Label { Text = "Safe = can close  |  Caution = check first  |  Critical = system process, protected from kill", Location = new Point(195, 669), Size = new Size(500, 18), Font = new Font("Segoe UI", 8), ForeColor = Color.Gray };
        Controls.Add(lnote);

        statusLabel = MakeLabel("", new Point(12, 692), new Size(1160, 18), 7, gray: true);

        // Events
        btnCpu.Click += (_, _) => { sortMode = "CPU"; RefreshGrid(); };
        btnRam.Click += (_, _) => { sortMode = "RAM"; RefreshGrid(); };
        btnName.Click += (_, _) => { sortMode = "NAME"; RefreshGrid(); };
        btnRefresh.Click += (_, _) => RefreshGrid();
        catCombo.SelectedIndexChanged += (_, _) => RefreshGrid();
        riskCombo.SelectedIndexChanged += (_, _) => RefreshGrid();
        btnKill.Click += (_, _) => KillProcess();

        searchTimer = new Timer { Interval = 400 };
        searchTimer.Tick += (_, _) => { searchTimer.Stop(); RefreshGrid(); };
        searchBox.TextChanged += (_, _) => { searchTimer.Stop(); searchTimer.Start(); };

        grid.SelectionChanged += (_, _) => {
            if (grid.SelectedRows.Count > 0) {
                try {
                    var r = grid.SelectedRows[0];
                    var pid = Convert.ToInt32(r.Cells[0].Value);
                    var name = r.Cells[1].Value?.ToString() ?? "";
                    var cat = r.Cells[2].Value?.ToString() ?? "";
                    var risk = r.Cells[3].Value?.ToString() ?? "";
                    var desc = r.Cells[4].Value?.ToString() ?? "";
                    selectedPid = pid;
                    var info = GetProcessInfo(name);
                    if (risk == "Critical") {
                        btnKill.Enabled = false;
                        btnKill.BackColor = Color.FromArgb(100, 100, 100);
                        btnKill.Text = "Protected";
                        detailLabel.Text = $"[PROTECTED] PID: {pid} | {name} | {cat} | Critical\n{desc}\n{info.Note}";
                    } else {
                        btnKill.Enabled = true;
                        btnKill.BackColor = Color.FromArgb(198, 40, 40);
                        btnKill.Text = "Kill Process";
                        var tag = risk == "Caution" ? "[CHECK FIRST]" : "[SAFE]";
                        detailLabel.Text = $"{tag} PID: {pid} | {name} | {cat} | Risk: {risk}\n{desc}\n{info.Note}";
                    }
                } catch { }
            }
        };

        autoTimer = new Timer { Interval = 3000 };
        autoTimer.Tick += (_, _) => { try { RefreshGrid(); } catch { } };
        Shown += (_, _) => { RefreshGrid(); autoTimer.Start(); };
        FormClosing += (_, _) => autoTimer.Stop();
    }

    private Label MakeLabel(string text, Point loc, Size size, int fontSize, bool bold = false, bool gray = false)
    {
        return new Label { Text = text, Location = loc, Size = size, Font = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = gray ? Color.Gray : Color.White };
    }

    private Button MakeButton(string text, int x, int y, int w, Color bg)
    {
        return new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 30), FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Font = new Font("Segoe UI", 9) };
    }

    private Label MakeLegendBox(string text, int x, Color bg)
    {
        return new Label { Text = text, Location = new Point(x, 668), Size = new Size(55, 18), Font = new Font("Segoe UI", 7, FontStyle.Bold), BackColor = bg, TextAlign = ContentAlignment.MiddleCenter };
    }

    private void KillProcess()
    {
        if (selectedPid == null) return;
        try {
            var p = Process.GetProcessById(selectedPid.Value);
            var info = GetProcessInfo(p.ProcessName);
            if (info.Risk == "Critical") {
                MessageBox.Show("Cannot terminate system-critical process.", "Protected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var msg = $"Terminate:\n\n{p.ProcessName} (PID: {selectedPid})\nCategory: {info.Category}\nRisk: {info.Risk}\n\n{info.Description}";
            if (MessageBox.Show(msg, "Confirm Kill", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                p.Kill();
                MessageBox.Show($"Terminated {p.ProcessName}.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                selectedPid = null;
                RefreshGrid();
            }
        } catch (Exception ex) {
            MessageBox.Show($"Process may have already exited.\n{ex.Message}", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void RefreshGrid()
    {
        try {
            var catF = catCombo.SelectedItem?.ToString() ?? "All";
            var riskF = riskCombo.SelectedItem?.ToString() ?? "All";
            var search = searchBox.Text;

            // CPU
            int totalCpu = 0;
            try { using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor"); foreach (var obj in searcher.Get()) totalCpu += Convert.ToInt32(obj["LoadPercentage"]); totalCpu /= Environment.ProcessorCount; } catch { }
            // Memory via WMI
            ulong totalMem = 0, freeMem = 0;
            try { using var memSearcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem"); foreach (var obj in memSearcher.Get()) { totalMem = (ulong)obj["TotalVisibleMemorySize"] * 1024; freeMem = (ulong)obj["FreePhysicalMemory"] * 1024; } } catch { }
            var ramPct = totalMem > 0 ? Math.Round((1.0 - (double)freeMem / totalMem) * 100, 1) : 0;
            var ramGB = totalMem > 0 ? Math.Round((totalMem - freeMem) / 1073741824.0, 1) : 0;
            var totalGB = totalMem > 0 ? Math.Round(totalMem / 1073741824.0, 1) : 0;
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var upStr = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
            var d = DriveInfo.GetDrives().FirstOrDefault(x => x.Name.StartsWith("C"));
            var diskFree = 0.0;
            var diskTotal = 0.0;
            if (d != null) { diskFree = Math.Round(d.AvailableFreeSpace / 1073741824.0, 1); diskTotal = Math.Round(d.TotalSize / 1073741824.0, 1); }

            cpuLabel.Text = $"CPU: {totalCpu}% (Cores: {Environment.ProcessorCount})";
            cpuLabel.ForeColor = totalCpu > 85 ? Color.OrangeRed : totalCpu > 55 ? Color.Orange : Color.LimeGreen;
            ramLabel.Text = $"RAM: {ramGB} GB / {totalGB} GB ({ramPct}%)";
            ramLabel.ForeColor = ramPct > 85 ? Color.OrangeRed : ramPct > 60 ? Color.Orange : Color.LimeGreen;
            diskLabel.Text = $"Disk C:: {diskFree} GB free / {diskTotal} GB total";
            uptimeLabel.Text = $"Uptime: {upStr} | Processes: {Process.GetProcesses().Length}";

            // Build process list
            var procs = Process.GetProcesses()
                .Where(p => { try { return p.WorkingSet64 > 1_000_000; } catch { return false; } })
                .Select(p => {
                    string name = "";
                    double cpu = 0;
                    long ram = 0;
                    try { name = p.ProcessName; cpu = p.TotalProcessorTime.TotalSeconds; ram = p.WorkingSet64 / 1_048_576; } catch { }
                    var info = GetProcessInfo(name);
                    return new { PID = p.Id, Name = name, info.Category, info.Risk, info.Description, info.Note, CPU = Math.Round(cpu, 1), RAM = ram };
                }).ToList();

            var catCounts = new Dictionary<string, int>();
            var riskCounts = new Dictionary<string, int> { ["Safe"] = 0, ["Caution"] = 0, ["Critical"] = 0 };
            foreach (var p in procs) {
                catCounts.TryGetValue(p.Category, out int cv);
                catCounts[p.Category] = cv + 1;
                riskCounts.TryGetValue(p.Risk, out int rv);
                riskCounts[p.Risk] = rv + 1;
            }

            var filtered = procs.AsEnumerable();
            if (catF != "All") filtered = filtered.Where(p => p.Category == catF);
            if (riskF != "All") filtered = filtered.Where(p => p.Risk == riskF);
            if (!string.IsNullOrWhiteSpace(search))
                filtered = filtered.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || p.PID.ToString() == search);

            if (sortMode == "CPU") filtered = filtered.OrderByDescending(p => p.CPU);
            else if (sortMode == "RAM") filtered = filtered.OrderByDescending(p => p.RAM);
            else filtered = filtered.OrderBy(p => p.Name);

            var filteredList = filtered.ToList();

            catSummary.Text = "Categories: " + string.Join("  |  ", catCounts.OrderByDescending(kv => kv.Value).Take(6).Select(kv => $"{kv.Key}: {kv.Value}"));
            riskSummary.Text = $"Safe: {riskCounts.GetValueOrDefault("Safe",0)}  |  Caution: {riskCounts.GetValueOrDefault("Caution",0)}  |  Critical: {riskCounts.GetValueOrDefault("Critical",0)}\nTip: Filter by 'Safe' to see processes\nyou can safely terminate.";
            countLabel.Text = $"{filteredList.Count} shown";
            statusLabel.Text = $"Last refresh: {DateTime.Now:HH:mm:ss} | Auto-refresh: every 3s";

            var dt = new System.Data.DataTable();
            dt.Columns.Add("PID", typeof(int));
            dt.Columns.Add("Process");
            dt.Columns.Add("Category");
            dt.Columns.Add("Risk");
            dt.Columns.Add("Description");
            dt.Columns.Add("CPU (s)", typeof(double));
            dt.Columns.Add("RAM (MB)", typeof(long));
            foreach (var p in filteredList)
                dt.Rows.Add(p.PID, p.Name, p.Category, p.Risk, p.Description, p.CPU, p.RAM);

            int? savedPid = selectedPid;
            grid.DataSource = dt;
            if (grid.Columns.Count > 0) {
                grid.Columns[0].Width = 55;
                grid.Columns[1].Width = 130;
                grid.Columns[2].Width = 72;
                grid.Columns[3].Width = 60;
                grid.Columns[4].Width = 420;
                grid.Columns[5].Width = 70;
                grid.Columns[6].Width = 70;
            }

            // Color rows
            foreach (DataGridViewRow row in grid.Rows) {
                var risk = row.Cells[3].Value?.ToString();
                if (risk == "Critical") {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(75, 25, 25);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 140, 140);
                } else if (risk == "Caution") {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(75, 50, 15);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 200, 100);
                }
            }

            // Reselect
            if (savedPid != null) {
                foreach (DataGridViewRow row in grid.Rows)
                    if (row.Cells[0].Value is int v && v == savedPid) { row.Selected = true; return; }
                selectedPid = null;
                btnKill.Enabled = false;
                btnKill.BackColor = Color.FromArgb(100, 100, 100);
                btnKill.Text = "Kill Process";
            }
        } catch { }
    }
}
