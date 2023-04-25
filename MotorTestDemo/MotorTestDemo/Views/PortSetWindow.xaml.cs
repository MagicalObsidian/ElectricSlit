using MotorAPIPlus;
using OpticalPlatform.Model;
using OpticalPlatform.Util;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace MotorTestDemo.Views
{
    /// <summary>
    /// PortSetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PortSetWindow : Window
    {
        public event Func<bool, bool> Refresh;

        public static string path = AppDomain.CurrentDomain.BaseDirectory + "/Config/PortSet.INI";
        public PortSetEntity portSetEntity { get; set; } = new PortSetEntity();
        public ObservableCollection<string> PortList { get; set; } = new ObservableCollection<string>();

        public PortSetWindow()
        {
            InitializeComponent();

            DataContext = this;
            this.Loaded += PortSetWindow_Loaded;
        }

        private void CboPortName_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void PortSetWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetPortList();
            Init();
        }

        private Result Init()
        {
            Result result = new Result();
            try
            {
                if (File.Exists(path))
                {
                    string PortName = INIFileHelper.INIRead("PortSet", "PortName", path);
                    string BaudRate = INIFileHelper.INIRead("PortSet", "BaudRate", path);
                    string Parity = INIFileHelper.INIRead("PortSet", "Parity", path);
                    string DataBits = INIFileHelper.INIRead("PortSet", "DataBits", path);
                    string StopBits = INIFileHelper.INIRead("PortSet", "StopBits", path);
                    portSetEntity.PortName = PortName;
                    portSetEntity.BaudRate = BaudRate;
                    portSetEntity.DataBits = DataBits;
                    portSetEntity.StopBits = StopBits;
                    portSetEntity.Parity = Parity;
                    return result;
                }
                throw new Exception("The serial port configuration file does not exist");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }

        public void GetPortList()
        {
            PortList?.Clear();
            SerialPort.GetPortNames().ToList().ForEach(p => PortList.Add(p));
        }

        private void SaveCmd_CanExecute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(this.CboPortName.Text))
                {
                    MessageBox.Show("The serial port cannot be empty");
                    return;
                }
                Type type = portSetEntity.GetType();
                PropertyInfo[] propertyInfos = type.GetProperties();
                foreach (PropertyInfo prop in propertyInfos)
                {
                    INIFileHelper.INIWrite("PortSet", prop.Name, prop.GetValue(portSetEntity).ToString(), path);
                }
                Refresh?.Invoke(false);
                this.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }


    }
}
