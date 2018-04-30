using OpenBveApi.Runtime;

namespace Plugin
{
    /// <summary>Represents an AWS Device.</summary>
    internal partial class AWS : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;
        // --- members ---
        internal bool enabled;
        //Internal Variables
        private bool suppressionactive;
        private double suppressionlocation;
        private SafetyStates MySafetyState;
        private SunflowerStates SunflowerState;
        private int detectiontimer;
        private int canceltimer;
        internal double canceltimeout = 2000;
        private int blinktimer;
        internal static bool startuphorntriggered;

        /// <summary>Default paramaters</summary>
        /// Used if no value is loaded from the config file
        internal int awsindicator = 10;
        internal int WarningSound = -1;
        internal int ClearSound = -1;
        internal int TPWSWarningSound = -1;
	    internal int CancelButtonIndex = -1;

        /// <summary>Gets the current warning state of the Automatic Warning System.</summary>
        internal SafetyStates SafetyState
        {
            get { return this.MySafetyState; }
        }

	    internal bool CancelButtonPressed;


	    internal AWS(Train train)
        {
            this.Train = train;
        }

        internal override void Initialize(InitializationModes mode)
        {
        }

        //<param name="mode">The initialization mode.</param>
        internal void OnStartUp(SunflowerStates state)
        {
            if (Train.selftest == true)
            {
                suppressionactive = false;
                canceltimer = (int)canceltimeout;
                this.MySafetyState = SafetyStates.None;
	            this.SunflowerState = state;
            }

        }



        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (this.enabled)
            {
                if (this.SafetyState != SafetyStates.Isolated)
                {

                    if (this.suppressionactive)
                    {
                        /* Cancel any suppression which is in effect, if the train is not within range of the last suppression magnet location */
                        if (Train.CurrentSpeed > 0)
                        {
                            if (Train.TrainLocation > this.suppressionlocation + 2)
                            {
                                this.suppressionlocation = 0;
                                this.suppressionactive = false;
                            }
                        }
                        else
                        {
                            if (Train.TrainLocation < this.suppressionlocation - 2)
                            {
                                this.suppressionlocation = 0;
                                this.suppressionactive = false;
                            }
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.Primed)
                    {
                        /* An AWS magnet south pole has primed the AWS */
                        this.SunflowerState = SunflowerStates.Clear;
                        this.detectiontimer = this.detectiontimer + (int)data.ElapsedTime.Milliseconds;
                        if (this.detectiontimer > 1000)
                        {
                            /* No north pole has been detected within the timeout period, so reset the detection timer and issue an AWS warning */
                            this.detectiontimer = 0;
                            this.MySafetyState = SafetyStates.CancelTimerActive;
                        }

                    }
                    else if (this.MySafetyState == SafetyStates.Clear)
                    {
                        /* The AWS indicates a clear signal */
                        this.Reset();
                        this.SunflowerState = SunflowerStates.Clear;
                        if (this.ClearSound != -1)
                        {
                            SoundManager.Play(ClearSound, 1.0, 1.0, false);
                        }
                        if (this.WarningSound != -1)
                        {
                            SoundManager.Stop(WarningSound);
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.CancelTimerActive)
                    {

                        /* An AWS warning has been issued */
                        if (this.WarningSound != -1)
                        {
                            SoundManager.Play(WarningSound, 1.0, 1.0, true);
                        }
                        this.SunflowerState = SunflowerStates.Clear;
                        this.canceltimer = this.canceltimer - (int)data.ElapsedTime.Milliseconds;
                        if (this.canceltimer < 0)
                        {
                            this.canceltimer = 0;
                            this.MySafetyState = SafetyStates.CancelTimerExpired;
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.WarningAcknowledged)
                    {
                        /* An AWS warning was acknowledged in time */
                        if (this.WarningSound != -1)
                        {
                            SoundManager.Stop(WarningSound);
                        }
                        this.Reset();
                    }
                    else if (this.MySafetyState == SafetyStates.CancelTimerExpired)
                    {
                        /* An AWS warning was not acknowledged in time */
                        if (this.WarningSound != -1)
                        {
                            SoundManager.Play(WarningSound, 1.0, 1.0, true);
                        }
                        if (Train.TPWS.enabled)
                        {
                            Train.TPWS.IssueBrakeDemand();
                        }
                        else
                        {
                            if (Train.TractionManager.PowerCutoffDemanded == false)
                            {
                                Train.DebugLogger.LogMessage("Power cutoff was demanded by the AWS due to a warning not being acknowledged in time");
                                Train.TractionManager.DemandPowerCutoff();
                            }
                            if (Train.TractionManager.CurrentInterventionBrakeNotch != this.Train.Specs.BrakeNotches +1)
                            {
                                Train.DebugLogger.LogMessage("Emergency brakes were demanded by the AWS due to a warning not being acknowledged in time");
                                Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches + 1);
                            }
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.SelfTest)
                    {
                        
                        blinktimer += (int)data.ElapsedTime.Milliseconds;
                        if (blinktimer > 1200)
                        {
                            if (Train.AWS.WarningSound != -1)
                            {
                                if (startuphorntriggered == false)
                                {
                                    SoundManager.Play(WarningSound, 1.0, 1.0, true);
                                    startuphorntriggered = true;
                                }
                            }

                        }
                        else if (blinktimer > 1000)
                        {
                            this.SunflowerState = SunflowerStates.Clear;
                        }
                        else if (blinktimer > 400)
                        {
                            this.SunflowerState = SunflowerStates.Warn;
                        }
                        

                    
                    }
                    else if (this.MySafetyState == SafetyStates.TPWSAWSBrakeDemandIssued)
                    {
                        /* The TPWS issued an AWS Brake Demand due to the AWS not being acknowledged in time */
                        if (TPWSWarningSound != -1)
                        {
                            SoundManager.Play(TPWSWarningSound, 1.0, 1.0, true);
                        }
                    }
                }
                
                else if (this.SafetyState == SafetyStates.Isolated)
                {
                    if (WarningSound != -1)
                    {
                        if (SoundManager.IsPlaying(WarningSound))
                        {
                            SoundManager.Stop(WarningSound);
                        }
                    }
                    if (TPWSWarningSound != -1)
                    {
                        if (SoundManager.IsPlaying(TPWSWarningSound))
                        {
                            SoundManager.Stop(TPWSWarningSound);
                        }
                    }
                    this.canceltimer = (int)this.canceltimeout;
                    this.SunflowerState = SunflowerStates.Warn;
                }
                /* Set the state of the AWS Sunflower instrument */
                if (awsindicator != -1)
                {
                    if (this.SunflowerState == SunflowerStates.Warn)
                    {
                        this.Train.Panel[awsindicator] = 1;
                    }
                    else
                    {
                        this.Train.Panel[awsindicator] = 0;
                    }
                }
            }
			//Set the state of the cancel button panel index
			//As this is a physical button, it can be pressed at any time
			if (CancelButtonIndex != -1)
			{
				if (CancelButtonPressed)
				{
					this.Train.Panel[CancelButtonIndex] = 1;
				}
				else
				{
					this.Train.Panel[CancelButtonIndex] = 0;
				}
			}

        }

        /// <summary>Call this function to prime the AWS as a result of passing the south pole of an AWS permanent magnet. This should be done only via the SetBeacon() method.</summary>
        internal void Prime()
        {
            //Try to prime our ATS Device
            if (!Train.AWSIsolated && this.MySafetyState != SafetyStates.Primed)
                {
                    //If suppression of the next magnet is *not* active
                    if (!this.suppressionactive)
                        {
                        this.MySafetyState = SafetyStates.Primed;
                        }
                        else if (suppressionactive)
                        {
                        this.suppressionactive = false;
                        }
                } 
        }

        /// <summary>Call this function to suppress detection of an AWS permanent magnet. This should be done only via the SetBeacon() method.</summary>
        internal void Suppress(double location)
        {
            //If AWS is not isolated, turn on suppression of next magnet and record location
            if (!Train.AWSIsolated)
            {
                this.suppressionactive = true;
                this.suppressionlocation = location;
            }
        }

        /// <summary>Call this function to issue an AWS clear indication, as a result of passing the north pole of an AWS electromagnet. This should be done only via the SetBeacon() method.</summary>
        internal void IssueClear()
        {
            if (!Train.AWSIsolated && (this.MySafetyState == SafetyStates.Primed || this.MySafetyState == SafetyStates.CancelTimerActive))
            {
                this.MySafetyState = SafetyStates.Clear;
            }
        }

        /// <summary>Call this function to issue an AWS clear indication with legacy behaviour only. This should be done only via the SetBeacon() method.</summary>
        internal void IssueLegacyClear()
        {
            if (this.MySafetyState != SafetyStates.Isolated)
            {
                this.MySafetyState = SafetyStates.Clear;
            }
        }
        internal void Acknowlege()
        {
            this.MySafetyState = SafetyStates.WarningAcknowledged;
        }

        internal void Reset()
        {
            /* Unconditionally resets the Automatic Warning System, cancelling any warnings which are already in effect */
            this.canceltimer = (int)this.canceltimeout;
            this.detectiontimer = 0;
            this.suppressionactive = false;
            this.suppressionlocation = 0;
            this.MySafetyState = SafetyStates.None;
            this.SunflowerState = SunflowerStates.Warn;
            if (Train.TractionManager.BrakeInterventionDemanded == true)
            {
                Train.TractionManager.ResetBrakeApplication();
            }
            if (Train.TractionManager.PowerCutoffDemanded == true)
            {
                Train.TractionManager.ResetPowerCutoff();
            }
        }

        /// <summary>Call this method to isolate the Automatic Warning System</summary>
        internal void Isolate()
        {
            
                this.MySafetyState = SafetyStates.Isolated;
        }

        internal void handleawstpwsbrakedemand()
        {
            if (!Train.AWSIsolated)
            {
                this.MySafetyState = SafetyStates.TPWSAWSBrakeDemandIssued;
            }
        }

        internal void handleawstssbrakedemand()
        {
            if (!Train.AWSIsolated)
            {
                this.MySafetyState = SafetyStates.TPWSTssBrakeDemandIssued;
            }
        }

        internal void SelfTest()
        {
            this.MySafetyState = SafetyStates.SelfTest;
            blinktimer = 0;
        }
		



    }
}