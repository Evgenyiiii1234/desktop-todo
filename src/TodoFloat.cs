using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// ==================== 主题 ====================
static class T
{
    public static bool Dark = true;
    public static string Theme = "orange";

    public static Color Bg, Card, Soft, Ink, Muted, Line, Accent;
    public static Color[] Quad = new Color[4];

    static readonly string[] Keys = { "orange", "blue", "green", "purple", "rose", "teal", "gold" };

    public static string[] ThemeKeys { get { return Keys; } }

    public static string ThemeName(string k)
    {
        switch (k)
        {
            case "orange": return "落日橙";
            case "blue":   return "海盐蓝";
            case "green":  return "苔原绿";
            case "purple": return "葡萄紫";
            case "rose":   return "胭脂红";
            case "teal":   return "薄荷青";
            case "gold":   return "旧金";
        }
        return k;
    }

    public static void Build()
    {
        if (Dark)
        {
            Bg = Color.FromArgb(22, 26, 32);
            Card = Color.FromArgb(38, 44, 53);
            Soft = Color.FromArgb(48, 56, 66);
            Ink = Color.FromArgb(236, 234, 229);
            Muted = Color.FromArgb(139, 147, 157);
            Line = Color.FromArgb(50, 58, 68);
        }
        else
        {
            Bg = Color.FromArgb(243, 241, 235);
            Card = Color.FromArgb(255, 255, 255);
            Soft = Color.FromArgb(246, 243, 236);
            Ink = Color.FromArgb(28, 40, 51);
            Muted = Color.FromArgb(136, 142, 152);
            Line = Color.FromArgb(228, 223, 212);
        }

        switch (Theme)
        {
            case "blue":   Accent = Dark ? Color.FromArgb(102, 165, 228) : Color.FromArgb(48, 116, 186); break;
            case "green":  Accent = Dark ? Color.FromArgb(118, 186, 138) : Color.FromArgb(66, 134, 92);  break;
            case "purple": Accent = Dark ? Color.FromArgb(157, 139, 214) : Color.FromArgb(104, 86, 176); break;
            case "rose":   Accent = Dark ? Color.FromArgb(226, 122, 142) : Color.FromArgb(188, 76, 102); break;
            case "teal":   Accent = Dark ? Color.FromArgb(96, 186, 178) : Color.FromArgb(40, 138, 132);  break;
            case "gold":   Accent = Dark ? Color.FromArgb(216, 174, 100) : Color.FromArgb(166, 126, 40); break;
            default:       Accent = Dark ? Color.FromArgb(230, 116, 76)  : Color.FromArgb(192, 82, 44);  break;
        }

        // 四象限语义色固定，主题色只作用于界面强调
        Quad[0] = Dark ? Color.FromArgb(226, 110, 88)  : Color.FromArgb(188, 72, 58);
        Quad[1] = Dark ? Color.FromArgb(104, 156, 216) : Color.FromArgb(62, 118, 182);
        Quad[2] = Dark ? Color.FromArgb(216, 170, 84)  : Color.FromArgb(172, 126, 36);
        Quad[3] = Dark ? Color.FromArgb(134, 142, 152) : Color.FromArgb(140, 146, 155);
    }

    public static Color MenuBg
    {
        get { return Dark ? Color.FromArgb(36, 41, 50) : Color.White; }
    }

    public static string QuadName(int q)
    {
        switch (q)
        {
            case 0: return "重要 · 紧急";
            case 1: return "重要 · 不紧急";
            case 2: return "紧急 · 不重要";
            default: return "不紧急 · 不重要";
        }
    }

    public static string QuadHint(int q)
    {
        switch (q)
        {
            case 0: return "马上做";
            case 1: return "排进日程";
            case 2: return "尽快打发";
            default: return "有空再说";
        }
    }
}

public class Task
{
    public bool Done;
    public int Quad;
    public string Due;      // yyyy-MM-dd or ""
    public bool Pin;
    public DateTime DoneAt; // 完成时间，DateTime.MinValue 表示未记录
    public string Text;

    public Task Clone()
    {
        Task t = new Task();
        t.Done = Done; t.Quad = Quad; t.Due = Due; t.Pin = Pin;
        t.DoneAt = DoneAt; t.Text = Text;
        return t;
    }
}

static class Store
{
    public static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopTodo");
    public static readonly string ItemsPath = Path.Combine(Dir, "items.dat");
    public static readonly string CfgPath = Path.Combine(Dir, "cfg.dat");
    public static readonly string LastRemindPath = Path.Combine(Dir, "remind.dat");

    // 格式：done|quad|due|pin|doneTicks|base64(text)
    public static List<Task> LoadItems()
    {
        List<Task> list = new List<Task>();
        try
        {
            if (!File.Exists(ItemsPath)) return list;
            foreach (string line in File.ReadAllLines(ItemsPath, Encoding.UTF8))
            {
                if (line.Trim().Length == 0) continue;
                string[] p = line.Split('|');
                Task t = new Task();
                try
                {
                    if (p.Length >= 6)
                    {
                        t.Done = p[0] == "1";
                        int q; int.TryParse(p[1], out q);
                        t.Quad = (q >= 0 && q <= 3) ? q : 0;
                        t.Due = p[2];
                        t.Pin = p[3] == "1";
                        long ticks; if (long.TryParse(p[4], out ticks) && ticks > 0 && ticks < DateTime.MaxValue.Ticks)
                            t.DoneAt = new DateTime(ticks);
                        t.Text = Encoding.UTF8.GetString(Convert.FromBase64String(p[5]));
                    }
                    else if (p.Length >= 5)
                    {
                        t.Done = p[0] == "1";
                        int q; int.TryParse(p[1], out q);
                        t.Quad = (q >= 0 && q <= 3) ? q : 0;
                        t.Due = p[2];
                        t.Pin = p[3] == "1";
                        t.Text = Encoding.UTF8.GetString(Convert.FromBase64String(p[4]));
                    }
                    else if (p.Length >= 3)          // v0.2 及更早
                    {
                        t.Done = p[0] == "1";
                        t.Quad = p[1] == "1" ? 0 : 1;
                        t.Due = "";
                        t.Pin = false;
                        t.Text = Encoding.UTF8.GetString(Convert.FromBase64String(p[2]));
                    }
                    else continue;
                }
                catch { continue; }
                if (t.Due == null) t.Due = "";
                if (!string.IsNullOrEmpty(t.Text)) list.Add(t);
            }
        }
        catch { }
        return list;
    }

    public static void SaveItems(List<Task> list)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            StringBuilder sb = new StringBuilder();
            foreach (Task t in list)
                sb.Append(t.Done ? "1" : "0").Append('|')
                  .Append(t.Quad).Append('|')
                  .Append(t.Due == null ? "" : t.Due).Append('|')
                  .Append(t.Pin ? "1" : "0").Append('|')
                  .Append(t.Done && t.DoneAt != DateTime.MinValue ? t.DoneAt.Ticks.ToString() : "")
                  .Append('|')
                  .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(t.Text == null ? "" : t.Text)))
                  .Append("\r\n");
            File.WriteAllText(ItemsPath, sb.ToString(), new UTF8Encoding(false));
        }
        catch { }
    }

    public static string LoadLastRemind()
    {
        try { if (File.Exists(LastRemindPath)) return File.ReadAllText(LastRemindPath).Trim(); }
        catch { }
        return "";
    }

    public static void SaveLastRemind(string day)
    {
        try { Directory.CreateDirectory(Dir); File.WriteAllText(LastRemindPath, day); }
        catch { }
    }

    public static string[] LoadCfg()
    {
        try
        {
            if (File.Exists(CfgPath))
            {
                string s = File.ReadAllText(CfgPath).Trim();
                if (s.Length > 0)
                {
                    string[] parts = s.Split(',');
                    for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
                    return parts;
                }
            }
        }
        catch { }
        return new string[0];
    }

    public static void SaveCfg(string s)
    {
        try { Directory.CreateDirectory(Dir); File.WriteAllText(CfgPath, s); }
        catch { }
    }
}

static class Shapes
{
    public static GraphicsPath Round(Rectangle r, int rad)
    {
        GraphicsPath p = new GraphicsPath();
        if (rad <= 1 || r.Width < 4 || r.Height < 4) { p.AddRectangle(r); return p; }
        int d = rad * 2;
        if (d > r.Width) d = r.Width;
        if (d > r.Height) d = r.Height;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

class RoundPanel : Panel
{
    public int Radius = 10;
    public RoundPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using (GraphicsPath p = Shapes.Round(ClientRectangle, Radius)) Region = new Region(p);
    }
}

class SmoothList : ListBox
{
    public SmoothList()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        DoubleBuffered = true;
    }
}

// ==================== 菜单渲染 ====================
class TColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground { get { return T.MenuBg; } }
    public override Color ImageMarginGradientBegin { get { return T.MenuBg; } }
    public override Color ImageMarginGradientMiddle { get { return T.MenuBg; } }
    public override Color ImageMarginGradientEnd { get { return T.MenuBg; } }
    public override Color MenuBorder { get { return T.Line; } }
    public override Color MenuItemBorder { get { return T.Line; } }
    public override Color MenuItemSelected { get { return T.Soft; } }
    public override Color MenuItemSelectedGradientBegin { get { return T.Soft; } }
    public override Color MenuItemSelectedGradientEnd { get { return T.Soft; } }
    public override Color SeparatorDark { get { return T.Line; } }
    public override Color SeparatorLight { get { return T.Line; } }
    public override Color CheckBackground { get { return T.Accent; } }
    public override Color CheckSelectedBackground { get { return T.Accent; } }
    public override Color CheckPressedBackground { get { return T.Accent; } }
}

class TRenderer : ToolStripProfessionalRenderer
{
    public TRenderer() : base(new TColorTable()) { }
}

static class Menus
{
    public static ContextMenuStrip New()
    {
        ContextMenuStrip m = new ContextMenuStrip();
        m.Font = new Font("Microsoft YaHei", 9f);
        m.Renderer = new TRenderer();
        m.BackColor = T.MenuBg;
        m.ForeColor = T.Ink;
        m.ShowImageMargin = false;
        return m;
    }
}

// ==================== 任务编辑对话框 ====================
class TaskForm : Form
{
    public TextBox Tb;
    public DateTimePicker Dp;
    public CheckBox Cb;
    public ComboBox Cq;

    public TaskForm(string caption, string text, string due, int quad)
    {
        Text = caption;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        ClientSize = new Size(392, 214);
        Font = new Font("Microsoft YaHei", 9f);

        bool dark = T.Dark;
        Color bg = dark ? Color.FromArgb(30, 34, 41) : Color.FromArgb(250, 249, 245);
        Color fg = dark ? Color.FromArgb(230, 228, 223) : Color.FromArgb(30, 42, 53);
        Color lbl = dark ? Color.FromArgb(140, 148, 158) : Color.FromArgb(110, 116, 124);
        Color field = dark ? Color.FromArgb(44, 50, 60) : Color.White;
        BackColor = bg;
        ForeColor = fg;

        Label l1 = new Label();
        l1.Text = "要做的事";
        l1.SetBounds(16, 12, 200, 18);
        l1.ForeColor = lbl;
        Controls.Add(l1);

        Tb = new TextBox();
        Tb.Text = text;
        Tb.BorderStyle = BorderStyle.FixedSingle;
        Tb.SetBounds(16, 32, 360, 26);
        Tb.Font = new Font("Microsoft YaHei", 10f);
        Tb.BackColor = field;
        Tb.ForeColor = fg;
        Controls.Add(Tb);

        Cb = new CheckBox();
        Cb.Text = "截止日期";
        Cb.SetBounds(16, 70, 90, 22);
        Cb.ForeColor = fg;
        Cb.Checked = !string.IsNullOrEmpty(due);
        Controls.Add(Cb);

        Dp = new DateTimePicker();
        Dp.Format = DateTimePickerFormat.Custom;
        Dp.CustomFormat = "yyyy 年 M 月 d 日";
        Dp.SetBounds(108, 68, 170, 24);
        DateTime d;
        if (!string.IsNullOrEmpty(due) && DateTime.TryParse(due, out d)) Dp.Value = d;
        else Dp.Value = DateTime.Today;
        Dp.Enabled = Cb.Checked;
        Controls.Add(Dp);
        Cb.CheckedChanged += delegate { Dp.Enabled = Cb.Checked; };

        Label l2 = new Label();
        l2.Text = "放到";
        l2.SetBounds(16, 108, 40, 18);
        l2.ForeColor = lbl;
        Controls.Add(l2);

        Cq = new ComboBox();
        Cq.DropDownStyle = ComboBoxStyle.DropDownList;
        Cq.SetBounds(56, 105, 200, 24);
        Cq.FlatStyle = FlatStyle.Flat;
        Cq.BackColor = field;
        Cq.ForeColor = fg;
        for (int i = 0; i < 4; i++) Cq.Items.Add((i + 1) + "  " + T.QuadName(i));
        Cq.SelectedIndex = quad;
        Controls.Add(Cq);

        Button ok = new Button();
        ok.Text = "保存";
        ok.SetBounds(216, 158, 76, 30);
        ok.DialogResult = DialogResult.OK;
        ok.FlatStyle = FlatStyle.Flat;
        ok.BackColor = T.Accent;
        ok.ForeColor = Color.White;
        ok.FlatAppearance.BorderSize = 0;
        Button no = new Button();
        no.Text = "取消";
        no.SetBounds(300, 158, 76, 30);
        no.DialogResult = DialogResult.Cancel;
        no.FlatStyle = FlatStyle.Flat;
        no.BackColor = dark ? Color.FromArgb(52, 58, 68) : Color.FromArgb(235, 232, 225);
        no.ForeColor = fg;
        no.FlatAppearance.BorderColor = dark ? Color.FromArgb(64, 72, 84) : Color.FromArgb(214, 210, 200);
        Controls.Add(ok); Controls.Add(no);

        AcceptButton = ok; CancelButton = no;
        Tb.SelectAll();
    }
}

// ==================== 象限面板 ====================
class QuadPanel : Panel
{
    public int Q;
    public List<Task> Items;
    public FloatForm Owner;
    public SmoothList List;
    public Label Empty;
    public Font FTitle, FCount, FRow, FRowDone, FDue, FEmpty;
    int hot = -1;
    int headerH = 26;
    Label scrollMask;
    [DllImport("user32.dll")]
    static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
    Task dragTask = null;
    Task draggingItem = null;
    Point dragFrom;
    bool dragging = false;
    bool dropHot = false;

    public QuadPanel(int q, List<Task> items, FloatForm owner)
    {
        Q = q; Items = items; Owner = owner;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;

        FTitle = new Font("Microsoft YaHei", 9f, FontStyle.Bold);
        FCount = new Font("Microsoft YaHei", 7.5f);
        FRow = new Font("Microsoft YaHei", 8.75f);
        FRowDone = new Font("Microsoft YaHei", 8.75f, FontStyle.Strikeout);
        FDue = new Font("Microsoft YaHei", 7.25f);
        FEmpty = new Font("Microsoft YaHei", 8f);

        List = new SmoothList();
        List.DrawMode = DrawMode.OwnerDrawFixed;
        List.ItemHeight = 30;
        List.BorderStyle = BorderStyle.None;
        List.IntegralHeight = false;
        List.TabStop = false;
        List.BackColor = T.Card;
        List.DrawItem += DrawRow;
        List.MouseMove += Move;
        List.MouseLeave += delegate { if (hot != -1) { hot = -1; List.Invalidate(); } };
        List.MouseDown += Down;
        List.DoubleClick += delegate { if (List.SelectedItem != null) Owner.EditTask((Task)List.SelectedItem); };
        List.KeyDown += delegate (object s, KeyEventArgs e)
        {
            if (List.SelectedItem == null) return;
            Task t = (Task)List.SelectedItem;
            if (e.KeyCode == Keys.Delete) { Owner.DeleteTask(t); }
            else if (e.KeyCode == Keys.Space) { Owner.ToggleDone(t); }
        };
        Controls.Add(List);

        List.AllowDrop = true;
        List.DragEnter += DragEnterHandler;
        List.DragLeave += DragLeaveHandler;
        List.DragDrop += DragDropOn;
        AllowDrop = true;
        DragEnter += DragEnterHandler;
        DragLeave += DragLeaveHandler;
        DragDrop += DragDropOn;

        Empty = new Label();
        Empty.Text = "空";
        Empty.Font = FEmpty;
        Empty.ForeColor = T.Muted;
        Empty.BackColor = T.Card;
        Empty.TextAlign = ContentAlignment.MiddleCenter;
        Empty.Visible = false;
        Controls.Add(Empty);
        Empty.BringToFront();

        // 盖住系统滚动条，改画一条细指示条
        scrollMask = new Label();
        scrollMask.BackColor = T.Card;
        scrollMask.Paint += delegate (object s, PaintEventArgs pe) { DrawScroll(pe.Graphics); };
        Controls.Add(scrollMask);
        scrollMask.BringToFront();

        List.MouseWheel += delegate { scrollMask.Invalidate(); };
        List.MouseDown += delegate { scrollMask.Invalidate(); };
        List.MouseUp += delegate { scrollMask.Invalidate(); };
        List.KeyDown += delegate { scrollMask.Invalidate(); };
        List.MouseMove += delegate { }; 
    }

    public void HideNativeScrollbar()
    {
        try { ShowScrollBar(List.Handle, 1, false); }
        catch { }
    }

    void DrawScroll(Graphics g)
    {
        if (List == null || scrollMask == null) return;
        int n = List.Items.Count;
        int vis = Math.Max(1, List.ClientSize.Height / Math.Max(1, List.ItemHeight));
        if (n <= vis) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int track = scrollMask.Height - 6;
        if (track < 20) return;
        int barH = Math.Max(14, track * vis / n);
        int maxTop = Math.Max(1, n - vis);
        int top = List.TopIndex;
        if (top > maxTop) top = maxTop;
        if (top < 0) top = 0;
        int y = 3 + (track - barH) * top / maxTop;
        Rectangle r = new Rectangle(26, y, 3, barH);
        using (GraphicsPath gp = Shapes.Round(r, 2))
        using (SolidBrush b = new SolidBrush(T.Muted))
            g.FillPath(b, gp);
    }

    public void ApplyTheme()
    {
        BackColor = T.Bg;
        List.BackColor = T.Card;
        Empty.BackColor = T.Card;
        Empty.ForeColor = T.Muted;
        if (scrollMask != null) scrollMask.BackColor = T.Card;
        Invalidate(true);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using (GraphicsPath p = Shapes.Round(ClientRectangle, 12)) Region = new Region(p);
        if (List != null)
        {
            List.SetBounds(1, headerH, Math.Max(10, Width - 2), Math.Max(20, Height - headerH - 2));
            Empty.SetBounds(8, headerH, Math.Max(10, Width - 16), Math.Max(20, Height - headerH - 8));
            if (scrollMask != null)
            {
                // 盖住系统滚动条（17px），只留右边的细指示条
                scrollMask.SetBounds(Width - 32, headerH, 32, Math.Max(16, Height - headerH - 4));
                scrollMask.BringToFront();
            }
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        Rectangle rr = new Rectangle(0, 0, Width - 1, Height - 1);
        using (GraphicsPath gp = Shapes.Round(rr, 12))
        {
            using (SolidBrush b = new SolidBrush(T.Card)) g.FillPath(b, gp);
            using (Pen p = new Pen(dropHot ? T.Quad[Q] : T.Line, dropHot ? 1.6f : 1f)) g.DrawPath(p, gp);
        }

        Color qc = T.Quad[Q];
        using (SolidBrush b = new SolidBrush(qc)) g.FillEllipse(b, 12, 10, 7, 7);

        string name = T.QuadName(Q);
        using (SolidBrush b = new SolidBrush(T.Ink)) g.DrawString(name, FTitle, b, 24f, 4f);
        float nw = g.MeasureString(name, FTitle).Width;

        int act = 0;
        foreach (Task t in Items) if (!t.Done && t.Quad == Q) act++;
        if (act > 0)
            using (SolidBrush b = new SolidBrush(T.Muted))
                g.DrawString(act.ToString(), FCount, b, 26f + nw, 7f);

        using (Pen p = new Pen(T.Muted, 1.3f))
        {
            int cx = Width - 19, cy = 13;
            g.DrawLine(p, cx - 4.5f, cy, cx + 4.5f, cy);
            g.DrawLine(p, cx, cy - 4.5f, cx, cy + 4.5f);
        }
        base.OnPaint(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && e.X > Width - 34 && e.Y < 28)
            Owner.QuickAdd(Q);
    }

    void Move(object sender, MouseEventArgs e)
    {
        int i = List.IndexFromPoint(e.Location);
        if (i != hot) { hot = i; List.Invalidate(); }

        if (dragTask != null && e.Button == MouseButtons.Left && !dragging)
        {
            int dx = Math.Abs(e.X - dragFrom.X);
            int dy = Math.Abs(e.Y - dragFrom.Y);
            if (dx > 5 || dy > 5)
            {
                dragging = true;
                Task payload = dragTask;
                dragTask = null;
                draggingItem = payload;
                List.Invalidate();
                try { List.DoDragDrop(payload, DragDropEffects.Move); }
                catch { }
                draggingItem = null;
                dragging = false;
                hot = -1;
                List.Invalidate();
            }
        }
    }

    void Down(object sender, MouseEventArgs e)
    {
        int i = List.IndexFromPoint(e.Location);
        dragTask = null;
        if (i < 0) { List.ClearSelected(); return; }
        Task t = (Task)List.Items[i];
        if (e.Button == MouseButtons.Right)
        {
            List.SelectedIndex = i;
            Owner.TaskMenu(t, List, e.Location);
            return;
        }
        if (e.Button != MouseButtons.Left) return;
        if (e.X > List.ClientSize.Width - 30) { Owner.DeleteTask(t); return; }
        if (e.X < 30) { Owner.ToggleDone(t); return; }
        List.SelectedIndex = i;
        dragTask = t;
        dragFrom = e.Location;
    }

    // ---- 拖放：把任务拖到另一个象限 ----
    void DragEnterHandler(object sender, DragEventArgs e)
    {
        Task t = e.Data.GetData(typeof(Task)) as Task;
        if (t != null)
        {
            e.Effect = t.Quad == Q ? DragDropEffects.None : DragDropEffects.Move;
            dropHot = t.Quad != Q;
            Invalidate();
        }
        else e.Effect = DragDropEffects.None;
    }

    void DragLeaveHandler(object sender, EventArgs e)
    {
        if (dropHot) { dropHot = false; Invalidate(); }
    }

    void DragDropOn(object sender, DragEventArgs e)
    {
        dropHot = false;
        Invalidate();
        Task t = e.Data.GetData(typeof(Task)) as Task;
        if (t == null || t.Quad == Q) return;
        t.Quad = Q;
        Owner.RefreshAll();
    }

    public void DrawRow(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= List.Items.Count) return;
        Task t = (Task)List.Items[e.Index];
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        Rectangle r = e.Bounds;
        bool ghost = (t == draggingItem);
        using (SolidBrush b = new SolidBrush(T.Card)) g.FillRectangle(b, r);
        if (ghost)
        {
            using (SolidBrush b = new SolidBrush(T.Soft)) g.FillRectangle(b, r);
            using (Pen p = new Pen(T.Line, 1f)) g.DrawRectangle(p, r.X + 6, r.Y + 2, r.Width - 12, r.Height - 4);
        }

        bool sel = (e.State & DrawItemState.Selected) != 0;
        bool hotRow = e.Index == hot;
        if (sel || hotRow)
        {
            Rectangle card = new Rectangle(r.X + 4, r.Y + 1, r.Width - 8, r.Height - 2);
            using (GraphicsPath gp = Shapes.Round(card, 7))
            {
                using (SolidBrush b = new SolidBrush(T.Soft)) g.FillPath(b, gp);
            }
        }

        int cy = r.Y + r.Height / 2;
        Rectangle circ = new Rectangle(r.X + 11, cy - 6, 12, 12);
        if (t.Done)
        {
            using (SolidBrush b = new SolidBrush(T.Quad[Q])) g.FillEllipse(b, circ);
            using (Pen p = new Pen(T.Card, 1.6f))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                g.DrawLines(p, new PointF[] {
                    new PointF(circ.X + 3.1f, circ.Y + 6.2f),
                    new PointF(circ.X + 5.2f, circ.Y + 8.4f),
                    new PointF(circ.X + 9.1f, circ.Y + 3.6f) });
            }
        }
        else
        {
            using (Pen p = new Pen(T.Muted, 1.3f)) g.DrawEllipse(p, circ);
        }

        // 置顶标记
        if (t.Pin && !t.Done)
        {
            using (SolidBrush b = new SolidBrush(T.Quad[Q]))
                g.FillRectangle(b, r.X + 4, cy - 6, 2f, 12);
        }

        bool showDue = !hotRow && !string.IsNullOrEmpty(t.Due);
        int dueRight = Width - 38;
        int tx = r.X + 30;
        int tw = (showDue ? dueRight - 48 : Width - 16) - tx;
        if (tw < 24) tw = 24;
        RectangleF tr = new RectangleF(tx, r.Y, tw, r.Height);
        using (StringFormat sf = new StringFormat())
        {
            sf.LineAlignment = StringAlignment.Center;
            sf.FormatFlags = StringFormatFlags.NoWrap;
            sf.Trimming = StringTrimming.EllipsisCharacter;
            using (SolidBrush b = new SolidBrush(ghost || t.Done ? T.Muted : T.Ink))
                g.DrawString(t.Text, t.Done ? FRowDone : FRow, b, tr, sf);
        }

        if (showDue)
        {
            Color dc = T.Muted;
            string label = DueLabel(t.Due, ref dc);
            RectangleF dr = new RectangleF(dueRight - 40, r.Y, 40, r.Height);
            using (StringFormat sf = new StringFormat())
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Far;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                using (SolidBrush b = new SolidBrush(t.Done ? T.Muted : dc))
                    g.DrawString(label, FDue, b, dr, sf);
            }
        }

        if (hotRow)
        {
            using (Pen p = new Pen(T.Muted, 1.3f))
            {
                int xx = Width - 20;
                g.DrawLine(p, xx - 3.5f, cy - 3.5f, xx + 3.5f, cy + 3.5f);
                g.DrawLine(p, xx - 3.5f, cy + 3.5f, xx + 3.5f, cy - 3.5f);
            }
        }
    }

    public static string DueLabel(string due, ref Color c)
    {
        DateTime d;
        if (!DateTime.TryParseExact(due, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out d))
            return "";
        int diff = (d.Date - DateTime.Today).Days;
        if (diff < 0) { c = Color.FromArgb(226, 96, 96); return d.Month + "/" + d.Day; }
        if (diff == 0) { c = Color.FromArgb(230, 150, 74); return "今天"; }
        if (diff == 1) { c = Color.FromArgb(230, 150, 74); return "明天"; }
        if (diff <= 7) { c = Color.FromArgb(190, 154, 74); return diff + "天"; }
        return d.Month + "/" + d.Day;
    }
}

// ==================== 主窗口 ====================
class FloatForm : Form
{
    const int HEADER = 44;
    const int FOOTER = 52;
    const int RAD = 18;
    const string PH = "快速添加，回车放进去…";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string AppKey = "DesktopTodoFloat";

    public List<Task> items;
    QuadPanel[] qps = new QuadPanel[4];
    RoundPanel footPanel;
    TextBox editor;
    Label target;
    Label dots;
    NotifyIcon tray;
    ContextMenuStrip menu;
    ToolStripMenuItem miTop, miDark, miShowDone, miAuto, miExit, miOpacity, miTheme;
    int hotIndex = -1;
    bool exiting = false;
    bool phActive = true;
    bool firstHide = true;
    bool showDone = true;
    int targetQ = 0;

    ToolStripMenuItem miHeat;
    System.Windows.Forms.Timer remindTimer;
    const int HOTKEY_ID = 0x0311;
    const int WM_HOTKEY = 0x0312;
    bool hotkeyOk = false;

    Font fTitle, fSub, fDots;

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr h);
    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")]
    static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

    public FloatForm()
    {
        items = Store.LoadItems();
        T.Build();

        Text = "待办矩阵";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        MinimumSize = new Size(400, 380);
        Size = new Size(492, 566);
        BackColor = T.Bg;
        DoubleBuffered = true;
        KeyPreview = true;
        Opacity = 0.85;

        fTitle = new Font("Microsoft YaHei", 12f, FontStyle.Bold);
        fSub = new Font("Microsoft YaHei", 8f);
        fDots = new Font("Segoe UI", 13f, FontStyle.Bold);

        for (int i = 0; i < 4; i++)
        {
            qps[i] = new QuadPanel(i, items, this);
            Controls.Add(qps[i]);
        }

        footPanel = new RoundPanel();
        footPanel.Radius = 11;
        footPanel.BackColor = T.Card;
        Controls.Add(footPanel);

        target = new Label();
        target.Size = new Size(14, 14);
        target.Cursor = Cursors.Hand;
        target.Click += delegate
        {
            targetQ = (targetQ + 1) % 4;
            PaintTarget();
        };
        footPanel.Controls.Add(target);

        editor = new TextBox();
        editor.BorderStyle = BorderStyle.None;
        editor.Font = new Font("Microsoft YaHei", 9.25f);
        editor.BackColor = T.Card;
        editor.ForeColor = T.Muted;
        editor.Text = PH;
        editor.LostFocus += delegate
        {
            if (editor.Text.Trim().Length == 0)
            { editor.Text = PH; editor.ForeColor = T.Muted; phActive = true; }
        };
        editor.KeyDown += delegate (object s, KeyEventArgs e)
        {
            if (phActive && e.KeyCode != Keys.Escape)
            { editor.Text = ""; phActive = false; editor.ForeColor = T.Ink; }
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddFromEditor(); }
            else if (e.KeyCode == Keys.Escape) { editor.Text = ""; }
        };
        footPanel.Controls.Add(editor);

        dots = new Label();
        dots.Text = "⋯";
        dots.Font = fDots;
        dots.TextAlign = ContentAlignment.MiddleCenter;
        dots.Cursor = Cursors.Hand;
        dots.Click += delegate { menu.Show(Cursor.Position); };
        Controls.Add(dots);

        BuildMenu();
        BuildTray();
        RestoreCfg();
        LayoutAll();
        PaintTarget();
        ApplyTheme();
        RefreshAll();

        this.Shown += delegate
        {
            ActiveControl = null;
            BeginInvoke((Action)delegate
            {
                editor.SelectionStart = 0;
                editor.SelectionLength = 0;
                CheckDueReminder();
            });
        };
        this.MouseWheel += delegate { };

        // 每 30 分钟检查一次到期提醒（跨天只提醒一次）
        remindTimer = new System.Windows.Forms.Timer();
        remindTimer.Interval = 30 * 60 * 1000;
        remindTimer.Tick += delegate { CheckDueReminder(); };
        remindTimer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            // Ctrl + Alt + T
            hotkeyOk = RegisterHotKey(Handle, HOTKEY_ID, 0x0002u | 0x0001u, (uint)Keys.T);
        }
        catch { hotkeyOk = false; }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (hotkeyOk)
        {
            try { UnregisterHotKey(Handle, HOTKEY_ID); } catch { }
            hotkeyOk = false;
        }
        if (remindTimer != null) remindTimer.Stop();
        base.OnHandleDestroyed(e);
    }

    // ================= 菜单 =================
    void BuildMenu()
    {
        menu = Menus.New();

        miTop = new ToolStripMenuItem("悬浮在最前");
        miTop.CheckOnClick = true;
        miTop.Checked = true;
        miTop.CheckedChanged += delegate { TopMost = miTop.Checked; };

        miDark = new ToolStripMenuItem("深色外观");
        miDark.CheckOnClick = true;
        miDark.Checked = true;
        miDark.CheckedChanged += delegate { T.Dark = miDark.Checked; T.Build(); ApplyTheme(); };

        miOpacity = new ToolStripMenuItem("不透明度");
        int[] vals = { 55, 70, 80, 85, 95, 100 };
        foreach (int v in vals)
        {
            int vv = v;
            ToolStripMenuItem it = new ToolStripMenuItem(v + "%");
            it.Tag = vv;
            it.Click += delegate { SetOpacity(vv); };
            miOpacity.DropDownItems.Add(it);
        }

        miTheme = new ToolStripMenuItem("配色");
        foreach (string k in T.ThemeKeys)
        {
            string kk = k;
            ToolStripMenuItem it = new ToolStripMenuItem(T.ThemeName(k));
            it.Tag = kk;
            it.Click += delegate { T.Theme = kk; T.Build(); ApplyTheme(); SyncMenuChecks(); };
            miTheme.DropDownItems.Add(it);
        }

        miShowDone = new ToolStripMenuItem("显示已完成");
        miShowDone.CheckOnClick = true;
        miShowDone.Checked = true;
        miShowDone.CheckedChanged += delegate { showDone = miShowDone.Checked; RefreshAll(); };

        miAuto = new ToolStripMenuItem("开机自启");
        miAuto.CheckOnClick = true;
        miAuto.Checked = IsAutoStart();
        miAuto.CheckedChanged += delegate { SetAutoStart(miAuto.Checked); };

        miExit = new ToolStripMenuItem("退出");
        miExit.Click += delegate { exiting = true; Close(); };

        miHeat = new ToolStripMenuItem("完成热力图…");
        miHeat.Click += delegate { ShowHeatmap(); };

        menu.Items.Add(miTop);
        menu.Items.Add(miDark);
        menu.Items.Add(miOpacity);
        menu.Items.Add(miTheme);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miShowDone);
        menu.Items.Add(miHeat);
        menu.Items.Add(miAuto);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miExit);
    }

    void SyncMenuChecks()
    {
        foreach (ToolStripItem it in miOpacity.DropDownItems)
            ((ToolStripMenuItem)it).Checked = it.Tag != null && (int)it.Tag == (int)Math.Round(Opacity * 100);
        foreach (ToolStripItem it in miTheme.DropDownItems)
            ((ToolStripMenuItem)it).Checked = it.Tag != null && (string)it.Tag == T.Theme;
    }

    void SetOpacity(int pct)
    {
        Opacity = pct / 100.0;
        SyncMenuChecks();
        SaveCfg();
    }

    void BuildTray()
    {
        tray = new NotifyIcon();
        tray.Icon = MakeIcon();
        tray.Text = "待办矩阵";
        tray.Visible = true;
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += delegate { ToggleShow(); };
    }

    static Icon MakeIcon()
    {
        Bitmap bmp = new Bitmap(32, 32);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using (SolidBrush b = new SolidBrush(T.Accent)) g.FillEllipse(b, 1, 1, 30, 30);
            using (Pen p = new Pen(Color.White, 3.4f))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                g.DrawLines(p, new PointF[] {
                    new PointF(9f, 16.5f), new PointF(14f, 21.5f), new PointF(23.5f, 10.5f) });
            }
        }
        IntPtr h = bmp.GetHicon();
        try { return (Icon)Icon.FromHandle(h).Clone(); }
        finally { DestroyIcon(h); bmp.Dispose(); }
    }

    // ================= 配置 =================
    void RestoreCfg()
    {
        string[] c = Store.LoadCfg();
        int x = -9999, y = -9999, w = 492, h = 566;
        bool top = true, dark = true;
        double op = 0.85;
        string theme = "orange";
        bool show = true;

        if (c.Length >= 9)
        {
            int.TryParse(c[0], out x);
            int.TryParse(c[1], out y);
            int.TryParse(c[2], out w);
            int.TryParse(c[3], out h);
            top = c[4] == "1";
            dark = c[5] == "1";
            double.TryParse(c[6], NumberStyles.Float, CultureInfo.InvariantCulture, out op);
            theme = c[7];
            show = c[8] == "1";
        }
        if (w < 400) w = 492;
        if (h < 380) h = 566;
        if (op < 0.4 || op > 1) op = 0.85;

        T.Dark = dark;
        T.Theme = theme;
        T.Build();
        TopMost = top;
        showDone = show;
        Opacity = op;

        Rectangle wa = Screen.PrimaryScreen.WorkingArea;
        bool off = x < wa.Left - 80 || y < wa.Top - 40 || x > wa.Right - 160 || y > wa.Bottom - 120;
        if (x == -9999 || off) { x = wa.Right - w - 30; y = wa.Top + 70; }
        Size = new Size(w, h);
        Location = new Point(x, y);

        miTop.Checked = top;
        miDark.Checked = dark;
        miShowDone.Checked = show;
        SyncMenuChecks();
    }

    void SaveCfg()
    {
        Store.SaveCfg(string.Join(",", new string[] {
            Left.ToString(), Top.ToString(), Width.ToString(), Height.ToString(),
            TopMost ? "1" : "0", T.Dark ? "1" : "0",
            Opacity.ToString("0.##", CultureInfo.InvariantCulture), T.Theme,
            showDone ? "1" : "0" }));
    }

    static bool IsAutoStart()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, false))
                return k != null && k.GetValue(AppKey) != null;
        }
        catch { return false; }
    }

    static void SetAutoStart(bool on)
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (k == null) return;
                if (on) k.SetValue(AppKey, "\"" + Application.ExecutablePath + "\"");
                else k.DeleteValue(AppKey, false);
            }
        }
        catch { }
    }

    // ================= 主题 / 布局 =================
    int cellH = 0;
    public void ApplyTheme()
    {
        BackColor = T.Bg;
        footPanel.BackColor = T.Card;
        editor.BackColor = T.Card;
        editor.ForeColor = phActive ? T.Muted : T.Ink;
        dots.BackColor = T.Bg;
        dots.ForeColor = T.Muted;
        foreach (QuadPanel qp in qps) qp.ApplyTheme();
        using (GraphicsPath p = Shapes.Round(ClientRectangle, RAD)) Region = new Region(p);
        PaintTarget();
        Invalidate(true);
    }

    void PaintTarget()
    {
        if (target == null) return;
        target.BackColor = T.Quad[targetQ];
        target.ForeColor = T.Quad[targetQ];
        toolTipSet();
    }

    void toolTipSet()
    {
        // 无 balloon；靠颜色点提示，title 里带象限名
        if (target != null) target.Text = "";
    }

    void LayoutAll()
    {
        if (dots == null || footPanel == null || editor == null || target == null) return;
        int w = ClientSize.Width, h = ClientSize.Height;
        int margin = 10, gap = 10;
        int areaH = h - HEADER - FOOTER - margin;
        int cw = (w - margin * 2 - gap) / 2;
        int chh = (areaH - gap) / 2;
        cellH = chh;

        qps[0].SetBounds(margin, HEADER, cw, chh);
        qps[1].SetBounds(margin + cw + gap, HEADER, cw, chh);
        qps[2].SetBounds(margin, HEADER + chh + gap, cw, chh);
        qps[3].SetBounds(margin + cw + gap, HEADER + chh + gap, cw, chh);

        dots.SetBounds(w - 42, 8, 28, 28);
        footPanel.SetBounds(14, h - FOOTER + 10, w - 28, 34);
        target.SetBounds(11, 10, 14, 14);
        editor.SetBounds(34, 9, Math.Max(40, footPanel.Width - 44), 20);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using (GraphicsPath p = Shapes.Round(ClientRectangle, RAD)) Region = new Region(p);
        LayoutAll();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using (SolidBrush b = new SolidBrush(T.Bg)) g.FillRectangle(b, ClientRectangle);

        using (SolidBrush b = new SolidBrush(T.Accent)) g.DrawString("待办矩阵", fTitle, b, 15f, 10f);
        float tw = g.MeasureString("待办矩阵", fTitle).Width;

        int act = 0, late = 0;
        foreach (Task t in items)
        {
            if (t.Done) continue;
            act++;
            if (!string.IsNullOrEmpty(t.Due))
            {
                DateTime d;
                if (DateTime.TryParseExact(t.Due, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out d) && d.Date < DateTime.Today) late++;
            }
        }
        string sub = items.Count == 0 ? "还没有待办" : (act == 0 ? "都清完了" : "剩 " + act + " 项" + (late > 0 ? " · " + late + " 逾期" : ""));
        using (SolidBrush b = new SolidBrush(T.Muted)) g.DrawString(sub, fSub, b, 17f + tw, 17f);

        using (Pen p = new Pen(T.Line)) g.DrawLine(p, 14, HEADER - 7, ClientSize.Width - 14, HEADER - 7);

        // 坐标系十字轴
        if (cellH > 0)
        {
            int cx = ClientSize.Width / 2;
            int cy = HEADER + cellH + 5;
            using (Pen p = new Pen(T.Line, 1f))
            {
                g.DrawLine(p, 16, cy, ClientSize.Width - 16, cy);
                g.DrawLine(p, cx, HEADER - 4, cx, HEADER + cellH * 2 + 16);
            }
            using (SolidBrush b = new SolidBrush(T.Line)) g.FillEllipse(b, cx - 2.5f, cy - 2.5f, 5, 5);
        }

        base.OnPaint(e);
    }

    // ================= 数据 =================
    List<Task> ForQuad(int q)
    {
        IEnumerable<Task> src = items.Where(t => t.Quad == q);
        if (!showDone) src = src.Where(t => !t.Done);
        return src
            .OrderByDescending(t => t.Pin && !t.Done)
            .ThenBy(t => t.Done)
            .ThenBy(t => string.IsNullOrEmpty(t.Due) ? 1 : 0)
            .ThenBy(t => t.Due)
            .ToList();
    }

    public void RefreshAll()
    {

        for (int i = 0; i < 4; i++)
        {
            List<Task> view = ForQuad(i);
            qps[i].List.BeginUpdate();
            qps[i].List.Items.Clear();
            foreach (Task t in view) qps[i].List.Items.Add(t);
            qps[i].List.EndUpdate();
            qps[i].Empty.Visible = view.Count == 0;
            qps[i].Empty.Text = T.QuadHint(i);
            qps[i].Invalidate();
        }
        Store.SaveItems(items);
        Invalidate();
    }

    void AddFromEditor()
    {
        string t = phActive ? "" : editor.Text.Trim();
        if (t.Length == 0) return;
        AddTask(new Task { Text = t, Quad = targetQ, Due = "", Pin = false, Done = false });
        editor.Text = PH;
        editor.ForeColor = T.Muted;
        phActive = true;
        RefreshAll();
    }

    void AddTask(Task t)
    {
        items.Add(t);
        Store.SaveItems(items);
        RefreshAll();
    }

    public void QuickAdd(int q)
    {
        using (TaskForm f = new TaskForm("新建待办", "", "", q))
        {
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                string v = f.Tb.Text.Trim();
                if (v.Length == 0) return;
                AddTask(new Task
                {
                    Text = v,
                    Quad = f.Cq.SelectedIndex,
                    Due = f.Cb.Checked ? f.Dp.Value.ToString("yyyy-MM-dd") : "",
                    Pin = false,
                    Done = false
                });
            }
        }
    }

    public void EditTask(Task t)
    {
        using (TaskForm f = new TaskForm("编辑待办", t.Text, t.Due, t.Quad))
        {
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                string v = f.Tb.Text.Trim();
                if (v.Length == 0) return;
                t.Text = v;
                t.Quad = f.Cq.SelectedIndex;
                t.Due = f.Cb.Checked ? f.Dp.Value.ToString("yyyy-MM-dd") : "";
                RefreshAll();
            }
        }
    }

    public void DeleteTask(Task t)
    {
        items.Remove(t);
        RefreshAll();
    }

    public void ToggleDone(Task t)
    {
        t.Done = !t.Done;
        t.DoneAt = t.Done ? DateTime.Now : DateTime.MinValue;
        RefreshAll();
    }

    // ================= 到期提醒 =================
    void CheckDueReminder()
    {
        try
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (Store.LoadLastRemind() == today) return;

            int dueToday = 0, overdue = 0;
            foreach (Task t in items)
            {
                if (t.Done || string.IsNullOrEmpty(t.Due)) continue;
                DateTime d;
                if (!DateTime.TryParseExact(t.Due, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out d)) continue;
                if (d.Date < DateTime.Today) overdue++;
                else if (d.Date == DateTime.Today) dueToday++;
            }
            if (dueToday + overdue == 0) return;

            if (tray != null)
            {
                string title = overdue > 0
                    ? overdue + " 件已经逾期"
                    : "今天有 " + dueToday + " 件到期";
                string body = "今天到期 " + dueToday + " 件 · 逾期 " + overdue + " 件";
                try { tray.ShowBalloonTip(8000, title, body, ToolTipIcon.None); }
                catch { }
            }
            Store.SaveLastRemind(today);
        }
        catch { }
    }

    // ================= 热力图 =================
    void ShowHeatmap()
    {
        using (HeatmapForm f = new HeatmapForm(items))
        {
            f.ShowDialog(this);
        }
    }

    public void TaskMenu(Task t, Control anchor, Point p)
    {
        ContextMenuStrip m = Menus.New();
        m.Items.Add("编辑", null, delegate { EditTask(t); });

        ToolStripMenuItem mv = new ToolStripMenuItem("移动到");
        for (int i = 0; i < 4; i++)
        {
            int qq = i;
            ToolStripMenuItem it = new ToolStripMenuItem(T.QuadName(qq));
            it.Checked = t.Quad == qq;
            it.Click += delegate { t.Quad = qq; RefreshAll(); };
            mv.DropDownItems.Add(it);
        }
        m.Items.Add(mv);

        ToolStripMenuItem pin = new ToolStripMenuItem("置顶这条");
        pin.Checked = t.Pin;
        pin.Click += delegate { t.Pin = !t.Pin; RefreshAll(); };
        m.Items.Add(pin);

        ToolStripMenuItem dn = new ToolStripMenuItem(t.Done ? "标记未完成" : "标记完成");
        dn.Click += delegate { ToggleDone(t); };
        m.Items.Add(dn);

        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("删除", null, delegate { DeleteTask(t); });
        m.Show(anchor, p);
    }

    void ToggleShow()
    {
        if (Visible) { Hide(); SaveCfg(); }
        else { Show(); BringToFront(); Activate(); editor.Focus(); }
    }

    // ================= 窗口行为 =================
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x0084;
        const int WM_SYSCOMMAND = 0x0112;
        const int WM_EXITSIZEMOVE = 0x0232;
        const int HTCAPTION = 2;
        const int HTBOTTOMRIGHT = 17;
        const int SC_MINIMIZE = 0xF020;

        if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_MINIMIZE) return;
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID) { ToggleShow(); return; }
        if (m.Msg == WM_EXITSIZEMOVE) { SaveCfg(); }

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            Point p = PointToClient(new Point(m.LParam.ToInt32()));
            if (p.X > ClientSize.Width - 18 && p.Y > ClientSize.Height - 18)
            { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
            if (p.Y < HEADER && p.Y >= 0)
            { m.Result = (IntPtr)HTCAPTION; return; }
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!exiting)
        {
            e.Cancel = true;
            Hide();
            SaveCfg();
            if (firstHide && tray != null)
            {
                firstHide = false;
                try { tray.ShowBalloonTip(3000, "待办矩阵在后台", "窗口已隐藏，双击右下角托盘图标可以叫回来。", ToolTipIcon.None); }
                catch { }
            }
            return;
        }
        SaveCfg();
        if (tray != null) { tray.Visible = false; tray.Dispose(); }
        base.OnFormClosing(e);
    }
}

// ==================== 完成热力图 ====================
class HeatmapForm : Form
{
    const int COLS = 12, ROWS = 7, CELL = 16, GAP = 4;
    const int GX = 44, GY = 60;

    Dictionary<DateTime, int> counts = new Dictionary<DateTime, int>();
    DateTime startMonday;
    int hoverIdx = -1;
    Font fTitle, fSub, fSmall, fStat, fNum;

    public HeatmapForm(List<Task> list)
    {
        foreach (Task t in list)
        {
            if (!t.Done || t.DoneAt == DateTime.MinValue) continue;
            DateTime d = t.DoneAt.Date;
            if (counts.ContainsKey(d)) counts[d] = counts[d] + 1;
            else counts[d] = 1;
        }

        DateTime today = DateTime.Today;
        int dow = ((int)today.DayOfWeek + 6) % 7;      // 周一 = 0
        startMonday = today.AddDays(-dow - 7 * (COLS - 1));

        Text = "完成热力图";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ClientSize = new Size(432, 254);
        KeyPreview = true;
        BackColor = T.Bg;
        ForeColor = T.Ink;
        DoubleBuffered = true;

        fTitle = new Font("Microsoft YaHei", 11f, FontStyle.Bold);
        fSub   = new Font("Microsoft YaHei", 8f);
        fSmall = new Font("Microsoft YaHei", 7f);
        fStat  = new Font("Microsoft YaHei", 8.5f);
        fNum   = new Font("Microsoft YaHei", 13f, FontStyle.Bold);

        MouseMove += delegate (object s, MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            if (idx != hoverIdx) { hoverIdx = idx; Invalidate(); }
        };
        MouseLeave += delegate { if (hoverIdx != -1) { hoverIdx = -1; Invalidate(); } };
        Click += delegate { Close(); };
        KeyDown += delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
        Load += delegate
        {
            using (GraphicsPath gp = Shapes.Round(ClientRectangle, 14))
                Region = new Region(gp);
        };
    }

    int HitTest(Point p)
    {
        for (int c = 0; c < COLS; c++)
            for (int r = 0; r < ROWS; r++)
            {
                Rectangle rect = new Rectangle(GX + c * (CELL + GAP), GY + r * (CELL + GAP), CELL, CELL);
                if (rect.Contains(p)) return c * ROWS + r;
            }
        return -1;
    }

    static Color Mix(Color a, Color b, double t)
    {
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using (SolidBrush b = new SolidBrush(T.Bg)) g.FillRectangle(b, ClientRectangle);

        int total = 0;
        foreach (KeyValuePair<DateTime, int> kv in counts) total += kv.Value;
        int week = 0;
        DateTime monday = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        foreach (KeyValuePair<DateTime, int> kv in counts)
            if (kv.Key >= monday) week += kv.Value;

        // 标题
        using (SolidBrush b = new SolidBrush(T.Ink)) g.DrawString("完成热力图", fTitle, b, 16f, 11f);
        using (SolidBrush b = new SolidBrush(T.Muted))
            g.DrawString("最近 12 周 · 每格一天", fSub, b, 17f, 35f);

        // hover 信息
        string hov = "";
        if (hoverIdx >= 0)
        {
            DateTime d = startMonday.AddDays(hoverIdx);
            int n = 0;
            counts.TryGetValue(d, out n);
            hov = d.ToString("M月d日") + " · " + (n == 0 ? "没有完成" : n + " 件");
        }
        else
        {
            hov = "Esc 或点一下关闭";
        }
        using (SolidBrush b = new SolidBrush(T.Muted))
        {
            SizeF sz = g.MeasureString(hov, fSub);
            g.DrawString(hov, fSub, b, ClientSize.Width - sz.Width - 16, 20f);
        }

        // 星期标签
        string[] wd = { "一", "三", "五" };
        int[] wdRow = { 0, 2, 4 };
        using (SolidBrush b = new SolidBrush(T.Muted))
            for (int i = 0; i < wd.Length; i++)
                g.DrawString(wd[i], fSmall, b, 20f, GY + wdRow[i] * (CELL + GAP) + 3);

        DateTime todayD = DateTime.Today;
        DateTime lastDay = startMonday.AddDays(COLS * ROWS - 1);

        for (int c = 0; c < COLS; c++)
            for (int r = 0; r < ROWS; r++)
            {
                int idx = c * ROWS + r;
                DateTime d = startMonday.AddDays(idx);
                Rectangle rect = new Rectangle(GX + c * (CELL + GAP), GY + r * (CELL + GAP), CELL, CELL);
                if (d > lastDay) continue;

                int n = 0;
                counts.TryGetValue(d, out n);
                Color col;
                if (d > todayD) col = Mix(T.Bg, T.Line, 0.35);
                else if (n == 0) col = T.Line;
                else if (n <= 1) col = Mix(T.Line, T.Accent, 0.32);
                else if (n <= 3) col = Mix(T.Line, T.Accent, 0.62);
                else col = T.Accent;

                using (GraphicsPath gp = Shapes.Round(rect, 4))
                using (SolidBrush b = new SolidBrush(col))
                    g.FillPath(b, gp);

                if (idx == hoverIdx)
                    using (Pen p = new Pen(T.Ink, 1.2f))
                        g.DrawRectangle(p, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

                if (d == todayD)
                    using (Pen p = new Pen(T.Muted, 1f))
                        using (GraphicsPath gp = Shapes.Round(rect, 4))
                            g.DrawPath(p, gp);
            }

        // 图例
        int lx = GX;
        int ly = GY + ROWS * (CELL + GAP) + 12;
        using (SolidBrush b = new SolidBrush(T.Muted)) g.DrawString("少", fSmall, b, lx - 2, ly + 1);
        lx += 16;
        for (int i = 0; i < 4; i++)
        {
            Color col = i == 0 ? T.Line : Mix(T.Line, T.Accent, i == 1 ? 0.32 : (i == 2 ? 0.62 : 1.0));
            Rectangle rect = new Rectangle(lx + i * (CELL - 2), ly, CELL - 4, CELL - 4);
            using (GraphicsPath gp = Shapes.Round(rect, 3))
            using (SolidBrush b = new SolidBrush(col))
                g.FillPath(b, gp);
        }
        using (SolidBrush b = new SolidBrush(T.Muted))
            g.DrawString("多", fSmall, b, lx + 4 * (CELL - 2) + 2, ly + 1);

        // 右侧统计
        int sx = GX + COLS * (CELL + GAP) + 14;
        int sy = GY - 2;
        using (SolidBrush b = new SolidBrush(T.Accent)) g.DrawString(total.ToString(), fNum, b, sx, sy);
        using (SolidBrush b = new SolidBrush(T.Muted)) g.DrawString("件已完成", fSmall, b, sx + 30, sy + 9);
        sy += 34;

        using (SolidBrush b = new SolidBrush(T.Muted)) g.DrawString("本周 " + week + " 件", fStat, b, sx, sy);
        sy += 22;
        int best = 0;
        foreach (KeyValuePair<DateTime, int> kv in counts) if (kv.Value > best) best = kv.Value;
        using (SolidBrush b = new SolidBrush(T.Muted)) g.DrawString("单日最多 " + best + " 件", fStat, b, sx, sy);
        sy += 22;
        using (SolidBrush b = new SolidBrush(T.Muted)) g.DrawString("连续 " + LongestStreak() + " 天", fStat, b, sx, sy);

        base.OnPaint(e);
    }

    int LongestStreak()
    {
        if (counts.Count == 0) return 0;
        List<DateTime> days = new List<DateTime>(counts.Keys);
        days.Sort();
        int best = 1, cur = 1;
        for (int i = 1; i < days.Count; i++)
        {
            if ((days[i] - days[i - 1]).Days == 1) cur++;
            else cur = 1;
            if (cur > best) best = cur;
        }
        return best;
    }
}

static class Program
{
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();
    [STAThread]
    static void Main()
    {
        try { SetProcessDPIAware(); } catch { }

        string log = Path.Combine(Path.GetTempPath(), "desktoptodo-error.txt");
        AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs a)
        {
            try { File.AppendAllText(log, DateTime.Now + " UNHANDLED " + a.ExceptionObject + "\r\n"); } catch { }
        };

        bool created;
        using (Mutex mtx = new Mutex(true, "DesktopTodoFloat_v1", out created))
        {
            if (!created) return;
            try
            {
                Application.ThreadException += delegate (object s, ThreadExceptionEventArgs a)
                {
                    try { File.AppendAllText(log, DateTime.Now + " UI " + a.Exception + "\r\n"); } catch { }
                };
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FloatForm());
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(log, DateTime.Now + " MAIN " + ex + "\r\n"); } catch { }
            }
        }
    }
}
