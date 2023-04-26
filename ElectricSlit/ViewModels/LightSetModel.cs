using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricSlit.ViewModels
{
    public class LightSetModel : BaseModel
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

		private double light;
		/// <summary>
		/// 光强
		/// </summary>
		public double Light
		{
			get { return light; }
			set { light = value; OnPropertyChanged(); }
		}

		public LightSetModel() { }

		public LightSetModel(int index, double light)
		{
			this.Index = index;
			this.Light = light;
		}

	}
}
