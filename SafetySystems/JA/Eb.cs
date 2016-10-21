/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */

using OpenBveApi.Runtime;
using System;

namespace Plugin
{
    internal class Eb : Device
    {
        private Train Train;

        internal double Counter;

        internal double TimeUntilBell = 60;

        internal double TimeUntilBrake = 65;

        internal double SpeedThreshold = 1.38888888888889;

        internal int EBSound = -1;

        internal Eb(Train train)
        {
            this.Train = train;
            this.Counter = 0;
        }

        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (blocking)
            {
                this.Counter = 0;
            }
            else if (!(Math.Abs(data.Vehicle.Speed.MetersPerSecond) > this.SpeedThreshold | this.Counter >= this.TimeUntilBell))
            {
                this.Counter = 0;
            }
            else
            {
                Eb counter = this;
                counter.Counter = counter.Counter + data.ElapsedTime.Seconds;
                if (this.Counter < this.TimeUntilBrake)
                {
                    if (this.Counter >= this.TimeUntilBell)
                    {
                        SoundManager.Play(EBSound,1.0,1.0,false);
                    }
                }
                else if (this.Train.AtsSx == null || this.Train.AtsSx.State == AtsSx.States.Disabled)
                {
                    SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
	                if (!Train.tractionmanager.brakedemanded)
	                {
		                Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches +1);
	                }
                }
                else if (this.Train.AtsSx.State != AtsSx.States.Disabled)
                {
                    this.Train.AtsSx.State = AtsSx.States.Emergency;
                    if (this.Train.AtsP != null && this.Train.AtsP.State == AtsP.States.Normal | this.Train.AtsP.State == AtsP.States.Pattern | this.Train.AtsP.State == AtsP.States.Brake | this.Train.AtsP.State == AtsP.States.Service | this.Train.AtsP.State == AtsP.States.Emergency)
                    {
                        this.Train.AtsP.State = AtsP.States.Standby;
                    }
                    if (this.Train.Atc != null && this.Train.Atc.State == Atc.States.Normal | this.Train.Atc.State == Atc.States.ServiceHalf | this.Train.Atc.State == Atc.States.ServiceFull | this.Train.Atc.State == Atc.States.Emergency)
                    {
                        this.Train.Atc.State = Atc.States.Ats;
                    }
                    this.Counter = 0;
                }
            }
            if (this.Counter < this.TimeUntilBrake)
            {
                if (this.Counter >= this.TimeUntilBell)
                {
                    this.Train.Panel[8] = 1;
                    this.Train.Panel[270] = 1;
                }
                return;
            }
            int num = ((int)data.TotalTime.Milliseconds % 1000 < 500 ? 1 : 0);
            this.Train.Panel[8] = num;
            this.Train.Panel[270] = num;
        }

        internal override void HornBlow(HornTypes type)
        {
            if (this.Counter < this.TimeUntilBrake)
            {
                this.Counter = 0;
            }
        }

        internal override void Initialize(InitializationModes mode)
        {
            this.Counter = 0;
        }

        internal override void KeyDown(VirtualKeys key)
        {
            if (key != VirtualKeys.A2)
            {
                return;
            }
            if (this.Counter >= this.TimeUntilBell)
            {
                this.Counter = 0;
            }
        }

        internal override void SetBrake(int brakeNotch)
        {
            if (this.Counter < this.TimeUntilBell)
            {
                this.Counter = 0;
            }
        }

        internal override void SetPower(int powerNotch)
        {
            if (this.Counter < this.TimeUntilBell)
            {
                this.Counter = 0;
            }
        }

        internal override void SetReverser(int reverser)
        {
            if (this.Counter < this.TimeUntilBell)
            {
                this.Counter = 0;
            }
        }
    }
}