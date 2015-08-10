/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */

using OpenBveApi.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin
{
    internal class AtsP : Device
    {
        private Train Train;

        internal AtsP.States State;

        private bool Blocked;

        private bool AtsSxPMode;

        private bool BrakeRelease;

        private double BrakeReleaseCountdown;

        private double InitializationCountdown;

        private double Position;

        private double SwitchToAtsSxPosition;

        private List<AtsP.CompatibilityLimit> CompatibilityLimits;

        private int CompatibilityLimitPointer;

        internal bool DAtsPSupported;

        private bool DAtsPActive;

        private bool DAtsPContinuous;

        private int DAtsPAspect;

        private AtsP.Pattern DAtsPZerothSignalPattern;

        private AtsP.Pattern DAtsPFirstSignalPattern;

        private AtsP.Pattern DAtsPSecondSignalPattern;

        private AtsP.Pattern[] SignalPatterns;

        private AtsP.Pattern DivergencePattern;

        private AtsP.Pattern DownslopePattern;

        private AtsP.Pattern CurvePattern;

        private AtsP.Pattern TemporaryPattern;

        private AtsP.Pattern RoutePermanentPattern;

        internal AtsP.Pattern TrainPermanentPattern;

        private AtsP.Pattern CompatibilityTemporaryPattern;

        private AtsP.Pattern CompatibilityPermanentPattern;

        private AtsP.Pattern[] Patterns;

        internal double DurationOfInitialization = 3;

        internal double DurationOfBrakeRelease = 60;

        internal double DesignDeceleration = 0.679166666666667;

        internal double BrakePatternDelay = 0.5;

        internal double BrakePatternOffset;

        internal double BrakePatternTolerance;

        internal double WarningPatternDelay = 5.5;

        internal double WarningPatternOffset = 50;

        internal double WarningPatternTolerance = -1.38888888888889;

        internal double ReleaseSpeed = 4.16666666666667;

        internal AtsP(Train train)
        {
            this.Train = train;
            this.State = AtsP.States.Disabled;
            this.AtsSxPMode = false;
            this.InitializationCountdown = 0;
            this.SwitchToAtsSxPosition = double.MaxValue;
            this.CompatibilityLimits = new List<AtsP.CompatibilityLimit>();
            this.CompatibilityLimitPointer = 0;
            this.DAtsPSupported = false;
            this.DAtsPActive = false;
            this.DAtsPContinuous = false;
            this.SignalPatterns = new AtsP.Pattern[10];
            for (int i = 0; i < (int)this.SignalPatterns.Length; i++)
            {
                this.SignalPatterns[i] = new AtsP.Pattern(this);
            }
            this.DivergencePattern = new AtsP.Pattern(this);
            this.DownslopePattern = new AtsP.Pattern(this);
            this.CurvePattern = new AtsP.Pattern(this);
            this.TemporaryPattern = new AtsP.Pattern(this);
            this.RoutePermanentPattern = new AtsP.Pattern(this);
            this.TrainPermanentPattern = new AtsP.Pattern(this);
            this.CompatibilityTemporaryPattern = new AtsP.Pattern(this);
            this.CompatibilityPermanentPattern = new AtsP.Pattern(this);
            this.DAtsPZerothSignalPattern = new AtsP.Pattern(this);
            this.DAtsPFirstSignalPattern = new AtsP.Pattern(this);
            this.DAtsPSecondSignalPattern = new AtsP.Pattern(this);
            List<AtsP.Pattern> patterns = new List<AtsP.Pattern>();
            patterns.AddRange(this.SignalPatterns);
            patterns.Add(this.DivergencePattern);
            patterns.Add(this.DownslopePattern);
            patterns.Add(this.CurvePattern);
            patterns.Add(this.TemporaryPattern);
            patterns.Add(this.RoutePermanentPattern);
            patterns.Add(this.TrainPermanentPattern);
            patterns.Add(this.CompatibilityTemporaryPattern);
            patterns.Add(this.CompatibilityPermanentPattern);
            patterns.Add(this.DAtsPZerothSignalPattern);
            patterns.Add(this.DAtsPFirstSignalPattern);
            patterns.Add(this.DAtsPSecondSignalPattern);
            this.Patterns = patterns.ToArray();
        }

        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            this.Blocked = blocking;
            if (this.State == AtsP.States.Suppressed && data.Handles.BrakeNotch <= this.Train.Specs.BrakeNotches)
            {
                this.InitializationCountdown = this.DurationOfInitialization;
                this.State = AtsP.States.Initializing;
            }
            if (this.State == AtsP.States.Initializing)
            {
                AtsP initializationCountdown = this;
                initializationCountdown.InitializationCountdown = initializationCountdown.InitializationCountdown - data.ElapsedTime.Seconds;
                if (this.InitializationCountdown <= 0)
                {
                    this.State = AtsP.States.Standby;
                    this.BrakeRelease = false;
                    this.SwitchToAtsSxPosition = double.MaxValue;
                    AtsP.Pattern[] patterns = this.Patterns;
                    for (int i = 0; i < (int)patterns.Length; i++)
                    {
                        AtsP.Pattern pattern = patterns[i];
                        if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= pattern.WarningPattern)
                        {
                            pattern.Clear();
                        }
                    }
                    if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                    {
                        SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                    }
                }
            }
            if (this.BrakeRelease)
            {
                AtsP brakeReleaseCountdown = this;
                brakeReleaseCountdown.BrakeReleaseCountdown = brakeReleaseCountdown.BrakeReleaseCountdown - data.ElapsedTime.Seconds;
                if (this.BrakeReleaseCountdown <= 0)
                {
                    this.BrakeRelease = false;
                    if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                    {
                        SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                    }
                }
            }
            if (this.State != AtsP.States.Disabled & this.State != AtsP.States.Initializing)
            {
                AtsP position = this;
                position.Position = position.Position + data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
            }
            if (!blocking)
            {
                if (this.DAtsPSupported && this.DAtsPFirstSignalPattern.Position - this.Train.State.Location < 0)
                {
                    this.DAtsPZerothSignalPattern.Position = this.DAtsPFirstSignalPattern.Position;
                    this.DAtsPZerothSignalPattern.TargetSpeed = this.DAtsPFirstSignalPattern.TargetSpeed;
                    this.DAtsPFirstSignalPattern.Position = this.DAtsPSecondSignalPattern.Position;
                    this.DAtsPFirstSignalPattern.TargetSpeed = this.DAtsPSecondSignalPattern.TargetSpeed;
                    this.DAtsPSecondSignalPattern.Position = double.MaxValue;
                    this.DAtsPSecondSignalPattern.TargetSpeed = double.MaxValue;
                }
                if (this.DAtsPActive & this.DAtsPContinuous)
                {
                    switch (this.DAtsPAspect)
                    {
                        case 1:
                            {
                                this.DAtsPFirstSignalPattern.TargetSpeed = 6.94444444444444;
                                break;
                            }
                        case 2:
                            {
                                this.DAtsPFirstSignalPattern.TargetSpeed = 12.5;
                                break;
                            }
                        case 3:
                            {
                                this.DAtsPFirstSignalPattern.TargetSpeed = 20.8333333333333;
                                break;
                            }
                        case 4:
                        case 5:
                        case 6:
                            {
                                this.DAtsPFirstSignalPattern.TargetSpeed = double.MaxValue;
                                break;
                            }
                        default:
                            {
                                this.DAtsPFirstSignalPattern.TargetSpeed = 0;
                                break;
                            }
                    }
                    if (this.DAtsPZerothSignalPattern.TargetSpeed < this.DAtsPFirstSignalPattern.TargetSpeed)
                    {
                        this.DAtsPZerothSignalPattern.TargetSpeed = this.DAtsPFirstSignalPattern.TargetSpeed;
                    }
                }
                if (this.State == AtsP.States.Normal | this.State == AtsP.States.Pattern | this.State == AtsP.States.Brake)
                {
                    bool flag = false;
                    bool flag1 = false;
                    bool flag2 = true;
                    if (this.DivergencePattern.Position > double.MinValue & this.DivergencePattern.Position < double.MaxValue && Math.Abs(data.Vehicle.Speed.MetersPerSecond) < this.DivergencePattern.BrakePattern && this.DivergencePattern.Position - this.Position < -50)
                    {
                        this.DivergencePattern.Clear();
                    }
                    this.UpdateCompatibilityTemporarySpeedPattern();
                    AtsP.Pattern[] patternArray = this.Patterns;
                    for (int j = 0; j < (int)patternArray.Length; j++)
                    {
                        AtsP.Pattern pattern1 = patternArray[j];
                        pattern1.Perform(this, data);
                        if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= pattern1.WarningPattern - 0.277777777777778)
                        {
                            flag2 = false;
                        }
                        if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= pattern1.WarningPattern)
                        {
                            flag1 = true;
                        }
                        if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= pattern1.BrakePattern)
                        {
                            flag = true;
                        }
                    }
                    if (this.BrakeRelease)
                    {
                        flag = false;
                    }
                    if (flag & this.State != AtsP.States.Brake)
                    {
                        this.State = AtsP.States.Brake;
                        if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                        {
                            SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                        }
                    }
                    else if (flag1 & this.State == AtsP.States.Normal)
                    {
                        this.State = AtsP.States.Pattern;
                        if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                        {
                            SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                        }
                    }
                    else if (!flag & !flag1 & flag2 & (this.State == AtsP.States.Pattern | this.State == AtsP.States.Brake))
                    {
                        this.State = AtsP.States.Normal;
                        if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                        {
                            SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                        }
                    }
                    if (this.State == AtsP.States.Brake && data.Handles.BrakeNotch < this.Train.Specs.BrakeNotches)
                    {
                        data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches;
                    }
                    if (this.Position > this.SwitchToAtsSxPosition & this.State != AtsP.States.Brake & this.State != AtsP.States.Service & this.State != AtsP.States.Emergency)
                    {
                        this.SwitchToSx();
                    }
                }
                else if (this.State == AtsP.States.Service)
                {
                    if (data.Handles.BrakeNotch < this.Train.Specs.BrakeNotches)
                    {
                        data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches;
                    }
                }
                else if (this.State == AtsP.States.Emergency)
                {
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                }
                if (!this.AtsSxPMode & (this.State == AtsP.States.Normal | this.State == AtsP.States.Pattern | this.State == AtsP.States.Brake | this.State == AtsP.States.Service | this.State == AtsP.States.Emergency))
                {
                    blocking = true;
                }
                if (this.State != AtsP.States.Disabled & this.Train.Doors != DoorStates.None)
                {
                    data.Handles.PowerNotch = 0;
                }
            }
            else if (this.State != AtsP.States.Disabled & this.State != AtsP.States.Suppressed)
            {
                this.State = AtsP.States.Standby;
            }
            if (this.State != AtsP.States.Disabled & this.State != AtsP.States.Suppressed)
            {
                this.Train.Panel[2] = 1;
                this.Train.Panel[259] = 1;
            }
            if (this.State == AtsP.States.Pattern | this.State == AtsP.States.Brake | this.State == AtsP.States.Service | this.State == AtsP.States.Emergency)
            {
                this.Train.Panel[3] = 1;
                this.Train.Panel[260] = 1;
            }
            if (this.State == AtsP.States.Brake | this.State == AtsP.States.Service | this.State == AtsP.States.Emergency)
            {
                this.Train.Panel[5] = 1;
                this.Train.Panel[262] = 1;
            }
            if (this.State != AtsP.States.Disabled & this.State != AtsP.States.Suppressed & this.State != AtsP.States.Standby)
            {
                this.Train.Panel[6] = 1;
                this.Train.Panel[263] = 1;
            }
            if (this.State == AtsP.States.Initializing)
            {
                this.Train.Panel[7] = 1;
                this.Train.Panel[264] = 1;
            }
            if (this.State == AtsP.States.Disabled)
            {
                this.Train.Panel[50] = 1;
            }
            if (this.State != AtsP.States.Disabled & this.State != AtsP.States.Suppressed & this.State != AtsP.States.Standby & this.BrakeRelease)
            {
                this.Train.Panel[4] = 1;
                this.Train.Panel[261] = 1;
            }
            if (this.State == AtsP.States.Normal | this.State == AtsP.States.Pattern | this.State == AtsP.States.Brake | this.State == AtsP.States.Service | this.State == AtsP.States.Emergency)
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int k = 0; k < (int)this.SignalPatterns.Length; k++)
                {
                    this.SignalPatterns[k].AddToStringBuilder(string.Concat(k.ToString(), ":"), stringBuilder);
                }
                this.DivergencePattern.AddToStringBuilder("分岐/D:", stringBuilder);
                this.TemporaryPattern.AddToStringBuilder("臨時/T:", stringBuilder);
                this.CurvePattern.AddToStringBuilder("曲線/C:", stringBuilder);
                this.DownslopePattern.AddToStringBuilder("勾配/S:", stringBuilder);
                this.RoutePermanentPattern.AddToStringBuilder("P:", stringBuilder);
                this.TrainPermanentPattern.AddToStringBuilder("M:", stringBuilder);
                if (this.SwitchToAtsSxPosition != double.MaxValue)
                {
                    if (stringBuilder.Length != 0)
                    {
                        stringBuilder.Append(", ");
                    }
                    double switchToAtsSxPosition = this.SwitchToAtsSxPosition - this.Position;
                    stringBuilder.Append(string.Concat("Sx@", switchToAtsSxPosition.ToString("0")));
                }
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
            if (mode != InitializationModes.OffEmergency)
            {
                this.State = AtsP.States.Standby;
            }
            else
            {
                this.State = AtsP.States.Suppressed;
            }
            AtsP.Pattern[] patterns = this.Patterns;
            for (int i = 0; i < (int)patterns.Length; i++)
            {
                AtsP.Pattern pattern = patterns[i];
                if (Math.Abs(this.Train.State.Speed.MetersPerSecond) >= pattern.WarningPattern)
                {
                    pattern.Clear();
                }
            }
        }

        internal override void KeyDown(VirtualKeys key)
        {
            VirtualKeys virtualKey = key;
            switch (virtualKey)
            {
                case VirtualKeys.B1:
                    {
                        if (!((this.State == AtsP.States.Brake | this.State == AtsP.States.Service | this.State == AtsP.States.Emergency) & this.Train.Handles.Reverser == 0 & this.Train.Handles.PowerNotch == 0 & this.Train.Handles.BrakeNotch >= this.Train.Specs.BrakeNotches))
                        {
                            break;
                        }
                        AtsP.Pattern[] patterns = this.Patterns;
                        for (int i = 0; i < (int)patterns.Length; i++)
                        {
                            AtsP.Pattern pattern = patterns[i];
                            if (Math.Abs(this.Train.State.Speed.MetersPerSecond) >= pattern.WarningPattern)
                            {
                                pattern.Clear();
                            }
                        }
                        this.State = AtsP.States.Normal;
                        if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                        {
                            SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                        }
                        return;
                    }
                case VirtualKeys.B2:
                    {
                        if (!((this.State == AtsP.States.Normal | this.State == AtsP.States.Pattern) & !this.BrakeRelease & this.DurationOfBrakeRelease > 0))
                        {
                            break;
                        }
                        this.BrakeRelease = true;
                        this.BrakeReleaseCountdown = this.DurationOfBrakeRelease;
                        if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                        {
                            SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                        }
                        return;
                    }
                default:
                    {
                        if (virtualKey != VirtualKeys.E)
                        {
                            return;
                        }
                        if (this.State == AtsP.States.Disabled)
                        {
                            this.State = AtsP.States.Suppressed;
                            return;
                        }
                        this.State = AtsP.States.Disabled;
                        break;
                    }
            }
        }

        internal override void SetBeacon(BeaconData beacon)
        {
            int num;
            if (this.State != AtsP.States.Disabled & this.State != AtsP.States.Suppressed & this.State != AtsP.States.Initializing)
            {
                switch (beacon.Type)
                {
                    case 3:
                    case 4:
                    case 5:
                        {
                            this.Position = this.Train.State.Location;
                            if (!(this.State != AtsP.States.Service & this.State != AtsP.States.Emergency))
                            {
                                break;
                            }
                            if (this.State == AtsP.States.Standby & beacon.Optional != -1)
                            {
                                this.SwitchToP(AtsP.States.Normal);
                            }
                            if (this.State == AtsP.States.Standby)
                            {
                                break;
                            }
                            if (!(beacon.Type == 3 & beacon.Optional >= 10 & beacon.Optional <= 19))
                            {
                                num = (!(beacon.Type == 3 & beacon.Optional >= 1 & beacon.Optional <= 9) ? 0 : beacon.Optional);
                                double position = this.Position + beacon.Signal.Distance;
                                bool flag = false;
                                if (num != 0)
                                {
                                    flag = true;
                                }
                                else if (this.SignalPatterns[num].Position == double.MaxValue)
                                {
                                    flag = true;
                                }
                                else if (position > this.SignalPatterns[num].Position - 30)
                                {
                                    flag = true;
                                }
                                if (!flag)
                                {
                                    break;
                                }
                                if (!(beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10))
                                {
                                    this.SignalPatterns[num].SetGreenSignal(position);
                                    break;
                                }
                                else
                                {
                                    this.SignalPatterns[num].SetRedSignal(position);
                                    if (!(beacon.Type != 3 & beacon.Signal.Distance < 50 & !this.BrakeRelease))
                                    {
                                        break;
                                    }
                                    if (beacon.Type != 4)
                                    {
                                        this.SwitchToP(AtsP.States.Service);
                                        break;
                                    }
                                    else
                                    {
                                        this.SwitchToP(AtsP.States.Emergency);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                this.SignalPatterns[beacon.Optional - 10].Clear();
                                break;
                            }
                        }
                    case 6:
                        {
                            int optional = beacon.Optional / 1000;
                            if (optional <= 0)
                            {
                                break;
                            }
                            if (this.State == AtsP.States.Standby)
                            {
                                this.SwitchToP(AtsP.States.Normal);
                            }
                            this.Position = this.Train.State.Location;
                            int optional1 = beacon.Optional % 1000;
                            this.DivergencePattern.SetLimit((double)optional1 / 3.6, this.Position + (double)optional);
                            break;
                        }
                    case 7:
                        {
                            this.Position = this.Train.State.Location;
                            if (beacon.Optional <= 0)
                            {
                                this.SwitchToP(AtsP.States.Emergency);
                                break;
                            }
                            else
                            {
                                if (this.State == AtsP.States.Standby)
                                {
                                    this.SwitchToP(AtsP.States.Normal);
                                }
                                this.RoutePermanentPattern.SetLimit((double)beacon.Optional / 3.6, double.MinValue);
                                break;
                            }
                        }
                    case 8:
                        {
                            int num1 = beacon.Optional / 1000;
                            if (num1 <= 0)
                            {
                                break;
                            }
                            if (this.State == AtsP.States.Standby)
                            {
                                this.SwitchToP(AtsP.States.Normal);
                            }
                            this.Position = this.Train.State.Location;
                            int optional2 = beacon.Optional % 1000;
                            this.DownslopePattern.SetLimit((double)optional2 / 3.6, this.Position + (double)num1);
                            break;
                        }
                    case 9:
                        {
                            int num2 = beacon.Optional / 1000;
                            if (num2 <= 0)
                            {
                                break;
                            }
                            if (this.State == AtsP.States.Standby)
                            {
                                this.SwitchToP(AtsP.States.Normal);
                            }
                            this.Position = this.Train.State.Location;
                            int optional3 = beacon.Optional % 1000;
                            this.CurvePattern.SetLimit((double)optional3 / 3.6, this.Position + (double)num2);
                            break;
                        }
                    case 10:
                        {
                            int num3 = beacon.Optional / 1000;
                            int optional4 = beacon.Optional % 1000;
                            if (num3 == 0)
                            {
                                if (!(num3 == 0 & optional4 != 0))
                                {
                                    break;
                                }
                                this.Position = this.Train.State.Location;
                                this.SwitchToAtsSxPosition = this.Position + (double)optional4;
                                break;
                            }
                            else
                            {
                                if (this.State == AtsP.States.Standby)
                                {
                                    this.SwitchToP(AtsP.States.Normal);
                                }
                                this.Position = this.Train.State.Location;
                                this.TemporaryPattern.SetLimit((double)optional4 / 3.6, this.Position + (double)num3);
                                break;
                            }
                        }
                    case 16:
                        {
                            if (beacon.Optional != 0)
                            {
                                break;
                            }
                            this.Position = this.Train.State.Location;
                            this.DivergencePattern.Clear();
                            break;
                        }
                    case 18:
                        {
                            if (beacon.Optional != 0)
                            {
                                break;
                            }
                            this.Position = this.Train.State.Location;
                            this.DownslopePattern.Clear();
                            break;
                        }
                    case 19:
                        {
                            if (beacon.Optional != 0)
                            {
                                break;
                            }
                            this.Position = this.Train.State.Location;
                            this.CurvePattern.Clear();
                            break;
                        }
                    case 20:
                        {
                            if (beacon.Optional != 0)
                            {
                                break;
                            }
                            this.Position = this.Train.State.Location;
                            this.TemporaryPattern.Clear();
                            break;
                        }
                    case 25:
                        {
                            if (beacon.Optional == 0)
                            {
                                this.Position = this.Train.State.Location;
                                if (!(this.State == AtsP.States.Normal | this.State == AtsP.States.Pattern | this.State == AtsP.States.Brake | this.State == AtsP.States.Service | this.State == AtsP.States.Emergency))
                                {
                                    break;
                                }
                                this.SwitchToAtsSxPosition = this.Position;
                                break;
                            }
                            else if (beacon.Optional != 1)
                            {
                                if (beacon.Optional != 2)
                                {
                                    break;
                                }
                                this.Position = this.Train.State.Location;
                                if (this.State == AtsP.States.Standby)
                                {
                                    this.SwitchToP(AtsP.States.Normal);
                                }
                                if (this.AtsSxPMode)
                                {
                                    break;
                                }
                                this.AtsSxPMode = true;
                                if (!(this.Train.AtsSx != null & !this.Blocked))
                                {
                                    break;
                                }
                                if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                                {
                                    SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                                }
                                break;
                            }
                            else
                            {
                                this.Position = this.Train.State.Location;
                                if (this.State == AtsP.States.Standby)
                                {
                                    this.SwitchToP(AtsP.States.Normal);
                                }
                                if (!this.AtsSxPMode)
                                {
                                    break;
                                }
                                this.AtsSxPMode = false;
                                if (!(this.Train.AtsSx != null & !this.Blocked))
                                {
                                    break;
                                }
                                if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                                {
                                    SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                                }
                                break;
                            }
                        }
                }
            }
            switch (beacon.Type)
            {
                case -16777213:
                    {
                        double num4 = (double)(beacon.Optional & 4095) / 3.6;
                        double optional5 = (double)(beacon.Optional >> 12);
                        AtsP.CompatibilityLimit compatibilityLimit = new AtsP.CompatibilityLimit(num4, optional5);
                        if (this.CompatibilityLimits.Contains(compatibilityLimit))
                        {
                            break;
                        }
                        this.CompatibilityLimits.Add(compatibilityLimit);
                        return;
                    }
                case -16777212:
                    {
                        if (beacon.Optional == 0)
                        {
                            this.CompatibilityPermanentPattern.Clear();
                            return;
                        }
                        double num5 = (double)beacon.Optional / 3.6;
                        this.CompatibilityPermanentPattern.SetLimit(num5, double.MinValue);
                        break;
                    }
                default:
                    {
                        return;
                    }
            }
        }

        internal override void SetSignal(SignalData[] signal)
        {
            if ((int)signal.Length < 2)
            {
                this.DAtsPAspect = 5;
                return;
            }
            this.DAtsPAspect = signal[1].Aspect;
        }

        private void SwitchToP(AtsP.States state)
        {
            if (this.State == AtsP.States.Standby)
            {
                if (this.Train.AtsSx == null || this.Train.AtsSx.State != AtsSx.States.Emergency)
                {
                    this.State = state;
                    if (!this.Blocked)
                    {
                        if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                        {
                            SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                        }
                        return;
                    }
                }
            }
            else if (state == AtsP.States.Service | state == AtsP.States.Emergency)
            {
                if (this.State != AtsP.States.Brake & this.State != AtsP.States.Service & this.State != AtsP.States.Emergency && !this.Blocked)
                {
                    if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                    {
                        SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                    }
                }
                this.State = state;
            }
        }

        private void SwitchToSx()
        {
            if (this.Train.AtsSx != null)
            {
                AtsP.Pattern[] patterns = this.Patterns;
                for (int i = 0; i < (int)patterns.Length; i++)
                {
                    patterns[i].Clear();
                }
                this.State = AtsP.States.Standby;
                if (!this.Blocked)
                {
                    if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                    {
                        SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                    }
                }
                this.Train.AtsSx.State = AtsSx.States.Chime;
            }
            else if (this.State != AtsP.States.Emergency)
            {
                this.State = AtsP.States.Emergency;
                if (this.State != AtsP.States.Brake & this.State != AtsP.States.Service && !this.Blocked)
                {
                    if (!SoundManager.IsPlaying(CommonSounds.ATSPBell))
                    {
                        SoundManager.Play(CommonSounds.ATSPBell, 1.0, 1.0, false);
                    }
                }
            }
            this.SwitchToAtsSxPosition = double.MaxValue;
            this.DAtsPActive = false;
        }

        private void UpdateCompatibilityTemporarySpeedPattern()
        {
            if (this.CompatibilityLimits.Count != 0)
            {
                if (this.CompatibilityTemporaryPattern.Position != double.MaxValue)
                {
                    if (this.CompatibilityTemporaryPattern.BrakePattern < this.Train.State.Speed.MetersPerSecond)
                    {
                        return;
                    }
                    double position = this.CompatibilityTemporaryPattern.Position - this.Train.State.Location;
                    if (position >= -50 & position <= 0)
                    {
                        return;
                    }
                }
                while (this.CompatibilityLimitPointer > 0)
                {
                    if (this.CompatibilityLimits[this.CompatibilityLimitPointer].Location > this.Train.State.Location)
                    {
                        AtsP compatibilityLimitPointer = this;
                        compatibilityLimitPointer.CompatibilityLimitPointer = compatibilityLimitPointer.CompatibilityLimitPointer - 1;
                    }
                    else
                    {
                        break;
                    }
                }
                while (this.CompatibilityLimitPointer < this.CompatibilityLimits.Count - 1 && this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Location <= this.Train.State.Location)
                {
                    AtsP atsP = this;
                    atsP.CompatibilityLimitPointer = atsP.CompatibilityLimitPointer + 1;
                }
                if (this.CompatibilityLimitPointer == 0 && this.CompatibilityLimits[0].Location > this.Train.State.Location)
                {
                    this.CompatibilityTemporaryPattern.SetLimit(this.CompatibilityLimits[0].Limit, this.CompatibilityLimits[0].Location);
                    return;
                }
                if (this.CompatibilityLimitPointer < this.CompatibilityLimits.Count - 1)
                {
                    this.CompatibilityTemporaryPattern.SetLimit(this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit, this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Location);
                    return;
                }
                this.CompatibilityTemporaryPattern.Clear();
            }
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

        internal class Pattern
        {
            internal AtsP Device;

            internal double Position;

            internal double WarningPattern;

            internal double BrakePattern;

            internal double TargetSpeed;

            internal double Gradient;

            internal bool Persistent;

            internal Pattern(AtsP device)
            {
                this.Device = device;
                this.Position = double.MaxValue;
                this.WarningPattern = double.MaxValue;
                this.BrakePattern = double.MaxValue;
                this.TargetSpeed = double.MaxValue;
                this.Gradient = 0;
                this.Persistent = false;
            }

            internal void AddToStringBuilder(string prefix, StringBuilder builder)
            {
                string str;
                if (this.Position >= double.MaxValue | this.TargetSpeed >= double.MaxValue)
                {
                    return;
                }
                if (this.Position <= double.MinValue)
                {
                    double brakePattern = 3.6 * this.BrakePattern;
                    string str1 = string.Concat(prefix, brakePattern.ToString("0"));
                    if (builder.Length != 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(str1);
                    return;
                }
                double position = this.Position - this.Device.Position;
                if (position > 0)
                {
                    string[] strArrays = new string[] { prefix, null, null, null, null, null };
                    double targetSpeed = 3.6 * this.TargetSpeed;
                    strArrays[1] = targetSpeed.ToString("0");
                    strArrays[2] = "(";
                    double num = 3.6 * this.BrakePattern;
                    strArrays[3] = num.ToString("0");
                    strArrays[4] = ")@";
                    strArrays[5] = position.ToString("0");
                    str = string.Concat(strArrays);
                }
                else
                {
                    double brakePattern1 = 3.6 * this.BrakePattern;
                    str = string.Concat(prefix, brakePattern1.ToString("0"));
                }
                if (builder.Length != 0)
                {
                    builder.Append(", ");
                }
                builder.Append(str);
            }

            internal void Clear()
            {
                if (!this.Persistent)
                {
                    this.Position = double.MaxValue;
                    this.WarningPattern = double.MaxValue;
                    this.BrakePattern = double.MaxValue;
                    this.TargetSpeed = double.MaxValue;
                    this.Gradient = 0;
                }
            }

            internal void Perform(AtsP system, ElapseData data)
            {
                if (this.Position == double.MaxValue | this.TargetSpeed == double.MaxValue)
                {
                    this.WarningPattern = double.MaxValue;
                    this.BrakePattern = double.MaxValue;
                    return;
                }
                if (this.Position != double.MinValue)
                {
                    double designDeceleration = this.Device.DesignDeceleration + 9.81 * this.Gradient;
                    double position = this.Position - system.Position;
                    double warningPatternOffset = 2 * designDeceleration * (position - this.Device.WarningPatternOffset) + designDeceleration * designDeceleration * this.Device.WarningPatternDelay * this.Device.WarningPatternDelay + this.TargetSpeed * this.TargetSpeed;
                    if (warningPatternOffset > 0)
                    {
                        this.WarningPattern = Math.Sqrt(warningPatternOffset) - designDeceleration * this.Device.WarningPatternDelay;
                    }
                    else
                    {
                        this.WarningPattern = -designDeceleration * this.Device.WarningPatternDelay;
                    }
                    if (this.TargetSpeed > 0.277777777777778)
                    {
                        if (this.WarningPattern < this.TargetSpeed + this.Device.WarningPatternTolerance)
                        {
                            this.WarningPattern = this.TargetSpeed + this.Device.WarningPatternTolerance;
                        }
                    }
                    else if (this.WarningPattern < this.TargetSpeed)
                    {
                        this.WarningPattern = this.TargetSpeed;
                    }
                    double brakePatternOffset = 2 * designDeceleration * (position - this.Device.BrakePatternOffset) + designDeceleration * designDeceleration * this.Device.BrakePatternDelay * this.Device.BrakePatternDelay + this.TargetSpeed * this.TargetSpeed;
                    if (brakePatternOffset > 0)
                    {
                        this.BrakePattern = Math.Sqrt(brakePatternOffset) - designDeceleration * this.Device.BrakePatternDelay;
                    }
                    else
                    {
                        this.BrakePattern = -designDeceleration * this.Device.BrakePatternDelay;
                    }
                    if (this.TargetSpeed > 0.277777777777778)
                    {
                        if (this.BrakePattern < this.TargetSpeed + this.Device.BrakePatternTolerance)
                        {
                            this.BrakePattern = this.TargetSpeed + this.Device.BrakePatternTolerance;
                        }
                    }
                    else if (this.BrakePattern < this.TargetSpeed)
                    {
                        this.BrakePattern = this.TargetSpeed;
                    }
                    if (this.BrakePattern < this.Device.ReleaseSpeed)
                    {
                        this.BrakePattern = this.Device.ReleaseSpeed;
                    }
                }
                else
                {
                    if (this.TargetSpeed <= 0.277777777777778)
                    {
                        this.WarningPattern = this.TargetSpeed;
                        this.BrakePattern = this.TargetSpeed;
                    }
                    else
                    {
                        this.WarningPattern = this.TargetSpeed + this.Device.WarningPatternTolerance;
                        this.BrakePattern = this.TargetSpeed + this.Device.BrakePatternTolerance;
                    }
                    if (this.BrakePattern < this.Device.ReleaseSpeed)
                    {
                        this.BrakePattern = this.Device.ReleaseSpeed;
                        return;
                    }
                }
            }

            internal void SetGradient(double gradient)
            {
                this.Gradient = gradient;
            }

            internal void SetGreenSignal(double position)
            {
                this.Position = position;
                this.TargetSpeed = double.MaxValue;
            }

            internal void SetLimit(double speed, double position)
            {
                this.Position = position;
                this.TargetSpeed = speed;
            }

            internal void SetPersistentLimit(double speed)
            {
                this.Position = double.MinValue;
                this.TargetSpeed = speed;
                this.Persistent = true;
            }

            internal void SetRedSignal(double position)
            {
                this.Position = position;
                this.TargetSpeed = 0;
            }
        }

        internal enum States
        {
            Disabled,
            Suppressed,
            Initializing,
            Standby,
            Normal,
            Pattern,
            Brake,
            Service,
            Emergency
        }
    }
}