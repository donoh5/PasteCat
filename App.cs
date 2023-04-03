using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Resources;
using System.Windows.Interop;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

namespace PasteCat
{
    public class App : Application
    {
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            App app = new App();
            _ = app.Run();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case MYACTION_HOTKEY_ID_PASTE:
                            try
                            {
                                cc.Relocate();
                                cc.Show();
                                cc.Topmost = true;
                                _ = cc.Activate();
                            }
                            catch (Exception)
                            {

                            }
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            return IntPtr.Zero;
        }


        #region DLL

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion


        #region Constants

        private HwndSource _source;
        private ContextClipboard cc;
        private const int MYACTION_HOTKEY_ID_PASTE = 573;
        private System.Windows.Forms.NotifyIcon ni;
        private System.Windows.Forms.Timer _clipboardTimer;
        private string _lastClipboardText;
        private const int MaxHistorySize = 100;
        private readonly string _historyFilePath = "clipboard_history.json";
        private UserSettings userSettings;
        private bool _isRegistered = false;

        #endregion


        #region Override Functions

        protected override void OnStartup(StartupEventArgs e)
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = RenderMode.Default;

            base.OnStartup(e);

            userSettings = new UserSettings();
            userSettings.GenerateINI();

            InitializeClipboardTimer();
            GenerateAppIcon();

            cc = new ContextClipboard();

            _source = new HwndSource(new HwndSourceParameters());
            RegisterHotKey();

            if (!Clipboard.ContainsText())
            {
                Clipboard.SetText("");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_source != null)
            {
                UnregisterHotKey();
            }

            if (ni != null)
            {
                ni.Visible = false;
                ni.Dispose();
            }

            if (_clipboardTimer != null)
            {
                _clipboardTimer.Stop();
                _clipboardTimer.Dispose();
            }

            base.OnExit(e);
        }

        #endregion


        #region API

        public void RegisterHotKey()
        {
            if (_isRegistered)
            {
                return;
            }
            _source.AddHook(HwndHook);
            _ = RegisterHotKey(_source.Handle, MYACTION_HOTKEY_ID_PASTE, 2, 0x56); //CTRL V
            _isRegistered = true;
        }

        public void UnregisterHotKey()
        {
            if (!_isRegistered)
            {
                return;
            }
            _source.RemoveHook(HwndHook);
            _ = UnregisterHotKey(_source.Handle, MYACTION_HOTKEY_ID_PASTE);
            _isRegistered = false;
        }

        public void OnClickNoPaste(ContextClipboard cc)
        {
            cc.Hide();
            RegisterHotKey();
        }

        public void OnClickPasteCatem(ContextClipboard cc)
        {
            cc.Hide();
            UnregisterHotKey();
            System.Windows.Forms.SendKeys.SendWait("^v");
            RegisterHotKey();
        }

        public void OnClickDeleteItem(int index)
        {
            List<ClipboardHistoryItem> history = LoadClipboardHistory();
            history.RemoveAt(index);
            SaveClipboardHistory(history);
        }

        public void OrderListItem(bool fromTop)
        {
            List<ClipboardHistoryItem> history = LoadClipboardHistory();
            if (!fromTop)
            {
                history.Reverse();
            }
            SaveClipboardHistory(history);
        }

        #endregion


        #region History Controller

        private void InitializeClipboardTimer()
        {
            _clipboardTimer = new System.Windows.Forms.Timer
            {
                Interval = 100 // Check every 0.1 second
            };
            _clipboardTimer.Tick += ClipboardTimer_Tick;
            _clipboardTimer.Start();
        }

        private void ClipboardTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (Clipboard.ContainsText())
                {
                    RegisterHotKey();
                    string clipboardText = Clipboard.GetText();
                    if (clipboardText != _lastClipboardText && clipboardText != "")
                    {
                        _lastClipboardText = clipboardText;

                        try
                        {
                            AddToClipboardHistory(clipboardText);
                        }
                        catch (Exception)
                        {
                            Current.Shutdown();
                        }
                    }
                }
                else
                {
                    UnregisterHotKey();
                }
            });
        }

        private void AddToClipboardHistory(string text)
        {
            List<ClipboardHistoryItem> history = LoadClipboardHistory();

            history.Insert(0, new ClipboardHistoryItem
            {
                Timestamp = DateTime.UtcNow,
                Text = text
            });

            if (history.Count > MaxHistorySize)
            {
                history.RemoveAt(history.Count - 1);
            }

            SaveClipboardHistory(history);
        }

        private List<ClipboardHistoryItem> LoadClipboardHistory()
        {
            if (File.Exists(_historyFilePath))
            {
                string json = File.ReadAllText(_historyFilePath);
                List<ClipboardHistoryItem> items = JsonConvert.DeserializeObject<List<ClipboardHistoryItem>>(json);
                return items.OrderByDescending(item => item.Timestamp).ToList();
            }
            else
            {
                return new List<ClipboardHistoryItem>();
            }
        }

        private void SaveClipboardHistory(List<ClipboardHistoryItem> history)
        {
            string json = JsonConvert.SerializeObject(history, Formatting.Indented);
            File.WriteAllText(_historyFilePath, json);

            cc.ClipboardList.ItemsSource = history;
            cc.ClipboardList.DisplayMemberPath = "Text";
        }

        #endregion


        #region Icon Controller

        private void GenerateAppIcon()
        {
            ni = new System.Windows.Forms.NotifyIcon();

            ResourceManager resourceManager = new ResourceManager("PasteCat.Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());
            ni.Icon = (System.Drawing.Icon)resourceManager.GetObject("pasteCat");
            ni.Visible = true;
            ni.Text = "PasteCat";

            System.Windows.Forms.ContextMenuStrip contextMenu = new System.Windows.Forms.ContextMenuStrip();
            System.Windows.Forms.ToolStripMenuItem setAsStartupItem = new System.Windows.Forms.ToolStripMenuItem("Start with Windows")
            {
                CheckOnClick = true
            };
            setAsStartupItem.Click += SetAsStartupItem_Click;
            _ = contextMenu.Items.Add(setAsStartupItem);

            System.Windows.Forms.ToolStripMenuItem refreshItem = new System.Windows.Forms.ToolStripMenuItem("Enable");
            refreshItem.Click += RefreshItem_Click;
            _ = contextMenu.Items.Add(refreshItem);

            System.Windows.Forms.ToolStripMenuItem closeItem = new System.Windows.Forms.ToolStripMenuItem("Close");
            closeItem.Click += CloseItem_Click;
            _ = contextMenu.Items.Add(closeItem);

            ni.ContextMenuStrip = contextMenu;
        }

        private void SetAsStartupItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.ToolStripMenuItem setAsStartupItem = sender as System.Windows.Forms.ToolStripMenuItem;
            if (setAsStartupItem.Checked)
            {
                userSettings.SetStartUp();
            }
            else
            {
                userSettings.CancelStartUp();
            }
        }

        private void RefreshItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(_lastClipboardText ?? "");
        }

        private void CloseItem_Click(object sender, EventArgs e)
        {
            Current.Shutdown();
        }


        #endregion


        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _ = MessageBox.Show((e.ExceptionObject as Exception).Message);
        }
    }
}
