using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPlus
{
    /// <summary>
    /// 各种返回的结果（模板 T 表示返回结果中的数据类型）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T>
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 单个数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 数据列表
        /// </summary>
        public List<T> DataList { get; set; }

        /// <summary>
        /// 结果的描述信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 默认构造
        /// </summary>
        public Result()
        {
            Success = true;
            DataList = null;
            Message = "";
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="success"></param>
        /// <param name="dataList"></param>
        /// <param name="message"></param>
        public Result(bool success, List<T> dataList, string message)
        {
            Success = success;
            DataList = dataList;
            Message = message;
        }
    }

    /// <summary>
    /// 返回结果中数据类型为 string
    /// </summary>
    public class Result : Result<string>
    {

    }
}
