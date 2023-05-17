using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricSlit.ViewModels
{
    public class LightManulModel : BaseModel
    {
        private int index;
        /// <summary>
        /// 索引
        /// </summary>
        public int Index
        {
            get { return index; }
            set { index = value; OnPropertyChanged(); }
        }

        private double width;
        /// <summary>
        /// 狭缝宽度
        /// </summary>
        public double Width
        {
            get { return width; }
            set { width = value; OnPropertyChanged(); }
        }

        private double light;
        /// <summary>
        /// 照度
        /// </summary>
        public double Light
        {
            get { return light; }
            set { light = value; OnPropertyChanged(); }
        }

        public LightManulModel() { }

        public LightManulModel(int index, double width, double light)
        {
            this.Index = index;
            this.Width = width;
            this.Light = light;
        }
    }
}
