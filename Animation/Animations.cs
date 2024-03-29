﻿using System;
using OpenBveApi.Runtime;

namespace Plugin
{
    /// <summary>Provides a handler for complex animations such as valve gear.</summary>
    internal partial class Animations : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        //Variables


        private static DoorLightStates MyDoorLightState;

        /// <summary>Gets the current warning state of the flashing doors light.</summary>
        internal DoorLightStates DoorLightState
        {
            get { return MyDoorLightState; }
        }

        private static CylinderPuffStates CylinderPuffState_L;

        /// <summary>Gets the current warning state of the Automatic Warning System.</summary>
        internal CylinderPuffStates CylinderPuffStateL
        {
            get { return CylinderPuffState_L; }
        }

        private static CylinderPuffStates CylinderPuffState_R;

        /// <summary>Gets the current warning state of the Automatic Warning System.</summary>
        internal CylinderPuffStates CylinderPuffStateR
        {
            get { return CylinderPuffState_R; }
        }
        /// <summary>Stores the time for which we will be stopped (Minus 30 seconds for the flashing door light)</summary>
        internal static int stoptime;
        internal double doorlighttimer;
        internal double doorlighttimer2;
        /// <summary>Stores whether the flashing door light is currently lit</summary>
        internal bool doorlighton;
        internal static double departuretime;
        /// <summary>The panel variable for the flashing door light</summary>
        internal int doorlight = -1;


        /// <summary>Stores the current value of the headcode</summary>
        internal static int headcodestate;
        /// <summary>Stores the possible number of headcode states</summary>
        internal static int totalheadcodestates;
        /// <summary>The panel index of the headcode indicator</summary>
        internal int headcodeindex = -1;

        /// <summary>The variable for the left cylinder puff</summary>
        internal int cylinderpuff_L = -1;
        /// <summary>The variable for the right cylinder puff</summary>
        internal int cylinderpuff_R = -1;
        /// <summary>The Y variable for the valve gear motion [Right]</summary>
        internal int gear_Yvariable_R = -1;
        /// <summary>The Z variable for the valve gear motion [Right]</summary>
        internal int gear_Zvariable_R = -1;
        /// <summary>The Y variable for the valve gear motion [Left]</summary>
        internal int gear_Yvariable_L = -1;
        /// <summary>The Z variable for the valve gear motion [Left]</summary>
        internal int gear_Zvariable_L = -1;
        /// <summary>The radius of our connecting rod in millimeters.</summary>
        internal double rodradius = 0;
        /// <summary>The variable controlling the rotation of our wheel.</summary>
        internal int wheelrotation_variable = -1;
        /// <summary>The variable controlling Y movement of the piston crank crosshead [Left]</summary>
        internal int crankvariable_L = -1;
        /// <summary>The variable controlling Z rotation of the piston crank [Left]</summary>
        internal int crankrotation_L = -1;
        /// <summary>The variable controlling Y movement of the piston crank crosshead [Right]</summary>
        internal int crankvariable_R = -1;
        /// <summary>The variable controlling Z rotation of the piston crank [Right]</summary>
        internal int crankrotation_R = -1;
        /// <summary>The radius of the crank in millimeters.</summary>
        internal double crankradius = 0;
        /// <summary>The length of the crank in millimeters.</summary>
        internal double cranklength = 0;
        //rodradius , wheelradius & the animation stepper should all be set when animating valve gear
        //This keeps all three items fully in sync

        //Internal variables
        /// <summary>The Y position of the valve gear motion [Right]</summary>
        internal double gear_Ylocation_R;
        /// <summary>The Z position of the valve gear motion [Right]</summary>
        internal double gear_Zlocation_R;
        /// <summary>The Y position of the valve gear motion [Left]</summary>
        internal double gear_Ylocation_L;
        /// <summary>The Z position of the valve gear motion [Left]</summary>
        internal double gear_Zlocation_L;
        /// <summary>The Z position of the piston crank [Right]</summary>
        internal double cranklocation_R;
        internal double crankangle_R;
        internal double cranklocation_L;
        internal double crankangle_L;
        /// <summary>The circumference in millimeters</summary>
        internal double wheelcircumference;
        internal double wheelpercentage;
        internal double degreesturned;
        /// <summary>The distance travelled in meters</summary>
        internal double distancetravelled;
        
        /// <summary>Creates a new instance of this system.</summary>
        /// <param name="train">The train.</param>
        internal Animations(Train train)
        {
            this.Train = train;
        }

        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            //Set gear position to initial [12PM] position if all variables are correct
            //Calculate wheel circumference
            if (rodradius != 0)
            {
                wheelcircumference = 2 * Math.PI * rodradius;
            }
            if (gear_Yvariable_R != 0 && gear_Zvariable_R != 0 && rodradius != 0)
            {
                gear_Ylocation_R = rodradius;
                gear_Zlocation_R = 0;
            }
            if (gear_Yvariable_L != 0 && gear_Zvariable_L != 0 && rodradius != 0)
            {
                gear_Ylocation_L = 0;
                gear_Zlocation_L = rodradius;
            }
            //Set the initial state of the doors light
            if (Train.Doors != DoorStates.None)
            {
                MyDoorLightState = DoorLightStates.DoorsOpen;
            }
            else
            {
                MyDoorLightState = DoorLightStates.InMotion;
            }
            headcodestate = 0;
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            
            //Steam Locomotive Valve Gear
                distancetravelled = Train.TrainLocation - Train.PreviousLocation;
                //Then divide the distance travelled by the circumference to get us the percentage around the wheel travelled in this turn
                double percentage = ((distancetravelled * 1000) / wheelcircumference) * 35;
                //Multiply by 1000 to get the distance travelled in millimeters
                //Then divide by the wheel's circumference
                //Finally multiply by 100 to get a figure out of 100
                if (Math.Abs(percentage) > 100)
                {
                    //If percentage is above 100%, set to zero
                    //This should never go above 100% unless with tiny wheels at silly speed
                    //When initialising, the train location returns zero until everything has fully loaded
                    //This can produce very large percentage values when the starting location is 100s of m
                    //down the route
                    percentage = 0;
                }
                //Figure out where we are in relation to 100%
                if (wheelpercentage - percentage <= 100 && wheelpercentage - percentage >= 0)
                {
                    wheelpercentage -= percentage;
                }
                else
                {
                    wheelpercentage = 100 - percentage;
                }
                
                //Now calculate the number of degrees turn this represents
                degreesturned = 3.6 * wheelpercentage;

                //Right Gear Position
                if (gear_Yvariable_R != -1 && gear_Zvariable_R != -1 && rodradius != 0)
                {
                    //Calculate the Y and Z positions
                    gear_Ylocation_R = (-rodradius * Math.Sin((Math.PI / 180) * degreesturned));
                    gear_Zlocation_R = (rodradius * Math.Cos((Math.PI / 180) * degreesturned));
                    this.Train.Panel[gear_Yvariable_R] = (int)gear_Ylocation_R;
                    this.Train.Panel[gear_Zvariable_R] = (int)gear_Zlocation_R;
                }
                //Left Gear Position
                if (gear_Yvariable_L != -1 && gear_Zvariable_L != -1 && rodradius != 0)
                {
                    //Calculate the Y and Z positions
                    gear_Ylocation_L = (-rodradius * Math.Sin((Math.PI / 180) * (degreesturned + 90)));
                    gear_Zlocation_L = (rodradius * Math.Cos((Math.PI / 180) * (degreesturned + 90)));
                    this.Train.Panel[gear_Yvariable_L] = (int)gear_Ylocation_L;
                    this.Train.Panel[gear_Zvariable_L] = (int)gear_Zlocation_L;
                }
                if (wheelrotation_variable != -1)
                {
                    //Calculate the wheel rotation value- Use this rather than an animated formula, as otherwise the gear and wheel may become out of sync
                    this.Train.Panel[wheelrotation_variable] = (int)(((Math.PI / 180) * (360 - degreesturned)) * 1000);
                }

                //Piston Crosshead Location Right
                if (crankvariable_R != -1 && crankrotation_R != -1 && crankradius != 0 && cranklength !=0)
                {
                    //We've already worked out the number of degrees turned for the main crankshaft.
                    //Work out the crank throw distance
                    cranklocation_R = crankradius * Math.Cos((Math.PI / 180) * (degreesturned + 90)) + Math.Sqrt(Math.Pow(cranklength, 2) - Math.Pow(crankradius, 2) * Math.Pow(Math.Sin((Math.PI / 180) * (degreesturned + 90)), 2));
                    this.Train.Panel[crankvariable_R] = (int)cranklocation_R;
                    crankangle_R = Math.Asin(crankradius * Math.Sin((Math.PI / 180) * (degreesturned + 90)) / cranklength);
                    this.Train.Panel[crankrotation_R] = (int)((crankangle_R * 1000) /2);
                }

                //Piston Crosshead Location Left
                if (crankvariable_L != -1 && crankrotation_L != -1 && crankradius != 0 && cranklength != 0)
                {
                    //We've already worked out the number of degrees turned for the main crankshaft.
                    //Work out the crank throw distance
                    cranklocation_L = crankradius * Math.Cos((Math.PI / 180) * (degreesturned + 180)) + Math.Sqrt(Math.Pow(cranklength, 2) - Math.Pow(crankradius, 2) * Math.Pow(Math.Sin((Math.PI / 180) * (degreesturned + 180)), 2));
                    this.Train.Panel[crankvariable_L] = (int)cranklocation_L;
                    crankangle_L = Math.Asin(crankradius * Math.Sin((Math.PI / 180) * (degreesturned + 180)) / cranklength);
                    this.Train.Panel[crankrotation_L] = (int)((crankangle_L * 1000) / 2);
                }

            //Cylinder cocks puff state is handled in the animations class, but pressure usage is handled in the steam traction class
            if (this.Train.SteamEngine != null && cylinderpuff_L != -1)
            {
                if (Train.SteamEngine.cylindercocks)
                {
                    if (Train.CurrentSpeed == 0 && Train.Handles.PowerNotch == 0)
                    {
                        CylinderPuffState_L = CylinderPuffStates.OpenStationary;
                    }
                    else if (Train.CurrentSpeed == 0 && Train.Handles.PowerNotch > 0)
                    {
                        CylinderPuffState_L = CylinderPuffStates.OpenStationaryPowered;
                    }
                    else if (Train.CurrentSpeed > 0 && Train.Handles.PowerNotch == 0)
                    {
                        if (wheelpercentage > 25 && wheelpercentage < 65)
                        {
                            CylinderPuffState_L = CylinderPuffStates.OpenPuffingUnpowered;
                        }
                        else
                        {
                            CylinderPuffState_L = CylinderPuffState_L = CylinderPuffStates.OpenNoPuff;
                        }
                    }
                    else
                    {
                        if (wheelpercentage > 30 && wheelpercentage < 70)
                        {
                            CylinderPuffState_L = CylinderPuffStates.OpenPuffingPowered;
                        }
                        else
                        {
                            CylinderPuffState_L = CylinderPuffState_L = CylinderPuffStates.OpenNoPuff;
                        }
                    }
                }
                else
                {
                    CylinderPuffState_L = CylinderPuffStates.CockClosed;
                }
                this.Train.Panel[(cylinderpuff_L)] = (int)CylinderPuffState_L;
            }
            if (this.Train.SteamEngine != null && cylinderpuff_R != -1)
            {
                if (Train.SteamEngine.cylindercocks)
                {
                    if (Train.CurrentSpeed == 0 && Train.Handles.PowerNotch == 0)
                    {
                        CylinderPuffState_R = CylinderPuffStates.OpenStationary;
                    }
                    else if (Train.CurrentSpeed == 0 && Train.Handles.PowerNotch > 0)
                    {
                        CylinderPuffState_R = CylinderPuffStates.OpenStationaryPowered;
                    }
                    else if (Train.CurrentSpeed > 0 && Train.Handles.PowerNotch == 0)
                    {
                        if (wheelpercentage > 5 && wheelpercentage < 45)
                        {
                            CylinderPuffState_R = CylinderPuffStates.OpenPuffingUnpowered;
                        }
                        else
                        {
                            CylinderPuffState_R = CylinderPuffState_L = CylinderPuffStates.OpenNoPuff;
                        }
                    }
                    else
                    {
                        if (wheelpercentage > 5 && wheelpercentage < 45)
                        {
                            CylinderPuffState_R = CylinderPuffStates.OpenPuffingPowered;
                        }
                        else
                        {
                            CylinderPuffState_R = CylinderPuffState_L = CylinderPuffStates.OpenNoPuff;
                        }
                    }
                }
                else
                {
                    CylinderPuffState_R = CylinderPuffStates.CockClosed;
                }
                this.Train.Panel[(cylinderpuff_R)] = (int)CylinderPuffState_R;
            }
            //Flashing door light
            {
                
                if (MyDoorLightState == DoorLightStates.InMotion)
                {
                    doorlighttimer = 0;
                }
                else if (MyDoorLightState == DoorLightStates.Primed)
                {
                    if (Train.CurrentSpeed == 0)
                    {
                        if (Train.Doors != DoorStates.None)
                        {
                            MyDoorLightState = DoorLightStates.DoorsOpen;
                        }
                    }
                }
                else if (MyDoorLightState == DoorLightStates.DoorsOpen)
                {
                    MyDoorLightState = DoorLightStates.Countdown;
                }
                else if (MyDoorLightState == DoorLightStates.Countdown)
                {
                    
                    //We've opened our train doors-
                    //Compare the current time with the departure time
                    //Do nothing if we are early, just wait
                    if (Train.SecondsSinceMidnight > (departuretime - stoptime))
                    {
                        doorlighttimer += data.ElapsedTime.Milliseconds;
                        if (doorlighttimer > stoptime)
                        {
                            MyDoorLightState = DoorLightStates.DoorsClosing;
                            doorlighttimer = 0;
                        }
                    }
                }
                else if (MyDoorLightState == DoorLightStates.DoorsClosing)
                {
                    doorlighttimer += data.ElapsedTime.Milliseconds;
                    doorlighttimer2 += data.ElapsedTime.Milliseconds;
                    if (doorlighttimer2 > 1000)
                    {
                        if (doorlighton)
                        {
                            doorlighton = false;
                        }
                        else
                        {
                            doorlighton = true;
                        }
                        doorlighttimer2 = 0;
                    }

                    if (doorlighttimer > 30000)
                    {
                        doorlighton = false;
                        MyDoorLightState = DoorLightStates.DoorsClosed;
                    }
                }
                else if (MyDoorLightState == DoorLightStates.DoorsClosed)
                {
                    if (Train.CurrentSpeed > 0)
                    {
                        MyDoorLightState = DoorLightStates.InMotion;
                    }
                }
            }
            //Headcode state
            {
                if (headcodeindex != -1)
                {
                    this.Train.Panel[headcodeindex] = headcodestate;
                }
            }
        }

        //Called by the beacon manager to set the door light state
        internal static void doorlighttrigger()
        {
            if (MyDoorLightState == DoorLightStates.InMotion)
            {
                MyDoorLightState = DoorLightStates.Primed;
            }
        }

        //Called from the Traction Manager when headcodes toggle is pressed
        internal static void headcodetoggle()
        {
            if (totalheadcodestates != 0)
            {
                if (headcodestate < totalheadcodestates)
                {
                    headcodestate += 1;
                }
                else
                {
                    headcodestate = 0;
                }
            }
        }


    }
}
