using OpenBveApi.Runtime;

namespace Plugin
{
    /// <summary>Represents a German PZB Device.</summary>
    internal partial class PZB : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        internal bool enabled;

        /// <summary>The aspect of the signal passed over at the last beacon.</summary>
        internal int BeaconAspect;
        /// <summary>Stores whether the Stop Override key is currently pressed (To pass red signal under authorisation).</summary>
        internal bool StopOverrideKeyPressed;
        /// <summary>The warning tone played continuosly when a red signal has been passed under authorisation.</summary>
        internal int RedSignalWarningSound = -1;
        /// <summary>The warning tone played continuosly when a red signal has been passed under authorisation.</summary>
        internal int RedSignalWarningLight = -1;
        /// <summary>The light lit when an EB application has been triggered.</summary>
        internal int EBLight = -1;
        private SafetyStates MySafetyState;
        /// <summary>Gets the current warning state of the PZB System.</summary>
        internal SafetyStates SafetyState
        {
            get { return this.MySafetyState; }
        }

        internal PZB(Train train)
        {
            this.Train = train;
        }

        internal override void Initialize(InitializationModes mode)
        {
            MySafetyState = SafetyStates.None;
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (this.enabled)
            {
                if (MySafetyState == SafetyStates.HomeStopPassed)
                {
                    if (StopOverrideKeyPressed == true)
                    {
                        MySafetyState = SafetyStates.HomeStopPassedAuthorised;
                    }
                    else
                    {
                        MySafetyState = SafetyStates.HomeStopEBApplication;
                    }
                }
                if (MySafetyState == SafetyStates.HomeStopPassedAuthorised)
                {
                    if (RedSignalWarningSound != -1)
                    {
                        SoundManager.Play(RedSignalWarningSound, 1.0, 1.0, true);
                    }
                    if ((Train.trainspeed != 0 && !StopOverrideKeyPressed) || Train.trainspeed > 40)
                    {
                        MySafetyState = SafetyStates.HomeStopEBApplication;
                    }
                }
                if (MySafetyState == SafetyStates.HomeStopEBApplication)
                {
                    if (RedSignalWarningSound != -1)
                    {
                        SoundManager.Stop(RedSignalWarningSound);
                    }
                    //Demand EB application
                    Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                }


                //Panel Lights
                {
                    if (EBLight != -1)
                    {
                        if (MySafetyState == SafetyStates.DistantEBApplication || MySafetyState == SafetyStates.HomeStopEBApplication)
                        {
                            this.Train.Panel[EBLight] = 1;
                        }
                        else
                        {
                            this.Train.Panel[EBLight] = 0;
                        }
                    }
                    if (RedSignalWarningLight != -1)
                    {
                        if (MySafetyState == SafetyStates.HomeStopPassedAuthorised)
                        {
                            this.Train.Panel[RedSignalWarningLight] = 1;
                        }
                        else
                        {
                            this.Train.Panel[RedSignalWarningLight] = 0;
                        }
                    }
                }
            }
        }

        /// <summary>Call this function to trigger a PZB alert.</summary>
        internal void Trigger(int frequency)
        {
            //Distant signals have a 1000hz frequency beacon attached
            if (frequency == 1000)
            {
            }
            //Home signal speed control
            else if (frequency == 500)
            {
            }
            //Home signal red check
            else if (frequency == 2000)
            {
                if (BeaconAspect == 0)
                {
                    //Change PZB phase to passed red signal
                    MySafetyState = SafetyStates.HomeStopPassed;
                }
            }
        }


    }
}
