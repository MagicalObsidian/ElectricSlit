using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;

namespace MotorAPIPro
{
    /// <summary>
    /// 串口组件
    /// </summary>
    public class SerialComponent : IComponent
    {
        private SerialPort _serialPort = null;//串口
        private ComProperty _comProperty;//串口属性
        private Stopwatch stopwatch;//用于准确地测量运行时间
        public bool comStatus = false;//串口连接状态

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="comProperty"></param>
        public SerialComponent(ComProperty comProperty)
        {
            _comProperty = comProperty;
            Init();
        }

        /// <summary>
        /// 串口初始化配置
        /// </summary>
        private void Init()
        {
            if (_serialPort == null)
            {
                _serialPort = new SerialPort();
            }
            try
            {
                _serialPort.PortName = _comProperty.PortName;
                _serialPort.BaudRate = _comProperty.BaudRate;
                _serialPort.Parity = _comProperty.Parity;
                _serialPort.DataBits = _comProperty.DataBit;
                _serialPort.StopBits = _comProperty.StopBit;
                _serialPort.ReadTimeout = _comProperty.ReadTimeOut;
                //_serialPort.DataReceived += DataReceived;//
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        /// <returns></returns>
        public Result Close()
        {
            Result result = new Result();
            if (_serialPort == null)
            {
                result.Success = false;
                result.Message = "串口未初始化或初始化失败";
            }
            else
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();//关闭
                    _serialPort.Dispose();//释放
                    GC.Collect();//回收
                    _serialPort = null;
                }
            }
            comStatus = false;
            return result;
        }

        /// <summary>
        /// 连接串口
        /// </summary>
        /// <param name="outtime"></param>
        /// <returns></returns>
        public Result Connect(int outTime = 2000)
        {
            Result result = new Result();
            if (_serialPort == null)
            {
                result.Success = false;
                result.Message = "串口未初始化";
            }
            else
            {
                try
                {
                    if (_serialPort == null)
                    {
                        result.Success = true;
                    }
                    else//重试连接
                    {
                        stopwatch = new Stopwatch();
                        stopwatch.Restart();

                        while (stopwatch.ElapsedMilliseconds <= outTime)
                        {
                            try
                            {
                                _serialPort.Open();//打开串口
                                if (_serialPort.IsOpen)
                                {
                                    result.Message = "打开成功";
                                    comStatus = true;//串口已经连接
                                    break;
                                }
                            }
                            catch (IOException ex)
                            {
                                result.Success = false;
                                result.Message = ex.Message;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = ex.Message;
                }
                finally
                {
                    stopwatch.Reset();
                }
            }
            return result;
        }

        /// <summary>
        /// 发送和接收
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="receivelen"></param>
        /// <param name="outTime"></param>
        /// <returns></returns>
        public Result<byte> SendAndReceive(byte[] cmd, int receivelen, int outTime = 2000)
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
                        stopwatch.Restart();
                        List<byte> data = new List<byte>();


                        int totalBytes = _serialPort.BytesToRead;
                        if (totalBytes > 0)
                        {
                            byte[] buffer = new byte[totalBytes];
                            _serialPort.Read(buffer, 0, totalBytes);
                            data.AddRange(buffer);
                        }

                        if (data.Count == 0)
                        {
                            throw new Exception("接收异常");
                        }
                        result.DataList = data;
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.Message = ex.Message;
                    }
                    finally
                    {
                        _serialPort.DiscardInBuffer();//丢弃串口缓冲区
                        stopwatch.Reset();
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
        /// 发送和接收
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="receivelen"></param>
        /// <param name="errorlen"></param>
        /// <param name="outtime"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public Result<byte> SendAndReceive(byte[] cmd, int receivelen, int errorlen, int outTime = 2000, int len = 5)
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
                        Thread.Sleep(150);
                        stopwatch.Restart();
                        List<byte> data = new List<byte>();
                        /*
                        while (true) //stopwatch.ElapsedMilliseconds < outTime && data.Count < receivelen * 2 + len
                        {
                            if (_serialPort.BytesToRead > 0) //接收缓冲区中的字节数大于0
                            {
                                byte buff = (byte)_serialPort.ReadByte();//读一个字节
                                data.Add(buff);
                            }
                            else break;
                        }
                        */
                        int totalBytes = _serialPort.BytesToRead;
                        if (totalBytes > 0)
                        {
                            byte[] buffer = new byte[totalBytes];
                            _serialPort.Read(buffer, 0, totalBytes);
                            data.AddRange(buffer);
                        }
                        if (data.Count == 0)
                        {
                            throw new Exception("接收异常");
                        }
                        if (data.Count == errorlen)
                        {
                            //
                        }
                        result.DataList = data;
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.Message = ex.Message;
                    }
                    finally
                    {
                        _serialPort.DiscardInBuffer();//丢弃串口缓冲区
                        stopwatch.Reset();
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
