using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPro
{
    public class ModbusRTU : ModbusBase
    {
        /// <summary>
        /// 获取 读命令 重写
        /// </summary>
        /// <param name="modbusPointEntity"></param>
        /// <param name="slaveID"></param>
        /// <returns></returns>
        protected override byte[] GetReadCommand(ModbusPointEntity modbusPointEntity, byte slaveID)
        {
            byte[] bytes = base.GetReadCommand(modbusPointEntity, slaveID);

            List<byte> cmd = bytes.ToList();
            cmd.AddRange(CRC16(bytes.ToList()));
            return cmd.ToArray();
        }

        /// <summary>
        /// 获取 写命令 重写
        /// </summary>
        /// <param name="modbusPointEntity"></param>
        /// <param name="slaveID"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override byte[] GetWriteCommand(ModbusPointEntity modbusPointEntity, byte slaveID, byte[] data)
        {
            byte[] bytes = base.GetWriteCommand(modbusPointEntity, slaveID, data);
            List<byte> cmd = bytes.ToList();
            cmd.AddRange(CRC16(bytes.ToList()));
            return cmd.ToArray();
        }

        /// <summary>
        /// 读命令 重写
        /// </summary>
        /// <param name="pointEntity"></param>
        /// <param name="component"></param>
        /// <param name="comProperty"></param>
        /// <returns></returns>
        public override Result Read(PointEntity pointEntity, IComponent component, ComProperty comProperty)
        {

            Result result = new Result();
            ModbusPointEntity modbusPointEntity = new ModbusPointEntity();

            modbusPointEntity.RegisterAddress = pointEntity.RegisterAddress;
            modbusPointEntity.FunCode = pointEntity.FunCode;
            modbusPointEntity.ByteCount = pointEntity.ByteCount;
            try
            {
                byte[] bytes = this.GetReadCommand(modbusPointEntity, (byte)comProperty.SlaveID);
                Result<byte> data = component.SendAndReceive(bytes, modbusPointEntity.ByteCount, 5);
                if (data.Success)
                {
                    //校验
                    Result check = CheckCRC(data.DataList);

                    if (!check.Success) return check;
                    //提取数据
                    data.DataList.RemoveRange(data.DataList.Count - 2, 2);
                    data.DataList.RemoveRange(0, 3);
                    pointEntity.Data = data.DataList.ToArray();
                }
                else
                {
                    throw new Exception(data.Message);
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
        /// 写命令 重写
        /// </summary>
        /// <param name="pointEntity"></param>
        /// <param name="component"></param>
        /// <param name="comProperty"></param>
        /// <returns></returns>
        public override Result Write(PointEntity pointEntity, IComponent component, ComProperty comProperty)
        {
            // points[0].ValueBytes
            // 不考虑同时读写的操作
            Result result = new Result();
            try
            {
                ModbusPointEntity modbusPointEntity = new ModbusPointEntity();

                modbusPointEntity.RegisterAddress = pointEntity.RegisterAddress;
                modbusPointEntity.FunCode = pointEntity.FunCode;
                modbusPointEntity.ByteCount = pointEntity.ByteCount;
                List<byte> write_cmd = new List<byte>(this.GetWriteCommand(modbusPointEntity, (byte)comProperty.SlaveID, pointEntity.Data));

                Result<byte> send_result = component.SendAndReceive(write_cmd.ToArray(), modbusPointEntity.ByteCount);
                if (!send_result.Success || send_result.DataList.Count == 0) throw new Exception("响应报文接收异常！" + send_result.Message);

                result = CheckCRC(send_result.DataList);
                // 检查CRC、异常码
                if (!CheckCRC(send_result.DataList).Success)
                {
                    throw new Exception(result.Message);
                }
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }



        /// <summary>
        /// 检查校验码
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected Result CheckCRC(List<byte> bytes)
        {
            Result result = new Result();
            List<byte> recebyte = bytes.GetRange(0, bytes.Count - 2);
            List<byte> temp = bytes.GetRange(bytes.Count - 2, 2);
            List<byte> crc = CRC16(recebyte);

            if (!temp.SequenceEqual(crc))
            {
                result.Success = false;
                result.Message = "校验不通过";
                return result;
            }
            if ((bytes[1] & 0x80) == 0x80)
            {
                // ErrorFunCode
                string error = DicErrors.ContainsKey(bytes[2]) ? DicErrors[bytes[2]] : "其他异常";
                result.Message = error;
                result.Success = false;
                return result;
            }
            return result;
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
    }
}
