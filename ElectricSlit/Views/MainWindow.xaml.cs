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

namespace ElectricSlit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const int SC_CLOSE = 0xF060;
        private const int WM_SYSCOMMAND = 0x0112;

        public string portName = "";

        public SerialPortHelper _serialPort_Motor = null;
        public MotorEntity _motorEntity = null;
        public MotorFunc _motorFunc = null;

        public PortSetWindow portSetWindow = null;
        public ToolWindow toolWindow = null;
        public AboutWindow aboutWindow = null;
        public TipsWindow tipsWindow = null;

        private MainWindowViewModel mainWindowviewModel = null;

        private int tableCount = 0;//列表计数
        public List<double> list_Light = new List<double>();//主光源照度设定列表
        public List<double> list_ManualLight = new List<double>();//手动光源照度设定列表

        public List<WLModel> list_wl = null; //当前listview的 宽度-照度 列表 电动
        public List<WLModel> list_wlM = null; //当前listview的 宽度-照度 列表 手动

        public List<IColorTempViewModel> list_ic = null; //电流-色温 列表

        public PlotModel pointModel = null;//图表

        public double a, b, c = 0;//映射 二次多项式系数
        public double maxLight = 10000;//不用
        public double maxLight2; //最大照度 全开/50mm

        public ObservableCollection<string> PortList { get; set; } = new ObservableCollection<string>();//当前串口列表
        public double CurrentPosition;
        private Thread thread_getPosition = null;

        private static string exePath;
        private static string debugFolderPath;
        private static string projectFolderPath;


        public MainWindow()
        {
            InitializeComponent();

            mainWindowviewModel = new MainWindowViewModel();
            this.Loaded += MainWindow_Loaded;
        }

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

        //界面进度条
        private void SetUI()
        {
            CurrentPosition = 0.0;//mm
            double sliderWidth = 200;
            //Slider_Position.Width = CurrentPosition / 50 * sliderWidth;
            //ProgressBar_Light.Width = CurrentPosition / 50 * sliderWidth;

            if(list_ic == null && list_wl == null && list_wlM == null)
            {
                list_ic = new List<IColorTempViewModel>();
                list_wl = new List<WLModel>();
                list_wlM = new List<WLModel>();

                list_ic.Add(new IColorTempViewModel(0, 0));
                //list_wl.Add(new WLModel(0, 0));
                //list_wlM.Add(new WLModel(0, 0));
                //ListView_Set.Items.Add(new LightSetModel(1, 0, 0));
                Readic();

                DataGrid_ColorTemp.ItemsSource = list_ic;

                DataGrid_ColorTemp.Columns[0].Header = "电流(A)    ";
                DataGrid_ColorTemp.Columns[1].Header = "色温(K)";

                DataGrid_Manual.ItemsSource = list_wlM;

                DataGrid_Manual.Columns[0].Header = "宽度(mm)            ";
                DataGrid_Manual.Columns[1].Header = "照度(lx)";

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

        //移动到指定亮度位置
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

        //添加光强百分比设定值
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

                lightSetModel.Width = Convert.ToDouble(CurrentPosition.ToString("f1"));
                lightManulModel.Width = lightSetModel.Width;//对应主光源狭缝宽度

                double lightset = Convert.ToDouble(TextBox_LightSet.Text.ToString());
                lightSetModel.Light = lightset;

                ListView_Set.Items.Add(lightSetModel); //添加到 ListView

                list_Light.Add(lightset); //主光源照度表

                if(CurrentPosition <= 50)
                {
                    list_wl.Add(new WLModel(CurrentPosition, lightset));//CurrentPosition 电动宽度-照度表
                    list_wlM.Add(new WLModel(lightSetModel.Width, 0));//手动宽度-照度表
                }

                if(lightSetModel.Width == 50)
                {
                    maxLight2 = lightset;//得到当前全开状态下输入的最大照度
                }

                CurrentPosition += 5;

                DataGrid_Manual.ItemsSource = null;
                DataGrid_Manual.ItemsSource = list_wlM;

                DataGrid_Manual.Columns[0].Header = "宽度(mm)            ";
                DataGrid_Manual.Columns[1].Header = "照度(lx)";
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

            if(TextBox_LightSet.Text != "" && selectedIndex >= 0)
            {
                if(list_Light[selectedIndex] > maxLight)
                {
                    //MessageBox.Show("设定值超出最大照度！", "错误");
                }

                else
                {
                    //double targetPosition = toolWindow.Gx(list_Light[selectedIndex]);//光强对应的实际位置
                    double targetPosition = list_wl[selectedIndex].Width;

                    if (_motorEntity != null)
                    {
                        _motorFunc.MoveToPosition(targetPosition, true);
                        Thread.Sleep(100);
                    }
            
                    //TextBox_Light.Text = list_Light[selectedIndex].ToString();
                    //ProgressBar_Light.Value = (list_Light[selectedIndex] / maxLight) * 100;
                }
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
                    int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...                            
                    ListView_Set.Items.RemoveAt(selectedIndex);
                    list_Light.RemoveAt(selectedIndex);

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

        //读取电流色温表
        private void Readic()
        {
            list_ic = new List<IColorTempViewModel>();

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

        //计算映射 显示图表
        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            if(ListView_Set.Items.Count < 2)
            {
                MessageBox.Show("请添加两组及以上的值", "错误");
            }

            if(ListView_Set.Items.Count >= 2)
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

                //对排序后区间 进行插值计算 并得到每个区间的新的照度列表
                if (list_wl.Count >= 2)
                {
                    for(int i = 0; i < list_wl.Count - 1; i++)
                    {
                        //IInterpolation interpolation = CubicSpline.InterpolateNaturalSorted(dList_Width, dList_Light);
                        IInterpolation interpolation = LinearSpline.InterpolateSorted(dList_Width, dList_Light);
                        IInterpolation interpolation1 = LinearSpline.InterpolateSorted(dList_Width, dList_LightM);

                        List<double> list_LightInter = new List<double>();//一个区间的插值后照度列表
                        List<double> list_LightInterOverlay = new List<double>(); //一个区间插值后再叠加辅助光源照度的列表

                        var series = new LineSeries
                        {
                            //MarkerType = MarkerType.Circle, // 散点的形状
                            Color = OxyColor.FromRgb(50, 108, 243),
                            Title = "S1"
                        };

                        var series1 = new LineSeries
                        {
                            Color = OxyColor.FromRgb(21, 211, 106),
                            Title = "S2"
                        };

                        //每个细分区间的上下限
                        int tx = Convert.ToInt32(Convert.ToDouble(dList_Width[i].ToString("f1")) * 10);
                        int ty = Convert.ToInt32(Convert.ToDouble(dList_Width[i+1].ToString("f1")) * 10);
                        
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
                            if(j == 500)
                            {
                                series.Points.Add(new DataPoint(j / 10, list_LightInter[j - tx]));
                                series1.Points.Add(new DataPoint(j / 10, list_LightInterOverlay[j - tx]));
                            }
                        }

                        /*//设置图例
                        Legend legend = new Legend
                        {
                            LegendPlacement = LegendPlacement.Outside,
                            LegendPosition = LegendPosition.TopRight,
                            LegendOrientation = LegendOrientation.Horizontal,
                            LegendBorderThickness = 1,
                            LegendTextColor = OxyColors.LightGray,
                        };
                        pointModel.Legends.Add(legend);*/

                        pointModel.Series.Add(series);
                        pointModel.Series.Add(series1);
                    }
                }
            }
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

    }
}
