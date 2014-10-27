using System;
using OpenBveApi.Runtime;


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

        /// <summary>Default paramaters</summary>
        /// Used if no value is loaded from the config file
        internal int overspeedcontrol = 0;
        internal double warningspeed = -1;
        internal double overspeed = 1000;
        internal double safespeed = 0;
        internal int overspeedindicator = -1;
        internal int overspeedalarm = -1;
        internal double overspeedtime = 0;

        internal string vigilancetimes = "60000";
        internal int vigilanceautorelease = 0;
        internal int vigilancecancellable = 0;
        internal double vigilancedelay1 = 3000;
        internal double vigilancedelay2 = 3000;
        internal int vigilancelamp = -1;
        internal int draenabled = -1;
        internal int drastartstate = -1;
        internal int draindicator = -1;
        internal int independantvigilance = 0;
        internal double vigilanceinactivespeed = 0;
        /// <summary>Timers</summary>
        internal double overspeedtimer;
        internal int deadmanshandle = 0;
        internal double deadmanstimer;

        internal double deadmansalarmtimer;
        internal double deadmansbraketimer;

        //Sound Indicies
        internal int vigilancealarm = -1;


        int[] vigilancearray;

        /// <summary>Gets the current state of the deadman's handle.</summary>
        internal DeadmanStates DeadmansHandle
        {
            get { return this.DeadmansHandleState; }
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
                InternalFunctions.LogError("vigilancetimes");
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
                tractionmanager.demandpowercutoff();
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
                                        tractionmanager.resetbrakeapplication();
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
                            tractionmanager.demandbrakeapplication();
                            //If we auto-release on coming to a full-stop
                            if (vigilanceautorelease != 0 && Train.trainspeed == 0)
                            {
                                tractionmanager.resetbrakeapplication();
                                deadmansalarmtimer = 0.0;
                                deadmansbraketimer = 0.0;
                                deadmanstimer = 0.0;
                            }
                        }



                    }

                }

                //Consequences
                if (Train.overspeedtripped == true)
                {
                    //Overspeed has tripped, apply service brakes
                    tractionmanager.demandbrakeapplication();
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
    }
}
