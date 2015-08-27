using OpenBveApi.Runtime;

namespace Plugin
{
    /// <summary>Represents an AWS Device.</summary>
    internal partial class TPWS : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;
        // --- members ---
        internal bool enabled;

		//Internal Variables
		private int brakesappliedtimer;
		private int overridetimer;
		private bool tssndactive;
		private double tssndlastlocation;
		private bool tssodactive;
		private double tssodlastlocation;

        private double osslastspeed;
        private int ossndtimer;
        private bool ossndtimeractive;
        private int ossodtimer;
        private bool ossodtimeractive;
        private int indicatorblinktimer;
        private int overrideblinktimer;
		
        //Default Variables if none are loaded from configuration file
		internal double brakesappliedtimeout = 60000;
		internal double overridetimeout = 20000;
		internal double loopmaxspacing = 2;
        internal double osstimeout = 974;
        internal int brakedemandindicator = -1; 
        internal int brakeindicatorblinkrate = 300;
        internal int twpsoverrideindicator = -1;
        internal int tpwsoverrideblinkrate = -1;
        internal int tpwsisolatedindicator = -1;
        //Panel Variables

        private SafetyStates MySafetyState;

        /// <summary>Gets the current warning state of the Train Protection and Warning System.</summary>
        internal SafetyStates SafetyState
        {
            get { return this.MySafetyState; }
        }

		/// <summary>Gets the current warning state of the Train Protection and Warning System.</summary>
		internal double OssLastSpeed {
			get { return this.osslastspeed; }
		}
        

        internal TPWS(Train train)
        {
            this.Train = train;
        }

        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            if (Train.selftest == true)
            {
                brakesappliedtimer = (int)brakesappliedtimeout;
                overridetimer = (int)overridetimeout;
                this.MySafetyState = SafetyStates.None;
            }
        }

        internal void Reinitialise(InitializationModes mode)
        {
            this.osslastspeed = 0;
            this.brakesappliedtimer = (int)this.brakesappliedtimeout;
            this.overridetimer = (int)this.overridetimeout;
            this.tssndactive = false;
            this.tssndlastlocation = 0;
            this.tssodactive = false;
            this.tssodlastlocation = 0;
            this.ossndtimer = 0;
            this.ossndtimeractive = false;
            this.ossodtimer = 0;
            this.ossodtimeractive = false;
            this.MySafetyState = SafetyStates.None;
        }

        

        /// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
        /// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
        /// the Update() method instead.</remarks>
        internal void Reset()
        {
            /* Unconditionally resets the Train Protection and Warning System, cancelling any warnings which are already in effect */
            this.Reinitialise(InitializationModes.OnService);
            Train.tractionmanager.resetbrakeapplication();
            Train.tractionmanager.resetpowercutoff();
        }

        /// <summary>Call this method from the Traction Manager to isolate the TPWS.</summary>
        internal void Isolate()
        {
            this.MySafetyState = SafetyStates.Isolated;
        }

        /// <summary>Call this method from the Traction Manager to acknowledge a TPWS Brake Demand.</summary>
        internal void AcknowledgeBrakeDemand()
        {
            /* Unconditionally resets the Train Protection and Warning System, cancelling any warnings which are already in effect */
            this.MySafetyState = SafetyStates.BrakeDemandAcknowledged;
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
                    if (this.MySafetyState == SafetyStates.LegacyOssArmed)
                    {
                        /* TPWS OSS is enabled with legacy behaviour, so check the train's current speed, and issue a Brake Demand if travelling too fast */
                        if (Train.trainspeed > this.osslastspeed)
                        {
                            if (Train.tractionmanager.currentbrakenotch != this.Train.Specs.BrakeNotches + 1)
                            {
                                Train.DebugLogger.LogMessage("Emergency brakes were demanded by the TPWS Overspeed system");
                                Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                            }
                            this.MySafetyState = SafetyStates.TssBrakeDemand;
                            this.osslastspeed = 0;
                        }
                        else
                        {
                            this.MySafetyState = SafetyStates.None;
                        }

                    }
                    else if (this.MySafetyState == SafetyStates.OssArmed)
                    {
                        /* TPWS OSS is armed, so handle the OSS timers */
                        if (this.ossndtimeractive)
                        {
                            this.ossndtimer = this.ossndtimer + (int)data.ElapsedTime.Milliseconds;
                        }
                        if (this.ossodtimeractive)
                        {
                            this.ossodtimer = this.ossodtimer + (int)data.ElapsedTime.Milliseconds;
                        }
                        if (this.ossndtimer > this.osstimeout)
                        {
                            /* The OSS ND timer has expired and no matching trigger loop has been detected, so the train is travelling within the permitted speed */
                            this.ossndtimer = 0;
                            this.ossndtimeractive = false;

                        }
                        if (this.ossodtimer > this.osstimeout)
                        {
                            /* The OSS OD timer has expired and no matching trigger loop has been detected, so the train is travelling within the permitted speed */
                            this.ossodtimer = 0;
                            this.ossodtimeractive = false;

                        }
                        if (!this.ossndtimeractive && !this.ossodtimeractive)
                        {
                            /* Disarm the OSS when neither ND or OD timer is active */
                            this.MySafetyState = SafetyStates.None;
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.TssArmed)
                    {
                        /* Check whether the maximum allowable distance between a TSS arming and trigger loop has been exceeded, and if so, reset the TSS */
                        if (this.tssndactive)
                        {
                            if (Train.trainlocation >= this.tssndlastlocation + this.loopmaxspacing || Train.trainlocation < this.tssndlastlocation - this.loopmaxspacing)
                            {
                                /* The TSS ND timer has expired and no matching trigger loop has been detected, so the train has not encountered a valid TSS */
                                this.tssndactive = false;
                                this.tssndlastlocation = 0;
                            }
                        }
                        if (this.tssodactive)
                        {
                            if (Train.trainlocation >= this.tssodlastlocation + this.loopmaxspacing || Train.trainlocation < this.tssodlastlocation - this.loopmaxspacing)
                            {
                                /* The TSS OD timer has expired and no matching trigger loop has been detected, so the train has not encountered a valid TSS */
                                this.tssodactive = false;
                                this.tssodlastlocation = 0;
                            }
                        }
                        if (!this.tssndactive && !this.tssodactive)
                        {
                            /* Disarm the TSS when neither ND or OD detection is present */
                            this.MySafetyState = SafetyStates.None;
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.TemporaryOverride)
                    {
                        /* The TPWS Temporary Override is active */
                        if (tpwsoverrideblinkrate != -1)
                        {
                            this.overrideblinktimer = this.overrideblinktimer + (int)data.ElapsedTime.Milliseconds;
                            if (this.overrideblinktimer >= 0 && overrideblinktimer < this.tpwsoverrideblinkrate)
                            {
                                if (this.twpsoverrideindicator != -1)
                                {
                                    this.Train.Panel[twpsoverrideindicator] = 1;
                                }
                            }
                            else if (this.indicatorblinktimer >= (this.brakeindicatorblinkrate * 2))
                            {
                                this.indicatorblinktimer = 0;
                            }
                        }
                        else
                        {
                            this.Train.Panel[twpsoverrideindicator] = 1;
                        }
                        /*  Handle the countdown timer */
                        this.overridetimer = this.overridetimer - (int)data.ElapsedTime.Milliseconds;
                        if (this.overridetimer < 0)
                        {
                            this.overridetimer = (int)this.overridetimeout;
                            this.MySafetyState = SafetyStates.None;
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.TssBrakeDemand)
                    {
                        /* A TPWS Brake Demand has been issued.
                         * Increment the blink timer to enable the Brake Demand indicator to flash */
                        if (Train.tractionmanager.currentbrakenotch != this.Train.Specs.BrakeNotches + 1)
                        {
                            Train.DebugLogger.LogMessage("Emergency brakes were demanded by the TPWS Trainstop system");
                            Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                        }
                        this.indicatorblinktimer = this.indicatorblinktimer + (int)data.ElapsedTime.Milliseconds;
                        if (this.indicatorblinktimer >= 0 && indicatorblinktimer < this.brakeindicatorblinkrate)
                        {
                            if (brakedemandindicator != -1)
                            {
                                this.Train.Panel[brakedemandindicator] = 1;
                            }
                        }
                        else if (this.indicatorblinktimer >= (this.brakeindicatorblinkrate * 2))
                        {
                            this.indicatorblinktimer = 0;
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.BrakeDemandAcknowledged)
                    {
                        /* The TPWS Brake Demand indication has been acknowledged by pressing the AWS Reset button,
                         * so stop the blinking light, wait for the train to stop, and start the timer */

                        //If the AWS triggered this, set it's state to warning acknowledged
                        if (Train.AWS.SafetyState == AWS.SafetyStates.TPWSAWSBrakeDemandIssued)
                        {
                            Train.AWS.Acknowlege();
                        }
                        if (brakedemandindicator != -1)
                        {
                            this.Train.Panel[brakedemandindicator] = 1;
                        }
                        if (Train.trainspeed == 0)
                        {
                            this.MySafetyState = SafetyStates.BrakesAppliedCountingDown;
                        }
                        
                    }
                    else if (this.MySafetyState == SafetyStates.BrakesAppliedCountingDown)
                    {
                        /* The train has been brought to a stand, so wait for the timeout to expire
                         * before stopping the TPWS safety intervention */
                        if (brakedemandindicator != -1)
                        {
                            this.Train.Panel[brakedemandindicator] = 1;
                        }

                        /*
                        if (Plugin.Diesel.Enabled) {
                            InterlockManager.DemandTractionPowerCutoff();
                        }
                         */
                        /* Handle the countdown timer */
                        this.brakesappliedtimer = this.brakesappliedtimer - (int)data.ElapsedTime.Milliseconds;
                        if (this.brakesappliedtimer < 0 && Train.Handles.PowerNotch == 0)
                        {
                            this.brakesappliedtimer = (int)this.brakesappliedtimeout;
                            this.MySafetyState = SafetyStates.None;
                            Train.tractionmanager.resetbrakeapplication();
                            //InterlockManager.RequestTractionPowerReset();
                        }
                    }
                    else if (this.MySafetyState == SafetyStates.SelfTest)
                    {
                        if (brakedemandindicator != -1)
                        {
                            this.Train.Panel[brakedemandindicator] = 1;
                        }
                        if (this.twpsoverrideindicator != -1)
                        {
                            this.Train.Panel[twpsoverrideindicator] = 1;
                        }
                        if (tpwsisolatedindicator != -1)
                        {
                            this.Train.Panel[tpwsisolatedindicator] = 1;
                        }
                    }
                    
                }

                else if (this.MySafetyState == SafetyStates.Isolated)
                {
                    /* The TPWS has been isolated */
                    if (tpwsisolatedindicator != -1)
                    {
                        this.Train.Panel[tpwsisolatedindicator] = 1;
                    }
                }

            }
        }

        /// <summary>Issues a TPWS brake demand</summary>
        internal void IssueBrakeDemand()
        {
            /* First, set the conditions necessary for this method to succeed */
            if (base.OperativeState != OperativeStates.Failed)
            {
                if (this.MySafetyState == SafetyStates.TemporaryOverride)
                {
                    /* If the TPWS TSS Override timer is active, ignore this brake demand, and instead, reset the TPWS */
                    this.Reset();
                }
                else if (this.MySafetyState != SafetyStates.Isolated)
                {
                    /* Only set a brake demand state, if a brake demand isn't already in effect */
                    if (this.MySafetyState != SafetyStates.TssBrakeDemand && this.MySafetyState != SafetyStates.BrakeDemandAcknowledged)
                    {
                        /* Reset any remaining active OSS timers or TSS detection states */
                        this.Reinitialise(InitializationModes.OnService);
                        /* Issue the brake demand */
                        this.MySafetyState = SafetyStates.TssBrakeDemand;
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);


                        /* Raise an event signalling that a TPWS Brake Demand has been made, for event subscribers (such as the AWS). */
                        if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerActive ||
                            Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
                        {
                            /* AWS initiated this (using this event leads to the AWS warning horn not being suppressed) */
                            Train.AWS.handleawstpwsbrakedemand();
                        }
                        else
                        {
                            /* A TPWS sensor intiated this (using this event, leads to the AWS warning horn being suppressed */
                            Train.AWS.handleawstssbrakedemand();
                        }

                    }
                }
            }
        }

        /// <remarks>This method should be called via the SetBeacon() method, upon passing a TPWS OSS arming induction loop.</remarks>
        /// <remarks>This method can also be used to invoke legacy OSS speed limit behaviour.</remarks>
        internal void ArmOss(int frequency)
        {
            /* First, set the conditions necessary for this method to succeed */
            
            {
                /* Next check the necessary safety system states before processing - we don't want OSS to be
                 * activated if there's a brake demand already in effect */
                if (this.MySafetyState != SafetyStates.Isolated &&
                    this.MySafetyState != SafetyStates.TssBrakeDemand && this.MySafetyState != SafetyStates.BrakeDemandAcknowledged)
                {
                    /* Process legacy behaviour as well as prototypical arming frequencies */
                    if (frequency < 60000)
                    {
                        /* Legacy OSS non-timer behaviour */
                        if (this.MySafetyState != SafetyStates.OssArmed && this.MySafetyState != SafetyStates.LegacyOssArmed)
                        {
                            this.osslastspeed = (double)frequency;
                            this.MySafetyState = SafetyStates.LegacyOssArmed;
                            
                        }
                    }
                    else if (frequency == 64250 && !this.ossndtimeractive)
                    {
                        /* New prototypical OSS arming behaviour - f1 normal direction frequency */
                        this.ossndtimer = 0;
                        this.ossndtimeractive = true;
                        this.MySafetyState = SafetyStates.OssArmed;
                        
                    }
                    else if (frequency == 64750 && !this.ossodtimeractive)
                    {
                        /* New prototypical OSS arming behaviour - f4 opposite direction frequency */
                        this.ossodtimer = 0;
                        this.ossodtimeractive = true;
                        this.MySafetyState = SafetyStates.OssArmed;
                        
                    }

                    /* Next, process new prototypical OSS trigger frequencies */
                    if (frequency == 65250)
                    {
                        /* New prototypical OSS trigger behaviour - f2 normal direction frequency */
                        if (this.ossndtimer > 0 && this.ossndtimer <= this.osstimeout)
                        {
                            /* The OSS ND timer is still active, so the train is travelling too fast - reset the OSS ND timer and issue an OSS brake demand */
                            this.ossndtimer = 0;
                            this.ossndtimeractive = false;
                            this.IssueBrakeDemand();
                            
                        }
                    }
                    else if (frequency == 65750)
                    {
                        /* New prototypical OSS trigger behaviour - f5 opposite direction frequency */
                        if (this.ossodtimer > 0 && this.ossodtimer <= this.osstimeout)
                        {
                            /* The OSS OD timer is still active, so the train is travelling too fast - reset the OSS OD timer and issue an OSS brake demand */
                            this.ossodtimer = 0;
                            this.ossodtimeractive = false;
                            this.IssueBrakeDemand();
                            
                        }
                    }
                }
            }
        }

        /// <remarks>This method should be called via the SetBeacon() method, upon passing a TPWS TSS arming induction loop.</remarks>
        /// <remarks>This method can also be used to invoke legacy TSS behaviour.</remarks>
        internal void ArmTss(int frequency, double location)
        {
            /* First, set the conditions necessary for this method to succeed */
            
            {
                /* Next check the necessary safety system states before processing - we don't want TSS to be
                 * activated if there's a brake demand already in effect */
                if (this.MySafetyState != SafetyStates.Isolated &&
                    this.MySafetyState != SafetyStates.TssBrakeDemand && this.MySafetyState != SafetyStates.BrakeDemandAcknowledged)
                {
                    if (frequency < 60000)
                    {
                        /* Legacy TSS non-timer behaviour */
                        this.IssueBrakeDemand();
                    }
                    else if (frequency == 66250 && !this.tssndactive)
                    {
                        /* New prototypical TSS arming behaviour - f3 normal direction frequency */
                        this.tssndlastlocation = location;
                        this.tssndactive = true;
                        this.MySafetyState = SafetyStates.TssArmed;
                    }
                    else if (frequency == 66750 && !this.tssodactive)
                    {
                        /* New prototypical TSS arming behaviour - f6 opposite direction frequency */
                        this.tssodlastlocation = location;
                        this.tssodactive = true;
                        this.MySafetyState = SafetyStates.TssArmed;
                    }

                    /* Next, process new prototypical TSS trigger frequencies */
                    if (frequency == 65250)
                    {
                        /* New prototypical TSS trigger behaviour - f2 normal direction frequency */
                        if (this.tssndactive && Train.trainlocation <= (this.tssndlastlocation + this.loopmaxspacing))
                        {
                            /* The TSS ND detection is still active, so this is a valid TSS - reset the state and issue a TSS brake demand */
                            this.tssndactive = false;
                            this.tssndlastlocation = 0;
                            this.IssueBrakeDemand();
                        }
                    }
                    else if (frequency == 65750)
                    {
                        /* New prototypical TSS trigger behaviour - f5 opposite direction frequency */
                        if (this.tssodactive && Train.trainlocation <= (this.tssodlastlocation + this.loopmaxspacing))
                        {
                            /* The TSS OD detection is still active, so this is a valid TSS - reset the state and issue a TSS brake demand */
                            this.tssodactive = false;
                            this.tssodlastlocation = 0;
                            this.IssueBrakeDemand();
                        }
                    }
                }
            }
        }

        internal void SelfTest()
        {
            this.MySafetyState = SafetyStates.SelfTest;
        }
            
        }



    }
