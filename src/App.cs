using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Nova {
    static class Theme {
        public static readonly Color Bg=Color.FromArgb(16,25,36),Nav=Color.FromArgb(12,20,30),Card=Color.FromArgb(25,39,55),Line=Color.FromArgb(43,61,80),Text=Color.FromArgb(235,243,251),Muted=Color.FromArgb(153,175,195),Accent=Color.FromArgb(106,218,231),Green=Color.FromArgb(127,222,174),Error=Color.FromArgb(255,179,149);
        public static Font Font(float size,FontStyle style){return new Font("Microsoft YaHei UI",size,style,GraphicsUnit.Point);}
        public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
        public static Label Label(string text,float size,Color color){return new Label{Text=text,Font=Font(size,FontStyle.Regular),ForeColor=color,AutoSize=false,BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleLeft};}
    }
    sealed class FlatButton:Button {
        public bool Accent,Selected;bool hover;
        public FlatButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Font=Theme.Font(9,FontStyle.Regular);SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Color fill=Selected?Color.FromArgb(32,65,82):Accent?Theme.Accent:hover?Color.FromArgb(44,63,83):Theme.Card;using(var p=Theme.Round(new RectangleF(0,0,Width-1,Height-1),6))using(var b=new SolidBrush(Enabled?fill:Color.FromArgb(29,40,52)))e.Graphics.FillPath(b,p);Color fg=!Enabled?Color.FromArgb(104,125,145):Accent?Theme.Nav:Selected?Theme.Accent:Theme.Text;TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,fg,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-6,Height-6),Theme.Accent,fill);}
    }
    sealed class Switch:CheckBox {
        public Switch(){Appearance=Appearance.Button;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;AutoSize=false;Size=new Size(70,30);SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var track=new RectangleF(1,(Height-23)/2f,42,23);using(var p=Theme.Round(track,11))using(var b=new SolidBrush(!Enabled?Theme.Line:Checked?Theme.Accent:Color.FromArgb(66,85,104)))e.Graphics.FillPath(b,p);using(var b=new SolidBrush(Checked&&Enabled?Theme.Nav:Theme.Text))e.Graphics.FillEllipse(b,Checked?24:5,(Height-15)/2f,15,15);TextRenderer.DrawText(e.Graphics,Checked?"开":"关",Font,new Rectangle(47,0,23,Height),Enabled?Checked?Theme.Accent:Theme.Muted:Theme.Line,TextFormatFlags.VerticalCenter);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);}
    }
    sealed class FeatureRow:UserControl {
        public readonly string Id;public readonly bool IsToggle,IsAction;public NumericUpDown Input;public Switch Toggle;public FlatButton Apply,Reset;readonly Label title,note,hotkey;readonly string hint;public Action<FeatureRow> Execute,Restore;bool updating;
        public double Value{get{return Input==null?0:(double)Input.Value;}}
        public FeatureRow(string id,string name,string hint,string key,string kind,double def,double min,double max,int decimals){
            Id=id;this.hint=hint;IsToggle=kind=="toggle";IsAction=kind=="action";Height=58;Margin=new Padding(0);BackColor=Theme.Card;DoubleBuffered=true;
            title=Theme.Label(name,10.2f,Theme.Text);title.SetBounds(17,5,300,24);Controls.Add(title);note=Theme.Label(hint,8.3f,Theme.Muted);note.AutoEllipsis=true;note.SetBounds(17,29,300,20);Controls.Add(note);hotkey=Theme.Label(key,8.2f,Theme.Muted);hotkey.TextAlign=ContentAlignment.MiddleCenter;Controls.Add(hotkey);
            if(IsToggle){Toggle=new Switch{BackColor=Theme.Card,Font=Theme.Font(8.5f,FontStyle.Regular),AccessibleName=name};Toggle.CheckedChanged+=delegate{if(!updating&&Execute!=null)Execute(this);};Controls.Add(Toggle);}
            else {Apply=new FlatButton{Text=IsAction?"执行":"应用",Accent=false,AccessibleName=name+(IsAction?" 执行":" 应用")};Apply.Click+=delegate{if(Execute!=null)Execute(this);};Controls.Add(Apply);if(!IsAction){Input=new NumericUpDown{Minimum=(decimal)min,Maximum=(decimal)max,Value=(decimal)def,DecimalPlaces=decimals,Increment=decimals==0?1:0.1m,BackColor=Theme.Bg,ForeColor=Theme.Text,BorderStyle=BorderStyle.FixedSingle,Font=Theme.Font(9.5f,FontStyle.Regular),TextAlign=HorizontalAlignment.Center,AccessibleName=name+" 数值"};Controls.Add(Input);if(id!="adrenaline"&&id!="ammo"){Reset=new FlatButton{Text="还原",AccessibleName=name+" 还原"};Reset.Click+=delegate{if(Restore!=null)Restore(this);};Controls.Add(Reset);}}}
        }
        protected override void OnResize(EventArgs e){base.OnResize(e);if(title==null)return;int right=Width-15;int actionX=right-72;if(Toggle!=null)Toggle.SetBounds(actionX,14,72,30);if(Apply!=null)Apply.SetBounds(actionX,13,72,32);int keyX=actionX-235;
            if(Input!=null){Input.SetBounds(actionX-107,15,93,29);if(Reset!=null)Reset.SetBounds(actionX-165,13,49,32);keyX=actionX-283;}hotkey.SetBounds(keyX,16,110,27);int labelWidth=Math.Max(130,keyX-24);title.Width=labelWidth;note.Width=labelWidth;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Theme.Line))e.Graphics.DrawLine(p,16,Height-1,Width-16,Height-1);}
        public void Sync(bool ready,bool active){updating=true;try{if(Toggle!=null){Toggle.Enabled=ready;Toggle.Checked=active;Toggle.Invalidate();}if(Apply!=null){Apply.Enabled=ready;Apply.Text=IsAction?"执行":active?"已应用":"应用";Apply.Selected=active;Apply.Invalidate();}if(Input!=null)Input.Enabled=ready;if(Reset!=null)Reset.Enabled=ready&&active;title.ForeColor=active?Theme.Accent:Theme.Text;note.Text=active&&!IsToggle?hint+" · 保持中":hint;}finally{updating=false;}}
        public void InvokeFeature(){if(Execute!=null)Execute(this);}
    }
    sealed class Telemetry:Control {
        public Trainer Trainer;
        public Telemetry(){Height=100;DoubleBuffered=true;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;string[] names={"生命值","法力","肾上腺素","弹匣 / 备弹"};bool ready=Trainer!=null&&Trainer.Ready;string[] values=ready?new[]{Trainer.Health+" / "+Trainer.MaxHealth,Trainer.Mana.ToString("0"),Trainer.Adrenaline.ToString("0"),Trainer.Ammo<0?"未装备枪械":Trainer.Ammo+" / "+Trainer.Reserve}:new[]{"—","—","—","—"};int cw=Width/4;using(var small=Theme.Font(9,FontStyle.Regular))using(var line=new Pen(Theme.Line))for(int i=0;i<4;i++){int x=i*cw;TextRenderer.DrawText(e.Graphics,names[i],small,new Point(x+18,17),Theme.Muted);float size=19;Font large=Theme.Font(size,FontStyle.Regular);try{while(size>10&&TextRenderer.MeasureText(e.Graphics,values[i],large,Size.Empty,TextFormatFlags.NoPadding).Width>cw-30){large.Dispose();size-=0.5f;large=Theme.Font(size,FontStyle.Regular);}TextRenderer.DrawText(e.Graphics,values[i],large,new Rectangle(x+18,39,cw-30,42),ready?Theme.Text:Theme.Muted,TextFormatFlags.Left|TextFormatFlags.NoPadding|TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter);}finally{large.Dispose();}if(i>0)e.Graphics.DrawLine(line,x,19,x,78);}}
    }
    sealed class MainForm:Form {
        [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
        [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
        [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
        [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
        readonly List<FeatureRow> rows=new List<FeatureRow>();readonly Dictionary<int,FeatureRow> hotkeys=new Dictionary<int,FeatureRow>();readonly List<int> registered=new List<int>();
        readonly FlatButton[] navButtons=new FlatButton[4];readonly Panel[] pages=new Panel[4];readonly Panel content=new Panel();readonly Label status,footer,countLabel,title,subtitle;readonly FlatButton connect,launch;readonly CheckBox useHotkeys,pin;readonly Telemetry telemetry=new Telemetry();readonly Timer timer=new Timer();
        Trainer trainer;bool attaching,closing,keysRegistered,f12Held;int ticks,page,attachGeneration;DateTime lastAttempt=DateTime.MinValue;string install=@"L:\SteamLibrary\steamapps\common\Greylock";
        string SettingsPath{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EchoPointNovaTrainer","settings.txt");}}
        public MainForm(){
            Text="Echo Point Nova · 单人修改器";Font=Theme.Font(9,FontStyle.Regular);AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;BackColor=Theme.Bg;ForeColor=Theme.Text;ClientSize=new Size(1080,758);MinimumSize=new Size(940,650);StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;
            try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
            var sidebar=new Panel{Dock=DockStyle.Left,Width=184,BackColor=Theme.Nav};Controls.Add(sidebar);
            var brand=Theme.Label("NOVA",22,Theme.Text);brand.Font=new Font("Segoe UI Semibold",24,FontStyle.Bold);brand.SetBounds(23,27,145,44);sidebar.Controls.Add(brand);var brandSmall=Theme.Label("单人修改器",9,Theme.Muted);brandSmall.SetBounds(26,76,140,25);sidebar.Controls.Add(brandSmall);
            string[] nav={"概览","生命与能量","武器","移动与抓钩"};for(int i=0;i<4;i++){int n=i;navButtons[i]=new FlatButton{Text=nav[i],TabIndex=i};navButtons[i].SetBounds(14,145+i*55,156,43);navButtons[i].Click+=delegate{ShowPage(n);};sidebar.Controls.Add(navButtons[i]);}
            countLabel=Theme.Label("20 项修改功能",8.5f,Theme.Muted);countLabel.SetBounds(25,398,149,28);sidebar.Controls.Add(countLabel);
            var bottom=new Panel{Dock=DockStyle.Bottom,Height=160};sidebar.Controls.Add(bottom);useHotkeys=new CheckBox{Text="启用快捷键",Checked=true,ForeColor=Theme.Muted,AutoSize=true,Location=new Point(24,20)};bottom.Controls.Add(useHotkeys);pin=new CheckBox{Text="窗口置顶",AutoSize=true,ForeColor=Theme.Muted,Location=new Point(24,54)};pin.CheckedChanged+=delegate{TopMost=pin.Checked;};bottom.Controls.Add(pin);var help=new FlatButton{Text="使用说明  F1"};help.SetBounds(16,99,150,34);help.Click+=delegate{Help();};bottom.Controls.Add(help);
            var area=new Panel{Dock=DockStyle.Fill,Padding=new Padding(26,0,26,0)};Controls.Add(area);area.BringToFront();
            var header=new Panel{Dock=DockStyle.Top,Height=100};area.Controls.Add(header);title=Theme.Label("Echo Point Nova",19,Theme.Text);title.Font=new Font("Segoe UI Semibold",20,FontStyle.Regular);title.SetBounds(0,13,430,38);header.Controls.Add(title);subtitle=Theme.Label("游戏 3.23  /  Steam 22752386",8.5f,Theme.Muted);subtitle.SetBounds(1,56,370,24);header.Controls.Add(subtitle);
            launch=new FlatButton{Text="启动游戏",Anchor=AnchorStyles.Top|AnchorStyles.Right};connect=new FlatButton{Text="连接游戏",Accent=true,Anchor=AnchorStyles.Top|AnchorStyles.Right};header.Controls.Add(launch);header.Controls.Add(connect);header.Resize+=delegate{connect.SetBounds(header.Width-104,29,104,36);launch.SetBounds(header.Width-219,29,104,36);};launch.Click+=delegate{Launch();};connect.Click+=delegate{Attach();};
            var connection=new Panel{Dock=DockStyle.Top,Height=45,BackColor=Theme.Card};area.Controls.Add(connection);status=Theme.Label("●  正在查找游戏",9,Theme.Muted);status.Dock=DockStyle.Fill;status.Padding=new Padding(13,0,0,0);connection.Controls.Add(status);
            var disconnect=new FlatButton{Text="断开",Width=70,Dock=DockStyle.Right};connection.Controls.Add(disconnect);disconnect.Click+=delegate{try{Detach();Note("已断开，持续修改的原值已恢复。",false);}catch(Exception e){Error(e);}};
            var footerPanel=new Panel{Dock=DockStyle.Bottom,Height=57};area.Controls.Add(footerPanel);footer=Theme.Label("先进入单人关卡，再启用需要的功能。",8.5f,Theme.Muted);footer.Dock=DockStyle.Fill;footerPanel.Controls.Add(footer);var reset=new FlatButton{Text="全部还原  F12",Dock=DockStyle.Right,Width=137};footerPanel.Controls.Add(reset);reset.Click+=delegate{ResetAll();};
            var spacer=new Panel{Dock=DockStyle.Top,Height=17};area.Controls.Add(spacer);content.Dock=DockStyle.Fill;area.Controls.Add(content);area.Controls.SetChildIndex(content,0);area.Controls.SetChildIndex(spacer,1);area.Controls.SetChildIndex(connection,2);area.Controls.SetChildIndex(header,3);area.Controls.SetChildIndex(footerPanel,4);
            for(int i=0;i<4;i++){pages[i]=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Theme.Bg};content.Controls.Add(pages[i]);}
            BuildOverview();BuildFeatures();LoadSettings();ShowPage(0);Shown+=delegate{int dark=1;try{DwmSetWindowAttribute(Handle,20,ref dark,4);}catch{}connect.SetBounds(header.Width-104,29,104,36);launch.SetBounds(header.Width-219,29,104,36);foreach(Control c in pages[0].Controls)if(c is Telemetry||c is Panel)c.Width=pages[0].ClientSize.Width;Attach();};timer.Interval=60;timer.Tick+=delegate{Pulse();};timer.Start();KeyPreview=true;KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.F1){Help();e.Handled=true;}};
        }
        void BuildOverview(){
            var p=pages[0];var heading=Theme.Label("准备好，再出发。",18,Theme.Text);heading.SetBounds(0,2,750,43);p.Controls.Add(heading);var desc=Theme.Label("连接本机角色，按需调整你的冒险。",9,Theme.Muted);desc.SetBounds(1,48,750,27);p.Controls.Add(desc);telemetry.BackColor=Theme.Card;telemetry.SetBounds(0,95,750,100);telemetry.Anchor=AnchorStyles.Top|AnchorStyles.Left;p.Controls.Add(telemetry);
            var range=new FlatButton{Text="前往移动与抓钩",Accent=true};range.SetBounds(19,290,196,37);range.Click+=delegate{ShowPage(3);};var feature=new Panel{BackColor=Theme.Card};feature.SetBounds(0,214,750,132);feature.Anchor=AnchorStyles.Top|AnchorStyles.Left;p.Controls.Add(feature);var t=Theme.Label("钩锁无视距离限制",13,Theme.Text);t.SetBounds(19,13,450,32);feature.Controls.Add(t);var d=Theme.Label("将搜索射程与绳长扩大至 100 km，仍需命中已加载的实体。",9,Theme.Muted);d.SetBounds(20,48,700,26);d.Anchor=AnchorStyles.Top|AnchorStyles.Left;feature.Controls.Add(d);range.Location=new Point(19,85);feature.Controls.Add(range);
            var how=Theme.Label("快速开始",11,Theme.Text);how.SetBounds(0,371,700,29);p.Controls.Add(how);var lines=Theme.Label("1   启动游戏，载入单人关卡。\n2   等待状态显示已连接，打开需要的开关或应用数值。\n3   F12 全部还原；退出修改器也会恢复持续修改的属性。",9,Theme.Muted);lines.SetBounds(0,410,750,90);p.Controls.Add(lines);p.Resize+=delegate{telemetry.Width=p.ClientSize.Width;feature.Width=p.ClientSize.Width;heading.Width=how.Width=lines.Width=p.ClientSize.Width;desc.Width=Math.Max(100,p.ClientSize.Width-2);d.Width=Math.Max(100,feature.ClientSize.Width-40);if(p.IsHandleCreated) p.BeginInvoke((Action)delegate{if(!p.IsDisposed)p.PerformLayout();});};
        }
        FeatureRow Add(int page,string id,string name,string hint,string key,string kind,double def,double min,double max,int decimals,uint mods,uint vk){
            var r=new FeatureRow(id,name,hint,key,kind,def,min,max,decimals);r.Execute=Execute;r.Restore=Restore;rows.Add(r);var p=pages[page];int index=p.Controls.OfType<FeatureRow>().Count();r.SetBounds(0,index*58,p.ClientSize.Width,58);r.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;p.Controls.Add(r);int hotid=(int)(vk|(mods<<8));hotkeys[hotid]=r;r.Tag=new uint[]{mods,vk};return r;
        }
        void BuildFeatures(){
            Add(1,"health","无限生命","保持生命，并关闭角色受伤标志","小键盘 1","toggle",0,0,0,0,0,0x61);
            Add(1,"heal","补充生命","立即恢复至当前生命上限","小键盘 2","action",0,0,0,0,0,0x62);
            Add(1,"lowHealth","低生命值","将当前生命降至 1","小键盘 3","action",0,0,0,0,0,0x63);
            Add(1,"mana","无限法力","持续补充法力至 100","小键盘 4","toggle",0,0,0,0,0,0x64);
            Add(1,"refillMana","补充法力","立即将法力设为 100","小键盘 5","action",0,0,0,0,0,0x65);
            Add(1,"lowMana","法力不足","将当前法力设为 5","小键盘 6","action",0,0,0,0,0,0x66);
            Add(1,"manaRate","法力恢复速度","每秒恢复量；还原恢复启用前原值","小键盘 7","value",10,0,1000,1,0,0x67);
            Add(1,"adrenalineLock","极限肾上腺素","持续保持肾上腺素为 100","小键盘 8","toggle",0,0,0,0,0,0x68);
            Add(1,"adrenaline","设定肾上腺素","立即设置，范围 0～100","小键盘 9","value",100,0,100,0,0,0x69);
            Add(2,"ammoLock","无限弹药","持续补充当前武器弹匣与备弹","Alt + 小键盘 1","toggle",0,0,0,0,1,0x61);
            Add(2,"ammo","设置弹药","同时设置当前枪械弹匣和备弹","Alt + 小键盘 2","value",999,0,99999,0,1,0x62);
            Add(3,"grappleRange","钩锁无视距离限制","搜索射程与两种绳长扩大至 100 km","Ctrl + 小键盘 9","toggle",0,0,0,0,2,0x69);
            Add(3,"grappleLock","无限抓钩射击","持续补充至当前最大抓钩次数","Ctrl + 小键盘 1","toggle",0,0,0,0,2,0x61);
            Add(3,"refillGrapple","补充抓钩弹药","立即恢复至当前最大抓钩次数","Ctrl + 小键盘 2","action",0,0,0,0,2,0x62);
            Add(3,"grappleCount","最大抓钩次数","维持设定上限；支持已升级角色","Ctrl + 小键盘 3","value",10,1,100000,0,2,0x63);
            Add(3,"jumpLock","无限跳跃","允许在空中连续跳跃","Ctrl + 小键盘 4","toggle",0,0,0,0,2,0x64);
            Add(3,"jumpCount","跳跃次数","关闭无限跳跃后使用此上限","Ctrl + 小键盘 5","value",10,1,1000,0,2,0x65);
            Add(3,"gravity","重力比例","0 为无重力；通常原值为 4","Ctrl + 小键盘 6","value",2,0,20,2,2,0x66);
            Add(3,"jumpHeight","跳跃高度","调整起跳速度；通常原值为 2100","Ctrl + 小键盘 7","value",3000,100,20000,0,2,0x67);
            Add(3,"speed","玩家速度","角色动作与移动倍率，1 为正常速度","Ctrl + 小键盘 8","value",2,0.1,10,2,2,0x68);
            foreach(var r in rows){r.Sync(false,false);if(r.Input!=null){r.Input.Enter+=delegate{UpdateKeys();};r.Input.Leave+=delegate{if(IsHandleCreated)BeginInvoke((Action)UpdateKeys);};}}
        }
        void ShowPage(int n){page=n;for(int i=0;i<4;i++){pages[i].Visible=i==n;navButtons[i].Selected=i==n;navButtons[i].Invalidate();}pages[n].BringToFront();pages[n].PerformLayout();}
        void Execute(FeatureRow r){try{if(trainer==null||!trainer.Ready)throw new InvalidOperationException("请先连接游戏并进入单人关卡");if(r.IsToggle)trainer.Toggle(r.Id,!trainer.Toggles.Contains(r.Id));else if(r.IsAction)trainer.Action(r.Id);else trainer.Apply(r.Id,r.Value);Note((r.IsToggle?(trainer.Toggles.Contains(r.Id)?"已开启：":"已关闭："):r.IsAction?"已执行：":"已应用：")+r.AccessibleNameOrTitle(),false);trainer.ReadStats();}catch(Exception e){Error(e);}Sync();}
        void Restore(FeatureRow r){try{if(trainer!=null)trainer.ResetValue(r.Id);Note("已恢复该项目的原值。",false);}catch(Exception e){Error(e);}Sync();}
        void ResetAll(){try{if(trainer!=null)trainer.ResetAll();Note("已关闭全部开关，并恢复持续修改的原值。",false);}catch(Exception e){Error(e);}Sync();}
        void Sync(){bool ready=trainer!=null&&trainer.Ready;foreach(var r in rows)r.Sync(ready,trainer!=null&&(r.IsToggle?trainer.Toggles.Contains(r.Id):trainer.Values.ContainsKey(r.Id)));telemetry.Trainer=trainer;telemetry.Invalidate();int active=trainer==null?0:trainer.Toggles.Count+trainer.Values.Count;countLabel.Text=active==0?"20 项修改功能":active+" 项保持中";status.Text="●  "+(trainer==null?attaching?"正在校验游戏版本…":"未连接游戏":trainer.Status);status.ForeColor=ready?Theme.Green:Theme.Muted;connect.Text=trainer!=null?"重新连接":"连接游戏";connect.Enabled=!attaching;}
        void Note(string text,bool error){footer.Text=text;footer.ForeColor=error?Theme.Error:Theme.Muted;}
        void Error(Exception e){Note(e.Message,true);}
        Process Target(){foreach(var p in Process.GetProcessesByName("Greylock-Win64-Shipping")){try{if(string.Equals(Path.GetFullPath(p.MainModule.FileName),Path.GetFullPath(Path.Combine(install,@"Greylock\Binaries\Win64\Greylock-Win64-Shipping.exe")),StringComparison.OrdinalIgnoreCase))return p;}catch{}}return null;}
        void Attach(){
            if(attaching||closing)return;lastAttempt=DateTime.UtcNow;Process p=Target();if(p==null){Note("未找到游戏进程，请先启动游戏。",false);Sync();return;}
            try{if(trainer!=null){trainer.Dispose();trainer=null;}}catch(Exception e){Error(e);return;}
            int generation=++attachGeneration;attaching=true;Sync();Task.Factory.StartNew(delegate{return new Trainer(p);}).ContinueWith(t=>{
                if(closing||generation!=attachGeneration){if(t.Status==TaskStatus.RanToCompletion)t.Result.Dispose();return;}attaching=false;if(t.IsFaulted){if(p.HasExited)Note("游戏已退出，等待下次启动。",false);else Error(t.Exception.GetBaseException());}else{trainer=t.Result;Note(trainer.Ready?"连接成功，可以启用修改功能。":"已识别游戏，等待进入单人关卡。",false);}Sync();
            },TaskScheduler.FromCurrentSynchronizationContext());
        }
        void Detach(){++attachGeneration;attaching=false;if(trainer!=null){trainer.Dispose();trainer=null;}lastAttempt=DateTime.UtcNow.AddYears(1);Sync();}
        void Launch(){try{if(!File.Exists(Path.Combine(install,"Greylock.exe")))throw new FileNotFoundException("找不到游戏目录，请在使用说明中更改目录。");Process.Start(new ProcessStartInfo("steam://run/1836730"){UseShellExecute=true});lastAttempt=DateTime.MinValue;Note("已请求 Steam 启动游戏，进入主菜单后自动连接。",false);}catch(Exception e){Error(e);}}
        void Pulse(){
            if(closing)return;ticks++;
            try{if(trainer!=null&&trainer.U.Process.HasExited){trainer.Dispose();trainer=null;Note("游戏已退出，等待下次启动。",false);Sync();}if(trainer!=null){trainer.Tick();if(ticks%7==0){trainer.ReadStats();Sync();}}else if(!attaching&&(DateTime.UtcNow-lastAttempt).TotalSeconds>3){lastAttempt=DateTime.UtcNow;if(Target()!=null)Attach();else Note("等待启动游戏。",false);}}
            catch(Exception e){if(trainer!=null&&trainer.U.Process.HasExited){trainer.Dispose();trainer=null;Note("游戏已退出，等待下次启动。",false);}else{Error(e);if(trainer!=null){try{trainer.ResetAll();}catch{} }}Sync();}
            uint foregroundPid;GetWindowThreadProcessId(GetForegroundWindow(),out foregroundPid);bool foreground=foregroundPid==(uint)Process.GetCurrentProcess().Id||trainer!=null&&foregroundPid==(uint)trainer.U.Process.Id;short f12=GetAsyncKeyState(0x7b);bool held=(f12&0x8000)!=0;if(useHotkeys.Checked&&foreground&&!f12Held&&(f12&0x8001)!=0)ResetAll();f12Held=held;
            if(ticks%2==0)UpdateKeys();
        }
        bool Editing(){Control focused=ActiveControl;while(focused is ContainerControl&&((ContainerControl)focused).ActiveControl!=null)focused=((ContainerControl)focused).ActiveControl;return focused is TextBoxBase||focused is UpDownBase;}
        void UpdateKeys(){uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);bool own=pid==(uint)Process.GetCurrentProcess().Id;bool wanted=useHotkeys.Checked&&!(own&&Editing())&&(own||trainer!=null&&pid==(uint)trainer.U.Process.Id);if(wanted==keysRegistered)return;foreach(int id in registered)UnregisterHotKey(Handle,id);registered.Clear();keysRegistered=wanted;if(!wanted)return;
            var failed=new List<string>();foreach(var kv in hotkeys){uint[] v=(uint[])kv.Value.Tag;if(RegisterHotKey(Handle,kv.Key,v[0]|0x4000,v[1]))registered.Add(kv.Key);else failed.Add(kv.Value.AccessibleNameOrTitle());}if(failed.Count>0)Note("快捷键被占用："+string.Join("、",failed)+"；请使用界面按钮。",true);
        }
        protected override void WndProc(ref Message m){if(m.Msg==0x312){if(Editing()&&ContainsFocus)return;int id=m.WParam.ToInt32();FeatureRow row;if(hotkeys.TryGetValue(id,out row))row.InvokeFeature();return;}base.WndProc(ref m);}
        void Help(){
            var dialog=new Form{Text="使用说明",Font=Theme.Font(9.5f,FontStyle.Regular),BackColor=Theme.Bg,ForeColor=Theme.Text,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(680,550),MinimumSize=new Size(600,450),MaximizeBox=false,MinimizeBox=false};
            var text=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BackColor=Theme.Bg,ForeColor=Theme.Text,BorderStyle=BorderStyle.None,Text="Echo Point Nova 单人修改器 1.0\r\n\r\n支持：游戏 3.23 / Steam build 22752386 / Windows x64。\r\n\r\n先启动游戏并载入单人关卡，再打开修改器。已运行的游戏会自动连接。所有开关初始关闭，输入数值后点击应用。\r\n\r\n持续开关与数值设置：关闭、还原、F12 或退出修改器，会恢复修改器首次应用该属性时的值。生命、法力、肾上腺素与弹药的即时补充会保留。\r\n\r\n快捷键与每行右侧一致，需要开启 Num Lock。仅在游戏或修改器位于前台时注册快捷键。F12 全部还原，F1 打开说明。\r\n\r\n钩锁无视距离限制：将搜索射程和普通/光环钩索绳长设为 100 km。仍需瞄准可抓取且已经加载的实体；不会穿墙或加载远处地形。\r\n\r\n玩家速度调整角色时间倍率，会同时改变移动、动作与角色相关计时。跳跃高度调整起跳速度。无限弹药作用于当前枪械，切枪后自动跟随。\r\n\r\n无限生命开启时，低生命操作会提示先关闭开关；无限法力同理。无限跳跃优先于设置的跳跃次数，关闭后恢复自定义次数。\r\n\r\n程序校验可执行文件指纹及属性。游戏升级后若提示未适配，应使用与版本对应的修改器。加入联机时会暂停修改。\r\n\r\n游戏目录：\r\n"+install};dialog.Controls.Add(text);text.Margin=new Padding(22);var wrap=new Panel{Dock=DockStyle.Fill,Padding=new Padding(22)};dialog.Controls.Clear();wrap.Controls.Add(text);dialog.Controls.Add(wrap);var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=53,Padding=new Padding(15,8,10,8),FlowDirection=FlowDirection.RightToLeft};var close=new FlatButton{Text="关闭",Size=new Size(100,32)};close.Click+=delegate{dialog.Close();};buttons.Controls.Add(close);var path=new FlatButton{Text="更改游戏目录",Size=new Size(130,32)};path.Click+=delegate{using(var f=new FolderBrowserDialog{Description="选择包含 Greylock.exe 的游戏安装目录",SelectedPath=install}){if(f.ShowDialog(dialog)==DialogResult.OK){if(File.Exists(Path.Combine(f.SelectedPath,"Greylock.exe"))){install=f.SelectedPath;SaveSettings();dialog.Close();Note("游戏目录已更新，请重新连接。",false);}else MessageBox.Show(dialog,"所选目录中没有 Greylock.exe。");}}};buttons.Controls.Add(path);dialog.Controls.Add(buttons);dialog.ShowDialog(this);
        }
        void LoadSettings(){try{if(!File.Exists(SettingsPath))return;foreach(string line in File.ReadAllLines(SettingsPath)){int eq=line.IndexOf('=');if(eq<=0)continue;string key=line.Substring(0,eq),val=line.Substring(eq+1);if(key=="path"){install=val;continue;}var r=rows.Find(x=>x.Id==key&&x.Input!=null);decimal v;if(r!=null&&decimal.TryParse(val,NumberStyles.Any,CultureInfo.InvariantCulture,out v)&&v>=r.Input.Minimum&&v<=r.Input.Maximum)r.Input.Value=v;}}catch{}}
        void SaveSettings(){try{Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));var lines=rows.Where(r=>r.Input!=null).Select(r=>r.Id+"="+r.Input.Value.ToString(CultureInfo.InvariantCulture)).ToList();lines.Add("path="+install);File.WriteAllLines(SettingsPath,lines);}catch{}}
        protected override void OnFormClosing(FormClosingEventArgs e){try{if(trainer!=null){trainer.Dispose();trainer=null;}}catch(Exception ex){Note(ex.Message,true);e.Cancel=true;MessageBox.Show(this,ex.Message+"\n请先在游戏中返回主菜单，或退出游戏后再关闭修改器。","原值恢复未完成",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}closing=true;timer.Stop();foreach(int id in registered)UnregisterHotKey(Handle,id);SaveSettings();base.OnFormClosing(e);}
        public void CapturePages(string folder){Directory.CreateDirectory(folder);for(int pass=0;pass<2;pass++){if(pass==1)Size=MinimumSize;for(int i=0;i<4;i++){ShowPage(i);PerformLayout();pages[i].PerformLayout();Application.DoEvents();using(var b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(0,0,Width,Height));b.Save(Path.Combine(folder,(pass==0?"page-":"minimum-")+i+".png"));}}}ShowPage(0);}
    }
    static class RowExtensions {public static string AccessibleNameOrTitle(this FeatureRow r){return r.Controls.OfType<Label>().First().Text;}}
    static class Program {
        [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
        [STAThread]static void Main(string[] args){try{SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var mutex=new System.Threading.Mutex(false,"Local\\EchoPointNovaTrainer_323")){bool acquired=false;try{acquired=mutex.WaitOne(0,false);if(!acquired){MessageBox.Show("修改器已经在运行。","Echo Point Nova");return;}var form=new MainForm();if(args.Length>1&&args[0]=="--capture"){form.Shown+=delegate{var t=new Timer{Interval=1600};t.Tick+=delegate{t.Stop();form.CapturePages(args[1]);form.Close();};t.Start();};}Application.Run(form);}finally{if(acquired)mutex.ReleaseMutex();}}}catch(Exception e){MessageBox.Show(e.Message,"Echo Point Nova 修改器",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    }
}
