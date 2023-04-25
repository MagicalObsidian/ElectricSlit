using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using static MotorAPIPro.RegisterAddress;

namespace MotorAPIPro
{
    /// <summary>
    /// 电机实体类
    /// </summary>
    public class MotorEntity : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Communication _communication = null;
        private ModbusRTU _modbusRTU = null;

        private static Int64 TRT = 3840000; //1 圈=3840000MMS
        public double K { get; set; } = 0.0025;//系数 //0.00025  //0.0000026

        /// <summary>
        /// 无参构造
        /// </summary>
        public MotorEntity()
        {

        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="communication"></param>
        public MotorEntity(Communication communication)
        {
            _communication = communication;

        }

        /// <summary>
        /// 最大电流值
        /// </summary>
        private float currentMax;
        public float CurrentMax
        {
            get { return currentMax; }
            set
            {
                currentMax = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentMax"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Len { get; set; }

        /// <summary>
        /// 电机速度?
        /// </summary>
        public int Speed { get; set; }


        #region 电机命令

        #region --电机启动配置
        /// <summary>
        /// 电机 false 脱机   true 使能
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Result SetEnable(bool state = true)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            byte[] bytes;
            if (state)//如果需要使能
            {
                bytes = new byte[] { 0x00, 0x00 };//01 06 00 00 00 00 89 CA 
            }
            else//如果需要脱机
            {
                bytes = new byte[] { 0x00, 0x04 };//01 06 00 00 00 04 88 09
            }
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0000, FunCode = 0x06, Data = bytes, ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取系数
        /// </summary>
        /// <returns></returns>
        public Result<ushort> GetKV()
        {
            Result state = new Result();
            Result<ushort> result = new Result<ushort>();
            ushort value;
            if (_communication == null) return new Result<ushort> { Success = false, Message = "Failed to initialize the communication component" };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0043, FunCode = 0x04, ByteCount = 1 };
            state = _communication.Read(pointEntity);
            if (!state.Success) return new Result<ushort>() { Success = false, Message = state.Message, Data = 0 };
            result.Data = value = Convert.ToUInt16(_communication.ConvertValue(pointEntity, typeof(ushort)));
            return result;
        }

        /// <summary>
        /// 设置上下限 true上限 ，false下限
        /// </summary>
        /// <param name="Dir"></param>
        /// <returns></returns>
        public Result SetMovePS(bool Dir = true)
        {
            Result result = new Result();
            Result<ushort> state = new Result<ushort>();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x06 0x00 0x00 0x00 0x20 0x88 0x12
            state = GetInputType();
            if (!state.Success) return new Result() { Success = false, Message = state.Message };
            ushort value = state.Data;
            byte[] bytes = BitConverter.GetBytes(value);
            byte temp = (byte)(bytes[0] << 4);
            if (temp != 0x80)
            {
                result.Success = false;
                result.Message = "InputType.PulseType 必须为 8";
                return result;
            }
            if (Dir)
            {
                bytes = new byte[] { 0x00, 0x20 };//上限位
            }
            else
            {
                bytes = new byte[] { 0x00, 0x10 };//下限位
            }
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0000, FunCode = 0x06, Data = bytes, ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 读取上下限 true上限 ，false下限
        /// </summary>
        /// <param name="Dir"></param>
        /// <returns></returns>
        public Result GetMovePS(bool Dir = true)
        {
            Result result = new Result();
            Result<ushort> state = new Result<ushort>();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            state = GetInputType();
            if (!state.Success) return new Result() { Success = false, Message = state.Message };
            ushort value = state.Data;
            byte[] bytes = BitConverter.GetBytes(value);
            byte temp = (byte)(bytes[0] << 4);
            if (temp != 0x80)
            {
                result.Success = false;
                result.Message = "InputType.PulseType 必须为 8";
                return result;
            }
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0000, FunCode = 0x04, ByteCount = 1 };
            result = _communication.Read(pointEntity);
            if (!result.Success) return new Result() { Success = false, Message = state.Message };
            byte bt = pointEntity.Data[1];
            Result result1 = new Result();
            if (Dir)
            {
                if ((bt & 0x20) != 0x20)
                {
                    result1.Success = true;
                    result1.Message = "运动至上限";
                    return result1;
                }
            }
            else
            {
                if ((bt & 0x10) != 0x10)
                {
                    result1.Success = true;
                    result1.Message = "运动至下限";
                    return result1;
                }
            }
            result1.Success = false;
            result1.Message = "运动中";
            return result1;
        }

        /// <summary>
        /// 设置输入类型寄存器
        /// </summary>
        /// <param name="inputType"></param>
        /// <returns></returns>
        public Result SetInputType(InputType inputType)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            var a = (Int16)inputType;
            List<byte> bytes = BitConverter.GetBytes(a).ToList();
            //byte[] bytes = BitConverter.GetBytes(a);
            bytes.Reverse();//0001 0000
            //0x01 0x06 0x00 0x08 0x00 0x08 0x88 0x0C
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0008, FunCode = 0x06, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取输入类型寄存器的输入类型
        /// </summary>
        /// <returns></returns>
        public Result<ushort> GetInputType()
        {
            //0x01 0x04 0x00 0x08 0x00 0x01 0xB0 0x08
            Result state = new Result();
            Result<ushort> result = new Result<ushort>();
            ushort value;
            byte[] bytes = new byte[] { 0x00, 0x01 };
            if (_communication == null) return new Result<ushort> { Success = false, Message = "Failed to initialize the communication component" };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0008, FunCode = 0x04, ByteCount = 1, Data = bytes };
            state = _communication.Read(pointEntity);
            if (!state.Success) return new Result<ushort>() { Success = false, Message = state.Message, Data = 0 };
            result.Data = value = Convert.ToUInt16(_communication.ConvertValue(pointEntity, typeof(ushort)));
            return result;
        }

        /// <summary>
        /// 设置转速 
        /// </summary>
        /// <param name="rpm">转/分</param>
        public Result SetRSpeed(double rpm)
        {
            ushort value;
            Result<ushort> result = GetKV();
            if (!GetKV().Success || result.Data == 0) return new Result() { Success = false, Message = result.Message };
            value = result.Data;
            value = (ushort)((rpm * TRT) / (60000 * value));
            Result state = SetVelSet(value);
            if (!state.Success) return state;
            return state;
        }

        /// <summary>
        /// 设置启动转速 
        /// </summary>
        /// <param name="rpm">转/分</param>
        public Result SetStartRSpeed(double rpm)
        {
            ushort value;
            Result<ushort> result = GetKV();
            if (!GetKV().Success || result.Data == 0) return new Result() { Success = false, Message = result.Message };
            value = result.Data;
            value = (ushort)((rpm * TRT) / (60000 * value));
            Result state = SetVelStart(value);
            if (!state.Success) return state;
            return state;
        }

        /// <summary>
        /// 设置电机的启动速度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Result SetVelStart(ushort data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0041, FunCode = 0x06, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取电机的启动速度
        /// </summary>
        /// <returns></returns>
        private Result<ushort> GetVelStart()
        {
            Result state = new Result();
            Result<ushort> result = new Result<ushort>();
            if (_communication == null) return new Result<ushort> { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x04 0x00 0x41 0x00 0x01 0x61 0xDE
            ushort value;
            byte[] bytes = new byte[] { 0x00, 0x01 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0041, FunCode = 0x04, ByteCount = 1, Data = bytes };
            state = _communication.Read(pointEntity);
            if (!state.Success) return new Result<ushort>() { Success = false, Message = state.Message, Data = 0 };
            result.Data = value = Convert.ToUInt16(_communication.ConvertValue(pointEntity, typeof(ushort)));
            return result;
        }

        /// <summary>
        /// 设置电机的设定速度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Result SetVelSet(ushort data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0040, FunCode = 0x06, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取电机的设定速度
        /// </summary>
        /// <returns></returns>
        private Result<ushort> GetVelSet()
        {
            Result state = new Result();
            Result<ushort> result = new Result<ushort>();
            if (_communication == null) return new Result<ushort> { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x03 0x00 0x2E 0x00 0x02 0XA4 0x02
            ushort value;
            byte[] bytes = new byte[] { 0x00, 0x02 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002E, FunCode = 0x03, ByteCount = 2, Data = bytes};
            state = _communication.Read(pointEntity);
            if (!state.Success) return new Result<ushort>() { Success = false, Message = state.Message, Data = 0 };
            result.Data = value = Convert.ToUInt16(_communication.ConvertValue(pointEntity, typeof(ushort)));
            return result;
        }

        /// <summary>
        /// 获取电机的实时速度寄存器
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Result<Int16> GetVel()
        {
            Result state = new Result();
            Result<Int16> result = new Result<Int16>();
            if (_communication == null) return new Result<Int16> { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x04 0x00 0x45 0x00 0x01 0x20 0x1F
            Int16 value;
            byte[] bytes = new byte[] { 0x00, 0x01 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0045, FunCode = 0x04, ByteCount = 1, Data = bytes };
            state = _communication.Read(pointEntity);
            if (!state.Success) return new Result<Int16>() { Success = false, Message = state.Message, Data = 0 };
            result.Data = value = Convert.ToInt16(_communication.ConvertValue(pointEntity, typeof(Int16)));
            return result;
        }

        /// <summary>
        /// 获取电机的真实实时速度
        /// </summary>
        /// <returns></returns>
        public Result<Int16> GetRSpeed()
        {
            Result<Int16> state = GetVel();
            Result<Int16> result = new Result<Int16>();
            if (!state.Success) return new Result<Int16>() { Success = false, Message = state.Message };
            ushort kv = GetKV().Data;
            if (kv == 0) return new Result<Int16>() { Success = false, Message = state.Message };
            Int16 value = Convert.ToInt16(state.Data * kv * 60000 / TRT);
            result.Success = true;
            result.Message = state.Message;
            result.Data = value;
            return result;
        }

        #endregion

        #region --电流设置
        /// <summary>
        /// 设置电流输出最大值
        /// </summary>
        /// <returns></returns>
        public Result SetCurrentMax(double data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "通信组件初始化失败" };
            //01 06 00 12 01 F4 29 D8
            data = data * 100;
            Int16 value = (short)data;
            byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0012, FunCode = 0x06, Data = bytes, ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取电流设定最大值
        /// </summary>
        /// <returns></returns>
        public float GetCurrentMax()
        {
            // Result<float> result = new Result<float>();
            Result state = new Result();
            if (_communication == null) return 0;
            //0x01 0x04 0x00 0x12 0x00 0x01 0x91 0xCF 
            byte[] bytes = new byte[] { 0x00, 0x01 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0012, FunCode = 0x04, ByteCount = 1, Data = bytes};
            state = _communication.Read(pointEntity);
            if (!state.Success) return 0;
            ushort value = (ushort)_communication.ConvertValue(pointEntity, typeof(ushort));
            return value / 100f;
        }

        /// <summary>
        /// 获取实时电流
        /// </summary>
        /// <returns></returns>
        public float GetCurrent()
        {
            Result state = new Result();
            if (_communication == null) return 0;
            //0x01 0x04 0x00 0x15 0x00 0x01 0x20 0x0E
            float value;
            byte[] bytes = new byte[] { 0x00, 0x01 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0015, FunCode = 0x04, ByteCount = 1, Data = bytes};
            state = _communication.Read(pointEntity);
            if (!state.Success) return 0;
            return value = Convert.ToSingle((ushort)_communication.ConvertValue(pointEntity, typeof(ushort)) / 100f);
        }

        /// <summary>
        /// 设置降流百分比
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result SetCurrentLow(Int16 data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            //0x01 0x06 0x00 0x13 0x00 0x1E 0xF8 0x07
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0013, FunCode = 0x06, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 设置降流电流
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result SetLow(double data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            float state = GetCurrentMax();
            if (state == 0) return new Result { Success = false, Message = "最大输出电流设置有误" };
            short value = (short)((data / state) * 100);
            result = SetCurrentLow(value);
            if (!result.Success) return result;
            return result;
        }

        /// <summary>
        /// 设置降流电流等待时间
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result SetCurrentLowWT(double data)
        {
            Result result = new Result();
            UInt16 value = (ushort)(data * 1000);
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            List<byte> bytes = BitConverter.GetBytes(value).ToList();
            bytes.Reverse();
            //0x01 0x06 0x00 0x13 0x00 0x1E 0xF8 0x07
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0013, FunCode = 0x06, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }
        #endregion

        #region --电机脉冲
        /* 电机运动主要有两组寄存器可以使用：
           一组是 Position 与 PositionSet，单位为 MMS，1 圈=3840000MMS。
           另一组是 PulsePosition 与 PulsePositionSet，单位为细分后 1 脉冲所代表的距离。
           电机运动方向为 PositionSet-Position 或者 PulsePositionSet-PulsePosition。所得差值为正，电机
           正向运转；所得差值为负，电机反向运转  */

        /// <summary>
        /// 设置脉冲单步长度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result SetPulseLength(int data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            //0x01 0x10 0x00 0x2A 0x00 0x02 0x04 0x00 0x00 0x06 0x00 0x72 0x68
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002A, FunCode = 0x10, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取脉冲单步长度
        /// </summary>
        /// <returns></returns>
        public Result<int> GetPulseLength()
        {
            Result state = new Result();
            int value;
            Result<int> result = new Result<int>();
            if (_communication == null) return new Result<int>() { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x03 0x00 0x2A 0x00 0x02 0xE5 0xC3
            byte[] bytes = new byte[] { 0x00, 0x02 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002A, FunCode = 0x03, Data = bytes, ByteCount = 2 };
            state = _communication.Read(pointEntity);
            if (!state.Success) return new Result<int>() { Success = false, Message = state.Message, Data = 0 };
            result.Data = value = Convert.ToInt32(_communication.ConvertValue(pointEntity, typeof(Int32)));
            return result;
        }

        /// <summary>
        /// 设置脉冲实时位置
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result SetPulsePosition(int data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            //功能码 0x04 0x06 读写单个寄存器  0x03 0x10 读写多个寄存器  
            //0x01 0x10 0x00 0x2C 0x00 0x02 0x04 0x00 0x00 0x27 0x10 0xEB 0xDE
            //0x01 0x06 0x00 0x2C 0x27 0x10 0x52 0x3F Example
            //PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002C, FunCode = 0x06, Data = bytes.ToArray(), ByteCount = 1 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002C, FunCode = 0x10, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取脉冲实时位置
        /// </summary>
        /// <returns></returns>
        public Result<int> GetPulsePosition()
        {
            Result state = new Result();
            Result<int> result = new Result<int>();
            if (_communication == null) return new Result<int> { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x03 0x00 0x2C 0x00 0x02 0x05 0xC2
            //0x01 0x04 0x00 0x2C 0x00 0x01 0xF0 0x03 低Word Example
            int value;
            byte[] bytes = new byte[] { 0x00, 0x02 };
            //PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002C, FunCode = 0x04, ByteCount = 2 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002C, FunCode = 0x03, Data = bytes, ByteCount = 2 };
            state = _communication.Read(pointEntity);
            if (!state.Success) return new Result<int>() { Success = false, Message = state.Message, Data = -1 };
            result.Data = value = Convert.ToInt32(_communication.ConvertValue(pointEntity, typeof(int)));
            return result;
        }

        /* 电机脉冲当前位置为 0，细分为 2500 脉冲/圈时，向
           PulsePosition 写入 10000，电机运行 4 圈。
           电机脉冲当前位置为-10000，细分为 2500 脉冲/圈时，
           向 PulsePosition 写入 10000，电机运行 8 圈。 */

        /// <summary>
        /// 设置脉冲设定位置
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result SetPulsePositionSet(int data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002E, FunCode = 0x10, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取脉冲设定位置
        /// </summary>
        /// <returns></returns>
        public Result<int> GetPulsePositionSet()
        {
            Result state = new Result();
            Result<int> result = new Result<int>();
            if (_communication == null) return new Result<int> { Success = false, Message = "Failed to initialize the communication component" };
            //01 03 00 2E 00 02 A4 02
            int value;
            byte[] bytes = new byte[] { 0x00, 0x02 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x002E, FunCode = 0x03, Data = bytes, ByteCount = 2 };
            state = _communication.Read(pointEntity);
            if (!state.Success) return new Result<int>() { Success = false, Message = state.Message, Data = 0 };
            result.Data = value = Convert.ToInt32(_communication.ConvertValue(pointEntity, typeof(int)));
            return result;
        }

        #endregion

        #region --电机位置
        /// <summary>
        /// 获取电机实时位置
        /// </summary>
        /// <returns></returns>
        public Result<Int64> GetPosition()
        {
            Result state = new Result();
            Result<Int64> a = new Result<long>();
            if (_communication == null)
            {
                a.Success = false;
                a.Message = "通讯异常";
            }
            // 0x01 0x03 0x00 0x20 0x00 0x04 0x45 0xC3
            Int64 value;
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0020, FunCode = 0x03, ByteCount = 4 };
            state = _communication.Read(pointEntity);
            if (!state.Success)
            {
                a.Success = false;
                a.Message = state.Message;
                return a;
            }
            a.Data = value = Convert.ToInt64(_communication.ConvertValue(pointEntity, typeof(Int64)));
            return a;
        }

        /// <summary>
        /// 设置绝对位置
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result SetPosition(Int64 data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "通信组件初始化失败" };
            byte[] bytes = BitConverter.GetBytes(data).ToArray();
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0020, FunCode = 0x10, Data = bytes, ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 设置移动到的绝对位置
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result SetPositionSet(Int64 data)
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "通信组件初始化失败" };
            List<byte> bytes = BitConverter.GetBytes(data).ToList();
            bytes.Reverse();
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0024, FunCode = 0x10, Data = bytes.ToArray(), ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 获取绝对位置
        /// </summary>
        /// <returns></returns>
        public Result<Int64> GetPositionSet()
        {
            Result state = new Result();
            Result<Int64> a = new Result<long>();
            if (_communication == null)
            {
                a.Success = false;
                a.Message = "Failed to initialize the communication component";
                return a;
            }
            // 0x01 0x03 0x00 0x20 0x00 0x04 0x45 0xC3
            Int64 value;
            byte[] bytes = new byte[] { 0x00, 0x04 };
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0024, FunCode = 0x03, ByteCount = 4, Data = bytes };
            state = _communication.Read(pointEntity);
            if (!state.Success)
            {
                a.Success = false;
                a.Message = state.Message;
                return a;
            }
            a.Data = value = Convert.ToInt64(_communication.ConvertValue(pointEntity, typeof(Int64)));
            return a;
        }

        #endregion

        #region --电机运动
        /* 电机运动方向：脉冲实时位置 PulsePostion→脉冲设定位置 PulsePositionSet，运动到达时 Position =
           PositionSet，注意运动的条件是 Position 和 PositionSet，而不是 PulsePositon 和 PulsePositionSet。
           PulsePosition 到达时 Position 可能还没到达，而 Position 到达时 PulsePosition 一定也已到达。*/

        /// <summary>
        /// 电机正反转
        /// </summary>
        /// <param name="r">圈数</param>
        /// <param name="direction">方向</param>
        public Result SetRevolution(double r, bool direction = true)
        {
            Result<long> result = GetPosition();
            if (!GetPosition().Success) return new Result() { Success = false, Message = result.Message };
            Int64 oldPosition = GetPosition().Data;
            Int64 currentPosition;
            if (direction)
            {
                currentPosition = (long)(r * TRT + oldPosition);
            }
            else
            {
                currentPosition = (long)(oldPosition - r * TRT);
            }
            Result state = SetPositionSet(currentPosition);
            if (!SetPositionSet(currentPosition).Success) return new Result() { Success = false, Message = state.Message };
            return state;
        }

        /// <summary>
        /// 脉冲一转
        /// </summary>
        /// <param name="r"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Result SetPulseRevolution(float r, bool direction = true)
        {
            Result<int> result = GetPulsePosition();
            if (!GetPosition().Success) return new Result() { Success = false, Message = result.Message };
            int oldPosition = result.Data;
            int currentPosition;
            int len = GetPulseLength().Data;
            if (len == 0) return new Result() { Success = false, Message = result.Message };
            //  if(GetPulseLength().Data==0) return new Result() { Success = result.Success, Message = result.Message };
            if (direction)
            {
                currentPosition = (int)(r * (int)(TRT / len)) + oldPosition;
            }
            else
            {
                currentPosition = oldPosition - (int)(r * (int)(TRT / GetPulseLength().Data));
            }
            Result state = SetPulsePositionSet(currentPosition);
            if (!state.Success) return new Result() { Success = false, Message = state.Message };
            return state;
        }

        /// <summary>
        /// 电机暂停/继续? 有问题(实际测试是暂停 Pause)
        /// </summary>
        /// <returns></returns>
        public Result SetPause()
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            byte[] bytes = new byte[] { 0x00, 0x08 };
            //0x01 0x06 0x00 0x00 0x00 0x08 0x88 0x0C
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0000, FunCode = 0x06, Data = bytes, ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 电机停止? 有问题(实际测试是继续 Continue)
        /// </summary>
        /// <returns></returns>
        public Result SetStop()
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            byte[] bytes = new byte[] { 0x00, 0x00 };
            //0x01 0x06 0x00 0x00 0x00 0x00 0x89 0xCA
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0000, FunCode = 0x06, Data = bytes, ByteCount = 1 };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 设定位置偏移至 0(即设置新的零位)
        /// </summary>
        /// <returns></returns>
        public Result SetZero()
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x06 0x00 0x00 0x01 0x00 0x88 0x5A
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0000, FunCode = 0x06, ByteCount = 1, Data = new byte[] { 0x01, 0x00 } };
            result = _communication.Write(pointEntity);
            return result;
        }

        /// <summary>
        /// 运动至下限位 需要接传感器
        /// </summary>
        /// <returns></returns>
        public Result MovePSL()
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x06 0x00 0x00 0x00 0x10 0x88 0x06
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0000, FunCode = 0x06, ByteCount = 1, Data = new byte[] { 0x00, 0x10 } };
            result = _communication.Write(pointEntity);

            return result;
        }

        /// <summary>
        /// 运动至上限位 需要接传感器
        /// </summary>
        /// <returns></returns>
        public Result MovePSH()
        {
            Result result = new Result();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            //0x01 0x06 0x00 0x00 0x00 0x20 0x88 0x12
            PointEntity pointEntity = new PointEntity { RegisterAddress = 0x0000, FunCode = 0x06, ByteCount = 1, Data = new byte[] { 0x00, 0x20 } };
            result = _communication.Write(pointEntity);

            return result;
        }


        /// <summary>
        /// 运动到指定位置
        /// </summary>
        /// <param name="d">指定位置</param>
        /// <returns></returns>
        public Result SetMoveTo(double d, bool positionSign, int pulseLen = 50)
        {
            Result result = new Result();
            int offset = 0;
            double APosition;//要移动到的绝对位置
            double ACurrentPosition;//当前实际绝对位置
            Result<int> state = new Result<int>();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            state = GetPulsePosition();
            if (!state.Success) return new Result() { Success = state.Success, Message = state.Message };
            SetPulseLength(pulseLen);//设置脉冲步进长度
            int current = state.Data;//实时脉冲位置
            int len = GetPulseLength().Data;
            if (len == 0) return new Result() { Success = false, Message = result.Message };

            APosition = positionSign ? d : -d;
            ACurrentPosition = Convert.ToDouble((current * K).ToString("f2"));

            int n = ((int)(Math.Abs(ACurrentPosition - APosition) / K));//脉冲数
            offset = Convert.ToInt32(n);
            int APulsePosition = 0;//要移动到的绝对脉冲位置

            if (ACurrentPosition - APosition > 0)//如果当前位置在指定位置右侧 则向左移动
            {
                APulsePosition = current - offset;
            }
            else if (ACurrentPosition - APosition < 0)//如果当前位置在指定位置左侧 则向右移动
            {
                APulsePosition = current + offset;
            }
            else if(ACurrentPosition - APosition == 0)
            {
                APulsePosition = current;
            }

            result = SetPulsePositionSet(APulsePosition);
            if (!result.Success) return new Result() { Success = result.Success, Message = result.Message };
            return result;
        }

        /// <summary>
        /// 运动指定距离
        /// </summary>
        /// <param name="d">移动的距离</param>
        /// <param name="Dir">true 正向 ，false 反向</param>
        /// <returns></returns>
        public Result SetSingleMove(double d, bool Dir = true, int pulseLen = 50)
        {
            Result result = new Result();
            int offset = 0;
            Result<int> state = new Result<int>();
            if (_communication == null) return new Result { Success = false, Message = "Failed to initialize the communication component" };
            state = GetPulsePosition();//
            if (!state.Success) return new Result() { Success = state.Success, Message = state.Message };
            SetPulseLength(pulseLen);//设置脉冲步进长度
            int len = GetPulseLength().Data;//
            if (len == 0) return new Result() { Success = false, Message = result.Message };
            int current = state.Data;//获取当前位置
            int n = (int)(d / K);
            offset = Convert.ToInt32(n);//脉冲数
            int pulsepositionSet;
            if (Dir)
            {
                pulsepositionSet = current + offset;
            }
            else
            {
                pulsepositionSet = current - offset;
            }
            result = SetPulsePositionSet(pulsepositionSet);
            //Result<int> result1 = GetPulsePositionSet();
            if (!result.Success) return new Result() { Success = result.Success, Message = result.Message };
            return result;
        }

        /// <summary>
        /// 持续运动
        /// </summary>
        /// <returns></returns>
        public Result SetContinuous(bool Dir = true)
        {
            Result result = new Result();
            if (Dir)
            {
                if (!result.Success) return result;
                Int32 a = 2147483647;
                result = SetPulsePositionSet(a);
                if (!result.Success) return result;
            }
            else
            {
                if (!result.Success) return result;
                result = SetPulsePositionSet(-2147483648);
                if (!result.Success) return result;
            }
            return result;
        }

        #endregion

        #endregion
    }
}
