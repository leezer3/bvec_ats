using System;
using OpenBveApi.Sounds;

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
    partial class StarterMotor
    {
        /// <summary>The time in milliseconds taken for the starter motor to run up</summary>
        internal int StartDelay = 0;
        /// <summary>The time in milliseconds taken for the starter motor to run down</summary>
        internal int RunDownDelay = 0;
        /// <summary>The time in milliseconds taken for the engine to fire up</summary>
        internal int FireUpDelay = 0;
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
        internal int MinimumFireProbability = 1000;
        internal int MaximumFireProbability = 1000;

        /// <summary>The minimum & maximum stall probabilities</summary>
        internal int MinimumStallProbability = 0;
        internal int MaximumStallProbability = 0;

        internal bool ComplexStarterModel;

        internal double StarterMotorTimer;
        internal bool SimpleStarterPlayer;

        readonly Random RandomNumber = new Random();

        /// <summary>Gets the state of the starter motor.</summary>
        internal StarterMotorStates StarterMotorState
        {
            get { return this.StarterMotorState; }
            set { this.StarterMotorState = value; }
        }

        /// <summary>Runs the complex starter model. If this method returns true, the engine has started.</summary>
        internal bool RunComplexStarter(double ElapsedTime, bool StarterKeyPresed)
        {
            if (!StarterKeyPresed && StarterMotorState == StarterMotorStates.Active)
            {
                //If our starter key is no longer pressed, then we should switch to the run-down state & stop the loop sound
                SoundManager.Stop(StarterLoopSound);
                StarterMotorState = StarterMotorStates.RunDown;
            }
            if (StarterKeyPresed && StarterMotorState == StarterMotorStates.None)
            {
                //If the starter key has been pressed and the starter motor is inactive, start the start sequence
                StarterMotorState = StarterMotorStates.Active;
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
                    //Generate our probabilities
                    var StartProbability = RandomNumber.Next(MinimumFireProbability, MaximumFireProbability);
                    var StallProbability = RandomNumber.Next(MinimumStallProbability, MaximumStallProbability);
                    //We've hit the firing trigger, so start the engine
                    if (StartProbability == MaximumFireProbability)
                    {
                        StarterMotorState = StarterMotor.StarterMotorStates.EngineFire;
                    }
                    //We've missed the firing trigger, but have hit the stall trigger- Stall
                    if (StallProbability == MaximumStallProbability)
                    {
                        StarterMotorState = StarterMotor.StarterMotorStates.EngineStall;
                    }
                    return false;
                case StarterMotor.StarterMotorStates.EngineFire:
                    SoundManager.Stop(StarterLoopSound);
                    SoundManager.Play(EngineFireSound, 1.0, 1.0, false);
                    //Elapse the timer
                    StarterMotorTimer += ElapsedTime;
                    //If the fireup sequence is complete, then drop back to inactive
                    if (StarterMotorTimer > FireUpDelay)
                    {
                        StarterMotorTimer = 0.0;
                        StarterMotorState = StarterMotor.StarterMotorStates.None;
                        return true;
                    }
                    return false;
                case StarterMotor.StarterMotorStates.EngineStall:
                    SoundManager.Stop(StarterLoopSound);
                    SoundManager.Play(EngineStallSound, 1.0, 1.0, false);
                    //Elapse the timer
                    StarterMotorTimer += ElapsedTime;
                    //If the fireup sequence is complete, then drop back to inactive
                    if (StarterMotorTimer > FireUpDelay)
                    {
                        StarterMotorTimer = 0.0;
                        StarterMotorState = StarterMotor.StarterMotorStates.None;
                        return true;
                    }
                    return false;
                case StarterMotor.StarterMotorStates.RunDown:
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
