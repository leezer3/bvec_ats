using System;
using System.Collections;
using OpenBveApi.Runtime;


namespace Plugin
{
    /// <summary>Represents the overspeed, deadman's handle and DRA vigilance devices.</summary>
    internal class vigilance : Device
    {

        // --- members ---

        /// <summary>The underlying train.</summary>
        private Train Train;
        //Internal Variables
        internal double vigilancetime;
        internal int trainspeed;

        /// <summary>Default paramaters</summary>
        /// Used if no value is loaded from the config file
        internal double overspeedcontrol = 0;
        internal double warningspeed = -1;
        internal double overspeed = 1000;
        internal double safespeed = 0;
        internal double overspeedindicator = -1;
        internal double overspeedalarm = -1;
        internal double overspeedtime = 0;

        internal string vigilancetimes = "60000";
        internal double vigilanceautorelease = 0;
        internal double vigilancecancellable = 0;
        internal double vigilancelamp = -1;
        internal double draenabled = -1;
        internal double drastartstate = -1;
        internal double draindicator = -1;
        internal double independantvigilance = 0;
        /// <summary>Timers</summary>
        internal double overspeedtimer;
        internal double deadmanshandle = 0;
        internal double deadmanstimer;
        internal bool debug;

        //Sound Indicies
        internal double vigilancealarm = -1;
        

        int[] vigilancearray;
        
        //Old Code

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
            string[] splitvigilancetimes = vigilancetimes.Split(',');
            vigilancearray = new int[splitvigilancetimes.Length];
            for (int i = 0; i < vigilancearray.Length; i++)
            {
                vigilancearray[i] = Int32.Parse(splitvigilancetimes[i]);
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
            }
            Train.deadmanstripped = false;
            Train.overspeedtripped = false;

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
                            if (this.overspeedtimer >= (int)overspeedtime)
                            {
                                //Apply max brake notches
                                Train.overspeedtripped = true;
                                if (data.Vehicle.Speed.KilometersPerHour == 0)
                                {
                                    this.overspeedtimer = 0.0;
                                    if (vigilanceautorelease == 0)
                                    {
                                        Train.overspeedtripped = false;
                                        tractionmanager.resetbrakeapplication();
                                    }
                                    
                                }
                            }
                            else
                            {

                                //Twiddle for the moment
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
                        
                        //Elapse Timer
                        this.deadmanstimer += data.ElapsedTime.Milliseconds;
                        //If timer is greater than time and tripped is false
                        if (this.deadmanstimer > vigilancetime && Train.deadmanstripped == false)
                        {
                            //Trip deadman's switch
                            Train.deadmanstripped = true;
                            debug = false;
                        }
                        //If vigilance is cancellable whilst moving and timer has been reset, reset the trip
                        else if (vigilancecancellable != 0 && Train.deadmanstripped == true && this.deadmanstimer < vigilancetime)
                        {
                            Train.deadmanstripped = false;
                        }
                        //Otherwise Unblock
                        else
                        {
                            if (data.Vehicle.Speed.KilometersPerHour <= safespeed)
                            {
                                if (safespeed == 0 && vigilanceautorelease == 1)
                                {
                                    //Automatically release deadman's handle on stop
                                    Train.deadmanstripped = false;
                                }
                                this.deadmanstimer = 0.0;
                            }


                        }
                    }
                    else
                    {
                        //Deadman's Handle disabled, reset the timer
                        this.deadmanstimer = 0.0;
                        Train.deadmanstripped = false;

                    }


                }
                
            }

            //Consequences
            if (Train.overspeedtripped == true)
            {
                //Overspeed has tripped, apply service brakes
                tractionmanager.demandbrakeapplication();
            }
            if (Train.deadmanstripped == true)
            {
                //Overspeed has tripped, apply service brakes
                tractionmanager.demandbrakeapplication();
            }
            if (Train.drastate == true)
            {
                //DRA is enabled, cut the power
                data.Handles.PowerNotch = 0;
            }
            {
                //Set Panel Variables
                if (draindicator != -1)
                {
                    if (Train.drastate == true)
                    {
                        this.Train.Panel[(int)(draindicator)] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(int)(draindicator)] = 0;
                    }
                }
                if (overspeedindicator != -1)
                {
                    if (Train.overspeedtripped == true || trainspeed > warningspeed)
                    {
                        this.Train.Panel[(int)(overspeedindicator)] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(int)(overspeedindicator)] = 0;
                    }
                }
                if (vigilancelamp != -1)
                {
                    if (Train.deadmanstripped == true)
                    {
                        this.Train.Panel[(int)(vigilancelamp)] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(int)(vigilancelamp)] = 0;
                    }
                }
                if (vigilancealarm != -1)
                    if (Train.deadmanstripped == true)
                    {
                        SoundManager.Play((int)vigilancealarm, 1.0, 1.0, true);
                    }
                    else
                    {
                        SoundManager.Stop((int)vigilancealarm);
                    }
                
            }
        }
    }
}
