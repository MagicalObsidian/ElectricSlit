using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotorAPIPlus
{
    /// <summary>
    /// 电机底层命令
    /// </summary>
    public class MotorEntity : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public byte slaveID = 0x01;//RTU地址
        private static Int64 TRT = 3840000; //1 圈=3840000MMS

        /// <summary>
        /// 系数 实际运动距离(mm) 与 脉冲数 的比值
        /// </summary>
        public double K { get; set; } = 0.0044706723891273;//系数 //0.00025  //0.0000025 //电动狭缝 0.0044706723891273

        public SerialPortHelper _serialPort = null;

        public MotorEntity()
        {

        }

        public MotorEntity(SerialPortHelper serialPortHelper)
        {
            _serialPort = serialPortHelper;
        }

        /* 功能码:
           0x04 读单个寄存器 
           0x03 读多个寄存器
           0x06 写单个寄存器
           0x10 写多个寄存器 */

        /* 向控制寄存器0x0000写入的数据域中的数据表示 
           0x00 0x04 脱机
           0x00 0x00 使能
           0x00 0x08 暂停
           0x00 0x10 运动至下限位
           0x00 0x20 运动至上限位
           0x01 0x00 设置位置偏移至0 即重设当前位置为零位
        */



        #region 串口命令获取和数据转换
        /// <summary>
        /// 获得读命令
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="funCode"></param>
        /// <param name="registerAddress"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] GetReadCommand(byte slaveID, byte funCode, ushort registerAddress, byte[] data)
        {
            List<byte> cmd = new List<byte>();
            cmd.Add(slaveID);
            cmd.Add(funCode);
            cmd.Add(BitConverter.GetBytes(registerAddress)[1]);
            cmd.Add(BitConverter.GetBytes(registerAddress)[0]);
            /* 读命令中数据域 data 表示需要读取的寄存器个数 */
            cmd.AddRange(data);
            cmd.AddRange(CRC16(cmd.ToList()));
            return cmd.ToArray();
        }

        /// <summary>
        /// 获得写命令
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="funCode"></param>
        /// <param name="registerAddress"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] GetWriteCommand(byte slaveID, byte funCode, ushort registerAddress, byte[] data)
        {
            List<byte> cmd = new List<byte>();
            cmd.Add(slaveID);
            cmd.Add(funCode);
            cmd.Add(BitConverter.GetBytes(registerAddress)[1]);
            cmd.Add(BitConverter.GetBytes(registerAddress)[0]);
            cmd.AddRange(data);
            cmd.AddRange(CRC16(cmd.ToList()));
            return cmd.ToArray();
        }

        /// <summary>
        /// CRC 校验
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private List<byte> CRC16(List<byte> value)
        {
            ushort poly = 0xA001;
            ushort crcInit = 0xFFFF;

            if (value == null || !value.Any())
                throw new ArgumentException("");

            //运算
            ushort crc = crcInit;
            for (int i = 0; i < value.Count; i++)
            {
                crc = (ushort)(crc ^ (value[i]));
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ poly) : (ushort)(crc >> 1);
                }
            }
            byte high = (byte)((crc & 0xFF00) >> 8);    //高位置
            byte low = (byte)(crc & 0x00FF);            //低位置

            List<byte> buffer = new List<byte>();
            buffer.Add(low);
            buffer.Add(high);
            return buffer;
        }

        /// <summary>
        /// 对响应报文中的数据进行数据转换
        /// </summary>
        /// <param name="result"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ConvertValue(Result<byte> result, Type type)
        {
            object res = null;
            byte[] data;
            if (result.DataList == null || result.DataList.Count == 0) return null;
            if(result.DataList.Count > 4)
            {
                byte[] datalist = result.DataList.ToArray();
                data = new byte[4];
                Array.Copy(datalist, 3, data, 0, 4);
            }
            else
            {
                data = result.DataList.ToArray();
            }
            BitConverter.ToString(data);
            if (type == typeof(UInt16))
            {
                var a = data.Reverse();
                res = BitConverter.ToUInt16(a.ToArray(), 0);
            }
            else if (type == typeof(Int64))
            {
                var a = data.Reverse();
                res = BitConverter.ToInt64(a.ToArray(), 0);
            }
            else if (type == typeof(Int32))
            {
                var a = data.Reverse();
                res = BitConverter.ToInt32(a.ToArray(), 0);
            }
            else if (type == typeof(Int16))
            {
                var a = data.Reverse();
                res = BitConverter.ToInt16(a.ToArray(), 0);
            }
            return res;
        }
        #endregion

        #region ---------------------电机命令

        #region --电机配置
        /// <summary>
        /// 电机 false 脱机   true 使能
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public void SetEnable(bool state = true)
        {
            byte[] data;
            if (state)//如果需要使能
            {
                data = new byte[] { 0x00, 0x00 };//01 06 00 00 00 00 89 CA 
            }
            else//如果需要脱机
            {
                data = new byte[] { 0x00, 0x04 };//01 06 00 00 00 04 88 09
            }
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0000, data);
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 设置输入寄存器类型
        /// </summary>
        /// <param name="inputType"></param>
        public void SetInputType(int inputType)
        {
            var a = (Int16)inputType;
            List<byte> bytes = BitConverter.GetBytes(a).ToList();
            bytes.Reverse();//0001 0000
            //0x01 0x06 0x00 0x08 0x00 0x08 0x88 0x0C
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0000, bytes.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 直接设置输入寄存器为上下限位模式
        /// </summary>
        public void SetPS()
        {
            byte[] bytes = new byte[] { 0x00, 0x08 };
            //0x01 0x06 0x00 0x08 0x00 0x08 0x88 0x0C
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0008, bytes.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 获取电机速度系数(出厂时固化不可写)
        /// </summary>
        /// <returns></returns>
        public Result<ushort> GetKV()
        {
            Result<byte> data = new Result<byte>();
            Result<ushort> result = new Result<ushort>();
            ushort value;
            //0x01 0x04 0x00 0x43 0x00 0x01 0xC0 0x1E
            byte[] bytes = new byte[] { 0x00, 0x01 };
            byte[] cmd = GetWriteCommand(slaveID, 0x04, 0x0043, bytes);
            data = _serialPort.SendAndReceive(cmd);
            result.Data = value = Convert.ToUInt16(ConvertValue(data, typeof(ushort)));
            return result;
        }

        /// <summary>
        /// 设置转速
        /// </summary>
        public void SetRSpeed(double rpm)
        {
            ushort value;
            Result<ushort> result = GetKV();
            value = result.Data;
            value = (ushort)((rpm * TRT) / (60000 * value));
            SetVelSet(value);
        }

        /// <summary>
        /// 设置电机的设定速度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void SetVelSet(double data)
        {
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0040, bytes.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 设置脉冲单步长度
        /// </summary>
        /// <param name="data"></param>
        public void SetPulseLength(int data)
        {
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            byte[] register = { 0x00, 0x02, 0x04};
            byte[] result = register.Concat(bytes).ToArray();
            //0x01 0x10 0x00 0x2A 0x00 0x02 0x04 0x00 0x00 0x06 0x00 0x72 0x68 example
            //byte[] temp = new byte[] { 0x00, 0x02, 0x04, 0x00, 0x00, 0x06, 0x00 };
            byte[] cmd = GetWriteCommand(slaveID, 0x10, 0x002A, result.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 获取脉冲单步长度
        /// </summary>
        /// <returns></returns>
        public Result<int> GetPulseLength()
        {
            Result<byte> data = new Result<byte>();
            int value;
            Result<int> result = new Result<int>();
            //0x01 0x03 0x00 0x2A 0x00 0x02 0xE5 0xC3
            byte[] bytes = new byte[] { 0x00, 0x02 };
            byte[] cmd = GetReadCommand(slaveID, 0x03, 0x002A, bytes);
            data = _serialPort.SendAndReceive(cmd);
            result.Data = value = Convert.ToInt32(ConvertValue(data, typeof(Int32)));
            return result;
        }


        /// <summary>
        /// 运动至上限位 需要接传感器
        /// </summary>
        /// <returns></returns>
        public void MovePSH()
        {
            byte[] bytes = new byte[] { 0x00, 0x20 };
            //0x01 0x06 0x00 0x00 0x00 0x20 0x88 0x12
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0000, bytes);
            _serialPort.SendAndReceive(cmd);
        }
        
        /// <summary>
        /// 运动至下限位 需要接传感器
        /// </summary>
        /// <returns></returns>
        public void MovePSL()
        {
            byte[] bytes = new byte[] { 0x00, 0x10 };
            //0x01 0x06 0x00 0x00 0x00 0x10 0x88 0x06
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0000, bytes);
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 读端口寄存器 PSL
        /// </summary>
        public int ReadPortPSL()
        {
            Result<byte> data = new Result<byte>();
            int value;
            int result = 0;
            //0x01 0x04 0x00 0x80 0x00 0x01 0x30 0x22
            byte[] bytes = new byte[] { 0x00, 0x12 };
            byte[] cmd = GetReadCommand(slaveID, 0x04, 0x0080, bytes.ToArray());
            data = _serialPort.SendAndReceive(cmd);
            result = Convert.ToInt32(ConvertValue(data, typeof(int)));
            return result;
        }

        /// <summary>
        /// 读端口寄存器 PSH
        /// </summary>
        public int ReadPortPSH()
        {
            Result<byte> data = new Result<byte>();
            int value;
            int result = 0;
            //0x01 0x04 0x00 0x80 0x00 0x01 0x30 0x22
            byte[] bytes = new byte[] { 0x00, 0x13 };
            byte[] cmd = GetReadCommand(slaveID, 0x04, 0x0080, bytes.ToArray());
            data = _serialPort.SendAndReceive(cmd);
            result = Convert.ToInt32(ConvertValue(data, typeof(int)));
            return result;
        }







        //-------读取上下限位
        /// <summary>
        /// 获取上限位?
        /// </summary>
        /// <returns></returns>
        public Result<int> GetPSH()
        {
            Result<byte> data = new Result<byte>();
            int value;
            Result<int> result = new Result<int>();
            //
            byte[] bytes = new byte[] { 0x00, 0x01 };
            byte[] cmd = GetReadCommand(slaveID, 0x04, 0x002C, bytes);
            data = _serialPort.SendAndReceive(cmd);
            result.Data = value = Convert.ToInt32(ConvertValue(data, typeof(Int32)));
            return result;
        }

        /// <summary>
        /// 获取下限位?
        /// </summary>
        /// <returns></returns>
        public Result<int> GetPSL()
        {
            Result<byte> data = new Result<byte>();
            int value;
            Result<int> result = new Result<int>();
            //
            byte[] bytes = new byte[] { };
            byte[] cmd = GetReadCommand(slaveID, 0x04, 0x002C, bytes);
            data = _serialPort.SendAndReceive(cmd);
            result.Data = value = Convert.ToInt32(ConvertValue(data, typeof(Int32)));
            return result;
        }

        /// <summary>
        /// 获取控制寄存器内的值?
        /// </summary>
        /// <returns></returns>
        public Result<int> GetControlRegisterData()
        {
            Result<byte> data = new Result<byte>();
            int value;
            Result<int> result = new Result<int>();
            //0x01 0x04 0x00 0x00 0x00 0x01 0x31 0xCA
            byte[] bytes = new byte[] { };
            byte[] cmd = GetReadCommand(slaveID, 0x04, 0x0000, bytes);
            data = _serialPort.SendAndReceive(cmd);
            result.Data = value = Convert.ToInt32(ConvertValue(data, typeof(Int32)));
            return result;
        }

        #endregion

        #region --电流设置
        /// <summary>
        /// 获取实时电流
        /// </summary>
        /// <returns></returns>
        public float GetCurrent()
        {
            Result<byte> data = new Result<byte>();
            //0x01 0x04 0x00 0x15 0x00 0x01 0x20 0x0E
            float value;
            byte[] bytes = new byte[] { 0x00, 0x01 };
            byte[] cmd = GetReadCommand(slaveID, 0x04, 0x0015, bytes);
            data = _serialPort.SendAndReceive(cmd);
            if(ConvertValue(data, typeof(ushort)) != null)
            {
                return value = Convert.ToSingle((ushort)ConvertValue(data, typeof(ushort)) / 100f);
            }
            return 0;
        }

        /// <summary>
        /// 设置降流百分比
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void SetCurrentLow(Int16 data)
        {
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            //0x01 0x06 0x00 0x13 0x00 0x1E 0xF8 0x07
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0013, bytes.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 设置降流电流等待时间
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void SetCurrentLowWT(double data)
        {
            Result result = new Result();
            UInt16 value = (ushort)(data * 1000);
            List<byte> bytes = BitConverter.GetBytes(value).ToList();
            bytes.Reverse();
            //0x01 0x06 0x00 0x14 0x01 0xC2 0x49 0xCF
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0014, bytes.ToArray());
            _serialPort.SendAndReceive(cmd);
        }
        #endregion

        #region --电机位置
        /* 电机运动主要有两组寄存器可以使用：
           一组是 Position 与 PositionSet，单位为 MMS，1 圈=3840000MMS。
           另一组是 PulsePosition 与 PulsePositionSet，单位为细分后 1 脉冲所代表的距离。
           电机运动方向为 PositionSet-Position 或者 PulsePositionSet-PulsePosition。所得差值为正，电机
           正向运转；所得差值为负，电机反向运转  */

        /// <summary>
        /// 获取电机实时位置
        /// </summary>
        /// <returns></returns>
        public Result<Int64> GetPosition()
        {
            Result<byte> data = new Result<byte>();
            Result<Int64> pos = new Result<long>();
            // 0x01 0x03 0x00 0x20 0x00 0x04 0x45 0xC3
            Int64 value;
            byte[] bytes = new byte[] { 0x00, 0x04 };
            byte[] cmd = GetReadCommand(slaveID, 0x03, 0x0020, bytes);
            data = _serialPort.SendAndReceive(cmd);
            pos.Data = value = Convert.ToInt64(ConvertValue(data, typeof(Int64)));
            return pos;
        }

        /// <summary>
        /// 设置位置?
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void SetPosition(Int64 data)
        {
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            byte[] cmd = GetWriteCommand(slaveID, 0x10, 0x0020, bytes.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 设置脉冲实时位置
        /// </summary>
        /// <param name="data"></param>
        public void SetPulsePosition(int data)
        {
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            byte[] register = { 0x00, 0x02, 0x04 };
            bytes.Reverse();
            byte[] result = register.Concat(bytes).ToArray();
            //0x01 0x10 0x00 0x2C 0x00 0x02 0x04 0x00 0x00 0x27 0x10 0xEB 0xDE example
            byte[] cmd = GetWriteCommand(slaveID, 0x10, 0x002C, result.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 获取脉冲实时位置
        /// </summary>
        /// <returns></returns>
        public int GetPulsePosition()
        {
            Result<byte> data = new Result<byte>();
            int value;
            int result = 0;
            //0x01 0x03 0x00 0x2C 0x00 0x02 0x05 0xC2
            byte[] bytes = new byte[] { 0x00, 0x02 };//需要读取的寄存器数量
            byte[] cmd = GetReadCommand(slaveID, 0x03, 0x002C, bytes.ToArray());
            data = _serialPort.SendAndReceive(cmd);
            result = Convert.ToInt32(ConvertValue(data, typeof(int)));
            return result;
        }

        /// <summary>
        /// 设置脉冲设定位置
        /// </summary>
        /// <param name="data"></param>
        public void SetPulsePositionSet(int data)
        {
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            byte[] register = { 0x00, 0x02, 0x04 };
            bytes.Reverse();
            byte[] result = register.Concat(bytes).ToArray();
            byte[] cmd = GetWriteCommand(slaveID, 0x10, 0x002E, result.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 获取脉冲设定位置
        /// </summary>
        /// <returns></returns>
        public Result<int> GetPulsePositionSet()
        {
            Result<byte> data = new Result<byte>();
            int value;
            Result<int> result = new Result<int>();
            //01 03 00 2E 00 02 A4 02
            byte[] bytes = new byte[] { 0x00, 0x02 };
            byte[] cmd = GetReadCommand(slaveID, 0x03, 0x002E, bytes);
            data = _serialPort.SendAndReceive(cmd);
            result.Data = value = Convert.ToInt32(ConvertValue(data, typeof(int)));
            return result;
        }

        /// <summary>
        /// 设置移动到的绝对位置
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void SetPositionSet(Int64 data)
        {
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            byte[] register = { 0x00, 0x04, 0x08 };
            bytes.Reverse();
            byte[] result = register.Concat(bytes).ToArray();
            byte[] cmd = GetWriteCommand(slaveID, 0x10, 0x0024, result.ToArray());
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 获取绝对位置
        /// </summary>
        /// <returns></returns>
        public Result<Int64> GetPositionSet()
        {
            Result<byte> data = new Result<byte>();
            Result<Int64> pos = new Result<long>();
            // 0x01 0x03 0x00 0x20 0x00 0x04 0x45 0xC3
            Int64 value;
            byte[] bytes = new byte[] { 0x00, 0x04 };
            byte[] cmd = GetReadCommand(slaveID, 0x03, 0x0024, bytes);
            data = _serialPort.SendAndReceive(cmd);
            pos.Data = value = Convert.ToInt64(ConvertValue(data, typeof(Int64)));
            return pos;
        }

        #endregion

        #region --电机运动

        /// <summary>
        /// 电机暂停
        /// </summary>
        /// <returns></returns>
        public void SetPause()
        {
            byte[] bytes = new byte[] { 0x00, 0x08 };
            //0x01 0x06 0x00 0x00 0x00 0x08 0x88 0x0C
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0000, bytes);
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 设定位置偏移至 0(即设置当前位置为新的零位)
        /// </summary>
        /// <returns></returns>
        public void SetZero()
        {
            //0x01 0x06 0x00 0x00 0x01 0x00 0x88 0x5A
            byte[] bytes = new byte[] { 0x01, 0x00 };
            byte[] cmd = GetWriteCommand(slaveID, 0x06, 0x0000, bytes);
            _serialPort.SendAndReceive(cmd);
        }

        /// <summary>
        /// 运动到指定位置
        /// </summary>
        /// <param name="d">指定位置</param>
        /// <returns></returns>
        public void SetMoveTo(double d, bool positionSign, int pulseLen = 19200)//3840000 / 19200 = 200
        {
            //Result<int> state = new Result<int>();
            int state;

            int offset = 0;
            double APosition;//要移动到的绝对位置
            double ACurrentPosition;//当前实际绝对位置
            
            state = GetPulsePosition();//获取实时脉冲位置
            int current = state;//实时脉冲位置

            SetPulseLength(pulseLen);//设置脉冲步进长度

            APosition = positionSign ? d : -d;//指定位置 positionSign是其相对于原点的方向， -为原点往左， +为原点向右
            ACurrentPosition = Convert.ToDouble((current * K).ToString("f2"));//实时脉冲位置转为实时绝对位置

            int n = ((int)(Math.Abs(ACurrentPosition - APosition) / K));//偏移脉冲数
            offset = Convert.ToInt32(n);//偏移脉冲数

            int APulsePosition = 0;//要移动到的绝对脉冲位置

            if (ACurrentPosition - APosition > 0)//如果当前位置在指定位置右侧 则向左移动
            {
                APulsePosition = current - offset;
            }
            else if (ACurrentPosition - APosition < 0)//如果当前位置在指定位置左侧 则向右移动
            {
                APulsePosition = current + offset;
            }
            else if (ACurrentPosition - APosition == 0)//如果不需要运动
            {
                APulsePosition = current;
            }

            SetPulsePositionSet(APulsePosition);
        }

        /// <summary>
        /// 运动指定距离(寸动)
        /// </summary>
        /// <param name="d">移动的距离</param>
        /// <param name="Dir">true 正向 ，false 反向</param>
        /// <returns></returns>
        public void SetSingleMove(double d, bool Dir = true, int pulseLen = 19200)//3840000 / 19200 = 200
        {
            //Result<int> state = new Result<int>();
            int state;

            int offset = 0;
            
            state = GetPulsePosition();//获取实时脉冲位置
            int current = state;//获取当前脉冲位置

            SetPulseLength(pulseLen);//设置脉冲步进长度

            int n = (int)(d / K);
            offset = Convert.ToInt32(n);//脉冲数

            int pulsepositionSet;//脉冲偏移位置

            if (Dir)//true 右
            {
                pulsepositionSet = current + offset;
            }
            else
            {
                pulsepositionSet = current - offset;
            }

            SetPulsePositionSet(pulsepositionSet);
        }


        #endregion

        #endregion
    }
}
