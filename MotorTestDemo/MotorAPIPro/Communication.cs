using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPro
{
    /// <summary>
    /// 设备通讯类
    /// </summary>
    public class Communication : IDisposable
    {
        private readonly static object lockobj = new object();
        private static List<Communication> communication = new List<Communication>();

        private ComProperty _comProperty = null;
        private SerialComponent _serialComponent = null;
        private ExecuteCommand _execute = null;
        private ModbusRTU _modbusRTU = null;    
        //private ModbusRTU _modbusRTU = null;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="comProperty"></param>
        public Communication(ComProperty comProperty)
        {
            if (comProperty == null) return;
            _comProperty = comProperty;
            _serialComponent = new SerialComponent(_comProperty);
            //MotorAPIPlus.ModbusRTU
            Type type = this.GetType().Assembly.GetType("MotorAPIPro." + comProperty.Protocol.ToString());
            _execute = (ExecuteCommand)Activator.CreateInstance(type);
        }

        /// <summary>
        /// 通讯类实例
        /// </summary>
        /// <param name="comProperty"></param>
        /// <returns></returns>
        public static Communication Instance(ComProperty comProperty)
        {
            var instance = communication.FirstOrDefault(li => li._comProperty.ComCompare(comProperty));
            if (instance != null) return instance;
            lock (lockobj)
            {
                if (instance == null)
                {
                    var state = communication.Where(p => p._comProperty.PortName == comProperty.PortName).ToList();
                    if (state.Count() > 0)
                    {
                        state.FirstOrDefault().Dispose();
                    }
                    instance = new Communication(comProperty);//如果不存在则创建实例
                    communication.Add(instance);
                }
            }
            return instance;
        }

        /// <summary>
        /// 返回串口连接结果
        /// </summary>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public Result Connection(int timeOut = 1000)
        {
            Result result = new Result();
            result = _serialComponent.Connect(timeOut);
            return result;
        }

        /// <summary>
        /// 读
        /// </summary>
        /// <param name="pointEntity"></param>
        /// <returns></returns>
        public Result Read(PointEntity pointEntity)
        {
            Result result = new Result();
            try
            {
                if (pointEntity == null || pointEntity.ByteCount == 0)//如果协议帧为空
                {
                    throw new Exception("No device request");//没有设备请求
                }
                if (_execute == null || _serialComponent == null)
                {
                    throw new Exception("Communication initialization error");//通讯错误
                }

                //result = _serialComponent.Connect();
                if (_serialComponent.comStatus)
                {
                    /*
                    //如果 读命令 失败
                    if (!_execute.Read(pointEntity, _serialComponent, _comProperty).Success)
                    {
                        result.Message = "The data is wrong";
                        result.Success = false;
                    }
                    */
                    _modbusRTU.Read(pointEntity, _serialComponent, _comProperty);


                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 写
        /// </summary>
        /// <returns></returns>
        public Result Write(PointEntity pointEntity)
        {
            Result result = new Result();
            try
            {
                if (pointEntity == null || pointEntity.ByteCount == 0)//如果协议帧为空
                {
                    throw new Exception("No device request");//没有设备请求
                }
                if (_execute == null || _serialComponent == null)
                {
                    throw new Exception("Communication initialization error");//通讯错误
                }

                //result = _serialComponent.Connect();
                if (_serialComponent.comStatus)//result.Success
                {
                    /*
                    // 如果 写命令 失败
                    if (!_execute.Write(pointEntity, _serialComponent, _comProperty).Success)
                    {
                        result.Message = "The data is wrong";
                        result.Success = false;
                    }
                    */
                    _modbusRTU.Write(pointEntity, _serialComponent, _comProperty);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 协议帧数据格式转换
        /// </summary>
        /// <param name="pointEntity"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ConvertValue(PointEntity pointEntity, Type type)
        {
            object res = null;
            try
            {
                if (type == typeof(UInt16))
                {
                    var a = pointEntity.Data.Reverse();
                    res = BitConverter.ToUInt16(a.ToArray(), 0);
                }
                else if (type == typeof(Int64))
                {
                    var a = pointEntity.Data.Reverse();
                    res = BitConverter.ToInt64(a.ToArray(), 0);
                }
                else if (type == typeof(Int32))
                {
                    var a = pointEntity.Data.Reverse();
                    res = BitConverter.ToInt32(a.ToArray(), 0);
                }
                else if (type == typeof(Int16))
                {
                    var a = pointEntity.Data.Reverse();
                    res = BitConverter.ToInt16(a.ToArray(), 0);
                }
            }
            catch (Exception)
            {

            }
            return res;
        }



        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            communication.RemoveAll(i =>
                                    i._comProperty.ComCompare(this._comProperty) &&
                                    i._serialComponent.Close().Success);
        }
    }
}
