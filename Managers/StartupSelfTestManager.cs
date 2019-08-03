using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Manages the startup and self-test sequence.</summary>
	internal partial class StartupSelfTestManager : Device {
		
		// TODO: It is not yet possible for the startup/self-test procedure to fail rather than succeed.
        private readonly Train Train;
		// members

        internal bool firststart;

	    /// <summary>The startup self-test sequence duration in milliseconds.</summary>
	    private const int SequenceDuration = 1700;

	    /// <summary>The state of the train systems with regard to the startup self-test procedure.</summary>
		private static SequenceStates MySequenceState = SequenceStates.Pending;
		/// <summary>The timer used during the startup self-test sequence.</summary>
		private static int MySequenceTimer = SequenceDuration;

        internal static AWS.SunflowerStates SunflowerState;
		
		// properties
		
		/// <summary>Gets the state of the train systems with regard to the startup self-test procedure.</summary>
		internal SequenceStates SequenceState {
			get { return MySequenceState; }
		}
		
		/// <summary>Gets the current self-test sequence countdown timer value.</summary>
		internal static int SequenceTimer {
			get { return MySequenceTimer; }
		}
		
        internal StartupSelfTestManager(Train train)
        {
            this.Train = train;
        }

        internal override void Initialize(InitializationModes mode)
        {
            if (Train.WesternDiesel != null)
            {
                Train.AWS.Enabled = false;
                Train.TPWS.Enabled = false;
                return;
            }
            if (Train.AWS.Enabled == false && Train.TPWS.Enabled == false)
            {
                //Set to initialised if no AWS/ TPWS is installed
                MySequenceState = SequenceStates.Initialised;
                SunflowerState = AWS.SunflowerStates.Clear;
                AWS.startuphorntriggered = false;
            }
            else if (mode == InitializationModes.OnService)
            {
                MySequenceTimer = SequenceDuration;
                MySequenceState = SequenceStates.Initialised;
                SunflowerState = AWS.SunflowerStates.Clear;
                AWS.startuphorntriggered = false;
	            Train.MasterSwitch = true;
            }
            else if (mode == InitializationModes.OnEmergency)
            {
                MySequenceTimer = SequenceDuration;
                MySequenceState = SequenceStates.Pending;
                SunflowerState = AWS.SunflowerStates.Warn;
                AWS.startuphorntriggered = false;
            }
            else if (mode == InitializationModes.OffEmergency)
            {
                MySequenceTimer = SequenceDuration;
                MySequenceState = SequenceStates.Pending;
                SunflowerState = AWS.SunflowerStates.Warn;
                AWS.startuphorntriggered = false;
            }
        }

		// methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal void Reinitialise(InitializationModes mode) {
            if (Train.WesternDiesel != null)
            {
                Train.AWS.Enabled = false;
                Train.TPWS.Enabled = false;
                return;
            }
			if (mode == InitializationModes.OnService) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Initialised;
                SunflowerState = AWS.SunflowerStates.Warn;
                AWS.startuphorntriggered = false;
				Train.MasterSwitch = true;
			} else if (mode == InitializationModes.OnEmergency) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Pending;
                SunflowerState = AWS.SunflowerStates.Clear;
                AWS.startuphorntriggered = false;
			} else if (mode == InitializationModes.OffEmergency) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Pending;
                SunflowerState = AWS.SunflowerStates.Clear;
                AWS.startuphorntriggered = false;
			}
		}

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking){
            //The Western requires special handling- Return if AWS has not been switched in from the cab
            if (Train.WesternDiesel != null && Train.AWS.Enabled == false)
            {
                if (Train.WesternDiesel.StartupManager.StartupState == WesternStartupManager.SequenceStates.AWSOnline)
                {
                    //Enable AWS if the Western has switched it online
                    Train.AWS.Enabled = true;
                }
                else
                {
                    //Otherwise, we don't want to do any processing so back out
                    //Doing anything else messes up the timing of the self-test sequence
                    return;
                }
            }
			if (MySequenceState == SequenceStates.Pending)
			{
				Train.MasterSwitch = false;
				Train.selftest = false;
				/* Check the reverser state to see if the master switch has been set to on */
				if (Train.Handles.Reverser == 1 || Train.Handles.Reverser == -1) {
					MySequenceState = SequenceStates.WaitingToStart;
				}
			} else if (MySequenceState == SequenceStates.WaitingToStart) {
				if (Train.Handles.Reverser == 0) {
					/* Turn the master switch on, and begin the startup and self-test procedure */
					Train.selftest = true;
					Train.MasterSwitch = true;
					MySequenceState = SequenceStates.Initialising;
					/* Start the in-cab blower */
//					if (PowerSupplyManager.SelectedPowerSupply != Plugin.MainBattery) {
//						Plugin.Fan.Reset();
//					}
					/* Place the Automatic Warning System, and Train Protection and Warning System, into self-test mode */
					Train.AWS.SelfTest();
					Train.TPWS.SelfTest();
                    
				}
            }
            else if (MySequenceState != SequenceStates.Initialised)
            {
                /* Make sure that the master switch is on after reinitialisation */
                Train.selftest = true;
	            Train.MasterSwitch = true;
				/* Hold the brakes on until the AWS button is depressed */
				if (MySequenceState == SequenceStates.AwaitingDriverInteraction)
                {
                    Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches, "Brake application was demanded by the startup self-test sequence");
                }
                else if (MySequenceState == SequenceStates.Finalising)
                {
                    if (Train.AWS.WarningSound != -1)
                    {
                        if (SoundManager.IsPlaying(Train.AWS.WarningSound))
                        {
                            SoundManager.Stop(Train.AWS.WarningSound);
                        }
                    }
                    MySequenceState = SequenceStates.Initialised;
                    Train.TractionManager.ResetBrakeApplication();
                }
                /* Lastly, decrement the timer */
                if (MySequenceState == SequenceStates.Initialising)
                {
                    MySequenceTimer = MySequenceTimer - (int)data.ElapsedTime.Milliseconds;
                    if (MySequenceTimer < 0)
                    {
                        MySequenceTimer = 0;
                        MySequenceState = SequenceStates.AwaitingDriverInteraction;
                    }
                }
            }
            else
            {
                if (this.firststart == false)
                {
                    this.firststart = true;
                    if (Train.AWS.Enabled == true)
                    {
                        Train.AWS.OnStartUp(SunflowerState);
                    }
                    if (Train.TPWS.Enabled)
                    {
                        Train.TPWS.Initialize(InitializationModes.OnService);
                    }
                }
            }
		}

        internal void DriverAcknowledge()
        {
            MySequenceState = SequenceStates.Finalising;
        }
	}
}
