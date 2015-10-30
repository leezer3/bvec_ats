using System;
using BlinkStickDotNet;
using OpenBveApi.Runtime;

//This class is used to light a Blinkstick LED

namespace Plugin
{
    internal class LEDLights: Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        /// <summary>Creates a new instance of this system.</summary>
        /// <param name="train">The train.</param>
        internal LEDLights(Train train)
        {
            this.Train = train;
        }

        internal bool Initialised;

        private BlinkStick device;
        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            try
            {
                //When we initialise, find the first attached BlinkStick
                device = BlinkStick.FindFirst();
                if (device != null)
                {
                    Initialised = true;
                    Train.DebugLogger.LogMessage("Blinkstick device sucessfully found");
                }
                else
                {
                    Train.DebugLogger.LogMessage("No blinkstick devices found");
                }
            }
            catch (Exception)
            {
                Train.DebugLogger.LogMessage("An error occured whilst trying to detect attached Blinksticks");
            }
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (Initialised)
            {
                //If the train doors state is not none, then set the LED to yellow
                if (Train.Doors != DoorStates.None)
                {
                    device.SetColor("yellow");
                }
                else
                {
                    device.TurnOff();
                }
            }
        }
    }
}
