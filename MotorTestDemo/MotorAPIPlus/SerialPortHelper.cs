using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace MotorAPIPlus
{
    public class SerialPortHelper
    {

        /// <summary>
        /// 串口名
        /// </summary>
        public string PortName { get; set; } = "";

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; } = 115200;

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBit { get; set; } = StopBits.One;

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBit { get; set; } = 8;

        /// <summary>
        /// 校验位
        /// </summary>
        public Parity Parity { get; set; } = Parity.Even;

        /// <summary>
        /// 超时重连时间
        /// </summary>
        public int ReadTimeOut = 100;

        /// <summary>
        /// 从机 ID
        /// </summary>
        public int SlaveID { get; set; } = 1;


        private SerialPort _serialPort;//串口
        public bool comStatus = false;//串口连接状态
        //private readonly Lazy<SerialPort> _instance = new Lazy<SerialPort>(() => new SerialPort()); //单例模式

        /// <summary>
        /// 默认构造 只需指定串口名
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="parity"></param>
        /// <param name="dataBit"></param>
        /// <param name="stopBits"></param>
        public SerialPortHelper(string portName, int baudRate = 115200, Parity parity = Parity.Even, int dataBit = 8, StopBits stopBits = StopBits.One)
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;
            _serialPort.Parity = parity;
            _serialPort.DataBits = dataBit;
            _serialPort.StopBits = stopBits;
            //_serialPort.ReadTimeout = 2000;
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        /// <returns></returns>
        public bool Close()
        {
            bool res = false;
            if (_serialPort == null)
            {
                res = false;
            }
            else
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();//关闭
                    _serialPort.Dispose();//释放
                    GC.Collect();//回收
                    _serialPort = null;
                    res = true;
                }
            }
            return res;
        }

        /// <summary>
        /// 连接串口
        /// </summary>
        /// <param name="outtime"></param>
        /// <returns></returns>
        public bool Connect(int outTime = 2000)
        {
            bool res = false;
            if (_serialPort == null)
            {
                return false;
            }
            else
            {
                try
                {
                    _serialPort.Open();//打开串口
                }
                catch(Exception e)
                {
                    return false;
                }
                if (_serialPort.IsOpen)
                {
                    res = true;
                }
            }
            return res;
        }

        /// <summary>
        /// 发送和接收
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="receivelen"></param>
        /// <param name="outTime"></param>
        /// <returns></returns>
        public Result<byte> SendAndReceive(byte[] cmd)
        {
            Result<byte> result = new Result<byte>();
            if (_serialPort == null)
            {
                result.Success = false;
                result.Message = "发送失败";
            }
            else
            {
                if (_serialPort.IsOpen)
                {
                    try
                    {
                        _serialPort.Write(cmd, 0, cmd.Length);
                        Thread.Sleep(150);//发送命令和读取数据之间延时

                        //读取串口缓冲区中的数据
                        List<byte> data = new List<byte>();
                        int totalBytes = _serialPort.BytesToRead;
                        if (totalBytes > 0)
                        {
                            byte[] buffer = new byte[totalBytes];
                            _serialPort.Read(buffer, 0, totalBytes);
                            data.AddRange(buffer);
                        }

                        if (data.Count == 0) { result.Success = false; }
                        result.DataList = data;//获得接收的数据

                        result.Success = true;
                        result.Message = "发送成功";
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.Message = ex.Message;
                    }
                    finally
                    {
                        _serialPort.DiscardInBuffer();//丢弃串口缓冲区
                    }
                }
                else
                {
                    result.Success = false;
                    result.Message = "发送失败";
                }
            }
            return result;
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public Result Send(byte[] cmd)
        {
            Result result = new Result();
            if (_serialPort == null)
            {
                result.Success = false;
                result.Message = "发送失败";
            }
            else
            {
                if (_serialPort.IsOpen)
                {
                    try
                    {
                        _serialPort.Write(cmd, 0, cmd.Length);
                        result.Success = true;
                        result.Message = "发送成功";
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.Message = "发送失败";
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            List<byte> data = new List<byte>();

            //SerialPort sp = (SerialPort)sender;
        }


    }
}
