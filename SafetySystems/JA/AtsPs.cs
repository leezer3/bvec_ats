/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */

using OpenBveApi.Runtime;
using System;
using System.Text;

namespace Plugin
{
    internal class AtsPs : Device
    {
        private Train Train;

        internal AtsPs.States State;

        private AtsPs.FunctionSelector Selector;

        private int CompatibilitySelector;

        private double CompatibilityDistanceAccumulator;

        internal AtsPs.Pattern TrainPermanentPattern;

        private AtsPs.Pattern SignalAPattern;

        private AtsPs.Pattern SignalBPattern;

        private AtsPs.Pattern DivergencePattern;

        private AtsPs.Pattern TemporaryPattern;

        private AtsPs.Pattern CurvePattern;

        private AtsPs.Pattern SlopePattern;

        private AtsPs.Pattern IrekaePattern;

        private AtsPs.Pattern YuudouPattern;

        internal AtsPs.Pattern[] Patterns;

        internal AtsPs(Train train)
        {
            this.Train = train;
            this.State = AtsPs.States.Disabled;
            this.Selector = new AtsPs.FunctionSelector();
            this.CompatibilitySelector = 0;
            this.CompatibilityDistanceAccumulator = 0;
            this.SignalAPattern = new AtsPs.Pattern();
            this.SignalBPattern = new AtsPs.Pattern();
            this.DivergencePattern = new AtsPs.Pattern();
            this.TemporaryPattern = new AtsPs.Pattern();
            this.CurvePattern = new AtsPs.Pattern();
            this.SlopePattern = new AtsPs.Pattern();
            this.IrekaePattern = new AtsPs.Pattern();
            this.YuudouPattern = new AtsPs.Pattern();
            this.TrainPermanentPattern = new AtsPs.Pattern();
            AtsPs.Pattern[] signalAPattern = new AtsPs.Pattern[] { this.SignalAPattern, this.SignalBPattern, this.DivergencePattern, this.TemporaryPattern, this.CurvePattern, this.SlopePattern, this.IrekaePattern, this.YuudouPattern, this.TrainPermanentPattern };
            this.Patterns = signalAPattern;
        }

        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (this.State == AtsPs.States.Suppressed)
            {
                if (this.Train.AtsSx.State != AtsSx.States.Disabled & this.Train.AtsSx.State != AtsSx.States.Suppressed & this.Train.AtsSx.State != AtsSx.States.Initializing)
                {
                    this.State = AtsPs.States.Standby;
                }
            }
            else if (this.State != AtsPs.States.Disabled && this.Train.AtsSx.State == AtsSx.States.Disabled | this.Train.AtsSx.State == AtsSx.States.Suppressed | this.Train.AtsSx.State == AtsSx.States.Initializing)
            {
                this.State = AtsPs.States.Suppressed;
            }
            if (blocking)
            {
                if (this.State != AtsPs.States.Disabled & this.State != AtsPs.States.Suppressed)
                {
                    this.State = AtsPs.States.Standby;
                }
            }
            else if (this.State != AtsPs.States.Disabled & this.State != AtsPs.States.Suppressed)
            {
                this.Selector.Perform(this, data);
                if (this.CompatibilitySelector != 0)
                {
                    AtsPs compatibilityDistanceAccumulator = this;
                    compatibilityDistanceAccumulator.CompatibilityDistanceAccumulator = compatibilityDistanceAccumulator.CompatibilityDistanceAccumulator + this.Train.State.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
                    if (this.CompatibilityDistanceAccumulator > 27.7)
                    {
                        this.CompatibilitySelector = 0;
                    }
                }
                if (this.State != AtsPs.States.Standby)
                {
                    AtsPs.Pattern[] patterns = this.Patterns;
                    for (int i = 0; i < (int)patterns.Length; i++)
                    {
                        patterns[i].Perform(data);
                    }
                    double speedPattern = double.MaxValue;
                    bool flag = false;
                    AtsPs.Pattern[] patternArray = this.Patterns;
                    for (int j = 0; j < (int)patternArray.Length; j++)
                    {
                        AtsPs.Pattern pattern = patternArray[j];
                        if (pattern.SpeedPattern < speedPattern)
                        {
                            speedPattern = pattern.SpeedPattern;
                        }
                        if (pattern != this.TrainPermanentPattern & pattern.SpeedPattern != double.MaxValue)
                        {
                            flag = true;
                        }
                    }
                    if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= speedPattern)
                    {
                        if (this.State != AtsPs.States.Emergency)
                        {
                            this.State = AtsPs.States.Emergency;
                            if (!SoundManager.IsPlaying(CommonSounds.ATSPsChime))
                            {
                                SoundManager.Play(CommonSounds.ATSPsChime, 1.0, 1.0, false);
                            }
                        }
                    }
                    else if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= speedPattern - 2.77777777777778)
                    {
                        if (this.State == AtsPs.States.Normal)
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ATSPsPatternEstablishment))
                            {
                                SoundManager.Play(CommonSounds.ATSPsPatternEstablishment, 1.0, 1.0, false);
                            }
                        }
                        else if (this.State == AtsPs.States.Pattern)
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ATSPsChime))
                            {
                                SoundManager.Play(CommonSounds.ATSPsChime, 1.0, 1.0, false);
                            }
                        }
                        if (this.State == AtsPs.States.Normal | this.State == AtsPs.States.Pattern)
                        {
                            this.State = AtsPs.States.Approaching;
                        }
                    }
                    else if (flag)
                    {
                        if (this.State == AtsPs.States.Normal)
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ATSPsPatternEstablishment))
                            {
                                SoundManager.Play(CommonSounds.ATSPsPatternEstablishment, 1.0, 1.0, false);
                            }
                        }
                        else if (this.State == AtsPs.States.Approaching)
                        {
                            if (!SoundManager.IsPlaying(CommonSounds.ATSPsChime))
                            {
                                SoundManager.Play(CommonSounds.ATSPsChime, 1.0, 1.0, false);
                            }
                        }
                        if (this.State == AtsPs.States.Normal | this.State == AtsPs.States.Approaching)
                        {
                            this.State = AtsPs.States.Pattern;
                        }
                    }
                    else if (this.State == AtsPs.States.Pattern | this.State == AtsPs.States.Approaching)
                    {
                        this.State = AtsPs.States.Normal;
                        if (!SoundManager.IsPlaying(CommonSounds.ATSPsPatternRelease))
                        {
                            SoundManager.Play(CommonSounds.ATSPsPatternRelease, 1.0, 1.0, false);
                        }
                    }
                    if (this.State == AtsPs.States.Emergency)
                    {
                        if (!SoundManager.IsPlaying(CommonSounds.ATSBell))
                        {
                            SoundManager.Play(CommonSounds.ATSBell, 1.0, 1.0, false);
                        }
                        data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                    }
                }
            }
            if (!(this.State != AtsPs.States.Disabled & this.State != AtsPs.States.Suppressed & this.State != AtsPs.States.Standby))
            {
                this.Train.Panel[41] = 60;
            }
            else
            {
                double num = double.MaxValue;
                if (this.State == AtsPs.States.Normal | this.State == AtsPs.States.Pattern | this.State == AtsPs.States.Approaching | this.State == AtsPs.States.Emergency)
                {
                    AtsPs.Pattern[] patterns1 = this.Patterns;
                    for (int k = 0; k < (int)patterns1.Length; k++)
                    {
                        AtsPs.Pattern pattern1 = patterns1[k];
                        if (pattern1.SpeedPattern < num)
                        {
                            num = pattern1.SpeedPattern;
                        }
                    }
                }
                if (this.State == AtsPs.States.Pattern | this.State == AtsPs.States.Approaching | this.State == AtsPs.States.Emergency)
                {
                    this.Train.Panel[35] = 1;
                }
                if (this.State == AtsPs.States.Approaching)
                {
                    this.Train.Panel[36] = 1;
                }
                if (this.State == AtsPs.States.Emergency)
                {
                    this.Train.Panel[37] = 1;
                }
                if (!blocking)
                {
                    this.Train.Panel[40] = (int)Math.Min(Math.Floor(0.428571428571429 * Math.Abs(data.Vehicle.Speed.KilometersPerHour)), 60);
                    this.Train.Panel[41] = (int)Math.Min(Math.Floor(1.54285714285714 * num), 60);
                }
                else
                {
                    this.Train.Panel[41] = 60;
                }
            }
            if (this.State == AtsPs.States.Suppressed & (this.Train.AtsSx.State == AtsSx.States.Disabled | this.Train.AtsSx.State == AtsSx.States.Initializing))
            {
                this.Train.Panel[39] = 1;
            }
            if (this.State != AtsPs.States.Disabled & this.State != AtsPs.States.Suppressed)
            {
                this.Train.Panel[42] = 1;
            }
            if (this.State == AtsPs.States.Disabled)
            {
                this.Train.Panel[54] = 1;
            }
            if (this.SignalAPattern.ReleaseSpeed == 4.16666666666667)
            {
                this.Train.Panel[60] = 1;
                this.Train.Panel[61] = 1;
                this.Train.Panel[64] = 1;
            }
            else if (this.SignalAPattern.ReleaseSpeed == 2.77777777777778)
            {
                this.Train.Panel[60] = 1;
                this.Train.Panel[61] = 1;
            }
            else if (this.SignalAPattern.TargetSpeed != double.MaxValue)
            {
                this.Train.Panel[60] = 1;
            }
            if (this.SignalBPattern.ReleaseSpeed == 4.16666666666667)
            {
                this.Train.Panel[62] = 1;
                this.Train.Panel[63] = 1;
                this.Train.Panel[64] = 1;
            }
            else if (this.SignalBPattern.ReleaseSpeed == 2.77777777777778)
            {
                this.Train.Panel[62] = 1;
                this.Train.Panel[63] = 1;
            }
            else if (this.SignalBPattern.TargetSpeed != double.MaxValue)
            {
                this.Train.Panel[62] = 1;
            }
            if (this.DivergencePattern.TargetSpeed != double.MaxValue)
            {
                this.Train.Panel[65] = 1;
            }
            if (this.CurvePattern.TargetSpeed != double.MaxValue)
            {
                this.Train.Panel[66] = 1;
            }
            if (this.SlopePattern.TargetSpeed != double.MaxValue)
            {
                this.Train.Panel[67] = 1;
            }
            if (this.TemporaryPattern.TargetSpeed != double.MaxValue)
            {
                this.Train.Panel[68] = 1;
            }
            if (this.IrekaePattern.TargetSpeed != double.MaxValue)
            {
                this.Train.Panel[69] = 1;
            }
            if (this.YuudouPattern.TargetSpeed != double.MaxValue)
            {
                this.Train.Panel[70] = 1;
            }
            if (this.SignalAPattern.UpcomingGradient != 0)
            {
                this.Train.Panel[71] = 1;
            }
            if (this.SignalBPattern.UpcomingGradient != 0)
            {
                this.Train.Panel[72] = 1;
            }
            if (this.State == AtsPs.States.Normal | this.State == AtsPs.States.Pattern | this.State == AtsPs.States.Approaching | this.State == AtsPs.States.Emergency)
            {
                StringBuilder stringBuilder = new StringBuilder();
                this.SignalAPattern.AddToStringBuilder("A:", stringBuilder);
                this.SignalBPattern.AddToStringBuilder("B:", stringBuilder);
                this.DivergencePattern.AddToStringBuilder("分岐/D:", stringBuilder);
                this.TemporaryPattern.AddToStringBuilder("臨時/T:", stringBuilder);
                this.CurvePattern.AddToStringBuilder("曲線/C:", stringBuilder);
                this.SlopePattern.AddToStringBuilder("勾配/S:", stringBuilder);
                this.IrekaePattern.AddToStringBuilder("入替/I:", stringBuilder);
                this.YuudouPattern.AddToStringBuilder("誘導/Y:", stringBuilder);
                this.TrainPermanentPattern.AddToStringBuilder("P:", stringBuilder);
                if (stringBuilder.Length == 0)
                {
                    data.DebugMessage = this.State.ToString();
                    return;
                }
                data.DebugMessage = string.Concat(this.State.ToString(), " - ", stringBuilder.ToString());
            }
        }

        internal override void Initialize(InitializationModes mode)
        {
            if (this.Train.AtsSx.State == AtsSx.States.Disabled)
            {
                this.State = AtsPs.States.Disabled;
            }
            else if (mode != InitializationModes.OffEmergency)
            {
                this.State = AtsPs.States.Standby;
            }
            else
            {
                this.State = AtsPs.States.Suppressed;
            }
            this.Selector.Clear();
            AtsPs.Pattern[] patterns = this.Patterns;
            for (int i = 0; i < (int)patterns.Length; i++)
            {
                patterns[i].Clear();
            }
        }

        internal override void KeyDown(VirtualKeys key)
        {
            VirtualKeys virtualKey = key;
            if (virtualKey != VirtualKeys.B1)
            {
                if (virtualKey != VirtualKeys.F)
                {
                    return;
                }
                if (this.State == AtsPs.States.Disabled)
                {
                    this.State = AtsPs.States.Suppressed;
                    return;
                }
                this.State = AtsPs.States.Disabled;
            }
            else if (this.State == AtsPs.States.Emergency & this.Train.Handles.Reverser == 0 & this.Train.Handles.PowerNotch == 0 & this.Train.Handles.BrakeNotch == this.Train.Specs.BrakeNotches + 1)
            {
                this.State = AtsPs.States.Normal;
                this.Selector.Clear();
                if (this.SignalAPattern.SpeedPattern <= 0 & this.SignalAPattern.Distance <= 0)
                {
                    this.SignalAPattern.Clear();
                }
                if (this.SignalBPattern.SpeedPattern <= 0 & this.SignalBPattern.Distance <= 0)
                {
                    this.SignalBPattern.Clear();
                    return;
                }
            }
        }

        internal override void SetBeacon(BeaconData beacon)
        {
            if (this.State != AtsPs.States.Disabled)
            {
                switch (beacon.Type)
                {
                    case 0:
                        {
                            if (!(this.CompatibilitySelector == 90 & beacon.Optional < -1))
                            {
                                break;
                            }
                            this.SignalAPattern.SetGradient(0.001 * (double)beacon.Optional);
                            this.SignalBPattern.SetGradient(0.001 * (double)beacon.Optional);
                            this.CompatibilitySelector = 0;
                            this.SwitchToPs();
                            return;
                        }
                    case 1:
                        {
                            if (this.State == AtsPs.States.Standby)
                            {
                                break;
                            }
                            if (this.CompatibilitySelector == 95)
                            {
                                if (beacon.Signal.Aspect != 0)
                                {
                                    this.SignalBPattern.Clear();
                                }
                                else
                                {
                                    this.SignalBPattern.Set15Kmph();
                                }
                                this.CompatibilitySelector = 0;
                            }
                            else if (beacon.Signal.Aspect != 0)
                            {
                                this.SignalAPattern.Clear();
                            }
                            else
                            {
                                this.SignalAPattern.Set15Kmph();
                            }
                            this.IrekaePattern.Clear();
                            return;
                        }
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                        {
                            int frequencyFromBeacon = Train.GetFrequencyFromBeacon(beacon);
                            if (frequencyFromBeacon == 0)
                            {
                                break;
                            }
                            this.Selector.Add(frequencyFromBeacon);
                            break;
                        }
                    case 11:
                        {
                            if (this.CompatibilitySelector == 0)
                            {
                                if (beacon.Signal.Aspect != 0)
                                {
                                    this.SignalAPattern.Clear();
                                }
                                else
                                {
                                    this.SignalAPattern.SetPs1();
                                }
                                this.CompatibilitySelector = 0;
                            }
                            else if (this.CompatibilitySelector == 95)
                            {
                                if (!(beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10))
                                {
                                    this.SignalBPattern.Clear();
                                }
                                else
                                {
                                    this.SignalBPattern.SetPs1();
                                }
                                this.CompatibilitySelector = 0;
                            }
                            this.SwitchToPs();
                            return;
                        }
                    case 12:
                        {
                            if (this.CompatibilitySelector == 0)
                            {
                                this.CompatibilitySelector = 108;
                                this.CompatibilityDistanceAccumulator = 0;
                            }
                            else if (this.CompatibilitySelector == 95)
                            {
                                if (!(beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10))
                                {
                                    this.SignalBPattern.Clear();
                                    this.CompatibilitySelector = 0;
                                }
                                else
                                {
                                    this.CompatibilitySelector = 109;
                                    this.CompatibilityDistanceAccumulator = 0;
                                }
                            }
                            else if (this.CompatibilitySelector == 108)
                            {
                                if (beacon.Signal.Aspect != 0)
                                {
                                    this.SignalAPattern.Clear();
                                }
                                else
                                {
                                    this.SignalAPattern.SetPs2();
                                    this.CompatibilitySelector = 0;
                                }
                            }
                            else if (this.CompatibilitySelector == 109)
                            {
                                if (beacon.Signal.Aspect != 0)
                                {
                                    this.SignalBPattern.Clear();
                                }
                                else
                                {
                                    this.SignalBPattern.SetPs2();
                                    this.CompatibilitySelector = 0;
                                }
                            }
                            this.SwitchToPs();
                            return;
                        }
                    case 13:
                        {
                            if (beacon.Signal.Aspect != 0 & beacon.Signal.Aspect <= 100)
                            {
                                this.SignalAPattern.Clear();
                                this.SignalBPattern.Clear();
                            }
                            this.SwitchToPs();
                            return;
                        }
                    case 14:
                        {
                            if (!(beacon.Optional == 90 | beacon.Optional == 95 | beacon.Optional == 108))
                            {
                                break;
                            }
                            this.CompatibilitySelector = beacon.Optional;
                            this.CompatibilityDistanceAccumulator = 0;
                            this.SwitchToPs();
                            return;
                        }
                    case 15:
                        {
                            if (this.CompatibilitySelector != 90)
                            {
                                break;
                            }
                            if (beacon.Optional == 0)
                            {
                                this.DivergencePattern.Clear();
                            }
                            else if (beacon.Optional > 0)
                            {
                                this.DivergencePattern.SetUpcomingLimit((double)beacon.Optional / 3.6, true);
                            }
                            this.CompatibilitySelector = 0;
                            this.SwitchToPs();
                            return;
                        }
                    case 16:
                        {
                            if (this.CompatibilitySelector != 90)
                            {
                                break;
                            }
                            if (beacon.Optional == 0)
                            {
                                this.CurvePattern.Clear();
                            }
                            else if (beacon.Optional > 0)
                            {
                                this.CurvePattern.SetUpcomingLimit((double)beacon.Optional / 3.6, false);
                            }
                            this.CompatibilitySelector = 0;
                            this.SwitchToPs();
                            return;
                        }
                    case 17:
                        {
                            if (this.CompatibilitySelector != 90)
                            {
                                break;
                            }
                            if (beacon.Optional == 0)
                            {
                                this.SlopePattern.Clear();
                            }
                            else if (beacon.Optional > 0)
                            {
                                this.SlopePattern.SetUpcomingLimit((double)beacon.Optional / 3.6, false);
                            }
                            this.CompatibilitySelector = 0;
                            this.SwitchToPs();
                            return;
                        }
                    case 18:
                        {
                            if (this.CompatibilitySelector != 90)
                            {
                                break;
                            }
                            if (beacon.Optional == 0)
                            {
                                this.TemporaryPattern.Clear();
                            }
                            else if (beacon.Optional > 0)
                            {
                                this.TemporaryPattern.SetUpcomingLimit((double)beacon.Optional / 3.6, false);
                            }
                            this.CompatibilitySelector = 0;
                            this.SwitchToPs();
                            return;
                        }
                    case 19:
                        {
                            if (this.CompatibilitySelector != 108)
                            {
                                break;
                            }
                            if (beacon.Optional > 0)
                            {
                                this.IrekaePattern.SetImmediateLimit((double)beacon.Optional / 3.6);
                            }
                            this.CompatibilitySelector = 0;
                            this.SwitchToPs();
                            return;
                        }
                    case 20:
                        {
                            if (this.CompatibilitySelector != 108)
                            {
                                break;
                            }
                            if (beacon.Optional == 0)
                            {
                                this.YuudouPattern.SetImmediateLimit(6.94444444444444);
                            }
                            this.CompatibilitySelector = 0;
                            this.SwitchToPs();
                            return;
                        }
                    default:
                        {
                            goto case 10;
                        }
                }
            }
        }

        private void SwitchToPs()
        {
            if (this.State == AtsPs.States.Standby)
            {
                this.State = AtsPs.States.Normal;
                if (!SoundManager.IsPlaying(CommonSounds.ATSPsPatternEstablishment))
                {
                    SoundManager.Play(CommonSounds.ATSPsPatternEstablishment, 1.0, 1.0, false);
                }
            }
        }

        private void SwitchToSx()
        {
            if (this.State == AtsPs.States.Emergency)
            {
                this.State = AtsPs.States.Standby;
                this.Train.AtsSx.State = AtsSx.States.Emergency;
            }
            else if (this.State != AtsPs.States.Standby)
            {
                this.State = AtsPs.States.Standby;
                if (!SoundManager.IsPlaying(CommonSounds.ATSPsPatternRelease))
                {
                    SoundManager.Play(CommonSounds.ATSPsPatternRelease, 1.0, 1.0, false);
                }
            }
            this.Selector.Clear();
            AtsPs.Pattern[] patterns = this.Patterns;
            for (int i = 0; i < (int)patterns.Length; i++)
            {
                patterns[i].Clear();
            }
        }

        private class FunctionSelector
        {
            internal int FirstBeaconFrequency;

            internal int SecondBeaconFrequency;

            internal int ThirdBeaconFrequency;

            internal double DistanceAccumulator;

            internal FunctionSelector()
            {
                this.FirstBeaconFrequency = 0;
                this.SecondBeaconFrequency = 0;
                this.ThirdBeaconFrequency = 0;
                this.DistanceAccumulator = 0;
            }

            internal void Add(int frequency)
            {
                if (frequency != 0)
                {
                    if (this.FirstBeaconFrequency == 0)
                    {
                        this.FirstBeaconFrequency = frequency;
                        return;
                    }
                    if (this.SecondBeaconFrequency == 0)
                    {
                        this.SecondBeaconFrequency = frequency;
                        return;
                    }
                    if (this.ThirdBeaconFrequency == 0)
                    {
                        this.ThirdBeaconFrequency = frequency;
                    }
                }
            }

            internal void Clear()
            {
                this.FirstBeaconFrequency = 0;
                this.SecondBeaconFrequency = 0;
                this.ThirdBeaconFrequency = 0;
                this.DistanceAccumulator = 0;
            }

            internal void Perform(AtsPs system, ElapseData data)
            {
                double num;
                double num1;
                double num2;
                double num3;
                double num4;
                double num5;
                if (this.FirstBeaconFrequency != 0)
                {
                    if (this.FirstBeaconFrequency == 73)
                    {
                        system.SwitchToPs();
                        this.Clear();
                    }
                    else if (this.FirstBeaconFrequency == 80)
                    {
                        system.SignalAPattern.SetPs1();
                        system.SwitchToPs();
                        this.Clear();
                    }
                    else if (this.FirstBeaconFrequency == 85)
                    {
                        if (this.SecondBeaconFrequency != 0)
                        {
                            if (this.SecondBeaconFrequency != 108)
                            {
                                this.Clear();
                            }
                            else if (this.ThirdBeaconFrequency != 0)
                            {
                                this.Clear();
                            }
                        }
                    }
                    else if (this.FirstBeaconFrequency == 90)
                    {
                        if (this.SecondBeaconFrequency != 0)
                        {
                            if (this.SecondBeaconFrequency == 80)
                            {
                                if (this.DistanceAccumulator > 1 & this.DistanceAccumulator < 2.7)
                                {
                                    num = double.MaxValue;
                                }
                                else if (this.DistanceAccumulator > 3.3 & this.DistanceAccumulator < 4.8)
                                {
                                    num = 26.3888888888889;
                                }
                                else if (this.DistanceAccumulator > 5.7 & this.DistanceAccumulator < 7.2)
                                {
                                    num = 23.6111111111111;
                                }
                                else if (this.DistanceAccumulator > 8.3 & this.DistanceAccumulator < 9.8)
                                {
                                    num = 20.8333333333333;
                                }
                                else if (this.DistanceAccumulator > 11.5 & this.DistanceAccumulator < 13)
                                {
                                    num = 18.0555555555556;
                                }
                                else if (this.DistanceAccumulator > 14.4 & this.DistanceAccumulator < 15.9)
                                {
                                    num = 15.2777777777778;
                                }
                                else if (!(this.DistanceAccumulator > 17.9 & this.DistanceAccumulator < 19.4))
                                {
                                    num = (!(this.DistanceAccumulator > 21.8 & this.DistanceAccumulator < 23.3) ? 0 : 9.72222222222222);
                                }
                                else
                                {
                                    num = 12.5;
                                }
                                if (num == 0)
                                {
                                    system.State = AtsPs.States.Emergency;
                                }
                                else
                                {
                                    system.SlopePattern.SetUpcomingLimit(num + 2.77777777777778, false);
                                }
                                system.SwitchToPs();
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 85)
                            {
                                if (this.DistanceAccumulator > 1 & this.DistanceAccumulator < 2.7)
                                {
                                    num1 = double.MaxValue;
                                }
                                else if (this.DistanceAccumulator > 3.3 & this.DistanceAccumulator < 4.8)
                                {
                                    num1 = 27.7777777777778;
                                }
                                else if (this.DistanceAccumulator > 5.7 & this.DistanceAccumulator < 7.2)
                                {
                                    num1 = 25;
                                }
                                else if (this.DistanceAccumulator > 8.3 & this.DistanceAccumulator < 9.8)
                                {
                                    num1 = 22.2222222222222;
                                }
                                else if (this.DistanceAccumulator > 11.5 & this.DistanceAccumulator < 13)
                                {
                                    num1 = 19.4444444444444;
                                }
                                else if (this.DistanceAccumulator > 14.4 & this.DistanceAccumulator < 15.9)
                                {
                                    num1 = 16.6666666666667;
                                }
                                else if (!(this.DistanceAccumulator > 17.9 & this.DistanceAccumulator < 19.4))
                                {
                                    num1 = (!(this.DistanceAccumulator > 21.8 & this.DistanceAccumulator < 23.3) ? 0 : 11.1111111111111);
                                }
                                else
                                {
                                    num1 = 13.8888888888889;
                                }
                                if (num1 == 0)
                                {
                                    system.State = AtsPs.States.Emergency;
                                }
                                else
                                {
                                    system.CurvePattern.SetUpcomingLimit(num1 + 2.77777777777778, false);
                                }
                                system.SwitchToPs();
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 90)
                            {
                                if (this.DistanceAccumulator > 1 & this.DistanceAccumulator < 2.7)
                                {
                                    num2 = double.MaxValue;
                                }
                                else if (this.DistanceAccumulator > 3.3 & this.DistanceAccumulator < 4.8)
                                {
                                    num2 = 15.2777777777778;
                                }
                                else if (this.DistanceAccumulator > 5.7 & this.DistanceAccumulator < 7.2)
                                {
                                    num2 = 13.8888888888889;
                                }
                                else if (this.DistanceAccumulator > 8.3 & this.DistanceAccumulator < 9.8)
                                {
                                    num2 = 12.5;
                                }
                                else if (this.DistanceAccumulator > 11.5 & this.DistanceAccumulator < 13)
                                {
                                    num2 = 11.1111111111111;
                                }
                                else if (this.DistanceAccumulator > 14.4 & this.DistanceAccumulator < 15.9)
                                {
                                    num2 = 9.72222222222222;
                                }
                                else if (!(this.DistanceAccumulator > 17.9 & this.DistanceAccumulator < 19.4))
                                {
                                    num2 = (!(this.DistanceAccumulator > 21.8 & this.DistanceAccumulator < 23.3) ? 0 : 6.94444444444444);
                                }
                                else
                                {
                                    num2 = 8.33333333333333;
                                }
                                if (num2 == 0)
                                {
                                    system.State = AtsPs.States.Emergency;
                                }
                                else
                                {
                                    system.TemporaryPattern.SetUpcomingLimit(num2 + 2.77777777777778, false);
                                }
                                system.SwitchToPs();
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 95)
                            {
                                if (this.DistanceAccumulator > 1 & this.DistanceAccumulator < 2.7)
                                {
                                    num3 = double.MaxValue;
                                }
                                else if (this.DistanceAccumulator > 3.3 & this.DistanceAccumulator < 4.8)
                                {
                                    num3 = 16.6666666666667;
                                }
                                else if (this.DistanceAccumulator > 5.7 & this.DistanceAccumulator < 7.2)
                                {
                                    num3 = 15.2777777777778;
                                }
                                else if (this.DistanceAccumulator > 8.3 & this.DistanceAccumulator < 9.8)
                                {
                                    num3 = 13.8888888888889;
                                }
                                else if (this.DistanceAccumulator > 11.5 & this.DistanceAccumulator < 13)
                                {
                                    num3 = 12.5;
                                }
                                else if (this.DistanceAccumulator > 14.4 & this.DistanceAccumulator < 15.9)
                                {
                                    num3 = 11.1111111111111;
                                }
                                else if (!(this.DistanceAccumulator > 17.9 & this.DistanceAccumulator < 19.4))
                                {
                                    num3 = (!(this.DistanceAccumulator > 21.8 & this.DistanceAccumulator < 23.3) ? 0 : 6.94444444444444);
                                }
                                else
                                {
                                    num3 = 9.72222222222222;
                                }
                                if (num3 == 0)
                                {
                                    system.State = AtsPs.States.Emergency;
                                }
                                else
                                {
                                    system.DivergencePattern.SetUpcomingLimit(num3 + 2.77777777777778, true);
                                }
                                system.SwitchToPs();
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency != 108)
                            {
                                this.Clear();
                            }
                            else
                            {
                                system.SwitchToSx();
                                this.Clear();
                            }
                        }
                    }
                    else if (this.FirstBeaconFrequency == 95)
                    {
                        if (this.SecondBeaconFrequency != 0)
                        {
                            if (this.SecondBeaconFrequency == 80)
                            {
                                system.SignalBPattern.SetPs1();
                                system.SwitchToPs();
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 103)
                            {
                                system.SignalBPattern.Clear();
                                system.SwitchToPs();
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 108)
                            {
                                if (this.ThirdBeaconFrequency != 0)
                                {
                                    if (this.ThirdBeaconFrequency == 103)
                                    {
                                        system.SignalBPattern.Clear();
                                        system.SwitchToPs();
                                        this.Clear();
                                    }
                                    else if (this.ThirdBeaconFrequency != 108)
                                    {
                                        this.Clear();
                                    }
                                    else
                                    {
                                        system.SignalBPattern.SetPs2();
                                        system.SwitchToPs();
                                        this.Clear();
                                    }
                                }
                            }
                            else if (this.SecondBeaconFrequency == 123)
                            {
                                system.SignalBPattern.Set15Kmph();
                                system.SwitchToPs();
                                this.Clear();
                            }
                            else if (!(this.SecondBeaconFrequency == 129 | this.SecondBeaconFrequency == 130))
                            {
                                this.Clear();
                            }
                            else
                            {
                                if (this.DistanceAccumulator >= 1 & this.DistanceAccumulator <= 2.7)
                                {
                                    num4 = -0.015;
                                }
                                else if (!(this.DistanceAccumulator >= 3.3 & this.DistanceAccumulator <= 4.8))
                                {
                                    num4 = (!(this.DistanceAccumulator >= 5.7 & this.DistanceAccumulator <= 7.2) ? 0 : -0.035);
                                }
                                else
                                {
                                    num4 = -0.025;
                                }
                                system.SignalBPattern.SetGradient(num4);
                                system.SwitchToPs();
                                this.Clear();
                            }
                        }
                    }
                    else if (this.FirstBeaconFrequency == 103)
                    {
                        if (system.State != AtsPs.States.Standby)
                        {
                            system.SignalAPattern.Clear();
                            system.IrekaePattern.Clear();
                        }
                        this.Clear();
                    }
                    else if (this.FirstBeaconFrequency == 108)
                    {
                        if (this.SecondBeaconFrequency != 0)
                        {
                            if (this.SecondBeaconFrequency == 80)
                            {
                                system.SignalAPattern.SetPs3();
                                system.SwitchToPs();
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 85)
                            {
                                if (this.DistanceAccumulator >= 1 & this.DistanceAccumulator <= 2.7)
                                {
                                    system.YuudouPattern.SetImmediateLimit(6.94444444444444);
                                    system.SwitchToPs();
                                }
                                else if (this.DistanceAccumulator >= 3.3 & this.DistanceAccumulator <= 4.8)
                                {
                                    system.YuudouPattern.Clear();
                                    system.SwitchToPs();
                                }
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 90 | this.SecondBeaconFrequency == 95)
                            {
                                if (this.DistanceAccumulator >= 1 & this.DistanceAccumulator <= 2.7)
                                {
                                    system.IrekaePattern.SetImmediateLimit(8.33333333333333);
                                    system.SwitchToPs();
                                }
                                else if (this.DistanceAccumulator >= 3.3 & this.DistanceAccumulator <= 4.8)
                                {
                                    system.IrekaePattern.SetImmediateLimit(13.8888888888889);
                                    system.SwitchToPs();
                                }
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 103)
                            {
                                if (system.State != AtsPs.States.Standby)
                                {
                                    system.SignalAPattern.Clear();
                                }
                                this.Clear();
                            }
                            else if (this.SecondBeaconFrequency == 108)
                            {
                                if (system.State != AtsPs.States.Standby)
                                {
                                    system.SignalAPattern.SetPs2();
                                }
                                this.Clear();
                            }
                            else if (!(this.SecondBeaconFrequency == 129 | this.SecondBeaconFrequency == 130))
                            {
                                this.Clear();
                            }
                            else
                            {
                                if (system.State != AtsPs.States.Standby)
                                {
                                    if (this.DistanceAccumulator >= 1 & this.DistanceAccumulator <= 2.7)
                                    {
                                        num5 = -0.015;
                                    }
                                    else if (!(this.DistanceAccumulator >= 3.3 & this.DistanceAccumulator <= 4.8))
                                    {
                                        num5 = (!(this.DistanceAccumulator >= 5.7 & this.DistanceAccumulator <= 7.2) ? 0 : -0.035);
                                    }
                                    else
                                    {
                                        num5 = -0.025;
                                    }
                                    system.SignalAPattern.SetGradient(num5);
                                }
                                this.Clear();
                            }
                        }
                    }
                    else if (this.FirstBeaconFrequency != 123)
                    {
                        this.Clear();
                    }
                    else
                    {
                        if (system.State != AtsPs.States.Standby)
                        {
                            system.SignalAPattern.Set15Kmph();
                            system.IrekaePattern.Clear();
                        }
                        this.Clear();
                    }
                }
                if (this.FirstBeaconFrequency != 0)
                {
                    AtsPs.FunctionSelector distanceAccumulator = this;
                    distanceAccumulator.DistanceAccumulator = distanceAccumulator.DistanceAccumulator + data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
                    if (this.DistanceAccumulator > 25)
                    {
                        this.Clear();
                    }
                }
            }
        }

        internal class Pattern
        {
            internal double Distance;

            internal double TargetSpeed;

            internal double ReleaseSpeed;

            internal double ReleaseDistance;

            internal double SpeedPattern;

            internal double CurrentGradient;

            internal double UpcomingGradient;

            internal bool Persistent;

            internal Pattern()
            {
                this.Distance = double.MaxValue;
                this.TargetSpeed = double.MaxValue;
                this.ReleaseSpeed = 0;
                this.ReleaseDistance = double.MaxValue;
                this.SpeedPattern = double.MaxValue;
                this.CurrentGradient = 0;
                this.UpcomingGradient = 0;
            }

            internal void AddToStringBuilder(string prefix, StringBuilder builder)
            {
                if (this.Distance <= 0)
                {
                    double speedPattern = 3.6 * this.SpeedPattern;
                    string str = string.Concat(prefix, speedPattern.ToString("0"));
                    if (builder.Length != 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(str);
                    return;
                }
                if (this.Distance < double.MaxValue)
                {
                    string[] strArrays = new string[] { prefix, null, null, null, null, null };
                    double targetSpeed = 3.6 * this.TargetSpeed;
                    strArrays[1] = targetSpeed.ToString("0");
                    strArrays[2] = "(";
                    double num = 3.6 * this.SpeedPattern;
                    strArrays[3] = num.ToString("0");
                    strArrays[4] = ")@";
                    strArrays[5] = this.Distance.ToString("0");
                    string str1 = string.Concat(strArrays);
                    if (builder.Length != 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(str1);
                }
            }

            internal void Clear()
            {
                if (!this.Persistent)
                {
                    this.Distance = double.MaxValue;
                    this.TargetSpeed = double.MaxValue;
                    this.ReleaseSpeed = 0;
                    this.ReleaseDistance = 0;
                    this.SpeedPattern = double.MaxValue;
                    this.CurrentGradient = 0;
                    this.UpcomingGradient = 0;
                }
            }

            private double GetPatternSpeed(double distance)
            {
                if (distance == double.MaxValue)
                {
                    return double.MaxValue;
                }
                if (distance <= 0)
                {
                    return this.TargetSpeed;
                }
                double currentGradient = 1.11111111111111 + 9.81 * this.CurrentGradient;
                double num = 2 * currentGradient * (this.Distance - 20) + currentGradient * currentGradient * 2 * 2 + this.TargetSpeed * this.TargetSpeed;
                if (num <= 0)
                {
                    return this.TargetSpeed;
                }
                double num1 = Math.Sqrt(num) - currentGradient * 2;
                if (num1 >= this.TargetSpeed)
                {
                    return num1;
                }
                return this.TargetSpeed;
            }

            internal void Perform(ElapseData data)
            {
                if (this.Distance == double.MaxValue)
                {
                    this.SpeedPattern = double.MaxValue;
                    return;
                }
                if (this.TargetSpeed == double.MaxValue)
                {
                    this.SpeedPattern = double.MaxValue;
                    return;
                }
                double metersPerSecond = data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
                if (this.ReleaseDistance != double.MaxValue)
                {
                    AtsPs.Pattern releaseDistance = this;
                    releaseDistance.ReleaseDistance = releaseDistance.ReleaseDistance - metersPerSecond;
                }
                if (this.ReleaseDistance <= 0)
                {
                    this.Clear();
                    return;
                }
                if (this.Distance != double.MinValue & this.Distance != double.MaxValue)
                {
                    AtsPs.Pattern distance = this;
                    distance.Distance = distance.Distance - metersPerSecond;
                }
                this.SpeedPattern = this.GetPatternSpeed(this.Distance);
                if (this.SpeedPattern < this.ReleaseSpeed)
                {
                    this.SpeedPattern = this.ReleaseSpeed;
                }
            }

            internal void Set15Kmph()
            {
                this.ReleaseDistance = 80;
                this.Distance = 0;
                this.TargetSpeed = 0;
                this.ReleaseSpeed = 4.16666666666667;
            }

            internal void SetGradient(double gradient)
            {
                this.UpcomingGradient = gradient;
            }

            internal void SetImmediateLimit(double speed)
            {
                if (speed == double.MaxValue)
                {
                    this.Clear();
                    return;
                }
                this.Distance = double.MinValue;
                this.TargetSpeed = speed;
                this.ReleaseDistance = double.MaxValue;
                this.ReleaseSpeed = this.TargetSpeed;
            }

            internal void SetPersistentLimit(double speed)
            {
                if (speed == double.MaxValue)
                {
                    this.Clear();
                    return;
                }
                this.Distance = double.MinValue;
                this.TargetSpeed = speed;
                this.ReleaseSpeed = 0;
                this.ReleaseDistance = double.MaxValue;
                this.Persistent = true;
            }

            internal void SetPs1()
            {
                if (this.UpcomingGradient < -0.025)
                {
                    this.ReleaseDistance = 1350;
                }
                else if (this.UpcomingGradient < -0.015)
                {
                    this.ReleaseDistance = 970;
                }
                else if (this.UpcomingGradient >= -0.005)
                {
                    this.ReleaseDistance = 655;
                }
                else
                {
                    this.ReleaseDistance = 775;
                }
                this.Distance = this.ReleaseDistance;
                this.TargetSpeed = 0;
                this.ReleaseSpeed = 18.0555555555556;
                this.CurrentGradient = this.UpcomingGradient;
            }

            internal void SetPs2()
            {
                if (this.UpcomingGradient < -0.025)
                {
                    this.ReleaseDistance = 765;
                }
                else if (this.UpcomingGradient < -0.015)
                {
                    this.ReleaseDistance = 560;
                }
                else if (this.UpcomingGradient >= -0.005)
                {
                    this.ReleaseDistance = 390;
                }
                else
                {
                    this.ReleaseDistance = 455;
                }
                this.Distance = this.ReleaseDistance;
                this.TargetSpeed = 0;
                this.ReleaseSpeed = 2.77777777777778;
                this.CurrentGradient = this.UpcomingGradient;
            }

            internal void SetPs3()
            {
                this.ReleaseDistance = 100;
                this.Distance = this.ReleaseDistance;
                this.TargetSpeed = 0;
                this.ReleaseSpeed = 2.77777777777778;
                this.CurrentGradient = 0;
            }

            internal void SetUpcomingLimit(double speed, bool clear)
            {
                if (speed == double.MaxValue)
                {
                    this.Clear();
                    return;
                }
                this.Distance = 555;
                this.TargetSpeed = speed;
                this.ReleaseDistance = (clear ? this.Distance + 50 : double.MaxValue);
                this.ReleaseSpeed = this.TargetSpeed;
            }
        }

        internal enum States
        {
            Disabled,
            Suppressed,
            Standby,
            Normal,
            Pattern,
            Approaching,
            Emergency
        }
    }
}