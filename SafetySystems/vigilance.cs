using System;
using System.Globalization;
using OpenBveApi.Runtime;


namespace Plugin
{
    /// <summary>Represents the overspeed, deadman's handle and DRA vigilance devices.</summary>
    internal partial class Vigilance : Device
    {

        // --- members ---

        /// <summary>The underlying train.</summary>
        private readonly Train Train;
        //Internal Variables
        internal double vigilancetime;

        internal DeadmanStates DeadmansHandleState;
        internal VigilanteStates VigilanteState;

	    internal OverspeedMonitor OverspeedDevice;

        /// <summary>Default paramaters</summary>
        /// Used if no value is loaded from the config file
        /// <summary>Stores the vigilance times for each power notch</summary>
        internal string vigilancetimes = "60000";
        /// <summary>Defines whether a vigilance intervention may be automatically released</summary>
        internal bool AutoRelease = false;
        /// <summary>Defines whether a vigilance intervention is cancellable</summary>
        internal int vigilancecancellable = 0;
        internal double vigilancedelay1 = 3000;
        internal double vigilancedelay2 = 3000;
        /// <summary>The panel index for the vigilance lamp</summary>
        internal int vigilancelamp = -1;
        /// <summary>Stores whether a Drivers Reminder Appliance [DRA] is fitted</summary>
        internal bool DRAEnabled = false;
        /// <summary>Defines the starting state for the DRA</summary>
        internal int DRAStartState = -1;
        /// <summary>The panel index for the DRA</summary>
        internal int DRAIndicator = -1;
        /// <summary>Defines whether the vigilance timer can be reset by any key, or only the vigilance key</summary>
        internal int independantvigilance = 0;
        /// <summary>The speed below which vigilance is inactive</summary>
        internal double vigilanceinactivespeed = 0;
        /// <summary>Timers</summary>
        internal int deadmanshandle = 0;
        internal double deadmanstimer;

        internal double deadmansalarmtimer;
        internal double deadmansbraketimer;

        //The timer index for the Italian Vigilante device
        internal double vigilanteTimer;
        internal bool vigilanteEnabled;

        //Sound Indicies
        /// <summary>The sound index for the audible vigilance alarm</summary>
        internal int vigilancealarm = -1;


        int[] vigilancearray;

        /// <summary>Gets the current state of the deadman's handle.</summary>
        internal DeadmanStates DeadmansHandle
        {
            get { return this.DeadmansHandleState; }
        }

        /// <summary>Gets the current state of the Vigilante Device.</summary>
        internal VigilanteStates VigilanteDevice
        {
            get { return this.VigilanteState; }
        }

        // --- constructors ---

        /// <summary>Creates a new instance of this system.</summary>
        /// <param name="train">The train.</param>
        internal Vigilance(Train train)
        {
            this.Train = train;
			OverspeedDevice = new OverspeedMonitor(train);
        }

        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            //Set Starting Paramaters Here

            //Timers to zero
            deadmanstimer = 0.0;
            //Split vigilance times into an array
            try
            {
                string[] splitvigilancetimes = vigilancetimes.Split(',');
                vigilancearray = new int[splitvigilancetimes.Length];
                for (int i = 0; i < vigilancearray.Length; i++)
                {
                    vigilancearray[i] = Int32.Parse(splitvigilancetimes[i], NumberStyles.Number, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                InternalFunctions.LogError("vigilancetimes",0);
            }
            //
            if (DRAEnabled == false || DRAStartState == -1)
            {
                Train.drastate = false;
            }
            else
            {
                Train.drastate = true;
                Train.TractionManager.DemandPowerCutoff("Power cutoff was demanded by the DRA");
            }
            DeadmansHandleState = DeadmanStates.None;
        }



        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
			OverspeedDevice.Update(data.ElapsedTime.Milliseconds);
	        {
		        //Deadman's Handle
		        if (deadmanshandle != 0)
		        {
			        //Initialise and set the start state
			        if (Train.StartupSelfTestManager != null &&
			            Train.StartupSelfTestManager.SequenceState != StartupSelfTestManager.SequenceStates.Initialised)
			        {
				        //Startup self-test has not been performed, no systems active
				        DeadmansHandleState = DeadmanStates.None;
			        }
			        else if (Train.ElectricEngine != null && Train.ElectricEngine.FrontPantograph.State != PantographStates.OnService &&
			                 Train.ElectricEngine.RearPantograph.State != PantographStates.OnService && Train.CurrentSpeed == 0)
			        {
				        //Stationary with no available pantographs
				        DeadmansHandleState = DeadmanStates.None;
			        }
			        else if (vigilanceinactivespeed == -2 && Train.Handles.Reverser == 0)
			        {
				        //Set to no action if inactive speed is -2 & in neutral
				        DeadmansHandleState = DeadmanStates.None;
			        }
			        else if (vigilanceinactivespeed == -2 && Train.Handles.Reverser != 0)
			        {
				        //Otherwise set to the timer state
				        DeadmansHandleState = DeadmanStates.OnTimer;
			        }
			        else if (vigilanceinactivespeed == -1)
			        {
				        //If inactive speed is -1 always set to the timer state
				        DeadmansHandleState = DeadmanStates.OnTimer;
			        }
			        else if (Train.CurrentSpeed < vigilanceinactivespeed && DeadmansHandleState == DeadmanStates.OnTimer)
			        {
				        //If train speed is than the inactive speed and we're in the timer mode
				        //Set to no action
				        DeadmansHandleState = DeadmanStates.None;
			        }
			        else if (Train.CurrentSpeed > vigilanceinactivespeed && DeadmansHandleState == DeadmanStates.None)
			        {
				        //Set to the timer state
				        DeadmansHandleState = DeadmanStates.OnTimer;
			        }

			        //Calculate vigilance time from the array
			        int vigilancelength = vigilancearray.Length;
			        if (Train.Handles.PowerNotch == 0)
			        {
				        vigilancetime = vigilancearray[0];
			        }
			        else if (Train.Handles.PowerNotch <= vigilancelength)
			        {
				        vigilancetime = vigilancearray[(Train.Handles.PowerNotch - 1)];
			        }
			        else
			        {
				        vigilancetime = vigilancearray[(vigilancelength - 1)];
			        }



			        if (DeadmansHandleState == DeadmanStates.OnTimer)
			        {
				        //Reset other timers
				        deadmansalarmtimer = 0.0;
				        deadmansbraketimer = 0.0;
				        //Elapse Timer
				        this.deadmanstimer += data.ElapsedTime.Milliseconds;
				        if (this.deadmanstimer > vigilancetime)
				        {
					        DeadmansHandleState = DeadmanStates.TimerExpired;
				        }
			        }
			        else if (DeadmansHandleState == DeadmanStates.TimerExpired)
			        {
				        //Start the next timer
				        deadmansalarmtimer += data.ElapsedTime.Milliseconds;
				        if (deadmansalarmtimer > vigilancedelay1)
				        {
					        DeadmansHandleState = DeadmanStates.OnAlarm;
				        }
			        }
			        else if (DeadmansHandleState == DeadmanStates.OnAlarm)
			        {
				        //Trigger the alarm sound and move on
				        if (vigilancealarm != -1)
				        {
					        SoundManager.Play(vigilancealarm, 1.0, 1.0, true);
				        }
				        DeadmansHandleState = DeadmanStates.AlarmTimer;
			        }
			        else if (DeadmansHandleState == DeadmanStates.AlarmTimer)
			        {
				        //Start the next timer
				        deadmansbraketimer += data.ElapsedTime.Milliseconds;
				        if (deadmansbraketimer > vigilancedelay2)
				        {
					        DeadmansHandleState = DeadmanStates.BrakesApplied;
				        }
			        }
			        else if (DeadmansHandleState == DeadmanStates.BrakesApplied)
			        {
				        //Demand brake application
				        Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches + 1, "Brake application demanded by the deadman's handle");
				        //If we auto-release on coming to a full-stop
				        if (AutoRelease && Train.CurrentSpeed == 0)
				        {
					        Train.TractionManager.ResetBrakeApplication();
					        deadmansalarmtimer = 0.0;
					        deadmansbraketimer = 0.0;
					        deadmanstimer = 0.0;
					        DeadmansHandleState = DeadmanStates.OnTimer;
				        }
			        }



		        }

	        }
	        if (Train.SCMT != null)
		        {
			        if (vigilanteEnabled && SCMT.testscmt == 4)
			        {
				        if (Train.CurrentSpeed > 2 && VigilanteState == VigilanteStates.None)
				        {
					        VigilanteState = VigilanteStates.AlarmSounding;
				        }
				        else if (VigilanteState == VigilanteStates.AlarmSounding)
				        {
					        if (vigilancealarm != -1)
					        {
						        SoundManager.Play(vigilancealarm, 1.0, 1.0, true);
					        }
					        if (Train.CurrentSpeed != 0)
					        {
						        vigilanteTimer += data.ElapsedTime.Milliseconds;
						        if (vigilanteTimer > vigilancedelay1)
						        {
							        VigilanteState = VigilanteStates.EbApplied;
						        }
					        }
				        }
				        else if (VigilanteState == VigilanteStates.EbApplied)
				        {
					        vigilanteTimer = 0.0;
					        Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches + 1, "Brake application demanded by the SCMT vigilante");
					        if (vigilancealarm != -1)
					        {
						        SoundManager.Stop(vigilancealarm);
					        }
					        if (SCMT.tpwswarningsound != -1)
					        {
						        SoundManager.Play(SCMT.tpwswarningsound, 1.0, 1.0, true);
					        }
				        }
				        else if (VigilanteState == VigilanteStates.OnService)
				        {
					        vigilanteTimer = 0.0;
					        if (Train.CurrentSpeed == 0)
					        {
						        VigilanteState = VigilanteStates.None;
					        }
				        }
			        }

		        }

	        {
		        //Set Panel Variables
		        if (DRAIndicator != -1)
		        {
			        if (Train.drastate)
			        {
				        this.Train.Panel[(DRAIndicator)] = 1;
			        }
			        else
			        {
				        this.Train.Panel[(DRAIndicator)] = 0;
			        }
		        }

		        if (vigilancelamp != -1)
		        {
			        if (DeadmansHandleState == DeadmanStates.None || DeadmansHandleState == DeadmanStates.OnTimer)
			        {
				        this.Train.Panel[(vigilancelamp)] = 0;
			        }
			        else
			        {
				        this.Train.Panel[(vigilancelamp)] = 1;
			        }
			        if (vigilanteEnabled)
			        {
				        if (VigilanteState == VigilanteStates.AlarmSounding || VigilanteState == VigilanteStates.EbApplied)
				        {
					        this.Train.Panel[(vigilancelamp)] = 1;
				        }
				        else
				        {
					        this.Train.Panel[(vigilancelamp)] = 0;
				        }
			        }
		        }

	        }

        }

        /// <summary>Call this function from the traction manager to attempt to reset a Vigilante intervention</summary>
        internal void VigilanteReset()
        {
            if (Train.CurrentSpeed == 0 && VigilanteState == VigilanteStates.EbApplied)
            {
                if (SCMT.tpwswarningsound != -1)
                {
                    SoundManager.Stop(SCMT.tpwswarningsound);
                }
                Train.TractionManager.ResetBrakeApplication();
                SCMT.spiarossi_act = false;
                VigilanteState = VigilanteStates.None;
            }
        }
    }
}
