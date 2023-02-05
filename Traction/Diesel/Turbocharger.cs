namespace Plugin
{
    class Turbocharger
    {
        /// <summary>The current state of the turbocharger.</summary>
        internal TurbochargerStates TurbochargerState { get; set; }
        /// <summary>Stores the timer used for the turbocharger running up/ down.</summary>
        internal double TurbochargerTimer;
        /// <summary>Stores the turbocharger run-up time.</summary>
        internal double TurbochargerRunUpTime = 0;
        /// <summary>Stores the turbocharger run-down time.</summary>
        internal double TurbochargerRunDownTime = 0;
        /// <summary>Stores the sound index for the turbocharger running up.</summary>
        internal int TurbochargerRunUpSound = -1;
        /// <summary>Stores the sound index for the turbocharger loop.</summary>
        internal int TurbochargerLoopSound = -1;
        /// <summary>Stores the sound index for the turbocharger running down.</summary>
        internal int TurbochargerRunDownSound = -1;

        internal enum TurbochargerStates
        {
            /// <summary>The turbocharger is currently inactive. The numerical value of this constant is 0.</summary>
            None = 0,
            /// <summary>The turbocharger is currently running up to speed.The numerical value of this constant is 1.</summary>
            RunUp = 1,
            /// <summary>The turbocharger is currently running. The numerical value of this constant is 2.</summary>
            Running = 2,
            /// <summary>The turbocharger is currently running down. The numerical value of this constant is 3.</summary>
            RunDown = 3,
        }

        internal bool RunTurbocharger(double ElapsedTime, int CurrenRPM)
        {
            bool Active = CurrenRPM > 1200;
            switch (TurbochargerState)
            {
                case TurbochargerStates.None:
                    if (Active)
                    {
                        //If our turbocharger is active, switch to the run-up state and play sound
                        TurbochargerState = TurbochargerStates.RunUp;
                        SoundManager.Play(TurbochargerRunUpSound, 5.0, 1.0, false);
                    }
                    return false;
                case TurbochargerStates.RunUp:
                    if (TurbochargerRunUpTime == 0)
                    {
                        //If no run-up time has been selected, switch to running once the sound has finished
                        if (!SoundManager.IsPlaying(TurbochargerRunUpSound))
                        {
                            TurbochargerState = TurbochargerStates.Running;
                        }
                    }
                    else
                    {
                        //Otherwise, elapse the timer and switch to running once expired
                        TurbochargerTimer += ElapsedTime;
                        if (TurbochargerTimer > TurbochargerRunUpTime)
                        {
                            TurbochargerState = TurbochargerStates.Running;
                            SoundManager.Stop(TurbochargerRunUpSound);
                            SoundManager.Play(TurbochargerLoopSound, 5.0, 1.0, true);
                            TurbochargerTimer = 0;
                        }
                    }
                    return true;
                case TurbochargerStates.Running:
                    if (Active != true)
                    {
                        //If the turbocharger is no longer being requested by the engine, then switch to run-down state
                        TurbochargerState = TurbochargerStates.RunDown;
                        SoundManager.Stop(TurbochargerLoopSound);
                        SoundManager.Play(TurbochargerRunDownSound, 5.0, 1.0, false);
                        return false;
                    }
                    break;
                case TurbochargerStates.RunDown:
                    if (TurbochargerRunDownTime == 0)
                    {
                        //If no run-down time is selected, switch to none once sound is finished
                        if (!SoundManager.IsPlaying(TurbochargerRunDownSound))
                        {
                            TurbochargerState = TurbochargerStates.None;
                        }
                    }
                    else
                    {
                        //Otherwise elapse timer
                        TurbochargerTimer += ElapsedTime;
                        if (TurbochargerTimer > TurbochargerRunDownTime)
                        {
                            TurbochargerState = TurbochargerStates.None;
                            SoundManager.Stop(TurbochargerRunDownSound);
                        }
                    }
                    return false;
            }
            //Default return type, should never be used
            return true;
        }
    }
}
