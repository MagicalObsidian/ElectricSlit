using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPro
{
    /// <summary>
    /// 
    /// </summary>
    public class ModbusPointEntity
    {
        /// <summary>
        /// 从站 ID
        /// </summary>
        public byte SlaveID { get; set; }

        /// <summary>
        /// 功能码
        /// </summary>
        public byte FunCode { get; set; }

        /// <summary>
        /// 寄存器地址
        /// </summary>
        public ushort RegisterAddress { get; set; }

        /// <summary>
        /// 数据字节数
        /// </summary>
        public ushort ByteCount { get; set; }



        /// <summary>
        /// 无参构造
        /// </summary>
        public ModbusPointEntity()
        {

        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="pointEntity"></param>
        public ModbusPointEntity(PointEntity pointEntity)
        {
            this.FunCode = pointEntity.FunCode;
            this.RegisterAddress = pointEntity.RegisterAddress;
            this.ByteCount = pointEntity.ByteCount;
        }

    }
}
