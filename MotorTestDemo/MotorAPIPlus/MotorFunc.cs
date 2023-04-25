using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorAPIPlus
{
    /// <summary>
    /// 封装底层基本命令为上位方法
    /// </summary>
    public class MotorFunc
    {
        public MotorEntity _motor = null;
        public MotorFunc(MotorEntity motor)
        {
            _motor = motor;
        }

        /// <summary>
        /// 进行一些电机基本配置
        /// </summary>
        public void BaseConfig()
        {

        }


        /// <summary>
        /// 电机脱机
        /// </summary>
        public void DisEnable()
        {
            _motor.SetEnable(false);
        }

        /// <summary>
        /// 电机使能
        /// </summary>
        public void Enable()
        {
            _motor.SetEnable(true);
        }

        /// <summary>
        /// 电机运动中暂停
        /// </summary>
        public void Pause()
        {
            _motor.SetPause();
        }

        /// <summary>
        /// 判断当前电机运动状态
        /// </summary>
        public void JudgeState(out int motorStatus)
        {
            int controlRegisterData;
            controlRegisterData = _motor.GetControlRegisterData().Data;
            if(controlRegisterData == 8)//0x0008 向电机发送过了暂停命令
            {
                _motor.GetPosition();
                motorStatus = 8;
            }
            else if(controlRegisterData == 16)//0x0010 电机正在向下限位运动
            {
                motorStatus = 16;
            }
            else if(controlRegisterData == 32)//0x0020 电机正在向上限位运动
            {
                motorStatus = 32;
            }
            else//
            {
                motorStatus = 0;
            }
        }

        /// <summary>
        /// 向左单歩移动
        /// </summary>
        /// <param name="distance"></param>
        public void MoveLeft(double distance)
        {
            _motor.SetSingleMove(distance, false);
        }

        /// <summary>
        /// 向右单歩移动
        /// </summary>
        /// <param name="distance"></param>
        public void MoveRight(double distance)
        {
            _motor.SetSingleMove(distance, true);
        }

        /// <summary>
        /// 移动到指定的绝对位置
        /// </summary>
        /// <param name="setPosition"></param>
        /// <param name="positionSign"></param>
        public void MoveToPosition(double setPosition, bool positionSign)
        {
            _motor.SetMoveTo(setPosition, positionSign);
        }

        /// <summary>
        /// 移动至零位
        /// </summary>
        public void MoveToZero()
        {
            _motor.SetPulsePositionSet(0);
        }



    }
}
