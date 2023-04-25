using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPro
{
    /// <summary>
    /// 数据协议帧信息类
    /// </summary>
    public class PointEntity
    {
        /// <summary>
        /// 从站ID
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
        /// 数据域
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 需要读取的寄存器个数
        /// </summary>
        public ushort ByteCount { get; set; }

    }
}
