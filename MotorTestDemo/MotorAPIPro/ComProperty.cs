using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace MotorAPIPro
{
    /// <summary>
    /// Com口属性
    /// </summary>
    public class ComProperty
    {
        /// <summary>
        /// 协议 默认 ModbusRTU
        /// </summary>
        public Protocol Protocol { get; set; } = Protocol.ModbusRTU;

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

        /// <summary>
        /// 检查串口信息是否匹配
        /// </summary>
        /// <param name="comProperty"></param>
        /// <returns></returns>
        public bool ComCompare(ComProperty comProperty)
        {
            if (this.Protocol == comProperty.Protocol &&
               this.PortName == comProperty.PortName &&
               this.BaudRate == comProperty.BaudRate &&
               this.StopBit == comProperty.StopBit &&
               this.DataBit == comProperty.DataBit &&
               this.Parity == comProperty.Parity)
            {
                return true;
            }
            return false;
        }

    }
}
