using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using Spire;
using Spire.Doc;
using Spire.Pdf;
using Microsoft.Win32;
using MoonPdfLib;
using HandyControl.Tools.Extension;

namespace ElectricSlit.Views
{
    /// <summary>
    /// AboutWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AboutWindow : Window
    {
        public MainWindow part_mainwindow = null;

        private const int SC_CLOSE = 0xF060;
        private const int WM_SYSCOMMAND = 0x0112;


        public AboutWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            this.part_mainwindow = mainWindow;
            this.Loaded += AboutWindow_Loaded;
        }

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

        private void AboutWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Test()
        {
            Thread thread = new Thread(OpenWord);

            if (thread.ThreadState == System.Threading.ThreadState.Unstarted)
            {
                thread.Start();
            }
        }

        private static void OpenWord()
        {
            //String fileName = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\doc\\readme.docx";//输入打开文件路径
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"doc\readme.docx");
            //String fileName = System.Environment.CurrentDirectory + "\\doc\\readme.docx";
            Process.Start(fileName);
        }

        //打开文档
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"doc\readme.pdf");
            moonPdfPanel.OpenFile(fileName);

            moonPdfPanel.Zoom(1.5);
            moonPdfPanel.ZoomToWidth();
        }



    }
}
