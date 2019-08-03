/*
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
        internal string currentColor;

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
            if (!Initialised) return;
            //If the train doors state is not none, then set the LED to yellow
            if (Train.Doors != DoorStates.None && currentColor != "yellow")
            {
                currentColor = "yellow";
                Train.DebugLogger.LogMessage("Blinkstick set to Yellow");
                SetColor(device, currentColor);
            }
            else if (Train.Doors == DoorStates.None && currentColor != null)
            {
                Train.DebugLogger.LogMessage("Blinkstick turned off");
                currentColor = null;
                TurnOff(device);
            }
        }

        /// <summary>Changes the color of a Blinkstick LED- This is a wrapper method for Blinkstick.Net to provide exception handling</summary>
        /// <param name="device">The device for which we wish to set the color</param>
        /// <param name="color">The color to set</param>
        internal void SetColor(BlinkStick device, string color)
        {
            if (device.OpenDevice())
            {
                try
                {
                    device.SetColor(color);
                }
                catch (Exception e)
                {
                    //If we fail to set the color, log the exception, and stop trying
                    Train.DebugLogger.LogMessage("An error occured whilst trying to set the Blinkstick color:");
                    Train.DebugLogger.LogMessage(e.Message);
                    Initialised = false;
                    Train.DebugLogger.LogMessage("No further attempts will be made to access this device.");
                }
            }
        }
        /// <summary> Turns off a Blinkstick LED- This is a wrapper method for Blinkstick.Net to provide exception handling</summary>
        /// <param name="device">The device to turn off</param>
        internal void TurnOff(BlinkStick device)
        {
            if (device.OpenDevice())
            {
                try
                {
                    device.TurnOff();
                }
                catch (Exception e)
                {
                    Train.DebugLogger.LogMessage("An error occured whilst trying turn off the Blinkstick:");
                    Train.DebugLogger.LogMessage(e.Message);
                    Initialised = false;
                    Train.DebugLogger.LogMessage("No further attempts will be made to access this device.");
                }
            }
        }
    }
}
*/