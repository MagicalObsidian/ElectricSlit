using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MotorTestDemo.Views;

using MotorAPIPlus;//
//using MotorAPIPro; //使用 .net 4.8 版本的API

using System.Collections.ObjectModel;
using OpticalPlatform.Util;
using System.IO;
using System.IO.Ports;
using OpticalPlatform.Model;

namespace MotorTestDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        //private Communication _communication = null;//通讯类

        //public ComProperty _comProperty { get; set; }//串口

        public RecordEntity _recordEntity { get; set; } = new RecordEntity();//状态信息

        public SerialPortHelper _serialPort = null;
        public MotorEntity _motorEntity = null;//电机实体
        public MotorFunc _motorFunc = null;

        public ObservableCollection<string> PortList { get; set; } = new ObservableCollection<string>();//当前串口列表



        /// <summary>
        /// 电机方向  false 向下限位   true 向上限位
        /// </summary>
        public bool Direction { get; set; } = false;

        /// <summary>
        /// 窗口按钮可用?
        /// </summary>
        private bool enble = true;
        public bool Enble
        {
            get { return enble; }
            set { enble = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Enble")); }
        }

        /// <summary>
        /// 串口名
        /// </summary>
        private string portName;
        public string PortName
        {
            get { return portName; }
            set { portName = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PortName")); }
        }

        /// <summary>
        /// 从机 ID
        /// </summary>
        private int slaveID;
        public int SlaveID
        {
            get { return slaveID; }
            set { slaveID = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SlaveId")); }
        }

        /// <summary>
        /// 单歩步长
        /// </summary>
        private double singlestep;
        public double SingleStep
        {
            get { return singlestep; }
            set { singlestep = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SingleStep")); }
        }

        /// <summary>
        /// 单次移动到的指定位置的距离
        /// </summary>
        private double positionGoTo;
        public double PositionGoTo
        {
            get { return positionGoTo; }
            set { positionGoTo = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionGoTo")); }
        }

        /// <summary>
        /// 绝对位置的符号， false - 表示原点向左   true + 表示原点向右
        /// </summary>
        private bool positionSign;
        public bool PositionSign
        {
            get { return positionSign; }
            set { positionSign = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PositionSign")); }
        }

        /// <summary>
        /// 系数
        /// </summary>
        private double k;
        public double K
        {
            get { return k; }
            set { k = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("K")); }
        }

        /// <summary>
        /// 发送的报文
        /// </summary>
        private string command_Send;
        public string Command_Send
        {
            get { return command_Send; }
            set { command_Send = value; }
        }

        /// <summary>
        /// 响应的报文
        /// </summary>
        private string command_Receive;
        public string Command_Receive
        {
            get { return command_Receive; }
            set { command_Receive = value; }
        }



        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        #region 主窗口方法
        /// <summary>
        /// 主窗口加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //GetPortList();
            //await Check();
            await Init();
        }

        /// <summary>
        /// 主窗口关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //_communication?.Dispose();
        }
        #endregion

        #region 退出程序
        private void Window_Closed(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //_communication?.Dispose();
            Environment.Exit(0);
        }
        #endregion



        #region 打开串口设置窗口
        private void MenuSerialPort_Click(object sender, RoutedEventArgs e)
        {

            PortSetWindow portSetWindow = new PortSetWindow();
            //portSetWindow.Refresh += PortInit;
            //if (monoWindow != null)
            //{
            //    portSetWindow.MonoRefresh += monoWindow.Refresh;
            //}
            portSetWindow.ShowDialog();

        }
        #endregion

        #region 基础配置
        private bool BaseInit()
        {
            bool state = false;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "/Config/BaseSet.INI";
                if (File.Exists(path))
                {
                    string Direction = INIFileHelper.INIRead("BaseSet", "Direction", path);
                    string StartSpeed = INIFileHelper.INIRead("BaseSet", "StartSpeed", path);
                    string CurrentMax = INIFileHelper.INIRead("BaseSet", "CurrentMax", path);
                    string CurrentLow = INIFileHelper.INIRead("BaseSet", "CurrentLow", path);
                    string CurrentLowWT = INIFileHelper.INIRead("BaseSet", "CurrentLowWT", path);
                    string ArrowDirection = INIFileHelper.INIRead("BaseSet", "ArrowDirection ", path);
                    if (ArrowDirection.ToLower() == "true")
                    {
                        this.Direction = true;
                    }
                    else if (ArrowDirection.ToLower() == "false")
                    {
                        this.Direction = false;
                    }
                    /*
                    Result result = new Result();
                    result = _motorEntity.SetCurrentMax(Convert.ToDouble(CurrentMax));
                    if (!result.Success) throw new Exception(result.Message);
                    result = _motorEntity.SetStartRSpeed(Convert.ToDouble(StartSpeed));
                    if (!result.Success) throw new Exception(result.Message);
                    result = _motorEntity.SetCurrentLow(Convert.ToInt16(CurrentLow));
                    if (!result.Success) throw new Exception(result.Message);
                    result = _motorEntity.SetCurrentLowWT(Convert.ToDouble(CurrentLowWT));
                    if (!result.Success) throw new Exception(result.Message);
                    if (!string.IsNullOrEmpty(Direction))
                        _recordEntity.Direction = Direction == "1" ? true : false;
                    result = _motorEntity.SetEnable();
                    if (!result.Success) throw new Exception(result.Message);
                    */
                    return true;
                }
            }
            catch (Exception)
            {
                state = false;
            }
            return state;
        }
        #endregion

        #region 串口配置
        private bool PortInit(bool first = true)
        {
            bool state = false;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "/Config/PortSet.INI";
                if (File.Exists(path))
                {
                    string PortName = INIFileHelper.INIRead("PortSet", "PortName", path);
                    string BaudRate = INIFileHelper.INIRead("PortSet", "BaudRate", path);
                    string Parity = INIFileHelper.INIRead("PortSet", "Parity", path);
                    string DataBits = INIFileHelper.INIRead("PortSet", "DataBits", path);
                    string StopBits = INIFileHelper.INIRead("PortSet", "StopBits", path);
                    this.PortName = PortName;

                    return true;
                }
            }
            catch (Exception)
            {
                state = false;
            }
            return state;
        }


        #endregion

        #region 通信配置
        /*
        private bool CommunicationInit(ComProperty comProperty)
        {
            Result result = new Result();

            _communication = Communication.Instance(comProperty);
            result = _communication.Connection();
            if (result.Success)
            {
                _motorEntity = new MotorEntity(_communication);
                return true;
            }
            else
            {
                _communication?.Dispose();
                return false;
            }
        }
        */
        #endregion

        #region 电机配置
        /*
        public Result MotorSet()
        {
            Result result = new Result();
            try
            {
                
                //发送电机 使能命令
                result = _motorEntity.SetEnable(true);
                if (!result.Success) throw new Exception("11111");

                result = _motorEntity.SetPulseLength(50);
                if (!result.Success) throw new Exception("11111");

                //result = _motorEntity.SetPulseLength(_motorEntity.Len);
                if (!result.Success) throw new Exception("Motion control initialization setup failed!");
                //result = _motorEntity.SetRSpeed(_motorEntity.Speed);
                if (!result.Success) throw new Exception("Motion control initialization setup failed!");
                
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "Please check whether the connection is normal and the motor is started!");
            }
            return result;
        }

        private bool MotorInit()
        {
            bool state = false;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "/Config/MotorSet.INI";
                if (File.Exists(path))
                {
                    string SlaveId = INIFileHelper.INIRead("MotorSet", "SlaveId", path);
                    K = Convert.ToDouble(INIFileHelper.INIRead("MotorSet", "K", path));//从配置文件得到系数
                    //K = _motorEntity.GetKV().Data;
                    string Len = INIFileHelper.INIRead("MotorSet", "Len", path);
                    string Speed = INIFileHelper.INIRead("MotorSet", "Speed", path);

                    _comProperty.SlaveID = int.Parse(SlaveId);
                    this.SlaveID = _comProperty.SlaveID;
                    _motorEntity.K = K;
                    _motorEntity.Len = int.Parse(Len);
                    _motorEntity.Speed = int.Parse(Speed);

                    MotorSet();
                    return true;
                }
            }
            catch (Exception)
            {
                state = false;
                throw;
            }
            return state;
        }
        */
        #endregion

        #region 初始化
        private async Task Init()
        {
            if (!Enble) return;
            await Task.Run(async () =>
            {
                try
                {
                    PortInit();
                    _serialPort = new SerialPortHelper(this.PortName);
                    _serialPort.Connect();
                    _motorEntity = new MotorEntity(_serialPort);
                    _motorFunc = new MotorFunc(_motorEntity);

                    K = _motorEntity.K;

                    //初始化基本配置
                    //bool state = BaseInit();
                    //if (!state) new Exception("Initialization failure!");

                    //初始化串口配置
                    //state = PortInit();
                    //if (!state) new Exception("Com Initialization failure!");

                    //初始化探头配置
                    //state = DetectorInit();
                    //if (!state) new Exception("Detector.INI Initialization failure，Check the configuration file");

                    //初始化电机配置
                    //state = MotorInit();
                    //f (!state) new Exception("MotorSet.INI Initialization failure，Check the configuration file");


                    //Result result = new Result();
                    //将输入类型选择为向上下限位运动
                    _motorEntity.SetPS();
                    //if (!result.Success) throw new Exception("Motion control initialization setup failed!");

                    await Task.Run(async () => {
                        try
                        {
                            await GetCurrentPosition();
                        }
                        catch (Exception)
                        {   
                            Enble = true;
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Initialization failure！", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }



        #endregion


        #region 获取当前位置
        private async Task GetCurrentPosition()
        {
            await Task.Run(async () => {
                bool state = true;
                try
                {
                    while (state)
                    {
                        await Task.Delay(200);
                        if (true)
                        {
                            //Result<int> result1 = _motorEntity.GetPulsePosition();
                            int result = _motorEntity.GetPulsePosition();
                            /*
                            foreach (var item in DetectorList)
                            {
                                if (Math.Abs(Math.Ceiling(_motorEntity.K * result1.Data)) == item.Posion)
                                {
                                    _recordEntity.Position = item.Name;
                                    state = false;
                                    return;
                                }
                            }
                            */
                            _recordEntity.Position = Math.Abs(_motorEntity.K * result).ToString("f2");
                            _recordEntity.DirectionSign = result >= 0 ? " " : "-";
                            state = false;
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            });
        }

        #endregion

        #region 按钮方法 测试
        /// <summary>
        /// 电机使能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Enable_Click(object sender, RoutedEventArgs e)
        {
            _motorFunc.Enable();
        }

        /// <summary>
        /// 电机脱机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Disenable_Click(object sender, RoutedEventArgs e)
        {
            _motorFunc.DisEnable();
        }

        /// <summary>
        /// 电机暂停
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            _motorFunc.Pause();
            await GetCurrentPosition();//实时位置
        }

        /// <summary>
        /// 电机继续
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            _motorFunc.Enable();
            await GetCurrentPosition();//实时位置
        }

        /// <summary>
        /// 左移
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Left_Click(object sender, RoutedEventArgs e)
        {
            if (SingleStep == 0) return;
            _motorFunc.MoveLeft(SingleStep);

            await GetCurrentPosition();//实时位置
        }

        /// <summary>
        /// 右移
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Right_Click(object sender, RoutedEventArgs e)
        {
            if (SingleStep == 0) return;
            _motorFunc.MoveRight(SingleStep);

            await GetCurrentPosition();//实时位置
        }

        /// <summary>
        /// 设置当前位置为零位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetZero_Click(object sender, RoutedEventArgs e)
        {
            _motorEntity.SetZero();
        }

        /// <summary>
        /// 移动至零位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MoveToZero_Click(object sender, RoutedEventArgs e)
        {
            //_motorEntity.SetPulsePositionSet(0);
            _motorFunc.MoveToZero();
            await GetCurrentPosition();//实时位置
        }

        /// <summary>
        /// 移动到指定位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Btn_PositionGoTo_Click(object sender, RoutedEventArgs e)
        {
            if(Cb_Sign.SelectedIndex == 0)
            {
                PositionSign = true;
            }
            else if(Cb_Sign.SelectedIndex == 1)
            {
                PositionSign = false;
            }
            _motorFunc.MoveToPosition(PositionGoTo, PositionSign);
            await GetCurrentPosition();//实时位置
        }

        int lowPosition= 0;
        bool status;
        int highPosition = 0;
        /// <summary>
        /// 连续移动至上限位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MoveToUpperLimit_Click(object sender, RoutedEventArgs e)
        {
            _motorEntity.MovePSH();//High 需要传感器
            
            await GetCurrentPosition();//实时位置
            status = false;
        }

        /// <summary>
        /// 连续移动至下限位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MoveToLowerLimit_Click(object sender, RoutedEventArgs e)
        {
            _motorEntity.MovePSL();//Low 需要传感器
            
            await GetCurrentPosition();//实时位置
            status = true;
        }


        /// <summary>
        /// 显示当前脉冲位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int result = _motorEntity.GetPulsePosition();
            current.Text = result.ToString();
            if (!status) lowPosition = result;
            if (status) highPosition = result;
            if(lowPosition < highPosition) { pulsenum.Text = (highPosition - lowPosition).ToString(); }
        }


        #endregion


    }
}
