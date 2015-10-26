using BlinkStickDotNet;
using OpenBveApi.Runtime;

//This class is used to light a Blinkstick LED

namespace Plugin
{
    internal class LEDLights: Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        private BlinkStick device;
        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            //When we initialise, find the first attached BlinkStick
            device = BlinkStick.FindFirst();
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
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
