using DevExpress.Pdf.Native;
using ElectricSlit.ViewModels;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using ImTools;
using MotorAPIPlus;
using MotorTestDemo.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;
using Task = System.Threading.Tasks.Task;
using MathNet.Numerics.Interpolation;
using OxyPlot;
using OxyPlot.Series;
using System.Windows.Markup;
using MathNet.Numerics;
using static ImTools.ImMap;
using OxyPlot.Axes;
using DevExpress.Xpf.Core.Native;
using OxyPlot.Legends;
using DevExpress.Utils.Filtering.Internal;
using Microsoft.Win32;

namespace ElectricSlit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const int SC_CLOSE = 0xF060;
        private const int WM_SYSCOMMAND = 0x0112;

        public string portName = ""; //连接的串口名

        public SerialPortHelper _serialPort_Motor = null; //串口实例
        public MotorEntity _motorEntity = null; //电机实例
        public MotorFunc _motorFunc = null; //电机功能实例

        public PortSetWindow portSetWindow = null; //子窗口对象
        public ToolWindow toolWindow = null;
        public AboutWindow aboutWindow = null;
        public TipsWindow tipsWindow = null;

        private MainWindowViewModel mainWindowviewModel = null;

        private int tableCount = 0;//列表计数
        public List<double> list_Light = null;//主光源照度设定列表
        public List<double> list_ManualLight = new List<double>();//手动光源照度设定列表

        public List<WLModel> list_wl = null; //当前listview的 宽度-照度 列表 电动
        public List<WLModel> list_wlM = null; //当前listview的 宽度-照度 列表 手动

        List<WLModel> list_LightInterWL = null;//插值后映射关系宽度照度表
        List<WLModel> list_LightInterWLM = null;//插值后映射关系宽度照度表(加辅助光源)

        List<double> list_k = null; //区间系数列表
        struct Interval //区间
        { 
            public double low { get; set; } //下限
            public double high { get; set; } //上限
            public double k { get; set; } //系数
        }
        List<Interval> list_interval = null; //插值后映射 区间和系数列表
        List<Interval> list_intervalPlus = null; //插值后映射 区间和系数列表（加辅助光源）

        public List<IColorTempViewModel> list_ic = null; //电流-色温 列表

        public bool useManual = false;//是否使用手动辅助光源

        public PlotModel pointModel = null;//图表

        public double a, b, c = 0;//映射 二次多项式系数
        public double maxLight = 10000;//不用
        public double maxLight2; //最大照度 全开/50mm

        public ObservableCollection<string> PortList { get; set; } = new ObservableCollection<string>();//当前串口列表
        public double CurrentPosition; //记录当前电机实时位置
        private Thread thread_getPosition = null; //获取电机实时位置的线程

        private static string exePath; //exe程序路径
        private static string debugFolderPath;
        private static string projectFolderPath;


        public MainWindow()
        {
            InitializeComponent();

            mainWindowviewModel = new MainWindowViewModel();
            this.Loaded += MainWindow_Loaded;
        }

        #region 窗体事件关闭进程
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
                this.Close();
                Process.GetCurrentProcess().Kill();
                //handled = true;
            }
            return IntPtr.Zero;
        }
        #endregion

        #region 初始化配置等
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Init();

            //开启显示实时位置的线程
            thread_getPosition = new Thread(new ThreadStart(RefreshPosition));
            thread_getPosition.Priority = ThreadPriority.Lowest;
            thread_getPosition.Start();

            //获取应用程序路径
            exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            debugFolderPath = System.IO.Path.GetDirectoryName(exePath);
            projectFolderPath = System.IO.Directory.GetParent(debugFolderPath).Parent.FullName;
        }

        //初始化
        private void Init()
        {
            //GroupBox_ControlPanel.IsEnabled = false;
            //GroupBox_Lux.IsEnabled = false;

            SetUI();

            toolWindow = new ToolWindow(this);
            //toolWindow.Hide();
            aboutWindow = new AboutWindow(this);
            tipsWindow = new TipsWindow(this);

            //初始化时打开串口连接窗口
            //portName = cbxSerialPortList.Text.ToString();
            portSetWindow = new PortSetWindow(this);
            //读取配置尝试连接
            portSetWindow.ReadCom();
            portSetWindow.CommonConnect();
            //portSetWindow.Show();
        }

        //界面初始化
        private void SetUI()
        {
            CurrentPosition = 0.0;//mm
            double sliderWidth = 200;
            //Slider_Position.Width = CurrentPosition / 50 * sliderWidth;
            //ProgressBar_Light.Width = CurrentPosition / 50 * sliderWidth;

            //初始化读取文件配置
            if(list_ic == null && list_wl == null && list_wlM == null && list_interval == null)
            {
                list_Light = new List<double>();
                list_ic = new List<IColorTempViewModel>();
                list_wl = new List<WLModel>();
                list_wlM = new List<WLModel>();
                list_LightInterWL = new List<WLModel>();
                list_LightInterWLM = new List<WLModel>();
                list_interval = new List<Interval>();
                list_intervalPlus = new List<Interval>();

                Readwl();
                ReadCurve();//读取映射
                Readic();

                //添加到 ListView
                for(int i = 0; i < list_wl.Count; i++)
                {
                    list_Light.Add(list_wl[i].Light);

                    LightSetModel lightSetModel = new LightSetModel();
                    lightSetModel.Width = list_wl[i].Width;
                    lightSetModel.Light = list_wl[i].Light;
                    ListView_Set.Items.Add(lightSetModel);
                }

                if (list_wl.Count > 0)
                {
                    maxLight2 = list_wl[list_wl.Count - 1].Light;

                    var xAxis = new LinearAxis
                    {
                        Position = AxisPosition.Bottom,
                        Title = "狭缝宽度(mm)",
                        Minimum = 0,
                        Maximum = 50
                    };
                    var yAxis = new LinearAxis
                    {
                        Position = AxisPosition.Left,
                        Title = "照度(lx)",
                        Minimum = 0
                    };

                    pointModel = new PlotModel();
                    var series = new LineSeries();
                    pointModel.Series.Add(series);
                    pointModel.Axes.Add(xAxis);
                    pointModel.Axes.Add(yAxis);
                    pointPlot.Model = pointModel;
                    pointPlot.InvalidatePlot();

                    CalAndPlot();
                }

                DataGrid_Manual.ItemsSource = null;
                DataGrid_Manual.ItemsSource = list_wlM;
                DataGrid_Manual.Columns[0].Header = "宽度(mm)            ";
                DataGrid_Manual.Columns[1].Header = "照度(lx)";
                DataGrid_Manual.IsEnabled = false;

                DataGrid_ColorTemp.ItemsSource = list_ic;
                DataGrid_ColorTemp.Columns[0].Header = "电流(A)    ";
                DataGrid_ColorTemp.Columns[1].Header = "色温(K)";
            }
        }

        //获取实时实际位置
/*        private async Task GetCurrentPositionAsync()
        {
            if (_motorEntity != null)
            {
                GetCurrentPosition();
                await Task.Delay(100);
            }
        }*/

        //间隔 1s 刷新显示位置
        public void RefreshPosition()
        {
            while(true)
            {
                GetCurrentPosition();
                Thread.Sleep(800);
            }
        }

        //获得实时位置
        public void GetCurrentPosition()
        {
            if(_motorEntity != null)
            {
                Thread.Sleep(100);
                CurrentPosition = 50 - _motorFunc.GetCurrentPosition();
                //CurrentPosition++;
                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    //范围外的值处理
                    if (CurrentPosition <= 0)
                    {
                        CurrentPosition = 0;
                    }
                    else if (CurrentPosition >= 50)
                    {
                        CurrentPosition = 50;
                    }

                    //四舍五入
                    if(CurrentPosition >= 49.9)
                    {
                        TextBlock_CurrentWidth.Text = Math.Ceiling(Convert.ToDouble(CurrentPosition.ToString("f1"))).ToString("f1");
                    }
                    else if(CurrentPosition > 0 && CurrentPosition <= 0.1)
                    {
                        TextBlock_CurrentWidth.Text = "0.0";
                    }
                    else
                    {
                        TextBlock_CurrentWidth.Text = CurrentPosition.ToString("f1");
                    }    
                });
            }
        }

        //获取串口列表
        public void GetPortList()
        {
            PortList?.Clear();
            SerialPort.GetPortNames().ToList().ForEach(p => PortList.Add(p));
            //cbxSerialPortList.DataContext = PortList;
        }

        //连接串口(未使用)
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        //电机初始化配置
        public void MotorConfig()
        {
            _motorEntity.SetPS();//设置为上下限位模式
            //_motorFunc.MoveToZero();//初始化置于零位
        }

        #endregion

        #region 按钮方法
        //单选按钮
        private double GetComboBox_SingleStep()
        {
            switch(ComboBox_SingleStep.SelectedIndex)
            {
                case 0 : 
                    return Convert.ToDouble(0.1);
                case 1:
                    return Convert.ToDouble(0.5);
                case 2:
                    return Convert.ToDouble(1);
                case 3:
                    return Convert.ToDouble(2);
                case 4:
                    return Convert.ToDouble(5);
            }
            return 0;
        }

        //狭缝调小
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            double singleStep = GetComboBox_SingleStep();
            if (CurrentPosition - singleStep >= 0)
            {
                if(_motorEntity != null)
                {
                    _motorFunc.MoveRight(singleStep);
                }
            }
        }

        //狭缝调大
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            double singleStep = GetComboBox_SingleStep();
            if (CurrentPosition + singleStep <= 50)
            { 
                if (_motorEntity != null)
                {
                    _motorFunc.MoveLeft(singleStep);
                }
            }
        }

        //移动到指定亮度位置(不用)
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            double targetPosition = 0;
/*            if (TextBox_targetPosition != null)
            {
                targetPosition = Convert.ToDouble(TextBox_targetPosition.Text.ToString());
            }*/

            if(_motorEntity != null)
            {
                _motorFunc.MoveToPosition(targetPosition, true);//仅设置可运动路径位原点右侧部分，位置坐标符号位正
            }
        }

        //狭缝全开
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.MoveToLowerLimmit();
            }
        }

        //狭缝全闭
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.MoveToUpperLimmit();
            }
        }

        //紧急停止
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            if(_motorEntity != null)
            {
                _motorFunc.DisEnable();
            }
        }

        //添加照度设定值
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            LightSetModel lightSetModel = new LightSetModel();
            LightManulModel lightManulModel = new LightManulModel();

            if(TextBox_LightSet.Text != "")
            {
                //编号(不用)
                tableCount++;
                lightSetModel.Index = tableCount;
                lightManulModel.Index = tableCount;

                GetCurrentPosition();
                Thread.Sleep(100);
                lightSetModel.Width = DoubleFormat(CurrentPosition);
                lightManulModel.Width = lightSetModel.Width;//对应主光源狭缝宽度

                double lightset = Convert.ToDouble(TextBox_LightSet.Text.ToString());
                lightSetModel.Light = lightset;

                ListView_Set.Items.Add(lightSetModel); //添加到 ListView

                list_Light.Add(lightset); //主光源照度表

                if(CurrentPosition <= 50)
                {
                    //添加数据
                    list_wl.Add(new WLModel(lightSetModel.Width, lightset));//CurrentPosition 电动宽度-照度表
                    list_wlM.Add(new WLModel(lightSetModel.Width, 0));//手动宽度-照度表
                }

                if(lightSetModel.Width == 50)
                {
                    maxLight2 = lightset;//得到当前全开状态下输入的最大照度
                }

                //CurrentPosition += 5;//测试 模拟狭缝移动

                DataGrid_Manual.ItemsSource = null;
                DataGrid_Manual.ItemsSource = list_wlM;

                DataGrid_Manual.Columns[0].Header = "宽度(mm)            ";
                DataGrid_Manual.Columns[1].Header = "照度(lx)";
            }
            else
            {
                MessageBox.Show("请输入值!", "错误");
            }
        }

        //删除选中项(不用)
        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            if (ListView_Set.SelectedIndex >= 0)
            {
                int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...
                ListView_Set.Items.RemoveAt(selectedIndex);
                //ListView_Manul.Items.RemoveAt(selectedIndex);
                list_Light.RemoveAt(selectedIndex);
                list_wl.RemoveAt(selectedIndex);
                list_wlM.RemoveAt(selectedIndex);
                tableCount--;
            }
        }

        //应用一个设定值
        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...

            //从列表中选择设定值
            if(TextBox_LightSet.Text != "" && selectedIndex >= 0)
            {
                if(list_Light[selectedIndex] > maxLight2)
                {
                    //MessageBox.Show("设定值超出最大照度！", "错误");
                }

                else
                {
                    //double targetPosition = toolWindow.Gx(list_Light[selectedIndex]);//光强对应的实际位置
                    double targetPosition = list_wl[selectedIndex].Width;

                    if (_motorEntity != null)
                    {
                        _motorFunc.MoveToPosition(50 - targetPosition, true);
                        Thread.Sleep(100);
                    }
            
                    //TextBox_Light.Text = list_Light[selectedIndex].ToString();
                    //ProgressBar_Light.Value = (list_Light[selectedIndex] / maxLight) * 100;
                }
            }

            //从输入框中获得应用值
            if(TextBox_LightTarget.Text != "")
            {
                double targetLight = Convert.ToDouble(TextBox_LightTarget.Text);

                if(targetLight >= 0 && targetLight < list_wl[list_wl.Count - 1].Light + list_wlM[list_wlM.Count-1].Light 
                    && list_interval.Count > 0 && list_intervalPlus.Count > 0)
                {
                    if(_motorEntity != null)//
                    {
                        double targetPosition = 0;
                        if(useManual) //如果使用辅助光源
                        {
                            targetPosition = GetIntervalPos(targetLight, list_intervalPlus);//应用主光源+辅助光源曲线
                        }
                        else
                        {
                            targetPosition = GetIntervalPos(targetLight, list_interval);
                        }

                        _motorFunc.MoveToPosition(50 - targetPosition, true);
                        Thread.Sleep(100);
                    }
                    else
                    {
                        MessageBox.Show("未连接设备!", "错误");
                    }
                }
                else
                {
                    MessageBox.Show("输入值不正确!", "错误");
                }
            }
            else
            {
                MessageBox.Show("未输入值!", "错误");
            }
        }

        private void ListView_Set_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(this);
            double x = position.X;
            double y = position.Y;

/*            ContextMenu_listview.PlacementTarget = sender as UIElement;
            ContextMenu_listview.Placement = PlacementMode.MousePoint;
            ContextMenu_listview.IsOpen = true;*/      
        }

        //Delete键删除
        private void ListView_Set_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                if (ListView_Set.SelectedIndex >= 0)//有选中一项
                {
                    int selectedIndex = ListView_Set.SelectedIndex;//0,1,2,...                            
                    ListView_Set.Items.RemoveAt(selectedIndex);

                    if(list_Light.Count > 0)
                    {
                        list_Light.RemoveAt(selectedIndex);
                    }

                    if (selectedIndex < list_wl.Count)
                    {
                        list_wl.RemoveAt(selectedIndex);
                        list_wlM.RemoveAt(selectedIndex);
                    }

                    tableCount--;

                    DataGrid_Manual.ItemsSource = null;
                    DataGrid_Manual.ItemsSource = list_wlM;

                    DataGrid_Manual.Columns[0].Header = "宽度(mm)            ";
                    DataGrid_Manual.Columns[1].Header = "照度(lx)";
                }
            }
        }

        //色温记录表 删除事件
        private void DataGrid_ColorTemp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if(DataGrid_ColorTemp.SelectedIndex >= 0)
                {
                    int selectedIndex = DataGrid_ColorTemp.SelectedIndex;

                    list_ic.RemoveAt(selectedIndex);

                    DataGrid_ColorTemp.ItemsSource = null;
                    DataGrid_ColorTemp.ItemsSource = list_ic;

                    DataGrid_ColorTemp.Columns[0].Header = "电流(A)            ";
                    DataGrid_ColorTemp.Columns[1].Header = "色温(K)      ";
                }
            }
        }

        //色温记录 修改
        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            DataGrid_ColorTemp.IsEnabled = true;

            list_ic.Add(new IColorTempViewModel(0, 0));

            DataGrid_ColorTemp.ItemsSource = null;
            DataGrid_ColorTemp.ItemsSource = list_ic;

            DataGrid_ColorTemp.Columns[0].Header = "电流(A)            ";
            DataGrid_ColorTemp.Columns[1].Header = "色温(K)      ";

            System.Windows.Media.Brush brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            DataGrid_ColorTemp.Background = brush;
        }

        //色温调节 保存
        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            DataGrid_ColorTemp.IsEnabled = false;
            System.Windows.Media.Brush brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
            DataGrid_ColorTemp.Background = brush;

            //保存到文件
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\ic.txt");

            if (filePath != null)
            {
                StreamWriter writer = new StreamWriter(filePath);
                writer.WriteLine("ic");
                for (int i = 0; i < list_ic.Count; i++)
                {
                    writer.WriteLine(list_ic[i].Current.ToString() + " " + list_ic[i].ColorTemp.ToString());
                }
                writer.Close();
                MessageBox.Show("保存完成!");
            }
        }

        //打开串口连接界面
        private void MenuSerialPort_Click(object sender, RoutedEventArgs e)
        {
            portSetWindow.Show();
        }

        //打开关于界面
        private void MenuAbout_Click_1(object sender, RoutedEventArgs e)
        {
            tipsWindow.Show();
        }

        //计算按钮事件
        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            CalAndPlot();
        }

        //保存映射到文件
        private void Button_Click_13(object sender, RoutedEventArgs e)
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\wl.txt");
            string filePath1 = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\curve.txt");
            string filePath2 = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\curvePlus.txt");

            if (filePath != null && filePath1 != null && filePath2 != null)
            {
                StreamWriter writer = new StreamWriter(filePath);
                StreamWriter writer1 = new StreamWriter(filePath1);
                StreamWriter writer2 = new StreamWriter(filePath2);

                writer.WriteLine("wl");
                writer1.WriteLine("curve");
                writer2.WriteLine("curvePlus");

                if (list_wl.Count > 1)
                {
                    for(int i = 0; i < list_wl.Count; i++)
                    {
                        writer.WriteLine(list_wl[i].Width + "\t"
                                        + list_wl[i].Light + "\t"
                                        + list_wlM[i].Light);
                    }

                    for(int j = 0; j < list_interval.Count; j++)
                    {
                        writer1.WriteLine(list_interval[j].low + "\t"
                                        + list_interval[j].high + "\t"
                                        + list_interval[j].k);
                    }

                    for(int k = 0; k < list_intervalPlus.Count; k++)
                    {
                        writer2.WriteLine(list_intervalPlus[k].low + "\t"
                                        + list_intervalPlus[k].high + "\t"
                                        + list_intervalPlus[k].k);
                    }

                    writer.Close();
                    writer1.Close();
                    writer2.Close();
                    MessageBox.Show("保存完成!");
                }
                else
                {
                    MessageBox.Show("没有输入值！");
                }
            }
        }

        //勾选辅助光源
        private void ToggleButton_ManualSwitch_Checked(object sender, RoutedEventArgs e)
        {
            useManual = true;
            TextBlock_ManualSwitch.Text = "辅助光源开";
            DataGrid_Manual.IsEnabled = true;
        }

        //取消辅助光源
        private void ToggleButton_ManualSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            useManual = false;
            TextBlock_ManualSwitch.Text = "辅助光源关";
            DataGrid_Manual.IsEnabled = false;
        }

        //文件导出 宽度-照度表
        private void MenuExportMap1_Click(object sender, RoutedEventArgs e)
        {
            if (list_wl.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "文本文件|*.txt";
                saveFileDialog.Title = "导出文件";
                saveFileDialog.ShowDialog();
                if (saveFileDialog.FileName != "")
                {
                    string outputPath = saveFileDialog.FileName;
                    StreamWriter writer = new StreamWriter(outputPath);
                    writer.WriteLine("wl");
                    for (int i = 0; i < list_wl.Count; i++)
                    {
                        writer.WriteLine(list_wl[i].Width + "\t"
                                        + list_wl[i].Light + "\t"
                                        + list_wlM[i].Light);
                    }
                    writer.Close();
                }
            }
        }

        //文件导出 主光源照度映射
        private void MenuExportMap2_Click(object sender, RoutedEventArgs e)
        {
            if (list_interval.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "文本文件|*.txt";
                saveFileDialog.Title = "导出文件";
                saveFileDialog.ShowDialog();
                if (saveFileDialog.FileName != "")
                {
                    string outputPath = saveFileDialog.FileName;
                    StreamWriter writer = new StreamWriter(outputPath);
                    writer.WriteLine("curve");
                    for (int i = 0; i < list_interval.Count; i++)
                    {
                        writer.WriteLine(list_interval[i].low + "\t"
                                        + list_interval[i].high + "\t"
                                        + list_interval[i].k);
                    }
                    writer.Close();
                }
            }
        }

        //文件导出 主光源+辅助光源映射
        private void MenuExportMap3_Click(object sender, RoutedEventArgs e)
        {
            if (list_intervalPlus.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "文本文件|*.txt";
                saveFileDialog.Title = "导出文件";
                saveFileDialog.ShowDialog();
                if (saveFileDialog.FileName != "")
                {
                    string outputPath = saveFileDialog.FileName;
                    StreamWriter writer = new StreamWriter(outputPath);
                    writer.WriteLine("curve");
                    for (int i = 0; i < list_intervalPlus.Count; i++)
                    {
                        writer.WriteLine(list_intervalPlus[i].low + "\t"
                                        + list_intervalPlus[i].high + "\t"
                                        + list_intervalPlus[i].k);
                    }
                    writer.Close();
                }
            }
        }

        //打开工具界面
        private void MenuTool_Click(object sender, RoutedEventArgs e)
        {
            //toolWindow.Show();
        }

        //打开关于界面
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            aboutWindow.Show();
        }


        #endregion

        #region 功能方法
        //计算映射 显示图表
        private void CalAndPlot()
        {
            if (ListView_Set.Items.Count < 2)
            {
                MessageBox.Show("请添加两组及以上的值", "错误");
            }

            if (ListView_Set.Items.Count >= 2)
            {
                double[] dList_Width = new double[list_wl.Count];//当前listview的 宽度 数组
                double[] dList_Light = new double[list_wl.Count];//当前listview的 照度 数组
                double[] dList_LightM = new double[list_wlM.Count]; //当前 datagrid 手动输入的照度 数组

                for (int i = 0; i < list_wl.Count; i++)
                {
                    //dList_Width[i] = CurrentPosition;
                    dList_Width[i] = list_wl[i].Width; //测试图表
                    dList_Light[i] = list_wl[i].Light;
                    dList_LightM[i] = list_wlM[i].Light;
                    //list_wl.Add(new WLModel(dList_Width[i], dList_Light[i]));
                }

                pointModel = new PlotModel();
                list_interval = new List<Interval>();
                list_intervalPlus = new List<Interval>();

                //对排序后区间 进行插值计算 并得到每个区间的新的照度列表
                if (list_wl.Count >= 2)
                {
                    for (int i = 0; i < list_wl.Count - 1; i++)
                    {
                        //IInterpolation interpolation = CubicSpline.InterpolateNaturalSorted(dList_Width, dList_Light);
                        IInterpolation interpolation = LinearSpline.InterpolateSorted(dList_Width, dList_Light);
                        IInterpolation interpolation1 = LinearSpline.InterpolateSorted(dList_Width, dList_LightM);

                        List<double> list_LightInter = new List<double>();//一个区间的插值后照度列表
                        List<double> list_LightInterOverlay = new List<double>(); //一个区间插值后再叠加辅助光源照度的列表

                        var series = new LineSeries
                        {
                            Color = OxyColor.FromRgb(50, 108, 243),
                        };
                        var series1 = new LineSeries
                        {
                            Color = OxyColor.FromRgb(21, 211, 106),
                        };

                        //只给最后一个 series 添加标签名
                        if (i == list_wl.Count - 2)
                        {
                            var seriesLast = new LineSeries
                            {
                                Color = OxyColor.FromRgb(50, 108, 243),
                                Title = "主光源照度"
                            };

                            var series1Last = new LineSeries
                            {
                                Color = OxyColor.FromRgb(21, 211, 106),
                                Title = "主光源+辅助光源照度"
                            };

                            int txLast = Convert.ToInt32(DoubleFormat(dList_Width[i]) * 10);
                            int tyLast = Convert.ToInt32(DoubleFormat(dList_Width[i + 1]) * 10);
                            double temp = DoubleFormat(dList_Width[i]);//保留小数点后一位 宽度
                            double kLast = (dList_Light[i + 1] - dList_Light[i])
                                / (DoubleFormat(dList_Width[i + 1]) - temp); //每个区间的系数 k//系数 k
                            double kLastM = ((dList_Light[i + 1] + dList_LightM[i + 1]) - (dList_Light[i] + dList_LightM[i]))
                                / (DoubleFormat(dList_Width[i + 1]) - temp); //每个区间的系数 k//系数 k

                            list_interval.Add(new Interval
                            {
                                low = DoubleFormat(dList_Light[i]),
                                high = DoubleFormat(dList_Light[i + 1]),
                                k = kLast
                            });

                            list_intervalPlus.Add(new Interval
                            {
                                low = DoubleFormat(dList_Light[i]) + DoubleFormat(dList_LightM[i]),
                                high = DoubleFormat(dList_Light[i + 1]) + DoubleFormat(dList_LightM[i + 1]),
                                k = kLastM
                            });

                            //步距为整数
                            for (int j = txLast; j <= tyLast; j++)
                            {
                                //得到每个细分区间插值后的照度列表
                                list_LightInter.Add(interpolation.Interpolate(j / 10));
                                list_LightInterOverlay.Add(list_LightInter[j - txLast] + interpolation1.Interpolate(j / 10));

                                //将点添加到图例中
                                seriesLast.Points.Add(new DataPoint(j / 10, list_LightInter[j - txLast]));
                                series1Last.Points.Add(new DataPoint(j / 10, list_LightInterOverlay[j - txLast]));

                                //添加最后一个点
                                if (j == 500)
                                {
                                    seriesLast.Points.Add(new DataPoint(j / 10, list_LightInter[j - txLast]));
                                    series1Last.Points.Add(new DataPoint(j / 10, list_LightInterOverlay[j - txLast]));

                                    list_LightInterWL.Add(new WLModel(j / 10, DoubleFormat(list_LightInter[j - txLast])));
                                    list_LightInterWLM.Add(new WLModel(j / 10, DoubleFormat(list_LightInterOverlay[j - txLast])));
                                }

                                //获得整个映射的列表
                                if (j < tyLast)
                                {
                                    list_LightInterWL.Add(new WLModel(j / 10, DoubleFormat(list_LightInter[j - txLast])));
                                    list_LightInterWLM.Add(new WLModel(j / 10, DoubleFormat(list_LightInterOverlay[j - txLast])));
                                }
                            }

                            //步距小数点后一位
                            /*for (int j = txLast; j <= tyLast; j++)
                            {
                                //得到每个细分区间插值后的照度列表
                                double dw = Convert.ToDouble(((j - txLast) * 0.1).ToString("f1"));
                                list_LightInter.Add(dList_Light[i] + kLast * dw);
                                list_LightInterOverlay.Add(list_LightInter[j - txLast] + dList_LightM[i] + kLast * dw);

                                //将点添加到图例中
                                seriesLast.Points.Add(new DataPoint(j, list_LightInter[j - txLast]));
                                series1Last.Points.Add(new DataPoint(j, list_LightInterOverlay[j - txLast]));

                                //添加最后一个点
                                if (j == 500)
                                {
                                    seriesLast.Points.Add(new DataPoint(j, list_LightInter[j - txLast]));
                                    series1Last.Points.Add(new DataPoint(j, list_LightInterOverlay[j - txLast]));

                                    list_LightInterWL.Add(new WLModel(temp, Convert.ToDouble(list_LightInter[j - txLast].ToString("f1"))));
                                    list_LightInterWLM.Add(new WLModel(temp, Convert.ToDouble(list_LightInterOverlay[j - txLast].ToString("f1"))));
                                }

                                //获得整个映射的列表
                                if (j < tyLast)
                                {
                                    list_LightInterWL.Add(new WLModel(temp, Convert.ToDouble(list_LightInter[j - txLast].ToString("f1"))));
                                    list_LightInterWLM.Add(new WLModel(temp, Convert.ToDouble(list_LightInterOverlay[j - txLast].ToString("f1"))));
                                }
                            }*/

                            pointModel.Series.Add(seriesLast);
                            pointModel.Series.Add(series1Last);
                        }
                        else
                        {
                            //每个细分区间的上下限
                            int tx = Convert.ToInt32(DoubleFormat(dList_Width[i]) * 10);
                            int ty = Convert.ToInt32(DoubleFormat(dList_Width[i + 1]) * 10);
                            double temp = DoubleFormat(dList_Width[i]);//保留小数点后一位 宽度
                            double k = (dList_Light[i + 1] - dList_Light[i])
                                / (DoubleFormat(dList_Width[i + 1]) - temp); //每个区间的系数 k
                            double kM = ((dList_Light[i + 1] + dList_LightM[i + 1]) - (dList_Light[i] + dList_LightM[i]))
                                / (DoubleFormat(dList_Width[i + 1]) - temp); //每个区间的系数 k

                            list_interval.Add(new Interval
                            {
                                low = DoubleFormat(dList_Light[i]),
                                high = DoubleFormat(dList_Light[i + 1]),
                                k = k
                            });

                            list_intervalPlus.Add(new Interval
                            {
                                low = DoubleFormat(dList_Light[i]) + DoubleFormat(dList_LightM[i]),
                                high = DoubleFormat(dList_Light[i + 1]) + DoubleFormat(dList_LightM[i + 1]),
                                k = kM
                            });

                            //步距为整数
                            for (int j = tx; j <= ty; j++)
                            {
                                //得到每个细分区间插值后的照度列表
                                list_LightInter.Add(interpolation.Interpolate(j / 10));
                                list_LightInterOverlay.Add(list_LightInter[j - tx] + interpolation1.Interpolate(j / 10));

                                //将点添加到图例中
                                //series.Points.Add(new ScatterPoint(j, list_LightInter[j - Convert.ToInt32(dList_Width[i])]));
                                series.Points.Add(new DataPoint(j / 10, list_LightInter[j - tx]));
                                series1.Points.Add(new DataPoint(j / 10, list_LightInterOverlay[j - tx]));

                                //添加最后一个点
                                if (j == 500)
                                {
                                    series.Points.Add(new DataPoint(j / 10, list_LightInter[j - tx]));
                                    series1.Points.Add(new DataPoint(j / 10, list_LightInterOverlay[j - tx]));

                                    list_LightInterWL.Add(new WLModel(j / 10, DoubleFormat(list_LightInter[j - tx])));
                                    list_LightInterWLM.Add(new WLModel(j / 10, DoubleFormat(list_LightInterOverlay[j - tx])));
                                }

                                //获得整个映射的列表
                                if (j < ty)
                                {
                                    list_LightInterWL.Add(new WLModel(j / 10, DoubleFormat(list_LightInter[j - tx])));
                                    list_LightInterWLM.Add(new WLModel(j / 10, DoubleFormat(list_LightInterOverlay[j - tx])));
                                }
                            }

                            //步距小数点后一位
                            /*for (int j = tx; j <= ty; j++)
                            {
                                //得到每个细分区间插值后的照度列表
                                double dw = Convert.ToDouble(((j - tx) * 0.1).ToString("f1"));
                                list_LightInter.Add(dList_Light[i] + k * dw);
                                list_LightInterOverlay.Add(list_LightInter[j - tx] + dList_LightM[i] + k * dw);
                                
                                //将点添加到图例中
                                //series.Points.Add(new ScatterPoint(j, list_LightInter[j - Convert.ToInt32(dList_Width[i])]));
                                series.Points.Add(new DataPoint(j, list_LightInter[j - tx]));
                                series1.Points.Add(new DataPoint(j, list_LightInterOverlay[j - tx]));

                                //添加最后一个点
                                if (j == 500)
                                {
                                    series.Points.Add(new DataPoint(j, list_LightInter[j - tx]));
                                    series1.Points.Add(new DataPoint(j, list_LightInterOverlay[j - tx]));

                                    list_LightInterWL.Add(new WLModel(temp, Convert.ToDouble(list_LightInter[j - tx].ToString("f1"))));
                                    list_LightInterWLM.Add(new WLModel(temp, Convert.ToDouble(list_LightInterOverlay[j - tx].ToString("f1"))));
                                }

                                //获得整个映射的列表
                                if (j < ty)
                                {
                                    list_LightInterWL.Add(new WLModel(temp, Convert.ToDouble(list_LightInter[j - tx].ToString("f1"))));
                                    list_LightInterWLM.Add(new WLModel(temp, Convert.ToDouble(list_LightInterOverlay[j - tx].ToString("f1"))));
                                }
                            }*/
                        }

                        //设置图例
                        Legend legend = new Legend
                        {
                            LegendPlacement = LegendPlacement.Inside,
                            LegendPosition = LegendPosition.LeftTop,
                            LegendOrientation = LegendOrientation.Horizontal,
                            LegendBorderThickness = 1,
                            LegendTextColor = OxyColors.DarkGray,
                        };
                        pointModel.Legends.Add(legend);

                        pointModel.Series.Add(series);
                        pointModel.Series.Add(series1);
                    }
                }

                //横纵坐标轴
                var xAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "狭缝宽度(mm)",
                    Minimum = 0,
                    Maximum = 50
                };
                var yAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "照度(lx)",
                    Minimum = 0
                };

                pointModel.Axes.Add(xAxis);
                pointModel.Axes.Add(yAxis);

                pointPlot.Model = pointModel;
                pointPlot.InvalidatePlot();
            }
        }

        //读取电流色温表
        private void Readic()
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\ic.txt");

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    StreamReader reader = new StreamReader(fs);
                    string line = reader.ReadLine();
                    int n = 0;
                    while (line != null)
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            var stringls = line.Split(' ');
                            if (stringls.Length == 2)
                            {
                                double.TryParse(stringls[0], out double i);
                                int.TryParse(stringls[1], out int c);
                                list_ic.Add(new IColorTempViewModel(i, c));
                            }
                        }
                        n++;
                    }
                }
                catch
                {

                }
            }
        }

        //读取宽度照度表
        private void Readwl()
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\wl.txt");

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    StreamReader reader = new StreamReader(fs);
                    string line = reader.ReadLine();
                    int n = 0;
                    while (line != null)
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            var stringls = line.Split("\t");
                            if (stringls.Length == 3)
                            {
                                double.TryParse(stringls[0], out double width);
                                double.TryParse(stringls[1], out double light);
                                double.TryParse(stringls[2], out double lightM);
                                list_wl.Add(new WLModel(width, light));
                                list_wlM.Add(new WLModel(width, lightM));
                            }
                        }
                        n++;
                    }
                }
                catch
                {

                }
            }
        }

        //读取主光源映射表/主光源+辅助光源映射表
        private void ReadCurve()
        {
            string filePath1 = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\curve.txt");
            string filePath2 = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, @"config\curvePlus.txt");

            using (FileStream fs = new FileStream(filePath1, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    StreamReader reader = new StreamReader(fs);
                    string line = reader.ReadLine();
                    int n = 0;
                    while (line != null)
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            var stringls = line.Split("\t");
                            if (stringls.Length == 3)
                            {
                                double.TryParse(stringls[0], out double low);
                                double.TryParse(stringls[1], out double high);
                                double.TryParse(stringls[2], out double k);
                                list_interval.Add(new Interval { low = low, high = high, k = k });
                            }
                        }
                        n++;
                    }
                }
                catch
                {

                }
            }

            using (FileStream fs = new FileStream(filePath2, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    StreamReader reader = new StreamReader(fs);
                    string line = reader.ReadLine();
                    int n = 0;
                    while (line != null)
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            var stringls = line.Split("\t");
                            if (stringls.Length == 3)
                            {
                                double.TryParse(stringls[0], out double low);
                                double.TryParse(stringls[1], out double high);
                                double.TryParse(stringls[2], out double k);
                                list_intervalPlus.Add(new Interval { low = low, high = high, k = k });
                            }
                        }
                        n++;
                    }
                }
                catch
                {

                }
            }
        }

        //输入指定照度值，从映射表中得到目标位置
        private double GetIntervalPos(double number, List<Interval> intervals)
        {
            for(int i = 0; i < intervals.Count; i++)
            {
                if(number >= intervals[i].low && number < intervals[i].high)
                {
                    return intervals[i].low + number / intervals[i].k;
                }
            }
            return 0;
        }
        
        //double 数据保留一位小数
        private double DoubleFormat(double num)
        {
            return Convert.ToDouble(num.ToString("f1"));
        }
        #endregion
    }
}
