using System;

namespace Plugin
{
    /*
     * 
     * Newer standard traction modelling
     * Diesel engine, to incorporate both UKDT and OS_ATS features
     * 
     */

    //This represents a Diesel engine starter motor
    //Basic paramaters as per UKDT
    /// <summary>Represents a diesel engine starter motor</summary>
    partial class StarterMotor
    {
        /// <summary>The time in milliseconds taken for the starter motor to run up</summary>
        internal double StartDelay = 1000;
        /// <summary>The time in milliseconds taken for the starter motor to run down</summary>
        internal double RunDownDelay = 1000;
        /// <summary>The time in milliseconds taken for the engine to fire up</summary>
        internal double FireUpDelay = 1000;
        /// <summary>The sound played whilst the starter motor is running up</summary>
        internal int StarterRunUpSound = -1;
        /// <summary>The sound played whilst the starter motor is active</summary>
        internal int StarterLoopSound = -1;
        /// <summary>The sound played whilst the starter motor is running down</summary>
        internal int StarterRunDownSound = -1;
        /// <summary>The sound played when the engine fires</summary>
        internal int EngineFireSound = -1;
        /// <summary>The sound played when the engine fires</summary>
        internal int EngineStallSound = -1;

        /// <summary>The minimum & maximum fire probabilities</summary>
        internal int FireProbability = 1000;
        internal int MaximumFireProbability = 1000;

        /// <summary>The minimum & maximum stall probabilities</summary>
        internal int StallProbability = 0;
        internal int MaximumStallProbability = 0;

        /// <summary>Stores whether the current start attempt is blocked (e.g. by a stall in progress)</summary>
        internal bool StartBlocked;

        /// <summary>Stores whether the complex starter model is to be used</summary>
        internal bool ComplexStarterModel;

        internal double StarterMotorTimer;
        internal bool SimpleStarterPlayer;

        /// <summary>Gets the state of the starter motor.</summary>
        internal StarterMotorStates StarterMotorState { get; set; }

        /// <summary>Runs the complex starter model. If this method returns true, the engine has started.</summary>
        internal bool RunComplexStarter(double ElapsedTime, bool StarterKeyPresed, bool FuelAvailable)
        {
            if (!StarterKeyPresed && StarterMotorState == StarterMotorStates.Active)
            {
                //If our starter key is no longer pressed, then we should switch to the run-down state & stop the loop sound
                SoundManager.Stop(StarterLoopSound);
                StarterMotorState = StarterMotorStates.RunDown;
            }
            if (StarterKeyPresed && StarterMotorState == StarterMotorStates.None && StartBlocked == false)
            {
                //If the starter key has been pressed and the starter motor is inactive, start the start sequence
                StarterMotorState = StarterMotorStates.RunUp;
            }
            if (!StarterKeyPresed && StarterMotorState == StarterMotorStates.None && StartBlocked)
            {
                //If the starter motor has returned to an inactive state and the starter key has been released, lift the block
                StartBlocked = false;
            }
            switch (StarterMotorState)
            {
                case StarterMotor.StarterMotorStates.RunUp:
                    //Start the runup sound
                    SoundManager.Play(StarterRunUpSound, 1.0, 1.0, false);
                    //Elapse the timer
                    StarterMotorTimer += ElapsedTime;
                    //If the runup sequence is complete, start the fire attempt
                    if (StarterMotorTimer > StartDelay)
                    {
                        StarterMotorTimer = 0.0;
                        StarterMotorState = StarterMotor.StarterMotorStates.Active;
                    }
                    return false;
                case StarterMotor.StarterMotorStates.Active:
                    //Start the starter loop sound
                    SoundManager.Play(StarterLoopSound, 1.0, 1.0, true);
                    //If fuel is not available, simply crank until the key is released
                    if (FuelAvailable)
                    {
                        //Generate our probabilities
                        var StartChance = Plugin.Random.Next(0, MaximumFireProbability);
                        var StallChance = Plugin.Random.Next(0, MaximumStallProbability);
                        //We've hit the firing trigger, so start the engine
                        if (StartChance > FireProbability)
                        {
                            StarterMotorState = StarterMotor.StarterMotorStates.EngineFire;
                            return false;
                        }
                        //We've missed the firing trigger, but have hit the stall trigger- Stall
                        if (StallChance > StallProbability)
                        {
                            StarterMotorState = StarterMotor.StarterMotorStates.EngineStall;
                            return false;
                        }
                    }
                    return false;
                case StarterMotor.StarterMotorStates.EngineFire:
                    if (!SoundManager.IsPlaying(EngineFireSound))
                    {
                        SoundManager.Play(EngineFireSound, 1.0, 1.0, false);
                    }
                    //Elapse the timer
                    StarterMotorTimer += ElapsedTime;
                    //If the fireup sequence is complete, then shift to the running state
                    if (StarterMotorTimer > FireUpDelay)
                    {
                        StarterMotorTimer = 0.0;
                        StarterMotorState = StarterMotor.StarterMotorStates.EngineRunning;
                        return true;
                    }
                    return false;
                case StarterMotor.StarterMotorStates.EngineStall:
                    SoundManager.Play(EngineStallSound, 1.0, 1.0, false);
                    //Elapse the timer
                    StarterMotorTimer += ElapsedTime;
                    //If the fireup sequence is complete, then drop back to inactive
                    if (StarterMotorTimer > FireUpDelay)
                    {
                        StarterMotorTimer = 0.0;
                        StarterMotorState = StarterMotor.StarterMotorStates.None;
                        StartBlocked = true;
                        return false;
                    }
                    return false;
                case StarterMotor.StarterMotorStates.RunDown:
                    SoundManager.Stop(StarterLoopSound);
                    SoundManager.Play(StarterRunDownSound, 1.0, 1.0, false);
                    //Elapse the timer
                    StarterMotorTimer += ElapsedTime;
                    //If the rundown sequence is complete, then stop the rundown sound
                    if (StarterMotorTimer > RunDownDelay)
                    {
                        StarterMotorTimer = 0.0;
                        StarterMotorState = StarterMotor.StarterMotorStates.None;
                    }
                    return false;
                default:
                    SoundManager.Stop(StarterLoopSound);
                    return false;
            }
        }

        /// <summary>Runs the simple starter model. When this method returns true, the engine has started.</summary>
        internal bool RunSimpleStarter(double ElapsedTime)
        {
            if (!SimpleStarterPlayer && !SoundManager.IsPlaying(StarterRunUpSound))
            {
                SoundManager.Play(StarterRunUpSound, 1.0, 1.0, false);
                SimpleStarterPlayer = true;
            }
            if (SimpleStarterPlayer && !SoundManager.IsPlaying(StarterRunUpSound))
            {
                SoundManager.Play(StarterLoopSound, 1.0, 1.0, false);
            }
            StarterMotorTimer += ElapsedTime;
            if (StarterMotorTimer >= 10000)
            {
                SoundManager.Stop(StarterLoopSound);
                SoundManager.Play(EngineFireSound, 1.0, 1.0, false);
                return true;
            }
            return false;
        }
    }
}
