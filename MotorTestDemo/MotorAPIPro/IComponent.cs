using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPro
{
    /// <summary>
    /// 组件 接口
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// 连接（超时重连时间 2 s）
        /// </summary>
        /// <param name="outTime"></param>
        /// <returns></returns>
        Result Connect(int outTime = 2000);

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        Result Close();

        /// <summary>
        /// 发送和接收
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="receivelen"></param>
        /// <param name="outtime"></param>
        /// <returns></returns>
        Result<byte> SendAndReceive(byte[] cmd, int receivelen, int outTime = 2000);

        /// <summary>
        /// 发送和接收
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="receivelen"></param>
        /// <param name="errorlen"></param>
        /// <param name="outtime"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        Result<byte> SendAndReceive(byte[] cmd, int receivelen, int errorlen, int outTime = 2000, int len = 5);

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        Result Send(byte[] cmd);

    }
}
