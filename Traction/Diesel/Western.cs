﻿using System.Runtime.CompilerServices;
using OpenBveApi.Runtime;

namespace Plugin
{
    //This class represents a BR Class 52 Western Locomotive
    class WesternDiesel : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        /// <summary>Creates a new instance of this system.</summary>
        /// <param name="train">The train.</param>
        internal WesternDiesel(Train train)
        {
            this.Train = train;
        }

        /// <summary>Stores whether the No. 1 Engine is currently running.</summary>
        internal bool Engine1Running;
        /// <summary>Stores whether the No. 2 Engine is currently running.</summary>
        internal bool Engine2Running;
        /// <summary>Stores which engine is currently selected for control.</summary>
        internal int EngineSelector;
        /// <summary>Stores whether the starter key is currently pressed.</summary>
        internal bool StarterKeyPressed;
        /// <summary>Stores whether the battery is currently isolated.</summary>
        internal bool BatteryIsolated = true;

        /// <summary>The panel variable for the current state of the instrument cluster lights (Drivers).</summary>
        internal int ILCluster1 = -1;
        /// <summary>The panel variable for the current state of the instrument cluster lights (Secondmans).</summary>
        internal int ILCluster2 = -1;
        /// <summary>The panel variable for the master key.</summary>
        internal int MasterKey = -1;
        internal bool ComplexStarterModel;

        internal int EngineLoopSound = -1;

        /// <summary>The sound index for the DSD Buzzer.</summary>
        internal int DSDBuzzer = -1;

        readonly StarterMotor Engine1Starter = new StarterMotor();
        readonly StarterMotor Engine2Starter = new StarterMotor();
        internal readonly WesternStartupManager StartupManager = new WesternStartupManager();
        readonly GearBox Gears = new GearBox();




        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (Engine1Running == false)
            {
                //The engine is *not* running
                //Check whether we can start the engine
                if (EngineSelector == 1 && StartupManager.StartupState == WesternStartupManager.SequenceStates.ReadyToStart)
                {
                    //If this method returns true, then our engine is now running
                    if (Engine1Starter.RunComplexStarter(data.ElapsedTime.Milliseconds, StarterKeyPressed))
                    {
                        Engine1Running = true;
                    }
                }

                //Stop the engine loop sound from playing & demand power cutoff
                SoundManager.Stop(EngineLoopSound);
                Train.tractionmanager.demandpowercutoff();
            }
            else
            {
                //Play the engine loop sound & reset power cutoff
                SoundManager.Play(EngineLoopSound, 1.0, 1.0, false);
                Train.tractionmanager.resetpowercutoff();
            }
            if (Engine2Running == false)
            {
                //The engine is *not* running
                //Check whether we can start the engine
                if (EngineSelector == 2 && StartupManager.StartupState == WesternStartupManager.SequenceStates.ReadyToStart)
                {
                    if (Engine2Starter.RunComplexStarter(data.ElapsedTime.Milliseconds, StarterKeyPressed))
                    {
                        Engine2Running = true;
                    }
                }

                //Stop the engine loop sound from playing & demand power cutoff
                SoundManager.Stop(EngineLoopSound);
                Train.tractionmanager.demandpowercutoff();
            }
            else
            {
                //Play the engine loop sound & reset power cutoff
                SoundManager.Play(EngineLoopSound, 1.0, 1.0, false);
                Train.tractionmanager.resetpowercutoff();
            }
            //This loco has a gearbox
            data.Handles.PowerNotch = Gears.RunGearBox();

            //This section of code handles the startup self-test routine
            if (StartupManager.StartupState != WesternStartupManager.SequenceStates.ReadyToStart)
            {
                switch (StartupManager.StartupState)
                {
                    case WesternStartupManager.SequenceStates.Pending:
                        if (BatteryIsolated == false)
                        {
                            StartupManager.StartupState = WesternStartupManager.SequenceStates.BatteryEnergized;
                        }
                        break;
                    case WesternStartupManager.SequenceStates.MasterKeyInserted:
                        if (Train.Handles.Reverser != 0)
                        {
                            //We have selected a direction, so change state
                            StartupManager.StartupState = WesternStartupManager.SequenceStates.DirectionSelected;
                        }
                        break;
                    case WesternStartupManager.SequenceStates.DirectionSelected:
                        //Start the DSD Buzzer on loop
                        SoundManager.Play(DSDBuzzer, 1.0, 1.0, true);
                        break;
                        case WesternStartupManager.SequenceStates.DSDAcknowledged:
                        //Stop the DSD Buzzer
                        SoundManager.Stop(DSDBuzzer);
                        if (Train.Handles.Reverser == 0)
                        {
                            StartupManager.StartupState = WesternStartupManager.SequenceStates.NeutralSelected;
                        }
                        break;
                    case WesternStartupManager.SequenceStates.NeutralSelected:
                        StartupManager.StartupState = WesternStartupManager.SequenceStates.ReadyToStart;
                        break;
                }
            }

            //This section of code handles the panel variables specific to this locomotive
            {
                if (ILCluster2 != -1)
                {
                    
                    if (StartupManager.StartupState == WesternStartupManager.SequenceStates.Pending)
                    {
                        //If we are still pending, all ILs should be dark
                        this.Train.Panel[ILCluster2] = 0;
                    }
                    else
                    {
                        //If we are not in the pending state, then IL cluster 2 should be all lit
                        this.Train.Panel[ILCluster2] = 1;
                    }
                }
                if (ILCluster1 != -1)
                {
                    if (StartupManager.StartupState == WesternStartupManager.SequenceStates.Pending)
                    {
                        //If we are still pending, all ILs should be dark
                        this.Train.Panel[ILCluster1] = 0;
                    }
                    else
                    {
                        if (Engine1Running == false && Engine2Running == false)
                        {
                            //We have started the self-test routine, but no engines are running
                            //Both engine ILs red, all other ILs blue
                            this.Train.Panel[ILCluster1] = 1;
                        }
                        else if (Engine1Running == true && Engine2Running == false)
                        {
                            //Engine 1 IL blue, engine 2 IL red
                            this.Train.Panel[ILCluster1] = 2;
                        }
                        else if (Engine1Running == false && Engine2Running == true)
                        {
                            //Engine 1 IL red, engine 2 IL blue
                            this.Train.Panel[ILCluster1] = 3;
                        }
                        else
                        {
                            //Both engine ILs blue
                            this.Train.Panel[ILCluster1] = 4;
                        }
                    }
                }
                if (MasterKey != -1)
                {
                    //If the startup sequence is greater than or equal to 3, then the master key has been inserted
                    if ((int)StartupManager.StartupState >= 3)
                    {
                        this.Train.Panel[MasterKey] = 1;
                    }
                    else
                    {
                        this.Train.Panel[MasterKey] = 0;   
                    }
                }
            }
            //Temporarily pass the startup self-test manager state out to string
            //Remove this later....
            data.DebugMessage = StartupManager.StartupState.ToString();
        }
    }
}