using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPro
{
    public abstract class ModbusBase : ExecuteCommand
    {
        /// <summary>
        /// 错误码 字典
        /// </summary>
        protected static Dictionary<byte, string> DicErrors = new Dictionary<byte, string>
        {
            { 0x01, "非法功能码"},
            { 0x02, "非法数据地址"},
            { 0x03, "非法寄存器数量"},
            { 0x04, "从站设备故障"},
            { 0x05, "确认，从站需要一个耗时操作"},
            { 0x06, "从站忙"},
            { 0x08, "存储奇偶性差错"},
            { 0x0A, "不可用网关路径"},
            { 0x0B, "网关目标设备响应失败"}
        };

        /// <summary>
        /// 获取 读命令
        /// </summary>
        /// <param name="modbusPointEntity"></param>
        /// <param name="slaveID"></param>
        /// <returns></returns>
        protected virtual byte[] GetReadCommand(ModbusPointEntity modbusPointEntity, byte slaveID)
        {
            if (modbusPointEntity == null) return null;
            List<byte> cmd = new List<byte>();
            cmd.Add(slaveID);
            cmd.Add(modbusPointEntity.FunCode);
            cmd.Add(BitConverter.GetBytes(modbusPointEntity.RegisterAddress)[1]);
            cmd.Add(BitConverter.GetBytes(modbusPointEntity.RegisterAddress)[0]);
            cmd.Add(BitConverter.GetBytes(modbusPointEntity.ByteCount)[1]);
            cmd.Add(BitConverter.GetBytes(modbusPointEntity.ByteCount)[0]);
            return cmd.ToArray();
        }

        /// <summary>
        /// 获取 写命令
        /// </summary>
        /// <param name="modbusPointEntity"></param>
        /// <param name="slaveID"></param>
        /// <returns></returns>
        protected virtual byte[] GetWriteCommand(ModbusPointEntity modbusPointEntity, byte slaveID, byte[] data)
        {
            if (modbusPointEntity == null) return null;
            List<byte> cmd = new List<byte>();
            cmd.Add(slaveID);
            cmd.Add(modbusPointEntity.FunCode);
            cmd.Add(BitConverter.GetBytes(modbusPointEntity.RegisterAddress)[1]);
            cmd.Add(BitConverter.GetBytes(modbusPointEntity.RegisterAddress)[0]);
            if (modbusPointEntity.FunCode == 0x10)//写多个寄存器
            {
                cmd.Add(BitConverter.GetBytes(data.Length / 2)[1]);//寄存器数量
                cmd.Add(BitConverter.GetBytes(data.Length / 2)[0]);
                cmd.Add((byte)data.Length);//字节数
            }
            cmd.AddRange(data);
            return cmd.ToArray();
        }

        /// <summary>
        /// 解析???
        /// </summary>
        /// <param name="pointEntity"></param>
        /// <returns></returns>
        public ModbusPointEntity AnalysisAddress(PointEntity pointEntity)
        {
            ModbusPointEntity modbusPointEntity = new ModbusPointEntity();
            modbusPointEntity.SlaveID = pointEntity.SlaveID;




            return modbusPointEntity;
        }

    }
}
