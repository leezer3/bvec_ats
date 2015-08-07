using System;
using System.Data.Odbc;
using OpenBveApi.Runtime;
using OpenBveApi.Sounds;


namespace Plugin
{
    /// <summary>Represents the overspeed, deadman's handle and DRA vigilance devices.</summary>
    internal partial class vigilance : Device
    {

        // --- members ---

        /// <summary>The underlying train.</summary>
        private readonly Train Train;
        //Internal Variables
        internal double vigilancetime;
        internal int trainspeed;

        internal DeadmanStates DeadmansHandleState;
        internal VigilanteStates VigilanteState;

        /// <summary>Default paramaters</summary>
        /// Used if no value is loaded from the config file
        /// <summary>Stores the current type of overspeed control in use</summary>
        internal int overspeedcontrol = 0;
        /// <summary>The speed at which an overspeed warning will be triggered in km/h</summary>
        internal double warningspeed = -1;
        /// <summary>The speed at which an overspeed intervention will be triggered in km/h</summary>
        internal double overspeed = 1000;
        /// <summary>The speed at which an overspeed intervention will automatically be cancelled in km/h</summary>
        internal double safespeed = 0;
        /// <summary>The panel index of the overspeed indicator</summary>
        internal int overspeedindicator = -1;
        /// <summary>The sound index of the audible overspeed alarm</summary>
        internal int overspeedalarm = -1;
        /// <summary>The time for which you may be overspeed before an intervention is triggered</summary>
        internal double overspeedtime = 0;
        /// <summary>Stores the vigilance times for each power notch</summary>
        internal string vigilancetimes = "60000";
        /// <summary>Defines whether a vigilance intervention may be automatically released</summary>
        internal int vigilanceautorelease = 0;
        /// <summary>Defines whether a vigilance intervention is cancellable</summary>
        internal int vigilancecancellable = 0;
        internal double vigilancedelay1 = 3000;
        internal double vigilancedelay2 = 3000;
        /// <summary>The panel index for the vigilance lamp</summary>
        internal int vigilancelamp = -1;
        /// <summary>Stores whether a Drivers Reminder Appliance [DRA] is fitted</summary>
        internal int draenabled = -1;
        /// <summary>Defines the starting state for the DRA</summary>
        internal int drastartstate = -1;
        /// <summary>The panel index for the DRA</summary>
        internal int draindicator = -1;
        /// <summary>Defines whether the vigilance timer can be reset by any key, or only the vigilance key</summary>
        internal int independantvigilance = 0;
        /// <summary>The speed below which vigilance is inactive</summary>
        internal double vigilanceinactivespeed = 0;
        /// <summary>Timers</summary>
        internal double overspeedtimer;
        internal int deadmanshandle = 0;
        internal double deadmanstimer;

        internal double deadmansalarmtimer;
        internal double deadmansbraketimer;

        //The timer index for the Italian Vigilante device
        internal double vigilanteTimer;
        internal bool vigilante;
        internal bool vigilanteTripped;
        internal int vigilantePhase;

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
        internal vigilance(Train train)
        {
            this.Train = train;


        }

        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            //Set Starting Paramaters Here

            //Timers to zero
            overspeedtimer = 0.0;
            deadmanstimer = 0.0;
            //Split vigilance times into an array
            try
            {
                string[] splitvigilancetimes = vigilancetimes.Split(',');
                vigilancearray = new int[splitvigilancetimes.Length];
                for (int i = 0; i < vigilancearray.Length; i++)
                {
                    vigilancearray[i] = Int32.Parse(splitvigilancetimes[i]);
                }
            }
            catch
            {
                InternalFunctions.LogError("vigilancetimes",0);
            }
            //Set warning to max speed if not selected
            if (warningspeed == -1)
            {
                warningspeed = overspeed;
            }
            //
            if (draenabled == -1 || drastartstate == -1)
            {
                Train.drastate = false;
            }
            else
            {
                Train.drastate = true;
                Train.tractionmanager.demandpowercutoff();
            }
            Train.overspeedtripped = false;
            DeadmansHandleState = DeadmanStates.None;
        }



        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            trainspeed = (int)data.Vehicle.Speed.KilometersPerHour;
            {
                //Vigilance Devices
                {
                    //Overspeed Device
                    if (overspeedcontrol != 0)
                    {
                        if (Math.Abs(data.Vehicle.Speed.KilometersPerHour) > overspeed || overspeedtimer > (int)overspeedtime)
                        {

                            this.overspeedtimer += data.ElapsedTime.Seconds;
                            if (this.overspeedtimer >= (int)overspeedtime && Train.overspeedtripped == false)
                            {
                                //Apply max brake notches
                                Train.overspeedtripped = true;
                            }
                            else if (Train.overspeedtripped == true)
                            {
                                if (data.Vehicle.Speed.KilometersPerHour <= safespeed)
                                {
                                    this.overspeedtimer = 0.0;
                                    if (vigilanceautorelease != 0)
                                    {
                                        Train.overspeedtripped = false;
                                        Train.tractionmanager.resetbrakeapplication();
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Train.overspeedtripped == false)
                            {
                                //We aren't overspeed, reset the timeer
                                this.overspeedtimer = 0.0;
                            }

                        }
                    }
                    else
                    {
                        //Overspeeed disabled, reset the timer
                        this.overspeedtimer = 0.0;
                        Train.overspeedtripped = false;
                    }
                }


                {
                    //Deadman's Handle
                    if (deadmanshandle != 0)
                    {
                        //Initialise and set the start state
                        if (Train.StartupSelfTestManager.SequenceState != StartupSelfTestManager.SequenceStates.Initialised)
                        {
                            //Startup self-test has not been performed, no systems active
                            DeadmansHandleState = DeadmanStates.None;
                        }
                        else if (Train.electric != null && Train.electric.FrontPantographState != electric.PantographStates.OnService && Train.electric.RearPantographState != electric.PantographStates.OnService && trainspeed == 0)
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
                        else if (trainspeed < vigilanceinactivespeed && DeadmansHandleState == DeadmanStates.OnTimer)
                        {
                            //If train speed is than the inactive speed and we're in the timer mode
                            //Set to no action
                            DeadmansHandleState = DeadmanStates.None;
                        }
                        else if (trainspeed > vigilanceinactivespeed && DeadmansHandleState == DeadmanStates.None)
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
                            Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                            //If we auto-release on coming to a full-stop
                            if (vigilanceautorelease != 0 && Train.trainspeed == 0)
                            {
                                Train.tractionmanager.resetbrakeapplication();
                                deadmansalarmtimer = 0.0;
                                deadmansbraketimer = 0.0;
                                deadmanstimer = 0.0;
                            }
                        }



                    }

                }
                if (Train.SCMT != null)
                {
                    if (vigilante == true && SCMT.testscmt == 4)
                    {
                        if (Train.trainspeed > 2 && VigilanteState == VigilanteStates.None)
                        {
                            VigilanteState = VigilanteStates.AlarmSounding;
                        }
                        else if (VigilanteState == VigilanteStates.AlarmSounding)
                        {
                            if (vigilancealarm != -1)
                            {
                                SoundManager.Play(vigilancealarm, 1.0, 1.0, true);
                            }
                            if (Train.trainspeed != 0)
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
                            Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
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
                            if (Train.trainspeed == 0)
                            {
                                VigilanteState = VigilanteStates.None;
                            }
                        }
                    }
                
                }

                //Consequences
                if (Train.overspeedtripped == true)
                {
                    //Overspeed has tripped, apply service brakes
                    Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
                }

                {
                    //Set Panel Variables
                    if (draindicator != -1)
                    {
                        if (Train.drastate == true)
                        {
                            this.Train.Panel[(draindicator)] = 1;
                        }
                        else
                        {
                            this.Train.Panel[(draindicator)] = 0;
                        }
                    }
                    if (overspeedindicator != -1)
                    {
                        if (Train.overspeedtripped == true || trainspeed > warningspeed)
                        {
                            this.Train.Panel[(overspeedindicator)] = 1;
                        }
                        else
                        {
                            this.Train.Panel[(overspeedindicator)] = 0;
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
                        if (vigilante == true)
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
                if (overspeedalarm != -1)
                {
                    if (Train.overspeedtripped == true || trainspeed > warningspeed)
                    {
                        SoundManager.Play(overspeedalarm, 1.0, 1.0, true);
                    }
                    else
                    {
                        SoundManager.Stop(overspeedalarm);
                    }
                }
                
                    
                }
        }

        /// <summary>Call this function from the traction manager to attempt to reset a Vigilante intervention</summary>
        internal void VigilanteReset()
        {
            if (Train.trainspeed == 0 && VigilanteState == VigilanteStates.EbApplied)
            {
                if (SCMT.tpwswarningsound != -1)
                {
                    SoundManager.Stop(SCMT.tpwswarningsound);
                }
                Train.tractionmanager.resetbrakeapplication();
                SCMT.spiarossi_act = false;
                VigilanteState = VigilanteStates.None;
            }
        }
    }
}
