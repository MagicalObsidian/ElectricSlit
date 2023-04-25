using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpticalPlatform.Model
{
   public class PortSetEntity : ModelBase
    {
        private string portName;

        public string PortName
        {
            get { return portName; }
            set { portName = value; OnPropertyChanged(); }
        }

        private string baudRate;

        public string BaudRate
        {
            get { return baudRate; }
            set { baudRate = value; OnPropertyChanged(); }
        }

        private string dataBits;
        public string DataBits
        {
            get { return dataBits; }
            set { dataBits = value; OnPropertyChanged(); }
        }

        private string stopBits;
        public string StopBits
        {
            get { return stopBits; }
            set { 
                stopBits = value; 
                OnPropertyChanged(); }
        }

        private string parity;
        public string Parity
        {
            get { return parity; }
            set { parity = value; OnPropertyChanged();}
        }

    }
}
