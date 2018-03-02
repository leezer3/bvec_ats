using OpenBveApi.Runtime;

namespace Plugin
{
    internal class CAWS : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;
        
        // --- members ---
        internal bool enabled;

        /// <summary>The current aspect.</summary>
        private int CurrentAspect = 0;

        private int PendingAspect;

        /// <summary>The location of the next signal.</summary>
        private double NextSignalLocation = double.MaxValue;

        /// <summary>The aspect of the next signal.</summary>
        private int NextSignalAspect = 0;

        /// <summary>The countdown for the acknowledgement switch. If zero, the switch is inactive.</summary>
        internal double AcknowledgementCountdown = 0.0;

        /// <summary>The countdown for the emergency brake operation. If zero, the brakes are inactive.</summary>
        internal double EmergencyBrakeCountdown = 0.0;

        /// <summary>The panel index of the CAWS aspect indcator.</summary>
        internal int AspectIndicator = -1;

        /// <summary>The sound index of the CAWS downgrade sound.</summary>
        internal int DowngradeSound = -1;

        /// <summary>The sound index of the CAWS upgrade sound.</summary>
        internal int UpgradeSound = -1;

        /// <summary>The panel index of the CAWS EB Indicator.</summary>
        internal int EBIndicator = -1;

        /// <summary>The panel index of the CAWS EB Indicator.</summary>
        internal static int AcknowlegementIndicator = -1;

        /// <summary>The current vehicle state.</summary>
        private VehicleState CurrentVehicleState = null;

        internal CAWS(Train train)
        {
            this.Train = train;
        }

        /// <summary>Whether there is a pending un-acknowledged CAWS alert.</summary>
        internal bool AcknowledgementPending;

        /// <summary>Stores whether CAWS has a current EB application.</summary>
        internal bool EBApplied;

        internal override void Initialize(InitializationModes mode)
        {
            AcknowledgementCountdown = 0.0;
            EmergencyBrakeCountdown = 0.0;
            EBApplied = false;
            AcknowledgementPending = false;
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (this.enabled)
            {
                CurrentVehicleState = data.Vehicle;
                if (NextSignalLocation < double.MaxValue)
                {
                    double distance = NextSignalLocation - data.Vehicle.Location;
                    if (distance < 350.0)
                    {
                        if (NextSignalAspect < CurrentAspect)
                        {
                            AcknowledgementCountdown = 7.0;
                        }
                        else if (NextSignalAspect > CurrentAspect && EBApplied != false)
                        {
                            if (UpgradeSound != -1)
                            {
                                SoundManager.Play(UpgradeSound, 1.0, 1.0, false);
                            }
                        }
                        CurrentAspect = NextSignalAspect;
                        NextSignalLocation = double.MaxValue;
                    }
                }
                if (data.ElapsedTime.Seconds > 0.0 & data.ElapsedTime.Seconds < 1.0)
                {
                    if (EmergencyBrakeCountdown > 0.0)
                    {
                        EmergencyBrakeCountdown -= data.ElapsedTime.Seconds;
                        if (EmergencyBrakeCountdown < 0.0)
                        {
                            EBApplied = false;
                            CurrentAspect = PendingAspect;
                            EmergencyBrakeCountdown = 0.0;
                            AcknowledgementCountdown = 0.0;
                            Train.TractionManager.ResetBrakeApplication();
                        }
                        else
                        {
                            EBApplied = true;
                            Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches + 1);
                        }
                    }
                    else if (AcknowledgementCountdown > 0.0)
                    {
                        
                        AcknowledgementPending = true;
                        AcknowledgementCountdown -= data.ElapsedTime.Seconds;
                        if (AcknowledgementCountdown < 0.0)
                        {
                            AcknowledgementCountdown = 0.0;
                            EmergencyBrakeCountdown = 60.0;
                            AcknowledgementPending = false;
                        }
                    }
                }
                
                //Panel Indicators
                if (AspectIndicator != -1)
                {
                    this.Train.Panel[AspectIndicator] = CurrentAspect;
                    if (EBApplied == true)
                    {
                        this.Train.Panel[AspectIndicator] = 0;
                    }
                }
                if (AcknowlegementIndicator != -1)
                {
                    if (AcknowledgementPending == true)
                    {
                        this.Train.Panel[AcknowlegementIndicator] = 1;
                    }
                    else
                    {
                        this.Train.Panel[AcknowlegementIndicator] = 0;
                    }
                }
                if (EBIndicator != -1)
                {
                    if (EBApplied == true)
                    {
                        this.Train.Panel[EBIndicator] = 1;
                    }
                    else
                    {
                        this.Train.Panel[EBIndicator] = 0;
                    }
                }
                //Sounds
                if (DowngradeSound != -1)
                {
                    if (AcknowledgementPending == true)
                    {
                        SoundManager.Play(DowngradeSound, 1.0, 1.0, true);
                    }
                    else
                    {
                        SoundManager.Stop(DowngradeSound);
                    }
                }
            }
        }

        /// <summary>Is called to inform about signals.</summary>
        /// <param name="signal">The signal data.</param>
        internal override void SetSignal(SignalData[] signal)
        {

            int newAspect;
            if (signal.Length >= 2)
            {
                if (signal[1].Distance < 350.0)
                {
                    newAspect = signal[1].Aspect;
                    NextSignalLocation = double.MaxValue;
                }
                else
                {
                    newAspect = signal[0].Aspect;
                    NextSignalLocation = CurrentVehicleState != null ? CurrentVehicleState.Location + signal[1].Distance : double.MaxValue;
                    NextSignalAspect = signal[1].Aspect;
                }
            }
            else
            {
                newAspect = signal[0].Aspect;
                NextSignalLocation = double.MaxValue;
            }
            if (newAspect < CurrentAspect && EBApplied == false)
            {
                if (EmergencyBrakeCountdown == 0.0)
                {
                    AcknowledgementCountdown = 7.0;
                }
            }
            else if (newAspect > CurrentAspect && EBApplied == false)
            {
                if (UpgradeSound != -1)
                {
                    SoundManager.Play(UpgradeSound, 1.0, 1.0, false);
                }
            }
            if (EBApplied == false)
            {
                CurrentAspect = newAspect;
            }
            else
            {
                PendingAspect = newAspect;
            }

        }
    }
}
