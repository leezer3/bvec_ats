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
        /// <summary>The Y position of the valve gear motion.</summary>
        internal double gear_Ylocation;
        /// <summary>The Z position of the valve gear motion.</summary>
        internal double gear_Zlocation;
        /// <summary>The Y variable for the valve gear motion.</summary>
        internal int gear_Yvariable = 0;
        /// <summary>The Z variable for the valve gear motion.</summary>
        internal int gear_Zvariable = 0;
        /// <summary>The radius of our wheel in millimeters.</summary>
        internal double wheelradius = 0;

        //Internal variables
        internal double currentlocation;
        internal double previouslocation;
        internal double wheelcircumference;
        internal double wheelpercentage;
        internal double degreesturned;

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
            if (gear_Yvariable != 0 && gear_Zvariable != 0 && wheelradius != 0)
            {
                gear_Ylocation = wheelradius;
                gear_Zlocation = 0;
                wheelcircumference = 2 * Math.PI * wheelradius;
            }
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            //Only calculate if all variables are set
            if (gear_Yvariable != 0 && gear_Zvariable != 0 && wheelradius != 0)
            {
                //Calculate the distance travelled
                double distancetravelled;
                previouslocation = currentlocation;
                currentlocation = Train.trainlocation;
                distancetravelled = currentlocation - previouslocation;
                //Then divide the distance travelled by the circumference to get us the percentage around the wheel travelled in this turn
                double percentage = (distancetravelled * 10000) / wheelcircumference;
                //Figure out where we are in relation to 100%
                if (wheelpercentage + percentage <= 99)
                {
                    wheelpercentage += percentage;
                }
                else
                {
                    wheelpercentage = (wheelpercentage + percentage) - 100;
                }
                //Now calculate the number of degrees turn this represents
                degreesturned = 3.6 * wheelpercentage;
                

                //Calculate the Y and Z positions
                gear_Ylocation = (-wheelradius * Math.Sin((Math.PI / 180) * degreesturned));
                gear_Zlocation = (wheelradius * Math.Cos((Math.PI / 180) * degreesturned));
                this.Train.Panel[gear_Yvariable] = (int)gear_Ylocation;
                this.Train.Panel[gear_Zvariable] = (int)gear_Zlocation;
            }
        }

    }
}
