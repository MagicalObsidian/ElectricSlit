using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ElectricSlit.Views
{
    /// <summary>
    /// TipsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TipsWindow : Window
    {
        public MainWindow part_mainwindow = null;

        private const int SC_CLOSE = 0xF060;
        private const int WM_SYSCOMMAND = 0x0112;

        #region 窗口关闭按钮功能为隐藏
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND && wParam.ToInt32() == SC_CLOSE)
            {
                // 在此处实现将窗口隐藏而不是关闭的代码
                this.Hide();
                handled = true;
            }
            return IntPtr.Zero;
        }
        #endregion

        public TipsWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            this.part_mainwindow = mainWindow;
        }
    }
}
