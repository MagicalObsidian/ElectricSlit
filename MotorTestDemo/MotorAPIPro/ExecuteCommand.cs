using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPro
{
    /// <summary>
    /// 命令执行
    /// </summary>
    public abstract class ExecuteCommand
    {
        /* 这里两个命令在 ModbusRTU 中被重写  */

        /// <summary>
        /// 读 
        /// </summary>
        /// <param name="pointEntities"></param>
        /// <param name="component"></param>
        /// <param name="comProperty"></param>
        /// <returns></returns>
        public virtual Result Read(PointEntity pointEntities, IComponent component, ComProperty comProperty) { return new Result(); }

        /// <summary>
        /// 写
        /// </summary>
        /// <param name="pointEntity"></param>
        /// <param name="component"></param>
        /// <param name="comProperty"></param>
        /// <returns></returns>
        public virtual Result Write(PointEntity pointEntity, IComponent component, ComProperty comProperty) { return new Result(); }

    }
}
