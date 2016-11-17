/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */

using OpenBveApi.Runtime;
using System;

namespace Plugin
{
    internal class Ato : Device
    {
        private readonly Train Train;

        private Ato.States State;

        private double Countdown;

        private int Notch;

        private readonly bool AutomaticallyDeactivates = true;

        private readonly double PowerApplicationTime = 1;

        internal Ato(Train train)
        {
            this.Train = train;
            this.State = Ato.States.Disabled;
            this.Countdown = 0;
            this.Notch = 0;
        }

        internal override void Elapse(ElapseData data, ref bool blocking)
        {
	        int BrakeNotch = 0;
            if (this.Train.Doors != DoorStates.None & this.AutomaticallyDeactivates)
            {
                this.State = Ato.States.Disabled;
            }
            if (this.State == Ato.States.Disabled)
            {
                this.Notch = 0;
            }
            else if (this.Train.Atc == null || !(this.Train.Atc.State == Atc.States.Normal | this.Train.Atc.State == Atc.States.ServiceHalf | this.Train.Atc.State == Atc.States.ServiceFull | this.Train.Atc.State == Atc.States.Emergency))
            {
                this.Notch = 0;
	            BrakeNotch = Train.Specs.BrakeNotches + 1;
            }
            else if (!(this.Train.Atc.State == Atc.States.Normal & data.Handles.Reverser == 1 & this.Train.TractionManager.CurrentInterventionBrakeNotch == 0))
            {
                this.Notch = 0;
                //Set maximum power notch to zero via the Traction Manager
                Train.TractionManager.SetMaxPowerNotch(0, true);

            }
            else
            {
                double metersPerSecond = this.Train.State.Speed.MetersPerSecond;
                double currentSpeed = this.Train.Atc.Pattern.CurrentSpeed;
                if (this.Train.Tasc != null && this.Train.Tasc.State == Tasc.States.Pattern && !this.Train.Tasc.Override && 6.94444444444444 < currentSpeed)
                {
                    currentSpeed = 6.94444444444444;
                }
                double num = currentSpeed - 4.16666666666667;
                double num1 = currentSpeed - 1.38888888888889;
                if (metersPerSecond <= num)
                {
                    this.State = Ato.States.Power;
                }
                else if (metersPerSecond >= num1)
                {
                    this.State = Ato.States.Idle;
                }
                if (this.State != Ato.States.Power)
                {
                    this.Notch = 0;
                    //Set maximum power notch to zero via the Traction Manager
                    Train.TractionManager.SetMaxPowerNotch(0, true);
                }
                else
                {
                    int powerNotches = (int)Math.Ceiling((num1 - metersPerSecond) / 1.38888888888889 * (double)this.Train.Specs.PowerNotches);
                    if (powerNotches < 1)
                    {
                        powerNotches = 1;
                    }
                    else if (powerNotches > this.Train.Specs.PowerNotches)
                    {
                        powerNotches = this.Train.Specs.PowerNotches;
                    }
                    if (this.Notch >= powerNotches)
                    {
                        this.Notch = powerNotches;
                    }
                    else if (this.Countdown <= 0)
                    {
                        Ato notch = this;
                        notch.Notch = notch.Notch + 1;
                        this.Countdown = this.PowerApplicationTime / (double)this.Train.Specs.PowerNotches;
                    }
                    //Pass the calculated maximum power notch to the traction manager
                    Train.TractionManager.SetMaxPowerNotch(this.Notch, true);
                }
            }
            if (this.Countdown > 0)
            {
                Ato countdown = this;
                countdown.Countdown = countdown.Countdown - data.ElapsedTime.Seconds;
            }
	        Train.TractionManager.SetBrakeNotch(BrakeNotch);
            if (this.State != Ato.States.Disabled)
            {
                this.Train.Panel[91] = 1;
                if (this.Train.Atc == null || this.Train.Atc.State != Atc.States.Normal & this.Train.Atc.State != Atc.States.ServiceHalf | this.Train.Atc.State == Atc.States.ServiceFull & this.Train.Atc.State != Atc.States.Emergency)
                {
                    this.Train.Panel[92] = 1;
                }
            }
        }

        internal override void KeyDown(VirtualKeys key)
        {
            if (key == VirtualKeys.J)
            {
                if (this.State == Ato.States.Disabled)
                {
                    this.State = Ato.States.Idle;
                    return;
                }
                this.State = Ato.States.Disabled;
            }
        }

        internal enum States
        {
            Disabled,
            Idle,
            Power
        }
    }
}