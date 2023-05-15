using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricSlit.ViewModels
{
    public class WLModel : BaseModel
    {
        private double width;
        public double Width
        {
            get { return width; }
            set { width = value; OnPropertyChanged(); }
        }

        private double light;
        public double Light
        {
            get { return light; }
            set { light = value; OnPropertyChanged(); }
        }

        public WLModel() { }

        public WLModel(double w, double l)
        {
            this.Width = w;
            this.Light = l;
        }
    }
}
