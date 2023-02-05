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

        internal int EngineLoopSound = -1;
        
        readonly StarterMotor Starter;

        readonly GearBox Gears;

        internal DieselEngine(Train train)
        {
	        Train = train;
	        Gears = new GearBox(train);
	        Starter = new StarterMotor(false);
        }



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
	                EngineRunning = Starter.Run(data.ElapsedTime.Milliseconds, Train.TractionManager.EngineStartKeyPressed);
                }
                //Stop the engine loop sound from playing & demand power cutoff
                SoundManager.Stop(EngineLoopSound);
                Train.TractionManager.DemandPowerCutoff("Power cutoff was demanded as the diesel engine was not running");
            }
            else
            {
                //Play the engine loop sound & reset power cutoff
                SoundManager.Play(EngineLoopSound, 1.0, 1.0, false);
                Train.TractionManager.ResetPowerCutoff();
            }

            if (HasGears)
            {
                data.Handles.PowerNotch = Gears.Run();
            }

        }
    }
}