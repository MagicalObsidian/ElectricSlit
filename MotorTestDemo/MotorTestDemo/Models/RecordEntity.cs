using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpticalPlatform.Model
{
    /// <summary>
    /// 状态信息实体类
    /// </summary>
    public class RecordEntity : ModelBase
    {
        /// <summary>
        /// 连接状态
        /// </summary>
        private string  stateMsg="Not Connected";
        public string StateMsg
        {
            get { return stateMsg; }
            set { stateMsg = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 位置
        /// </summary>
        private string position="0";
        public string Position
        {
            get { return position; }
            set { position = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 状态的表示颜色
        /// </summary>
        private string stateColor ="Red";
        public string StateColor
        {
            get { return stateColor; }
            set { stateColor = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 方向
        /// </summary>
        public bool Direction { get; set; }

        /// <summary>
        /// 方向正负表示
        /// </summary>
        private string directionSign = " ";
        public string DirectionSign
        {
            get { return directionSign; }
            set { directionSign = value; OnPropertyChanged(); }
        }


    }
}
