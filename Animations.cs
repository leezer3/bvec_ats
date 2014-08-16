using System;
using OpenBveApi.Runtime;

namespace Plugin
{
    /// <summary>Provides a handler for complex animations such as valve gear.</summary>
    internal class Animations : Device
    {
        /// <summary>The underlying train.</summary>
        private Train Train;

        //Variables
        
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
        /// <summary>The current train location.</summary>
        internal double currentlocation;
        /// <summary>The previous train location</summary>
        internal double previouslocation;
        internal double wheelcircumference;
        internal double wheelpercentage;
        internal double degreesturned;
        internal double wheelrotation;

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
            if (gear_Yvariable_R != 0 && gear_Zvariable_R != 0 && rodradius != 0)
            {
                gear_Ylocation_R = rodradius;
                gear_Zlocation_R = 0;
                wheelcircumference = 2 * Math.PI * rodradius;
            }
            if (gear_Yvariable_L != 0 && gear_Zvariable_L != 0 && rodradius != 0)
            {
                gear_Ylocation_L = 0;
                gear_Zlocation_L = rodradius;
                wheelcircumference = 2 * Math.PI * rodradius;
            }
            currentlocation = Train.trainlocation;
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            
                //Calculate the distance travelled
                double distancetravelled;
                previouslocation = currentlocation;
                currentlocation = Train.trainlocation;
                distancetravelled = currentlocation - previouslocation;
                //Then divide the distance travelled by the circumference to get us the percentage around the wheel travelled in this turn
                double percentage = (distancetravelled * 10000) / wheelcircumference;
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
                    data.DebugMessage = Convert.ToString(wheelpercentage);
                    //Calculate the wheel rotation value- Use this rather than an animated formula, as otherwise the gear and wheel may become out of sync
                    if (wheelrotation <= 360 && wheelrotation >= 0)
                    {
                        wheelrotation += degreesturned;
                    }
                    else
                    {
                        wheelrotation = 0 + degreesturned;
                    }
                    this.Train.Panel[wheelrotation_variable] = 360 - (int)(((Math.PI / 180) * degreesturned) * 1000);
                }
        }

    }
}
