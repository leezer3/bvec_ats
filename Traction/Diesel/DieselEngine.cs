﻿using System.Globalization;
using OpenBveApi.Runtime;

namespace Plugin.Traction.Diesel
{
    class DieselEngine : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        /// <summary>Stores whether the engine is currently running.</summary>
        internal bool EngineRunning;

        internal bool HasGears;

        internal bool ComplexStarterModel;

        internal int EngineLoopSound = -1;



        readonly StarterMotor Starter = new StarterMotor();
        readonly GearBox Gears = new GearBox();




        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (EngineRunning == false)
            {
                //The engine is *not* running
                //Check whether we can start the engine
                if (this.Train.StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.Initialised)
                {
                    //Use the complex starter model by default
                    if (ComplexStarterModel)
                    {
                        //If this method returns true, then our engine is now running
                        if (Starter.RunComplexStarter(data.ElapsedTime.Milliseconds))
                        {
                            EngineRunning = true;
                        }
                    }
                    else
                    {
                        //This method will always return true after 10s- After this delay our engine is now running
                        if (Starter.RunSimpleStarter(data.ElapsedTime.Milliseconds))
                        {
                            EngineRunning = true;
                        }
                    }
                }
                //Stop the engine loop sound from playing & demand power cutoff
                SoundManager.Stop(EngineLoopSound);
                tractionmanager.demandpowercutoff();
            }
            else
            {
                //Play the engine loop sound & reset power cutoff
                SoundManager.Play(EngineLoopSound, 1.0, 1.0, false);
                tractionmanager.resetpowercutoff();
            }

            if (HasGears)
            {
                data.Handles.PowerNotch = Gears.RunGearBox();
            }

        }
    }
}
