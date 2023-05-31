# 杭州聚讯电机通讯库

## 一、使用

+ 动态链接库目标框架 .NET Framework 4.8

+ 程序集名称: MotorAPIPlus

+ 快速使用：

  + 引用类库

    ```C#
    using MotorAPIPlus;
    ```

  + 初始化连接

    ```C#
    private SerialPortHelper _serialPort_Motor = new SerialPortHelper("COM1");//串口默认配置，只需指定串口名
    private _serialPort_Motor.Connect()；//打开串口连接
    private MotorEntity _motorEntity = new MotorEntity(_serialPort_Motor); //创建电机命令实例
    private MotorFunc _motorFunc = new MotorFunc(_motorEntity); //将封装的一些底层命令改写成上位方法的实例
    ```

  + 基本功能使用

    ```C#
    _motorFunc.MoveToZero();//电机移动至零位
    _motorFunc.MoveLeft(10);//电机向左移动指定距离(10为所指定的实际移动距离如10mm，而不是电机脉冲距离)
    _motorFunc.MoveToPosition(10, true);//电机移动到指定位置(10表示实际位置如10mm位置处，true表示是零点向右，如果需要移动到-20mm处，则函数参数写为(20, flase))
    _motorFunc.MoveToLowerLimmit();//移动至下限位(需要接传感器，且电机设置为上下限位模式)
    _motorFunc.MoveToUpperLimmit();//移动至上限位
    _motorFunc.SetZero();//设置当前的脉冲位置为新的零位
    ```

## 二、方法说明

1. SerialPortHelper 类
   + Close()    --关闭串口，返回是否成功结果
   + Connect()    --打开串口，返回是否成功结果
   + SendAndReceive(byte[] cmd)   --发送命令，同时接收数据
2. MotorEntity 类
   + SetEnable(bool state = true)  --电机使能或脱机
   + SetInputType(int inputType)  --设置输入寄存器类型
   + SetPS() --直接设置输入寄存器模式位上下限位模式
   + GetKV() --获取电机速度系数(出厂时固化不可写)
   + SetRSpeed(double rpm) --设置转速
   + SetVelSet(double data) --设置电机的设定速度
   + GetCurVel() --获取电机的实时速度
   + SetPulseLength(int data) --设置脉冲单步长度
   + GetPulseLength() -- 获取脉冲单步长度
   + MovePSH() --运动至上限位 需要接传感器
   + MovePSL() --运动至下限位 需要接传感器
   + ReadPort() --读端口寄存器
   + ReadPortPSL() --读端口寄存器 判断下限位
   + ReadPortPSH() --读端口寄存器 判断上限位
   + GetCurrent() --获得实时电流
   + SetCurrentLow(Int16 data) --设置降流百分比
   + SetCurrentLowWT(double data) --设置降流电流等待时间
   + SetPause() --电机暂停
   + SetZero() --设定位置偏移至 0(即设置当前位置为新的零位)
   + SetMoveTo(double d, bool positionSign, int pulseLen) --运动到指定位置
   + SetSingleMove(double d, bool Dir = true, int pulseLen) --运动指定距离(寸动)

