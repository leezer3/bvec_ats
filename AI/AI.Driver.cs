﻿using System.Data;
using System.Windows.Forms;
using OpenBveApi.Runtime;

namespace Plugin.AI
{
    public class AI_Driver
    {
        private readonly Train Train;

        /// <summary>Creates a new instance of this system.</summary>
        /// <param name="train">The train.</param>
        internal AI_Driver(Train train)
        {
            this.Train = train;
        }

        internal bool PantographRising;

        internal void TrainDriver(AIData data)
        {
            //Run the traction specific AI first
            switch (tractionmanager.tractiontype)
            {
                case 0:
                    SteamLocomotive(ref data);
                    break;
                case 1:
                    DieselLocomotive(ref data);
                    break;
                case 2:
                    ElectricLocomotive(ref data);
                    break;
            }
            //Hit the deadman's handle key if required
            if (Train.vigilance != null && Train.vigilance.deadmanshandle != 0)
            {
                if (Train.vigilance.deadmanstimer > (Train.vigilance.vigilancetime * 0.7))
                {
                    Train.vigilance.deadmanstimer = 0.0;
                }
            }
        }

        /// <summary>Represents the driver class for a steam locomotive</summary>
        internal void SteamLocomotive(ref AIData data)
        {
            if (Train.steam.automatic != true)
            {
                Train.steam.automatic = true;
            }
        }

        /// <summary>Represents the driver class for a steam locomotive</summary>
        internal void DieselLocomotive(ref AIData data)
        {
            if (Train.diesel.automatic != true)
            {
                Train.diesel.automatic = true;
            }
        }

        /// <summary>Represents the driver class for an electric locomotive</summary>
        internal void ElectricLocomotive(ref AIData data)
        {
            if (PantographRising == true)
            {
                //We need a delay if the pantograph is currently rising, as the default Long response isn't quite long enough....
                PantographRising = false;
                data.Response = AIResponse.Long;
                return;
            }

            //The first thing we need to do is to check the pantographs
            if (Train.electric.FrontPantographState != electric.PantographStates.VCBReady || Train.electric.RearPantographState != electric.PantographStates.VCBReady)
            {
                //First check whether we have any pantographs
                if (Train.tractionmanager.frontpantographkey != null || Train.tractionmanager.rearpantographkey != null)
                {
                    //Test the front pantograph first
                    if (Train.tractionmanager.frontpantographkey != null &&
                        Train.electric.FrontPantographState == electric.PantographStates.Lowered)
                    {
                        Train.electric.pantographtoggle(0);
                        data.Response = AIResponse.Long;
                        return;
                    }

                    //Then test the rear pantograph
                    if (Train.tractionmanager.rearpantographkey != null &&
                        Train.electric.RearPantographState != electric.PantographStates.Lowered)
                    {
                        Train.electric.pantographtoggle(1);
                        data.Response = AIResponse.Long;
                        return;
                    }
                }
            }
            else if (Train.electric.FrontPantographState == electric.PantographStates.VCBReady || Train.electric.RearPantographState == electric.PantographStates.VCBReady)
            {
                //We have a pantograph that's ready for usage, so turn on the ACB/VCB
                Train.electric.breakertrip();
                data.Response = AIResponse.Short;
                return;
            }

            if (Train.electric.powergap == true)
            {
                if (data.Handles.PowerNotch > 0)
                {
                    //We're in a power gap, so the driver should shut off power manually
                    data.Handles.PowerNotch -= 1;
                    data.Response = AIResponse.Short;
                }
                else
                {
                    data.Handles.PowerNotch = 0;
                    data.Response = AIResponse.Short;
                }

            }
            
        }
    }
}
