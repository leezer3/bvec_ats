using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Manages the startup and self-test sequence.</summary>
	internal partial class StartupSelfTestManager : Device {
		
		// TODO: It is not yet possible for the startup/self-test procedure to fail rather than succeed.
        private Train Train;
		// members

        internal bool firststart;
		/// <summary>The startup self-test sequence duration in milliseconds.</summary>
		private static int SequenceDuration = 1700;
		/// <summary>The state of the train systems with regard to the startup self-test procedure.</summary>
		private static SequenceStates MySequenceState = SequenceStates.Pending;
		/// <summary>The timer used during the startup self-test sequence.</summary>
		private static int MySequenceTimer = SequenceDuration;

        internal static int sunflowerstate;
		
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
            if (Train.AWS.enabled == false && Train.TPWS.enabled == false)
            {
                //Set to initialised if no AWS/ TPWS is installed
                MySequenceState = SequenceStates.Initialised;
                sunflowerstate = 0;
                AWS.startuphorntriggered = false;
            }
            else if (mode == InitializationModes.OnService)
            {
                MySequenceTimer = SequenceDuration;
                MySequenceState = SequenceStates.Initialised;
                sunflowerstate = 0;
                AWS.startuphorntriggered = false;
            }
            else if (mode == InitializationModes.OnEmergency)
            {
                MySequenceTimer = SequenceDuration;
                MySequenceState = SequenceStates.Pending;
                sunflowerstate = 1;
                AWS.startuphorntriggered = false;
            }
            else if (mode == InitializationModes.OffEmergency)
            {
                MySequenceTimer = SequenceDuration;
                MySequenceState = SequenceStates.Pending;
                sunflowerstate = 1;
                AWS.startuphorntriggered = false;
            }
        }

		// methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal static void Reinitialise(InitializationModes mode) {
			if (mode == InitializationModes.OnService) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Initialised;
                sunflowerstate = 0;
                AWS.startuphorntriggered = false;
			} else if (mode == InitializationModes.OnEmergency) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Pending;
                sunflowerstate = 1;
                AWS.startuphorntriggered = false;
			} else if (mode == InitializationModes.OffEmergency) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Pending;
                sunflowerstate = 1;
                AWS.startuphorntriggered = false;
			}
		}
		
		/// <summary>This should be called during each Elapse() call.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="handles">The handles of the cab.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal override void Elapse(ElapseData data, ref bool blocking){
			if (StartupSelfTestManager.MySequenceState == StartupSelfTestManager.SequenceStates.Pending) {
				Train.selftest = false;
				/* Check the reverser state to see if the master switch has been set to on */
				if (Train.Handles.Reverser == 1 || Train.Handles.Reverser == -1) {
					StartupSelfTestManager.MySequenceState = StartupSelfTestManager.SequenceStates.WaitingToStart;
				}
			} else if (StartupSelfTestManager.MySequenceState == StartupSelfTestManager.SequenceStates.WaitingToStart) {
				if (Train.Handles.Reverser == 0) {
					/* Turn the master switch on, and begin the startup and self-test procedure */
					Train.selftest = true;
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
                /* Hold the brakes on until the AWS button is depressed */
                if (MySequenceState == SequenceStates.AwaitingDriverInteraction)
                {
                    tractionmanager.demandbrakeapplication();
                }
                else if (MySequenceState == SequenceStates.Finalising)
                {
                    if (Train.AWS.awswarningsound != -1)
                    {
                        if (SoundManager.IsPlaying(Train.AWS.awswarningsound))
                        {
                            SoundManager.Stop(Train.AWS.awswarningsound);
                        }
                    }
                    MySequenceState = SequenceStates.Initialised;
                    tractionmanager.resetbrakeapplication();
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
                    if (Train.AWS.enabled == true)
                    {
                        Train.AWS.OnStartUp(sunflowerstate);
                    }
                    if (Train.TPWS.enabled)
                    {
                        Train.TPWS.Initialize(InitializationModes.OnService);
                    }
                }
            }
		}

        internal void driveracknowledge()
        {
            MySequenceState = SequenceStates.Finalising;
        }
	}
}
