using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricSlit.ViewModels
{
    public class IColorTempViewModel : BaseModel
    {
        private double current;

        public double Current
        {
            get { return current; }
            set { current = value; OnPropertyChanged(); }
        }

        private int colorTemp;

        public int ColorTemp
        {
            get { return colorTemp; }
            set { colorTemp = value; OnPropertyChanged(); }
        }

        public IColorTempViewModel() { }

        public IColorTempViewModel(double i, int c)
        {
            this.Current = i;
            this.ColorTemp = c;
        }
    }
}
