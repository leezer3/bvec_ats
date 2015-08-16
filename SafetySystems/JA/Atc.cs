﻿/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */
using OpenBveApi.Runtime;
using System;
using System.Collections.Generic;
using OpenBveApi.Sounds;

namespace Plugin
{
    internal class Atc : Device
    {
        private const double CompatibilitySuppressDistance = 50;

        private Train Train;

        internal Atc.States State;

        private bool SwitchToAtcOnce;

        internal bool EmergencyOperation;

        private int Aspect;

        private double BlockLocation;

        internal Atc.SignalPattern Pattern;

        private Atc.CompatibilityStates CompatibilityState;

        private List<Atc.CompatibilityLimit> CompatibilityLimits;

        private int CompatibilityLimitPointer;

        private double CompatibilitySuppressLocation = double.MinValue;

        private PrecedingVehicleState PrecedingTrain;

        private int RealTimeAdvanceWarningUpcomingSignalAspect = -1;

        private double RealTimeAdvanceWarningUpcomingSignalLocation = double.MinValue;

        private double RealTimeAdvanceWarningReferenceLocation = double.MinValue;

        private double ServiceBrakesTimer;

        private readonly Atc.Signal NoSignal = Atc.Signal.CreateNoSignal(-1);

        private readonly double[] CompatibilitySpeeds = new double[] { -1, 0, 4.16666666666667, 6.94444444444444, 12.5, 15.2777777777778, 18.0555555555556, 20.8333333333333, 25, 27.7777777777778, 30.5555555555556, 33.3333333333333 };

        private double MaximumDeceleration = 1.11111111111111;

        private double RegularDeceleration = 0.530555555555556;

        private double RegularDelay = 0.5;

        private double OrpDeceleration = 1.14444444444444;

        private double OrpDelay = 3.9;

        private double OrpReleaseSpeed = 2.77777777777778;

        private double Acceleration = 0.530555555555556;

        private double AccelerationDelay = 2;

        private double AccelerationTimeThreshold1 = 5;

        private double AccelerationTimeThreshold2 = 15;

        private double FinalApproachSpeed = 6.94444444444444;

        private double ServiceBrakesTimerMaximum = 2;

        private double ServiceBrakesSpeedDifference = 2.77777777777778;

        internal bool AutomaticSwitch;

        internal Atc.Signal EmergencyOperationSignal = Atc.Signal.CreateEmergencyOperationSignal(4.16666666666667);

        internal List<Atc.Signal> Signals = new List<Atc.Signal>();

        internal Atc(Train train)
        {
            this.Train = train;
            this.State = Atc.States.Disabled;
            this.EmergencyOperation = false;
            this.Aspect = 0;
            this.Pattern = new Atc.SignalPattern(this.NoSignal, this);
            this.CompatibilityState = Atc.CompatibilityStates.Ats;
            this.CompatibilityLimits = new List<Atc.CompatibilityLimit>();
        }

        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            double acceleration;
            object str;
            object obj;
            this.PrecedingTrain = data.PrecedingVehicle;
            foreach (Atc.Signal signal in this.Signals)
            {
                if (this.Aspect != signal.Aspect)
                {
                    continue;
                }
                this.CompatibilitySuppressLocation = this.Train.State.Location;
                break;
            }
            if (this.SwitchToAtcOnce)
            {
                this.State = Atc.States.Normal;
                this.SwitchToAtcOnce = false;
            }
            if (this.State == Atc.States.Suppressed && data.Handles.BrakeNotch <= this.Train.Specs.BrakeNotches)
            {
                this.State = Atc.States.Ats;
            }
            if (!blocking)
            {
                Atc.Signal currentSignal = this.GetCurrentSignal();
                Atc.SignalPattern pattern = this.Pattern;
                Atc.SignalPattern signalPattern = new Atc.SignalPattern(currentSignal, this);
                if (Math.Abs(this.RealTimeAdvanceWarningUpcomingSignalLocation - this.RealTimeAdvanceWarningReferenceLocation) < 5)
                {
                    if (!(currentSignal.FinalSpeed <= 0 | currentSignal.OverrunProtector))
                    {
                        Atc.Signal upcomingSignal = this.GetUpcomingSignal();
                        currentSignal.ZenpouYokoku = upcomingSignal.FinalSpeed < currentSignal.FinalSpeed & currentSignal.FinalSpeed > 0;
                    }
                    else
                    {
                        currentSignal.ZenpouYokoku = false;
                    }
                }
                signalPattern.Update(this);
                if (currentSignal.Distance > 0 & currentSignal.Distance < double.MaxValue)
                {
                    double blockLocation = this.BlockLocation + currentSignal.Distance - this.Train.State.Location;
                    double num = (currentSignal.OverrunProtector ? this.OrpDeceleration : this.RegularDeceleration);
                    double num1 = (currentSignal.OverrunProtector ? this.OrpDelay : this.RegularDelay);
                    double currentSpeed = (pattern.CurrentSpeed * pattern.CurrentSpeed - currentSignal.FinalSpeed * currentSignal.FinalSpeed) / (2 * num) + pattern.CurrentSpeed * num1;
                    double num2 = (blockLocation - currentSpeed) / Math.Max(pattern.CurrentSpeed, 1.38888888888889);
                    bool finalSpeed = currentSignal.FinalSpeed <= 0 & signalPattern.CurrentSpeed < this.FinalApproachSpeed | currentSignal.OverrunProtector;
                    if (num2 > this.AccelerationTimeThreshold2 & !finalSpeed)
                    {
                        double acceleration1 = 4 * this.Acceleration * this.Acceleration * num * num * (this.AccelerationTimeThreshold2 + num1) * (this.AccelerationTimeThreshold2 + num1) + 4 * (this.Acceleration + num) * (num * this.Train.State.Speed.MetersPerSecond * this.Train.State.Speed.MetersPerSecond + this.Acceleration * (2 * num * (blockLocation - this.Train.State.Speed.MetersPerSecond * this.AccelerationDelay) + currentSignal.FinalSpeed * currentSignal.FinalSpeed));
                        if (acceleration1 <= 0)
                        {
                            acceleration = Math.Max(currentSignal.FinalSpeed, pattern.CurrentSpeed);
                        }
                        else
                        {
                            acceleration = (-this.Acceleration * num * (this.AccelerationTimeThreshold2 + num1) + 0.5 * Math.Sqrt(acceleration1)) / (this.Acceleration + num);
                            if (acceleration < currentSignal.FinalSpeed)
                            {
                                acceleration = currentSignal.FinalSpeed;
                            }
                        }
                        acceleration = Math.Floor(0.72 * acceleration + 0.001) / 0.72;
                        if (acceleration < pattern.CurrentSpeed)
                        {
                            acceleration = pattern.CurrentSpeed;
                        }
                        currentSignal = new Atc.Signal(currentSignal.Aspect, currentSignal.Indicator, acceleration, acceleration, -1, currentSignal.Kirikae, currentSignal.ZenpouYokoku, currentSignal.OverrunProtector);
                        signalPattern = new Atc.SignalPattern(currentSignal, this);
                        signalPattern.Update(this);
                    }
                    else if (!(num2 > this.AccelerationTimeThreshold1 & !finalSpeed))
                    {
                        if (finalSpeed & currentSignal.Indicator == Atc.SignalIndicators.Green)
                        {
                            currentSignal = new Atc.Signal(currentSignal.Aspect, Atc.SignalIndicators.Red, currentSignal.InitialSpeed, currentSignal.FinalSpeed, currentSignal.Distance, currentSignal.Kirikae, currentSignal.ZenpouYokoku, currentSignal.OverrunProtector);
                            signalPattern = new Atc.SignalPattern(currentSignal, this);
                        }
                        if (!currentSignal.OverrunProtector)
                        {
                            signalPattern.TopSpeed = Math.Max(currentSignal.FinalSpeed, pattern.CurrentSpeed);
                        }
                        signalPattern.Update(this);
                    }
                    else if (!(pattern.Signal.Distance > 0 & pattern.Signal.Distance < double.MaxValue))
                    {
                        double currentSpeed1 = pattern.CurrentSpeed;
                        currentSignal = new Atc.Signal(pattern.Signal.Aspect, pattern.Signal.Indicator, currentSpeed1, currentSpeed1, -1, pattern.Signal.Kirikae, pattern.Signal.ZenpouYokoku, pattern.Signal.OverrunProtector);
                        signalPattern = new Atc.SignalPattern(currentSignal, this);
                        signalPattern.Update(this);
                    }
                    else
                    {
                        signalPattern.TopSpeed = Math.Max(currentSignal.FinalSpeed, pattern.CurrentSpeed);
                        signalPattern.Update(this);
                    }
                }
                if (this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency && !Atc.SignalPattern.ApperanceEquals(pattern, signalPattern))
                {
                    if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
                    {
                        SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
                    }
                }
                this.Pattern = signalPattern;
                if (!(this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency))
                {
                    this.ServiceBrakesTimer = 0;
                }
                else
                {
                    if (this.Pattern.CurrentSpeed < 0 || this.Pattern.Signal.OverrunProtector && (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= this.Pattern.CurrentSpeed || this.State == Atc.States.Emergency && (Math.Abs(data.Vehicle.Speed.MetersPerSecond) > 0 || this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)))
                    {
                        if (this.State != Atc.States.ServiceFull & this.State != Atc.States.Emergency)
                        {
                            this.ServiceBrakesTimer = 0;
                        }
                        if (this.State != Atc.States.Emergency)
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
                            {
                                SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
                            }
                            this.State = Atc.States.Emergency;
                        }
                        if (this.Pattern.Signal.OverrunProtector & Math.Abs(data.Vehicle.Speed.MetersPerSecond) > 0)
                        {
                            this.ServiceBrakesTimer = 0;
                        }
                        else if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
                        {
                            Atc serviceBrakesTimer = this;
                            serviceBrakesTimer.ServiceBrakesTimer = serviceBrakesTimer.ServiceBrakesTimer + data.ElapsedTime.Seconds;
                        }
                    }
                    else if (this.Pattern.CurrentSpeed == 0)
                    {
                        if (this.State != Atc.States.ServiceFull & this.State != Atc.States.Emergency)
                        {
                            this.ServiceBrakesTimer = 0;
                        }
                        if (this.State == Atc.States.Emergency)
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
                            {
                                SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
                            }
                        }
                        this.State = Atc.States.ServiceFull;
                        if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
                        {
                            Atc atc = this;
                            atc.ServiceBrakesTimer = atc.ServiceBrakesTimer + data.ElapsedTime.Seconds;
                        }
                    }
                    else if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) < this.Pattern.ReleaseSpeed)
                    {
                        if (this.State == Atc.States.Emergency)
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
                            {
                                SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
                            }
                            this.State = Atc.States.Normal;
                            this.ServiceBrakesTimer = 0;
                        }
                        if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
                        {
                            Atc serviceBrakesTimer1 = this;
                            serviceBrakesTimer1.ServiceBrakesTimer = serviceBrakesTimer1.ServiceBrakesTimer + data.ElapsedTime.Seconds;
                        }
                        if (this.ServiceBrakesTimer >= this.ServiceBrakesTimerMaximum)
                        {
                            if (this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency)
                            {
                                this.State = Atc.States.ServiceHalf;
                                this.ServiceBrakesTimer = 0;
                            }
                            else if (this.State == Atc.States.ServiceHalf)
                            {
                                this.State = Atc.States.Normal;
                                this.ServiceBrakesTimer = 0;
                            }
                        }
                    }
                    else if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= this.Pattern.CurrentSpeed + 0.277777777777778)
                    {
                        if (this.State == Atc.States.Emergency)
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
                            {
                                SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
                            }
                            this.State = Atc.States.ServiceFull;
                            this.ServiceBrakesTimer = 0;
                        }
                        if (!(this.Pattern.CurrentSpeed > 0 & this.Train.State.Speed.MetersPerSecond < this.Pattern.CurrentSpeed + this.ServiceBrakesSpeedDifference))
                        {
                            if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
                            {
                                Atc atc1 = this;
                                atc1.ServiceBrakesTimer = atc1.ServiceBrakesTimer + data.ElapsedTime.Seconds;
                            }
                            if (this.ServiceBrakesTimer >= this.ServiceBrakesTimerMaximum)
                            {
                                if (this.State == Atc.States.Normal)
                                {
                                    this.State = Atc.States.ServiceHalf;
                                    this.ServiceBrakesTimer = 0;
                                }
                                else if (this.State == Atc.States.ServiceHalf)
                                {
                                    this.State = Atc.States.ServiceFull;
                                    this.ServiceBrakesTimer = 0;
                                }
                            }
                        }
                        else
                        {
                            if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
                            {
                                Atc serviceBrakesTimer2 = this;
                                serviceBrakesTimer2.ServiceBrakesTimer = serviceBrakesTimer2.ServiceBrakesTimer + data.ElapsedTime.Seconds;
                            }
                            if (this.ServiceBrakesTimer >= this.ServiceBrakesTimerMaximum && this.State != Atc.States.ServiceHalf)
                            {
                                this.State = Atc.States.ServiceHalf;
                                this.ServiceBrakesTimer = 0;
                            }
                        }
                    }
                    if (this.State == Atc.States.ServiceHalf)
                    {
                        double num3 = (this.Pattern.Signal.OverrunProtector ? this.OrpDeceleration : this.RegularDeceleration);
                        int atsNotch = (int)Math.Round((double)(this.Train.Specs.BrakeNotches - this.Train.Specs.AtsNotch + 1) * (num3 / this.MaximumDeceleration));
                        atsNotch = atsNotch + (this.Train.Specs.AtsNotch - 1);
                        if (atsNotch > this.Train.Specs.BrakeNotches)
                        {
                            atsNotch = this.Train.Specs.BrakeNotches;
                        }
                        if (data.Handles.BrakeNotch < atsNotch)
                        {
                            data.Handles.BrakeNotch = atsNotch;
                        }
                    }
                    else if (this.State == Atc.States.ServiceFull)
                    {
                        data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches;
                    }
                    else if (this.State == Atc.States.Emergency)
                    {
                        data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                    }
                    blocking = true;
                }
                /*
                 * Now handled in the Traction Manager
                if (this.State != Atc.States.Disabled & this.Train.Doors != DoorStates.None)
                {
                    data.Handles.PowerNotch = 0;
                }
                 */
            }
            else
            {
                if (this.State != Atc.States.Disabled & this.State != Atc.States.Suppressed)
                {
                    this.State = Atc.States.Ats;
                }
                this.Pattern.Signal = this.NoSignal;
                this.Pattern.Update(this);
                this.ServiceBrakesTimer = 0;
            }
            //Panel Indicators Start Here

            /*
             * Reset Panel Indicators
             */
            
            if (this.State == Atc.States.Ats)
            {
                this.Train.Panel[21] = 1;
                this.Train.Panel[271] = 12;
            }
            else if (this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency)
            {
                this.Train.Panel[15] = 1;
                this.Train.Panel[265] = 1;
                if (this.Pattern.Signal.Indicator != Atc.SignalIndicators.X)
                {
                    if (this.Pattern.Signal.Aspect == 100)
                    {
                        this.Train.Panel[24] = 1;
                    }
                    else if (this.Pattern.Signal.Aspect >= 101 & this.Pattern.Signal.Aspect <= 112)
                    {
                        this.Train.Panel[this.Pattern.Signal.Aspect - 79] = 1;
                    }
                    for (int i = 11; i >= 1; i--)
                    {
                        if (this.Pattern.Signal.FinalSpeed + 0.001 >= this.CompatibilitySpeeds[i])
                        {
                            this.Train.Panel[271] = i;
                            break;
                        }
                    }
                }
                else
                {
                    this.Train.Panel[22] = 1;
                    this.Train.Panel[271] = 0;
                }
                switch (this.Pattern.Signal.Indicator)
                {
                    case Atc.SignalIndicators.Green:
                        {
                            this.Train.Panel[111] = 1;
                            goto case Atc.SignalIndicators.P;
                        }
                    case Atc.SignalIndicators.Red:
                        {
                            this.Train.Panel[110] = 1;
                            goto case Atc.SignalIndicators.P;
                        }
                    case Atc.SignalIndicators.P:
                        {
                            if (this.Pattern.Signal.OverrunProtector)
                            {
                                this.Train.Panel[112] = 1;
                            }
                            if (this.Pattern.Signal.ZenpouYokoku)
                            {
                                this.Train.Panel[113] = 1;
                            }
                            this.Train.Panel[34] = (int)Math.Round(3600 * Math.Max(0, this.Pattern.CurrentSpeed));
                            if (!this.Pattern.Signal.OverrunProtector)
                            {
                                if (this.Pattern.Signal.Indicator == Atc.SignalIndicators.X)
                                {
                                    break;
                                }
                                int num5 = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * this.Pattern.CurrentSpeed + 0.001)), 59);
                                int num6 = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * this.Pattern.Signal.FinalSpeed + 0.001)), 59);
                                for (int i = num6; i <= num5; i++)
                                {
                                    this.Train.Panel[120 + i] = 1;
                                }
                            }
                            else
                            {
                                this.Train.Panel[114] = (int)Math.Round(3600 * Math.Max(0, this.Pattern.CurrentSpeed));
                                break;
                            }
                            break;
                        }
                    case Atc.SignalIndicators.X:
                        {
                            this.Train.Panel[22] = 1;
                            goto case Atc.SignalIndicators.P;
                        }
                    default:
                        {
                            goto case Atc.SignalIndicators.P;
                        }
                }
            }
            if (this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull)
            {
                this.Train.Panel[16] = 1;
                this.Train.Panel[267] = 1;
            }
            else if (this.State == Atc.States.Emergency)
            {
                this.Train.Panel[17] = 1;
                this.Train.Panel[268] = 1;
            }
            if (this.State != Atc.States.Disabled & this.State != Atc.States.Suppressed)
            {
                this.Train.Panel[18] = 1;
                this.Train.Panel[266] = 1;
            }
            if (this.EmergencyOperation)
            {
                this.Train.Panel[19] = 1;
                this.Train.Panel[52] = 1;
            }
            if (this.State == Atc.States.Disabled)
            {
                this.Train.Panel[20] = 1;
                this.Train.Panel[53] = 1;
            }
            if (this.ShouldSwitchToAts())
            {
                if (!(this.AutomaticSwitch & Math.Abs(data.Vehicle.Speed.MetersPerSecond) < 0.277777777777778))
                {
                    if (!SoundManager.IsPlaying(CommonSounds.ToATSReminder))
                    {
                        SoundManager.Play(CommonSounds.ToATSReminder, 1.0, 1.0, false);
                    }
                }
                else
                {
                    this.KeyDown(VirtualKeys.C1);
                }
            }
            else if (this.ShouldSwitchToAtc())
            {
                if (!(this.AutomaticSwitch & Math.Abs(data.Vehicle.Speed.MetersPerSecond) < 0.277777777777778))
                {
                    if (!SoundManager.IsPlaying(CommonSounds.ToATCReminder))
                    {
                        SoundManager.Play(CommonSounds.ToATCReminder, 1.0, 1.0, false);
                    }
                }
                else
                {
                    this.KeyDown(VirtualKeys.C2);
                }
            }
            if (this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency)
            {
                ElapseData elapseDatum = data;
                object[] objArray = new object[] { this.State.ToString(), " - A:", this.Pattern.Signal.Aspect, " I:", null, null, null, null, null, null, null, null, null };
                object[] objArray1 = objArray;
                if (this.Pattern.Signal.InitialSpeed < double.MaxValue)
                {
                    double initialSpeed = 3.6 * this.Pattern.Signal.InitialSpeed;
                    str = initialSpeed.ToString("0");
                }
                else
                {
                    str = "∞";
                }
                objArray1[4] = str;
                objArray[5] = " F:";
                double finalSpeed1 = 3.6 * this.Pattern.Signal.FinalSpeed;
                objArray[6] = finalSpeed1.ToString("0");
                objArray[7] = " D=";
                object[] objArray2 = objArray;
                if (this.Pattern.Signal.Distance == double.MaxValue)
                {
                    obj = "∞";
                }
                else
                {
                    double distance = this.Pattern.Signal.Distance - (this.Train.State.Location - this.BlockLocation);
                    obj = distance.ToString("0");
                }
                objArray2[8] = obj;
                objArray[9] = " T:";
                double topSpeed = 3.6 * this.Pattern.TopSpeed;
                objArray[10] = topSpeed.ToString("0");
                objArray[11] = " C:";
                double currentSpeed2 = 3.6 * this.Pattern.CurrentSpeed;
                objArray[12] = currentSpeed2.ToString("0");
                elapseDatum.DebugMessage = string.Concat(objArray);
            }
        }

        private double GetAtcSpeedFromLimit()
        {
            if (this.CompatibilityState == Atc.CompatibilityStates.Ats)
            {
                return -1;
            }
            if (this.CompatibilityLimits.Count == 0)
            {
                return double.MaxValue;
            }
            if (this.CompatibilityLimits.Count == 1)
            {
                return this.CompatibilityLimits[0].Limit;
            }
            while (this.CompatibilityLimitPointer > 0)
            {
                if (this.CompatibilityLimits[this.CompatibilityLimitPointer].Location > this.Train.State.Location)
                {
                    Atc compatibilityLimitPointer = this;
                    compatibilityLimitPointer.CompatibilityLimitPointer = compatibilityLimitPointer.CompatibilityLimitPointer - 1;
                }
                else
                {
                    break;
                }
            }
            while (this.CompatibilityLimitPointer < this.CompatibilityLimits.Count - 1 && this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Location <= this.Train.State.Location)
            {
                Atc atc = this;
                atc.CompatibilityLimitPointer = atc.CompatibilityLimitPointer + 1;
            }
            if (this.CompatibilityLimitPointer == this.CompatibilityLimits.Count - 1)
            {
                return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
            }
            if (this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit <= this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit)
            {
                return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
            }
            double limit = this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
            double num = this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit;
            if (this.Train.State.Location < this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Location - ((limit * limit - num * num) / (2 * this.RegularDeceleration) + this.RegularDelay * limit))
            {
                return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
            }
            return this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit;
        }

        private double GetAtcSpeedFromTrain()
        {
            if (this.CompatibilityState == Atc.CompatibilityStates.Ats)
            {
                return -1;
            }
            if (this.PrecedingTrain == null)
            {
                return this.CompatibilitySpeeds[11];
            }
            switch ((int)Math.Floor(this.PrecedingTrain.Location / 100) - (int)Math.Floor(this.Train.State.Location / 100))
            {
                case 0:
                    {
                        return this.CompatibilitySpeeds[0];
                    }
                case 1:
                    {
                        return this.CompatibilitySpeeds[1];
                    }
                case 2:
                    {
                        return this.CompatibilitySpeeds[3];
                    }
                case 3:
                    {
                        return this.CompatibilitySpeeds[4];
                    }
                case 4:
                    {
                        return this.CompatibilitySpeeds[5];
                    }
                case 5:
                    {
                        return this.CompatibilitySpeeds[6];
                    }
                case 6:
                    {
                        return this.CompatibilitySpeeds[7];
                    }
                case 7:
                    {
                        return this.CompatibilitySpeeds[8];
                    }
                case 8:
                    {
                        return this.CompatibilitySpeeds[9];
                    }
                case 9:
                    {
                        return this.CompatibilitySpeeds[10];
                    }
            }
            return this.CompatibilitySpeeds[11];
        }

        private Atc.Signal GetCurrentSignal()
        {
            Atc.Signal signal;
            if (this.EmergencyOperation)
            {
                return this.EmergencyOperationSignal;
            }
            List<Atc.Signal>.Enumerator enumerator = this.Signals.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    Atc.Signal current = enumerator.Current;
                    if (current.Aspect != this.Aspect)
                    {
                        continue;
                    }
                    signal = current;
                    return signal;
                }
                if (Math.Abs(this.CompatibilitySuppressLocation - this.Train.State.Location) > 50 && this.CompatibilityState != Atc.CompatibilityStates.Ats)
                {
                    double atcSpeedFromTrain = this.GetAtcSpeedFromTrain();
                    double num = Math.Min(atcSpeedFromTrain, this.GetAtcSpeedFromLimit());
                    if (num > 0)
                    {
                        if (this.CompatibilityState != Atc.CompatibilityStates.ToAts)
                        {
                            return new Atc.Signal(-1, Atc.SignalIndicators.Green, num);
                        }
                        return new Atc.Signal(-1, Atc.SignalIndicators.Red, num, 0, double.MaxValue, Atc.KirikaeStates.ToAts, false, false);
                    }
                    if (num == 0)
                    {
                        return new Atc.Signal(-1, Atc.SignalIndicators.Red, 0);
                    }
                }
                return this.NoSignal;
            }
            finally
            {
                ((IDisposable)enumerator).Dispose();
            }
            return signal;
        }

        private Atc.Signal GetUpcomingSignal()
        {
            Atc.Signal signal;
            if (this.EmergencyOperation)
            {
                return this.EmergencyOperationSignal;
            }
            List<Atc.Signal>.Enumerator enumerator = this.Signals.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    Atc.Signal current = enumerator.Current;
                    if (current.Aspect != this.RealTimeAdvanceWarningUpcomingSignalAspect)
                    {
                        continue;
                    }
                    signal = current;
                    return signal;
                }
                return this.NoSignal;
            }
            finally
            {
                ((IDisposable)enumerator).Dispose();
            }
            return signal;
        }

        internal override void HornBlow(HornTypes type)
        {
        }

        internal override void Initialize(InitializationModes mode)
        {
            if (mode == InitializationModes.OffEmergency)
            {
                this.State = Atc.States.Suppressed;
                return;
            }
            this.State = Atc.States.Ats;
        }

        internal override void KeyDown(VirtualKeys key)
        {
            switch (key)
            {
                case VirtualKeys.C1:
                    {
                        if (!(this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency))
                        {
                            return;
                        }
                        this.State = Atc.States.Ats;
                        if (!this.ShouldSwitchToAtc())
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ToATS))
                            {
                                SoundManager.Play(CommonSounds.ToATS, 1.0, 1.0, false);
                            }
                        }
                        if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
                        {
                            SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
                        }
                        return;
                    }
                case VirtualKeys.C2:
                    {
                        if (this.State != Atc.States.Ats)
                        {
                            return;
                        }
                        this.State = Atc.States.Normal;
                        if (!this.ShouldSwitchToAts())
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ToATC))
                            {
                                SoundManager.Play(CommonSounds.ToATC, 1.0, 1.0, false);
                            }

                        }
                        if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
                        {
                            SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
                        }

                        return;
                    }
                case VirtualKeys.D:
                case VirtualKeys.E:
                case VirtualKeys.F:
                    {
                        return;
                    }
                case VirtualKeys.G:
                    {
                        if (this.State == Atc.States.Disabled)
                        {
                            this.State = Atc.States.Suppressed;
                            return;
                        }
                        this.State = Atc.States.Disabled;
                        return;
                    }
                case VirtualKeys.H:
                    {
                        if (this.EmergencyOperationSignal == null)
                        {
                            return;
                        }
                        this.EmergencyOperation = !this.EmergencyOperation;
                        return;
                    }
                default:
                    {
                        return;
                    }
            }
        }

        internal override void KeyUp(VirtualKeys key)
        {
        }

        internal override void SetBeacon(BeaconData beacon)
        {
            int type = beacon.Type;
            switch (type)
            {
                case -16777215:
                    {
                        if (!(beacon.Optional >= 0 & beacon.Optional <= 3))
                        {
                            break;
                        }
                        this.CompatibilityState = (Atc.CompatibilityStates)beacon.Optional;
                        return;
                    }
                case -16777214:
                    {
                        double optional = (double)(beacon.Optional & 4095) / 3.6;
                        double num = (double)(beacon.Optional >> 12);
                        Atc.CompatibilityLimit compatibilityLimit = new Atc.CompatibilityLimit(optional, num);
                        if (this.CompatibilityLimits.Contains(compatibilityLimit))
                        {
                            break;
                        }
                        this.CompatibilityLimits.Add(compatibilityLimit);
                        break;
                    }
                default:
                    {
                        if (type != 31)
                        {
                            return;
                        }
                        if (!(beacon.Signal.Distance > 0 & beacon.Optional == 0))
                        {
                            break;
                        }
                        this.RealTimeAdvanceWarningReferenceLocation = this.Train.State.Location + beacon.Signal.Distance;
                        return;
                    }
            }
        }

        internal override void SetSignal(SignalData[] signal)
        {
            this.BlockLocation = this.Train.State.Location + signal[0].Distance;
            this.Aspect = signal[0].Aspect;
            if ((int)signal.Length < 2)
            {
                this.RealTimeAdvanceWarningUpcomingSignalAspect = -1;
                this.RealTimeAdvanceWarningUpcomingSignalLocation = double.MaxValue;
                return;
            }
            this.RealTimeAdvanceWarningUpcomingSignalAspect = signal[1].Aspect;
            this.RealTimeAdvanceWarningUpcomingSignalLocation = this.Train.State.Location + signal[1].Distance;
        }

        internal bool ShouldSwitchToAtc()
        {
            if (this.State == Atc.States.Ats && this.Pattern.Signal.Kirikae == Atc.KirikaeStates.ToAtc && Math.Abs(this.Train.State.Speed.MetersPerSecond) < 0.277777777777778)
            {
                return true;
            }
            return false;
        }

        internal bool ShouldSwitchToAts()
        {
            if (this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency && this.Pattern.Signal.Kirikae == Atc.KirikaeStates.ToAts && Math.Abs(this.Train.State.Speed.MetersPerSecond) < 0.277777777777778)
            {
                return true;
            }
            return false;
        }

        private struct CompatibilityLimit
        {
            internal double Limit;

            internal double Location;

            internal CompatibilityLimit(double limit, double location)
            {
                this.Limit = limit;
                this.Location = location;
            }
        }

        private enum CompatibilityStates
        {
            Ats,
            ToAtc,
            Atc,
            ToAts
        }

        internal enum KirikaeStates
        {
            Unchanged,
            ToAts,
            ToAtc
        }

        internal class Signal
        {
            internal int Aspect;

            internal Atc.SignalIndicators Indicator;

            internal double InitialSpeed;

            internal double FinalSpeed;

            internal double Distance;

            internal Atc.KirikaeStates Kirikae;

            internal bool ZenpouYokoku;

            internal bool OverrunProtector;

            internal Signal(int aspect, Atc.SignalIndicators indicator, double finalSpeed)
            {
                this.Aspect = aspect;
                this.Indicator = indicator;
                this.InitialSpeed = finalSpeed;
                this.FinalSpeed = finalSpeed;
                this.Distance = -1;
                this.Kirikae = Atc.KirikaeStates.ToAtc;
                this.ZenpouYokoku = false;
                this.OverrunProtector = false;
            }

            internal Signal(int aspect, Atc.SignalIndicators indicator, double initialSpeed, double finalSpeed, double distance)
            {
                this.Aspect = aspect;
                this.Indicator = indicator;
                this.InitialSpeed = initialSpeed;
                this.FinalSpeed = finalSpeed;
                this.Distance = distance;
                this.Kirikae = Atc.KirikaeStates.ToAtc;
                this.ZenpouYokoku = false;
                this.OverrunProtector = false;
            }

            internal Signal(int aspect, Atc.SignalIndicators indicator, double initialSpeed, double finalSpeed, double distance, Atc.KirikaeStates kirikae, bool zenpouYokoku, bool overrunProtector)
            {
                this.Aspect = aspect;
                this.Indicator = indicator;
                this.InitialSpeed = initialSpeed;
                this.FinalSpeed = finalSpeed;
                this.Distance = distance;
                this.Kirikae = kirikae;
                this.ZenpouYokoku = zenpouYokoku;
                this.OverrunProtector = overrunProtector;
            }

            internal static Atc.Signal CreateEmergencyOperationSignal(double limit)
            {
                return new Atc.Signal(-1, Atc.SignalIndicators.None, limit, limit, -1, Atc.KirikaeStates.Unchanged, false, false);
            }

            internal static Atc.Signal CreateNoSignal(int aspect)
            {
                return new Atc.Signal(aspect, Atc.SignalIndicators.X, -1, -1, -1, Atc.KirikaeStates.ToAts, false, false);
            }
        }

        internal enum SignalIndicators
        {
            None,
            Green,
            Red,
            P,
            X
        }

        internal class SignalPattern
        {
            internal Atc.Signal Signal;

            internal double TopSpeed;

            internal double CurrentSpeed;

            internal double ReleaseSpeed;

            internal SignalPattern(Atc.Signal signal, Atc atc)
            {
                this.Signal = signal;
                this.TopSpeed = signal.InitialSpeed;
                this.Update(atc);
            }

            internal static bool ApperanceEquals(Atc.SignalPattern oldPattern, Atc.SignalPattern newPattern)
            {
                if (oldPattern.Signal.Indicator != newPattern.Signal.Indicator)
                {
                    return false;
                }
                if (!oldPattern.Signal.ZenpouYokoku & newPattern.Signal.ZenpouYokoku)
                {
                    return false;
                }
                if (oldPattern.Signal.OverrunProtector != newPattern.Signal.OverrunProtector)
                {
                    return false;
                }
                if (!newPattern.Signal.OverrunProtector)
                {
                    int num = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * oldPattern.CurrentSpeed + 0.001)), 59);
                    int num1 = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * newPattern.CurrentSpeed + 0.001)), 59);
                    if (num < num1)
                    {
                        return false;
                    }
                    if (num > num1 + 1)
                    {
                        return false;
                    }
                }
                else
                {
                    if (newPattern.CurrentSpeed <= 0 & oldPattern.CurrentSpeed > 0)
                    {
                        return false;
                    }
                    if (Math.Abs(newPattern.CurrentSpeed - oldPattern.CurrentSpeed) > 1.66666666666667)
                    {
                        return false;
                    }
                }
                if (Math.Min(Math.Max(0, (int)Math.Floor(0.72 * oldPattern.Signal.FinalSpeed + 0.001)), 59) != Math.Min(Math.Max(0, (int)Math.Floor(0.72 * newPattern.Signal.FinalSpeed + 0.001)), 59))
                {
                    return false;
                }
                return true;
            }

            internal void Update(Atc atc)
            {
                double regularDeceleration;
                double regularDelay;
                if (this.Signal.Distance == double.MaxValue)
                {
                    this.CurrentSpeed = this.Signal.InitialSpeed;
                }
                else if (this.Signal.Distance <= 0)
                {
                    this.CurrentSpeed = this.Signal.FinalSpeed;
                }
                else
                {
                    double blockLocation = atc.BlockLocation + this.Signal.Distance - atc.Train.State.Location;
                    if (!this.Signal.OverrunProtector)
                    {
                        regularDeceleration = atc.RegularDeceleration;
                        regularDelay = atc.RegularDelay;
                    }
                    else
                    {
                        regularDeceleration = atc.OrpDeceleration;
                        regularDelay = atc.OrpDelay;
                    }
                    double finalSpeed = 2 * regularDeceleration * blockLocation + regularDeceleration * regularDeceleration * regularDelay * regularDelay + this.Signal.FinalSpeed * this.Signal.FinalSpeed;
                    if (finalSpeed <= 0)
                    {
                        this.CurrentSpeed = this.Signal.FinalSpeed;
                    }
                    else
                    {
                        this.CurrentSpeed = Math.Sqrt(finalSpeed) - regularDeceleration * regularDelay;
                        if (this.CurrentSpeed > this.Signal.InitialSpeed)
                        {
                            this.CurrentSpeed = this.Signal.InitialSpeed;
                        }
                        else if (this.CurrentSpeed < this.Signal.FinalSpeed)
                        {
                            this.CurrentSpeed = this.Signal.FinalSpeed;
                        }
                    }
                    if (blockLocation > 0 & this.CurrentSpeed < atc.OrpReleaseSpeed)
                    {
                        this.CurrentSpeed = atc.OrpReleaseSpeed;
                    }
                }
                if (this.CurrentSpeed > this.TopSpeed)
                {
                    this.CurrentSpeed = this.TopSpeed;
                }
                this.ReleaseSpeed = Math.Max(this.Signal.FinalSpeed - 0.277777777777778, this.CurrentSpeed - 0.277777777777778);
            }
        }

        internal enum States
        {
            Disabled,
            Suppressed,
            Ats,
            Normal,
            ServiceHalf,
            ServiceFull,
            Emergency
        }
    }
}