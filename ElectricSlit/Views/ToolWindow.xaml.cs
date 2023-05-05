using Spire.Pdf.Exporting.XPS.Schema;
using Spire.Pdf.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
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
    /// ToolWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ToolWindow : Window
    {
        public MainWindow part_mainwindow = null;

        private const int SC_CLOSE = 0xF060;
        private const int WM_SYSCOMMAND = 0x0112;



        public ToolWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            this.part_mainwindow = mainWindow;
            this.Loaded += ToolWindow_Loaded;
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

        private void ToolWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        //狭缝调大
        private void Btn_Larger_Click(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

            if (TextBox_SlitStep != null)
            {
                singleStep = Convert.ToDouble(TextBox_SlitStep.Text.ToString());
            }

            if (part_mainwindow._motorEntity!= null)
            {
                part_mainwindow._motorFunc.MoveLeft(singleStep);
            }
        }

        //狭缝调小
        private void Btn_Smaller_Click(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

            if (TextBox_SlitStep != null)
            {
                singleStep = Convert.ToDouble(TextBox_SlitStep.Text.ToString());
            }

            if (part_mainwindow._motorEntity != null)
            {
                part_mainwindow._motorFunc.MoveRight(singleStep);
            }
        }
    
    
        private void Calculate()
        {
            double max = Convert.ToDouble(TextBox_SetMaxLight.Text.ToString());
            double half = Convert.ToDouble(TextBox_SetHalfLight.Text.ToString());

            part_mainwindow.a = (max - 2 * half) / 1250;
            part_mainwindow.b = (4 * half - max) / 50;
            part_mainwindow.c = 0;

            MessageBox.Show("计算完成!");
        }

        private void SaveConfig()
        {
            //string filePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\config\\abc.txt";
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\abc.txt");

            



            StreamWriter writer = new StreamWriter(filePath);
            writer.WriteLine(part_mainwindow.a.ToString());
            writer.WriteLine(part_mainwindow.b.ToString());
            writer.WriteLine(part_mainwindow.c.ToString());
            writer.WriteLine(part_mainwindow.maxLight.ToString());
            writer.Close();

            MessageBox.Show("保存完成!");
        }

        //f(x) = a*x^2 + b*x + c 解一元二次方程 得到光强对应位置
        public double Gx(double light)
        {
            ReadConfig();

            if(light >= 0 && light <= part_mainwindow.maxLight)
            { 
                double a = part_mainwindow.a;
                double b = part_mainwindow.b;
                double tc = part_mainwindow.c - light;

                double x1, x2;

                if(b * b - 4 * a * tc >= 0 && a != 0)
                {
                    x1 = (-b + Math.Sqrt(b * b - 4 * a * tc)) / (2 * a);
                    x2 = (-b - Math.Sqrt(b * b - 4 * a * tc)) / (2 * a);

                    if (x1 > 0 && x1 <= 50) return x1;
                    else if(x2 > 0 && x2 <= 50) return x2;
                }
                else
                {
                    return -1;
                }
            }
            return 0;
        }

        //读取 abc
        private void ReadConfig()
        {
            string filePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\config\\abc.txt";

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                StreamReader reader = new StreamReader(fs);
                string line1 = reader.ReadLine();
                string line2 = reader.ReadLine();
                string line3 = reader.ReadLine();
                string line4 = reader.ReadLine();

                part_mainwindow.a = Convert.ToDouble(line1);
                part_mainwindow.b = Convert.ToDouble(line2);
                part_mainwindow.c = Convert.ToDouble(line3);
                part_mainwindow.maxLight = Convert.ToDouble(line4);
            }
        }

        //计算系数
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Calculate();
        }

        //保存配置
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        //狭缝全开
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if(part_mainwindow._motorEntity != null)
            {
                part_mainwindow._motorFunc.MoveToLowerLimmit();
            }
        }

        //狭缝半开
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if(part_mainwindow._motorEntity != null)
            {
                part_mainwindow._motorFunc.MoveToPosition(25, true);
            }
        }
    }
}
