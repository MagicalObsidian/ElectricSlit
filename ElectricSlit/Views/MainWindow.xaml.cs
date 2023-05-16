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
        private int tableCount = 0;
        public List<double> list_Light = new List<double>();

        public List<IColorTempViewModel> list_ic = null;
        public List<WLModel> list_wl = null; //当前listview的 宽度-照度 列表 
        public PlotModel pointModel = null;
        public double a, b, c = 0;//映射 二次多项式系数
        public double maxLight = 10000;

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
            CurrentPosition = 50;//mm
            double sliderWidth = 200;
            //Slider_Position.Width = CurrentPosition / 50 * sliderWidth;
            //ProgressBar_Light.Width = CurrentPosition / 50 * sliderWidth;

            if(list_ic == null)//&& list_wl == null
            {
                list_ic = new List<IColorTempViewModel>();
                list_wl = new List<WLModel>();

                list_ic.Add(new IColorTempViewModel(0, 0));
                Readic();

                DataGrid_ColorTemp.ItemsSource = list_ic;

                DataGrid_ColorTemp.Columns[0].Header = "电流(A)            ";
                DataGrid_ColorTemp.Columns[1].Header = "色温(K)";

                var xAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "狭缝宽度(mm)"
                };
                var yAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "照度(Lx)"
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
                Thread.Sleep(1000);
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

                //TextBox_Position.Text = CurrentPosition.ToString();
                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    TextBlock_CurrentWidth.Text = CurrentPosition.ToString("f2");
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
        private double CheckRadioButton()
        {
            if(RadioButton_1.IsChecked == true)
            {
                return Convert.ToDouble(0.1);
            }
            else if(RadioButton_2.IsChecked == true)
            {
                return Convert.ToDouble(1);
            }
            else if (RadioButton_3.IsChecked == true)
            {
                return Convert.ToDouble(5);
            }
            return 0;
        }

        //狭缝调小
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

/*          if (TextBox_step != null)
            {
                //singleStep = Convert.ToDouble(TextBox_step.Text.ToString());

            }*/

            singleStep = CheckRadioButton();
            if (CurrentPosition - singleStep < 0)
            {
                if(_motorEntity != null)
                {
                    _motorFunc.MoveRight(singleStep);
                }
            }
            else
            {
                //MessageBox.Show("步距过大！", "错误");
            }
        }

        //狭缝调大
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            double singleStep = 0;

            /*            if (TextBox_step != null)
                        {
                            singleStep = Convert.ToDouble(TextBox_step.Text.ToString());
                        }*/

            singleStep = CheckRadioButton();
            if (CurrentPosition + singleStep > 50)
            { 
                if (_motorEntity != null)
                {
                    _motorFunc.MoveLeft(singleStep);
                }
            }
            else
            {
                //MessageBox.Show("步距过大！", "错误");
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

            if(TextBox_LightSet.Text != "")
            {
                tableCount++;
                lightSetModel.Index = tableCount;

                double lightset = Convert.ToDouble(TextBox_LightSet.Text.ToString());
                lightSetModel.Light = lightset;

                ListView_Set.Items.Add(lightSetModel);

                list_Light.Add(lightset);
                list_wl.Add(new WLModel(0, lightset));//CurrentPosition
            }
        }

        //删除选中项
        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            if (ListView_Set.SelectedIndex >= 0)
            {
                int selectedIndex = Convert.ToInt32(ListView_Set.SelectedIndex.ToString());//0,1,2,...
                ListView_Set.Items.RemoveAt(selectedIndex);
                list_Light.RemoveAt(selectedIndex);
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
            
                    TextBox_Light.Text = list_Light[selectedIndex].ToString();
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
                    tableCount--;
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
            if(ListView_Set.Items.Count >= 2)
            {
                double[] dList_Width = new double[list_Light.Count];//当前listview的 宽度 数组
                double[] dList_Light = new double[list_Light.Count];//当前listview的 照度 数组

                //添加按钮里加入到 list_wl 列表
                /*list_wl = new List<WLModel>();//当前listview的 宽度-照度 列表
                //添加到宽度-照度 列表
                for (int i = 0; i < list_Light.Count; i++)
                {
                    //dList_Width[i] = CurrentPosition;
                    dList_Width[i] = i * 5; //测试图表
                    dList_Light[i] = list_Light[i];

                    list_wl.Add(new WLModel(dList_Width[i], dList_Light[i]));
                }*/

                //list_wl.Sort();//排序

                pointModel = new PlotModel();
                //对排序后区间 进行插值计算 并得到每个区间的新的照度列表
                if (list_wl.Count >= 2)
                {
                    for(int i = 0; i < list_wl.Count - 1; i++)
                    {
                        //IInterpolation interpolation = CubicSpline.InterpolateNaturalSorted(dList_Width, dList_Light);
                        IInterpolation interpolation = LinearSpline.InterpolateSorted(dList_Width, dList_Light);

                        List<double> list_LightInter = new List<double>();//一个区间的插值后照度列表
                        var series = new LineSeries
                        {
                            //MarkerType = MarkerType.Circle, // 散点的形状
                        };
                        
                        for (int j = Convert.ToInt32(dList_Width[i]); j <= Convert.ToInt32(dList_Width[i + 1]); j++)
                        {
                            list_LightInter.Add(interpolation.Interpolate(j));
                            //series.Points.Add(new ScatterPoint(j, list_LightInter[j - Convert.ToInt32(dList_Width[i])]));
                            series.Points.Add(new DataPoint(j, list_LightInter[j - Convert.ToInt32(dList_Width[i])]));
                        }
                        pointModel.Series.Add(series);
                    }
                }
            }
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "狭缝宽度(mm)"
            };
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "照度(Lx)"
            };
            pointModel.Axes.Add(xAxis);
            pointModel.Axes.Add(yAxis);
            pointPlot.Model = pointModel;
            pointPlot.InvalidatePlot();
            //pointPlot.Model.Series.Clear();
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
