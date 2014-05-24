using System;
using System.Reflection;
using System.Collections;
using OpenBveApi.Runtime;


namespace Plugin
{
    /// <summary>Represents the AWS device.</summary>
    internal partial class 123 : Device
    {

        // --- members ---
        /// <summary>The underlying train.</summary>
        private Train Train;

        /// <summary>The Automatic Warning System acknowledgement timeout period.</summary>
        internal int CancelTimeout;
        /// <summary>The timer which keeps track of the Automatic Warning System acknowledgement countdown in milliseconds.</summary>
        private int MyCancelTimer;
        /// <summary>The timer which keeps track of the Automatic Warning System north pole detection countdown in milliseconds.</summary>
        private int MyDetectionTimer;
        /// <summary>Whether or not the next encountered AWS permanent magnet should be suppressed (ignored).</summary>
        private bool MySuppressionActive;
        /// <summary>The location where the last AWS suppression megnet was detected.</summary>
        private double MySuppressionLocation;
        /// <summary>The current warning state of the Automatic Warning System.</summary>
        private SafetyStates MySafetyState;
        /// <summary>The current state of the Automatic Warning System sunflower indicator.</summary>
        private SunflowerStates SunflowerState;

        // properties

        /// <summary>Gets the current warning state of the Automatic Warning System.</summary>
        internal SafetyStates SafetyState
        {
            get { return this.MySafetyState; }
        }

        /// <summary>Gets the current value of the Automatic Warning System cancellation timer.</summary>
        internal int CancelTimer
        {
            get { return this.MyCancelTimer; }
        }

        internal AWS(Train train)
        {
            this.Train = train;


        }


        /// <summary>Creates a new instance of this class.</summary>
        /// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
        /// and they will determine what values are written into a new configuration file if necessary.</remarks>
        internal AWS()
        {
            base.MyOperativeState = OperativeStates.Normal;
            base.MyEnabled = false;
            this.CancelTimeout = 3000;
            this.MyCancelTimer = this.CancelTimeout;
            this.MyDetectionTimer = 0;
            this.MySuppressionActive = false;
            this.MySuppressionLocation = 0;
            this.MySafetyState = SafetyStates.None;
            this.SunflowerState = SunflowerStates.Warn;
        }


        // events

        /// <summary>Event signalling that the AWS warning horn has been acknowledged.</summary>
        internal event EventHandler AwsWarningAcknowledged;

        // event publishing methods

        /// <summary>Publishes an event signalling that the AWS warning horn has been acknowledged.</summary>
        private void OnAwsWarningAcknowledged(EventArgs e)
        {
            EventHandler handler = AwsWarningAcknowledged;
            if (handler != null)
            {
                handler(this, e);
            }
            /*if (Plugin.DebugMode) {
                Plugin.ReportLogEntry(
                    string.Format(
                        "{0} {1} {2} Aws: [EVENT] - AWS warning acknowledged at {3} metres",
                        DateTime.Now.TimeOfDay.ToString(),
                        this.GetType().Name.ToString(),
                        MethodInfo.GetCurrentMethod().ToString(),
                        Plugin.TrainLocation.ToString()
                    )
                );
            }*/
        }

        // event handling methods

        /// <summary>This method is called when an AWS initiated TPWS Brake Demand event occurs.</summary>
        /// <remarks>Use this event to suppress an Automatic Warning System warning when a TPWS Brake Demand has already been issued.</remarks>
        internal void HandleTpwsAwsBrakeDemand(Object sender, EventArgs e)
        {
            if (this.MySafetyState != SafetyStates.Isolated)
            {
                this.MySafetyState = SafetyStates.TPWSAWSBrakeDemandIssued;
            }
        }

        /// <summary>This method is called when a TPWS Brake Demand event occurs.</summary>
        /// <remarks>Use this event to suppress an Automatic Warning System warning when a TPWS Brake Demand has already been issued.</remarks>
        internal void HandleTpwsTssBrakeDemand(Object sender, EventArgs e)
        {
            if (this.MySafetyState != SafetyStates.Isolated)
            {
                this.MySafetyState = SafetyStates.TPWSTssBrakeDemandIssued;
            }
        }

        /// <summary>This method is called when a TPWS isolation event occurs.</summary>
        /// <remarks>Use this event to disable the Automatic Warning System when the TPWS is isolated.</remarks>
        internal void HandleTpwsIsolated(Object sender, EventArgs e)
        {
            this.Isolate();
        }

        /// <summary>This method is called when a TPWS no-longer-isolated event occurs.</summary>
        /// <remarks>Use this event to re-enable the Automatic Warning System when the TPWS is no longer isolated.</remarks>
        internal void HandleTpwsNotIsolated(Object sender, EventArgs e)
        {
            this.Reset();
        }

        // instance methods

        /// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
        /// only do so via the Plugin.Initialize() method.</summary>
        /// <param name="mode">The initialisation mode as set by the host application.</param>
        internal void Reinitialise(InitializationModes mode)
        {
            this.MyCancelTimer = this.CancelTimeout;
            this.MyDetectionTimer = 0;
            this.MySuppressionActive = false;
            this.MySuppressionLocation = 0;
            this.MySafetyState = SafetyStates.None;
            this.SunflowerState = SunflowerStates.Clear;
        }

        /// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
        /// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
        /// the Update() method instead.</remarks>
        internal void Reset()
        {
            /* Unconditionally resets the Automatic Warning System, cancelling any warnings which are already in effect */
            this.MyCancelTimer = this.CancelTimeout;
            this.MyDetectionTimer = 0;
            this.MySuppressionActive = false;
            this.MySuppressionLocation = 0;
            this.MySafetyState = SafetyStates.None;
            this.SunflowerState = SunflowerStates.Warn;
            /*	InterlockManager.RequestBrakeReset();
                InterlockManager.RequestTractionPowerReset(); */
        }

        //Switch Block to enable/ disable to be added

        /// <summary>This should be called once during each Elapse() call, and defines the behaviour of the system.
        /// Power and brake handle positions should be controlled by issuing demands and requests to the Interlock Manager.</summary>
        /// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
        /// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
        /// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            /* If disabled, no processing is done */
            if (base.Enabled)
            {
                /* Set the conditions under which this system's on behaviour will function */
                if (base.OperativeState != OperativeStates.Failed)
                {

                    if (this.MySuppressionActive)
                    {
                        /* Cancel any suppression which is in effect, if the train is not within range of the last suppression magnet location */
                        if (Train.trainspeed > 0)
                        {
                            if (Train.trainlocation > this.MySuppressionLocation + 2)
                            {
                                this.MySuppressionLocation = 0;
                                this.MySuppressionActive = false;
                            }
                        }
                        else
                        {
                            if (Train.trainlocation < this.MySuppressionLocation - 2)
                            {
                                this.MySuppressionLocation = 0;
                                this.MySuppressionActive = false;
                            }
                        }
                    }
                    if (this.SafetyState != SafetyStates.Isolated)
                    {
                        /* Select behaviour depending upon the safety state */
                        if (this.MySafetyState == SafetyStates.SelfTest)
                        {
                            /* Show or hide the AWS sunflower during the AWS is in self-test mode */
                            if (StartupSelfTestManager.SequenceTimer < 1300 && StartupSelfTestManager.SequenceTimer > 500)
                            {
                                this.SunflowerState = SunflowerStates.Warn;
                            }
                            else if (StartupSelfTestManager.SequenceTimer <= 0)
                            {
                                this.MySafetyState = SafetyStates.SelfTestWarning;
                            }
                            else
                            {
                                this.SunflowerState = SunflowerStates.Clear;
                            }
                        }
                        else if (this.MySafetyState == SafetyStates.SelfTestWarning)
                        {
                            /* Clear the AWS sunflower indication while the warning is sounding during the self-test */
                            this.SunflowerState = SunflowerStates.Clear;
                            //	SoundManager.Play(SoundIndices.AwsHorn, 1.0, 1.0, true);
                        }
                        else if (this.MySafetyState == SafetyStates.None)
                        {
                            /*	if (SoundManager.IsPlaying(SoundIndices.AwsHorn)) {
                                    SoundManager.Stop(SoundIndices.AwsHorn);
                                } */
                        }
                        else if (this.MySafetyState == SafetyStates.Primed)
                        {
                            /* An AWS magnet south pole has primed the AWS */
                            this.SunflowerState = SunflowerStates.Clear;
                            this.MyDetectionTimer = this.MyDetectionTimer + (int)elapsedTime;
                            if (this.MyDetectionTimer > 1000)
                            {
                                /* No north pole has been detected within the timeout period, so reset the detection timer and issue an AWS warning */
                                this.MyDetectionTimer = 0;
                                this.MySafetyState = SafetyStates.CancelTimerActive;
                                /*	if (Plugin.DebugMode) {
                                        Plugin.ReportLogEntry(
                                            string.Format(
                                                "{0} {1} {2} AWS: [INFORMATION] - AWS delay period has elapsed at location {3} metres - issuing AWS warning",
                                                DateTime.Now.TimeOfDay.ToString(),
                                                this.GetType().Name.ToString(),
                                                MethodInfo.GetCurrentMethod().ToString(),
                                                Train.trainlocation.ToString()
                                            )
                                        ); 
                                    }*/
                            }
                        }
                        else if (this.MySafetyState == SafetyStates.Clear)
                        {
                            /* The AWS indicates a clear signal */
                            this.Reset();
                            this.SunflowerState = SunflowerStates.Clear;
                            /*SoundManager.Play(SoundIndices.AwsBing, 1.0, 1.0, false);
                            SoundManager.Stop(SoundIndices.AwsHorn); */
                        }
                        else if (this.MySafetyState == SafetyStates.CancelTimerActive)
                        {
                            /* An AWS warning has been issued */
                            /*SoundManager.Play(SoundIndices.AwsHorn, 1.0, 1.0, true); */
                            this.SunflowerState = SunflowerStates.Clear;
                            this.MyCancelTimer = this.MyCancelTimer - (int)elapsedTime;
                            if (this.MyCancelTimer < 0)
                            {
                                this.MyCancelTimer = 0;
                                this.MySafetyState = SafetyStates.CancelTimerExpired;
                            }
                        }
                        else if (this.MySafetyState == SafetyStates.WarningAcknowledged)
                        {
                            /* An AWS warning was acknowledged in time */
                            /*SoundManager.Stop(SoundIndices.AwsHorn);*/
                            this.Reset();
                        }
                        else if (this.MySafetyState == SafetyStates.CancelTimerExpired)
                        {
                            /* An AWS warning was not acknowledged in time */
                            /*SoundManager.Play(SoundIndices.AwsHorn, 1.0, 1.0, true);*/
                            if (Train.TPWS.Enabled)
                            {
                                Train.TPWS.IssueBrakeDemand();
                            }
                            else
                            {
                                /*InterlockManager.DemandBrakeApplication();
                                if (Plugin.Diesel.Enabled) {
                                    InterlockManager.DemandTractionPowerCutoff();*/
                            }
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.TPWSAWSBrakeDemandIssued)
                    {
                        /* The TPWS issued an AWS Brake Demand due to the AWS not being acknowledged in time */
                        /*SoundManager.Play(SoundIndices.AwsHorn, 1.0, 1.0, true);*/
                    }
                }
                else if (this.SafetyState == SafetyStates.Isolated)
                {
                    /*if (SoundManager.IsPlaying(SoundIndices.AwsHorn))
                    {
                        SoundManager.Stop(SoundIndices.AwsHorn);
                    }*/
                    this.MyCancelTimer = this.CancelTimeout;
                    this.SunflowerState = SunflowerStates.Warn;
                }
                /* Set the state of the AWS Sunflower instrument */
                if (this.SunflowerState == SunflowerStates.Warn)
                {
                    /*panel[PanelIndices.AwsSunflower] = (int)SunflowerState;*/
                }
            }
        }

        /* Add any information to display via openBVE's in-game debug interface mode below */
        /*if (Plugin.DebugMode) {
            debugBuilder.AppendFormat("[Aws:{0}]",
                                      this.MySafetyState.ToString());
        }*/


        // other methods


        /// <summary>Call this function to issue an AWS clear indication, as a result of passing the north pole of an AWS electromagnet. This should be done only via the SetBeacon() method.</summary>
        internal void IssueClear()
        {
            if (this.MySafetyState != SafetyStates.Isolated && (this.MySafetyState == SafetyStates.Primed || this.MySafetyState == SafetyStates.CancelTimerActive))
            {
                this.MySafetyState = SafetyStates.Clear;
               /* if (Plugin.DebugMode)
                {
                    Plugin.ReportLogEntry(
                        string.Format(
                            "{0} {1} {2} AWS: [INFORMATION] - AWS north pole detected at location {3} metres - issuing clear indication and resetting delay period timer",
                            DateTime.Now.TimeOfDay.ToString(),
                            this.GetType().Name.ToString(),
                            MethodInfo.GetCurrentMethod().ToString(),
                            Train.trainlocation.ToString()
                        )
                    );
                } */
            }
        }

        /// <summary>Call this function to issue an AWS clear indication with legacy behaviour only. This should be done only via the SetBeacon() method.</summary>
        internal void IssueLegacyClear()
        {
            if (this.MySafetyState != SafetyStates.Isolated)
            {
                this.MySafetyState = SafetyStates.Clear;
                /* if (Plugin.DebugMode)
                {
                    Plugin.ReportLogEntry(
                        string.Format(
                            "{0} {1} {2} AWS: [INFORMATION] - AWS magnet (legacy mode) detected at location {3} metres - issuing clear indication and resetting delay period timer",
                            DateTime.Now.TimeOfDay.ToString(),
                            this.GetType().Name.ToString(),
                            MethodInfo.GetCurrentMethod().ToString(),
                            Train.trainlocation.ToString()
                        )
                    );
                } */
            }
        }

        /// <summary>Call this function to prime the AWS as a result of passing the south pole of an AWS permanent magnet. This should be done only via the SetBeacon() method.</summary>
        internal void Prime()
        {
            /* Primes the AWS state, ready for north pole detection, unless a TPWS Brake Demand is already in effect */
            if (this.SafetyState != SafetyStates.Isolated && this.MySafetyState != SafetyStates.Primed &&
                this.MySafetyState != SafetyStates.TPWSAWSBrakeDemandIssued && Train.TPWS.SafetyState != TPWS.SafetyStates.TssBrakeDemand &&
                Train.TPWS.SafetyState != TPWS.SafetyStates.BrakeDemandAcknowledged)
            {
                if (!this.MySuppressionActive)
                {
                    /* Only act upon this, if the permanent magnet is not being suppressed */
                    this.MySafetyState = SafetyStates.Primed;
                }
                else if (this.MySuppressionActive)
                {
                    /* The magnet is being suppressed - reset state for the next magnet to be encountered */
                    this.MySuppressionActive = false;
                }
                /* if (Plugin.DebugMode)
                {
                    Plugin.ReportLogEntry(
                        string.Format(
                            "{0} {1} {2} AWS: [INFORMATION] - AWS south pole detected at location {3} metres - system is primed and delay period timer is active",
                            DateTime.Now.TimeOfDay.ToString(),
                            this.GetType().Name.ToString(),
                            MethodInfo.GetCurrentMethod().ToString(),
                            Train.trainlocation.ToString()
                        )
                    );
                } */
            }
        }

        /// <summary>Call this function to suppress detection of an AWS permanent magnet. This should be done only via the SetBeacon() method.</summary>
        internal void Suppress(double location)
        {
            if (this.MySafetyState != SafetyStates.Isolated)
            {
                this.MySuppressionActive = true;
                this.MySuppressionLocation = location;
               /* if (Plugin.DebugMode)
                {
                    Plugin.ReportLogEntry(
                        string.Format(
                            "{0} {1} {2} AWS: [INFORMATION] - AWS suppression at location {3} metres - ignoring the next permanent magnet within 2 metres",
                            DateTime.Now.TimeOfDay.ToString(),
                            this.GetType().Name.ToString(),
                            MethodInfo.GetCurrentMethod().ToString(),
                            Train.trainlocation.ToString()
                        )
                    );
                } */
            }
        }

        internal void AcknowledgeWarning()
        {
            bool issueWarningAcknowledgedEvent;
            if (!base.MyEnabled)
            {
                /* Raise an event signalling that an AWS warning has been acknowledged for event subscribers
                 * (such as the Vigilance Device and TPWS).
                 * 
                 * This is here so that TPWS can function without AWS enabled */
                issueWarningAcknowledgedEvent = true;
                this.MySafetyState = SafetyStates.None;
            }
            else if (this.MySafetyState != SafetyStates.Isolated &&
                     this.MySafetyState == SafetyStates.CancelTimerActive || this.MySafetyState == SafetyStates.SelfTestWarning ||
                     this.MySafetyState == SafetyStates.CancelTimerExpired || this.MySafetyState == SafetyStates.TPWSAWSBrakeDemandIssued ||
                     this.MySafetyState == SafetyStates.TPWSTssBrakeDemandIssued)
            {
                /* Handle situations where an AWS warning is indicated and/or a TPWS brake demand has been issued */
                this.MySafetyState = SafetyStates.WarningAcknowledged;
                issueWarningAcknowledgedEvent = true;
            }
            else if (Train.TPWS.Enabled)
            {
                if (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand && this.MySafetyState == SafetyStates.None)
                {
                    /* Handle a situation where AWS may be clear but a TPWS brake demand has been issued */
                    issueWarningAcknowledgedEvent = true;
                }
                else
                {
                    issueWarningAcknowledgedEvent = false;
                }
            }
            else
            {
                issueWarningAcknowledgedEvent = false;
            }

            if (issueWarningAcknowledgedEvent)
            {
                /* Raise an event signalling that an AWS warning has been acknowledged for event subscribers */
                this.OnAwsWarningAcknowledged(new EventArgs());
            }
        }

        /// <summary>Call this function to put the Automatic Warning System into self-test mode. This should only be done via the StartupSelfTestManager.Update() method.</summary>
        internal void SelfTest()
        {
            this.MySafetyState = SafetyStates.SelfTest;
        }

        /// <summary>Call this method to isolate the Automatic Warning System. This will only succeed if there is no safety intervention in effect.
        /// This does not override the behaviour of the Interlock Manager.</summary>
        internal void Isolate()
        {
            if (this.MySafetyState != SafetyStates.CancelTimerExpired &&
                this.MySafetyState != SafetyStates.TPWSAWSBrakeDemandIssued &&
                this.MySafetyState != SafetyStates.TPWSTssBrakeDemandIssued)
            {
                this.MySafetyState = SafetyStates.Isolated;
            }


        }
    }
}

