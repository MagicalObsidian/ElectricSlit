using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPro
{
    /// <summary>
    /// 寄存器地址
    /// </summary>
    public class RegisterAddress
    {
        protected static Dictionary<short, string> DirRegisterAddress = new Dictionary<short, string>
        {
            { 0x0000, "控制寄存器" },//Control
            { 0x0002, "故障寄存器" },//ErrorCode
            { 0x0008, "输入类型寄存器" },//InputType
            { 0x0010, "电流最大值寄存器" },//CurrentMax
            { 0x0011, "电流最小值寄存器" },//CurrentMin
            { 0x0012, "电流设定寄存器" },//CurrentSet
            { 0x0013, "电流降流寄存器" },//CurrentLow
            { 0x0014, "电流降流等待时间寄存器" },//CurrentLowWT

            { 0x0020, "电机实时位置寄存器" },//Position 
            { 0x0021, "电机实时位置寄存器" },//
            { 0x0022, "电机实时位置寄存器" },//
            { 0x0023, "电机实时位置寄存器" },//

            { 0x0024, "电机设定位置寄存器" },//PositionSet 
            { 0x0025, "电机设定位置寄存器" },//
            { 0x0026, "电机设定位置寄存器" },//
            { 0x0027, "电机设定位置寄存器" },//

            { 0x0028, "电机单齿分辨率寄存器" },//TResolution 
            { 0x0029, "电机单齿分辨率寄存器" },//

            { 0x002A, "脉冲步进长度寄存器" },//PulseLength 
            { 0x002B, "脉冲步进长度寄存器" },//

            { 0x002C, "电机脉冲实时位置寄存器" },//
            { 0x002D, "电机脉冲实时位置寄存器" },//

            { 0x002E, "电机脉冲设定位置寄存器" },//
            { 0x002F, "电机脉冲设定位置寄存器" },//

            { 0x0030, "位差报错寄存器" },//PositionErrorAlarm 
            { 0x0031, "位差报错寄存器" },//

            { 0x0032, "到位位差寄存器" },//PositionErrorAllowed
            { 0x0033, "到位位差寄存器" },//

            { 0x0034, "到位时间寄存器" },//TimeErrorAllowed

            { 0x0038, "位置误差寄存器" },//PositionError
            { 0x0039, "位置误差寄存器" },//
            { 0x003A, "位置误差寄存器" },//
            { 0x003B, "位置误差寄存器" },//

            { 0x003C, "脉冲位置误差寄存器" },//PulsePositionError
            { 0x003D, "脉冲位置误差寄存器" },//

            { 0x0040, "电机设定速度寄存器" },//VelSet
            { 0x0041, "电机启动速度寄存器" },//VelStart
            { 0x0042, "速度滤波器寄存器" },//VelFilter
            { 0X0043, "电机速度系数寄存器" },//KV
            { 0X0044, "速度滤波器(通讯)寄存器" },//VelFilterCom

            { 0x0060, "速度滤波器(总线看门狗定时寄存器)寄存器" },//BusWDT
            { 0x0061, "总线地址寄存器" },//BusAddress
            { 0x0062, "总线通讯速率寄存器" },//BusBand
            { 0x0063, "总线通讯速率寄存器" },//

            { 0x0080, "端口寄存器" },//Port
            { 0x0081, "端口上升标记寄存器" },//PortHiFlag
            { 0x0082, "端口下降标记寄存器" },//PortLoFlag
            { 0x0083, "端口翻转标记寄存器" },//PortFlipFlag

            { 0x0090, "输入带宽寄存器" },//InputBand
            { 0x0091, "输入带宽寄存器" },//
            



        };

        /// <summary>
        /// 输入类型寄存器：
        /// 设置驱动器脉冲端口和方向端口的输入信号类型
        /// </summary>
        public enum InputType
        {
            //0 方向+脉冲下降沿
            //1 方向+脉冲上升沿
            //2 方向+脉冲双边沿
            //3 QEP
            //8 下限+上限
            Low = 0x0000,
            Top = 0x0001,
            Both = 0x0002,
            QEP = 0x0003,
            Limit = 0x0008
        }


    }
}
