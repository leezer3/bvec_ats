using System;
using System.Runtime.CompilerServices;
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
        /// <summary>Stores the number of currently running engines.</summary>
        internal int NumberOfEnginesRunning;
        /// <summary>Stores which engine is currently selected for control.</summary>
        internal int EngineSelector = 2;
        /// <summary>Stores whether the starter key is currently pressed.</summary>
        internal bool StarterKeyPressed;
        /// <summary>Stores whether the engine stop key is currently pressed.</summary>
        internal bool StopKeyPressed;
        /// <summary>Stores whether the battery is currently isolated.</summary>
        internal bool BatteryIsolated = true;

        /// <summary>The panel variable for the current state of the instrument cluster lights (Drivers).</summary>
        internal int ILCluster1 = -1;
        /// <summary>The panel variable for the current state of the instrument cluster lights (Secondmans).</summary>
        internal int ILCluster2 = -1;
        /// <summary>The panel variable for the master key.</summary>
        internal int MasterKey = -1;
        /// <summary>The panel variable for the battery volts gauge.</summary>
        internal int BatteryVoltsGauge = -1;
        /// <summary>The panel variable for the battery charge gauge.</summary>
        internal int BatteryChargeGauge = -1;
        internal bool ComplexStarterModel;
        /// <summary>This is the number of RPM per notch.</summary>
        internal int RPMPerNotch = 100;
        /// <summary>This is the rate RPM increases per second up-to the maximum RPM.</summary>
        internal int RPMChange = 100;
        /// <summary>This stores the current RPM. It is shared across both engines, with a small random factor for display on the gauges</summary>
        internal int CurrentRPM;
        /// <summary>This stores the previous frames RPM.</summary>
        internal int PreviousRPM;
        /// <summary>The panel variable for the engine #1 RPM Gauge.</summary>
        internal int RPMGauge1 = -1;
        /// <summary>The panel variable for the engine #2 RPM Gauge.</summary>
        internal int RPMGauge2 = -1;
        /// <summary>The panel variable for the engine #1 button states.</summary>
        internal int Engine1Button = -1;
        /// <summary>The panel variable for the engine #2 button states.</summary>
        internal int Engine2Button = -1;

        /*
         * These represent engine loop sounds for each 1/4 of the revolution range
         */
        internal int EngineLoopSound = -1;
        internal int EngineLoopSound1 = -1;
        internal int EngineLoopSound2 = -1;
        internal int EngineLoopSound3 = -1;
        /*
         * These represent fade up sounds for the above
         */
        internal int EngineFadeUpSound1 = -1;
        internal int EngineFadeUpSound2 = -1;
        internal int EngineFadeUpSound3 = -1;

        internal bool EngineLoop = false;
        /*
         * This bool should be toggled when an engine starts-
         * It allows us to hold-on the brakes until the air compressors prototypically start
         */
        internal bool CompressorsRunning = false;
        internal double RPMTimer;
        /// <summary>The sound index for the DSD Buzzer.</summary>
        internal int DSDBuzzer = -1;
        /// <summary>The sound index for the DSD Buzzer.</summary>
        internal int NeutralSelectedSound = -1;
        /// <summary>The sound index for the battery isolation switch & master switch.</summary>
        internal int SwitchSound = -1;

        internal readonly StarterMotor Engine1Starter = new StarterMotor();
        internal readonly StarterMotor Engine2Starter = new StarterMotor();
        internal readonly WesternStartupManager StartupManager = new WesternStartupManager();
        internal readonly WesternGearBox GearBox = new WesternGearBox();
        internal readonly Turbocharger Turbocharger = new Turbocharger();

        internal override void Initialize(InitializationModes mode)
        {
            Engine1Starter.StarterMotorState = StarterMotor.StarterMotorStates.None;
            Engine2Starter.StarterMotorState = StarterMotor.StarterMotorStates.None;
            RPMTimer = 0;
        }


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
                        //Reset the power cutoff
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 1 started.");
                        Train.tractionmanager.resetpowercutoff();
                        NumberOfEnginesRunning += 1;
                    }
                }
            }
            else
            {
                //If the engine running state has been triggered, then this will start the loop sound with appropriate paramaters
                if (Engine1Starter.StarterMotorState == StarterMotor.StarterMotorStates.EngineRunning && !SoundManager.IsPlaying(Engine1Starter.EngineFireSound) && EngineLoop == false)
                {
                    if (Engine2Running == false)
                    {
                        SoundManager.Play(EngineLoopSound, 0.5, 1.0, true);
                    }
                    else
                    {
                        SoundManager.Play(EngineLoopSound, 1.0, 1.0, true);
                    }
                    Engine1Starter.StarterMotorState = StarterMotor.StarterMotorStates.None;
                    EngineLoop = true;
                }
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
                        //Reset the power cutoff
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 2 started.");
                        Train.tractionmanager.resetpowercutoff(); 
                        NumberOfEnginesRunning += 1;
                    }
                }
            }
            else
            {
                //If the engine running state has been triggered, then this will start the loop sound with appropriate paramaters
                if (Engine2Starter.StarterMotorState == StarterMotor.StarterMotorStates.EngineRunning && !SoundManager.IsPlaying(Engine1Starter.EngineFireSound) && EngineLoop == false)
                {
                    if (Engine1Running == false)
                    {
                        SoundManager.Play(EngineLoopSound, 0.5, 1.0, true);
                    }
                    else
                    {
                        SoundManager.Play(EngineLoopSound, 1.0, 1.0, true);
                    }
                    Engine2Starter.StarterMotorState = StarterMotor.StarterMotorStates.None;
                    EngineLoop = true;
                }
                
            }
            //If neither engine is running, stop the playing loop sound and demand power cutoff
            if (Engine1Running == false && Engine2Running == false)
            {
                
                SoundManager.Stop(EngineLoopSound);
                if (Train.tractionmanager.powercutoffdemanded == false)
                {
                    Train.DebugLogger.LogMessage("Western Diesel- Traction power was cutoff due to no available engines.");
                    Train.tractionmanager.demandpowercutoff();
                }
                if (Train.tractionmanager.brakedemanded == false)
                {
                    Train.DebugLogger.LogMessage("Western Diesel- Brakes applied due to no running compressors.");
                    Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
                }
            }
            else
            {
                if (CompressorsRunning == false)
                {
                    CompressorsRunning = true;
                    if (Train.tractionmanager.brakedemanded == true)
                    {
                        Train.DebugLogger.LogMessage("Western Diesel- An attempt was made to reset the current brake application due to the compressors starting.");
                        Train.tractionmanager.resetbrakeapplication();
                    }
                }
                if (GearBox.TorqueConvertorState != WesternGearBox.TorqueConvertorStates.OnService)
                {
                    //If the torque convertor is not on service, then cut power
                    if (Train.tractionmanager.powercutoffdemanded == false)
                    {
                        Train.DebugLogger.LogMessage("Western Diesel- Traction power was cutoff due the torque convertor being out of service.");
                        Train.tractionmanager.demandpowercutoff();
                    }
                    //Now, run the startup sequence
                    if (Train.Handles.Reverser != 0 && Train.Handles.PowerNotch != 0)
                    {
                        if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.Empty)
                        {
                            GearBox.TorqueConvertorState = WesternGearBox.TorqueConvertorStates.FillInProgress;
                        }
                        GearBox.TorqueConvertorTimer += data.ElapsedTime.Milliseconds;
                        if (GearBox.TorqueConvertorTimer > 6000)
                        {
                            GearBox.TorqueConvertorState = WesternGearBox.TorqueConvertorStates.OnService;
                            GearBox.TorqueConvertorTimer = 0.0;
                            Train.DebugLogger.LogMessage("Western Diesel- The gearbox fill cycle has completed sucessfully and the torque convertor is now on service");
                            Train.tractionmanager.resetpowercutoff();
                        }
                    }

                }
            }
            //This section of code handles the engine RPM calculations
            {
                int MaximumRPM;
                //Calculate the current maximum RPM figure
                if (Engine1Running || Engine2Running)
                {
                    MaximumRPM = 600;
                    //If the torque convertor is on service, then the max RPM may be increased above idle as per power notch
                    //I assume this is prototypical, however unsure; Seems logical if nothing else!
                    if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.OnService)
                    {
                        MaximumRPM += (RPMPerNotch*Train.Handles.PowerNotch);
                    }
                }
                else
                {
                    MaximumRPM = 0;
                }
                //Elapse our timer
                RPMTimer += data.ElapsedTime.Milliseconds;
                //If we are over 1 second, then either increase or decrease by the RPM change rate
                if (RPMTimer > 1000)
                {
                    if (CurrentRPM < MaximumRPM)
                    {
                        CurrentRPM += RPMChange;
                    }
                    else if (CurrentRPM > MaximumRPM)
                    {
                        CurrentRPM -= RPMChange;
                    }
                    RPMTimer = 0;
                }

            }
            if (Train.Handles.PowerNotch != 0 && Train.tractionmanager.powercutoffdemanded == false)
            {
                data.Handles.PowerNotch = Train.WesternDiesel.GearBox.PowerNotch(CurrentRPM,NumberOfEnginesRunning,Turbocharger.RunTurbocharger(data.ElapsedTime.Milliseconds, CurrentRPM));
            }

            //This section of code handles the startup self-test routine
            if (StartupManager.StartupState != WesternStartupManager.SequenceStates.ReadyToStart || StartupManager.StartupState != WesternStartupManager.SequenceStates.AWSOnline)
            {
                switch (StartupManager.StartupState)
                {
                    case WesternStartupManager.SequenceStates.Pending:
                        if (BatteryIsolated == false)
                        {
                            SoundManager.Play(SwitchSound, 1.0, 1.0 ,false);
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
                            //Play 3 clicks sound effect
                            SoundManager.Play(NeutralSelectedSound, 0.5, 1.0, false);
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
                        //If we are not in the pending state, but our torque convertor is not yet on service
                        //All lights should be blue, other than torque convertor (Red)
                        if (GearBox.TorqueConvertorState != WesternGearBox.TorqueConvertorStates.OnService)
                        {
                            this.Train.Panel[ILCluster2] = 1;
                        }
                        //Otherwise, all lights blue
                        else
                        {
                            this.Train.Panel[ILCluster2] = 2;
                        }
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
                            if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress)
                            {
                                this.Train.Panel[ILCluster1] = 5;
                            }
                            else
                            {
                                this.Train.Panel[ILCluster1] = 2;    
                            }
                            
                        }
                        else if (Engine1Running == false && Engine2Running == true)
                        {
                            //Engine 2 IL blue, engine 1 IL red
                            if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress)
                            {
                                this.Train.Panel[ILCluster1] = 6;
                            }
                            else
                            {
                                this.Train.Panel[ILCluster1] = 3;
                            }
                        }
                        else
                        {
                            //Both engine ILs blue
                            if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress)
                            {
                                this.Train.Panel[ILCluster1] = 7;
                            }
                            else
                            {
                                this.Train.Panel[ILCluster1] = 4;
                            }
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
                if (BatteryVoltsGauge != -1)
                {
                    //If the startup sequence is greater than or equal to 1, then there are battery volts available
                    //This panel index should rotate the volts switch and increase the voltmeter dial to running
                    if ((int) StartupManager.StartupState >= 1)
                    {
                        if (Engine1Starter.StarterMotorState != StarterMotor.StarterMotorStates.None || Engine2Starter.StarterMotorState != StarterMotor.StarterMotorStates.None)
                        {
                            this.Train.Panel[BatteryVoltsGauge] = 2;
                        }
                        else
                        {
                            this.Train.Panel[BatteryVoltsGauge] = 1;
                        }
                    }
                    else
                    {
                        this.Train.Panel[BatteryVoltsGauge] = 0;
                    }
                }
                if (BatteryChargeGauge != -1)
                {
                    //If the startup sequence is greater than or equal to 1, then the battery charge gauge should go to nil
                    if ((int)StartupManager.StartupState >= 1)
                    {
                        if (Engine1Starter.StarterMotorState != StarterMotor.StarterMotorStates.None || Engine2Starter.StarterMotorState != StarterMotor.StarterMotorStates.None)
                        {
                            //The battery is discharging due to a starter motor running
                            this.Train.Panel[BatteryChargeGauge] = 1;    
                        }
                        else if (Engine1Running || Engine2Running)
                        {
                            //The battery is charging as an engine is running
                            this.Train.Panel[BatteryChargeGauge] = 2;
                        }
                        else
                        {
                            //The battery is neither charging or discharging as no engines are running
                            this.Train.Panel[BatteryChargeGauge] = 0;
                        }
                    }
                    else
                    {
                        //The gauge is at the rest position
                        this.Train.Panel[BatteryChargeGauge] = 0;
                    }
                }
                if (RPMGauge1 != -1)
                {
                    if (Engine1Running == true)
                    {
                        this.Train.Panel[RPMGauge1] = CurrentRPM;
                    }
                    else
                    {
                        this.Train.Panel[RPMGauge1] = 0;
                    }
                }
                if (RPMGauge2 != -1)
                {
                    if (Engine2Running == true)
                    {
                        this.Train.Panel[RPMGauge2] = CurrentRPM;
                    }
                    else
                    {
                        this.Train.Panel[RPMGauge2] = 0;
                    }
                }
                if (Engine1Button != -1)
                {
                    if (EngineSelector == 1)
                    {
                        if (StarterKeyPressed)
                        {
                            this.Train.Panel[Engine1Button] = 1;    
                        }
                        else if (StopKeyPressed)
                        {
                            this.Train.Panel[Engine1Button] = 2;    
                        }
                        else
                        {
                            this.Train.Panel[Engine1Button] = 0;    
                        }
                    }
                    else
                    {
                        this.Train.Panel[Engine1Button] = 0;
                    }
                }
                if (Engine2Button != -1)
                {
                    if (EngineSelector == 2)
                    {
                        if (StarterKeyPressed)
                        {
                            this.Train.Panel[Engine2Button] = 1;
                        }
                        else if (StopKeyPressed)
                        {
                            this.Train.Panel[Engine2Button] = 2;
                        }
                        else
                        {
                            this.Train.Panel[Engine2Button] = 0;
                        }
                    }
                    else
                    {
                        this.Train.Panel[Engine2Button] = 0;
                    }
                }
            }
            //This section of code handles the power sounds
            //These are not being run by OpenBVE as they are RPM dependant
            if (EngineLoop == true)
            {
                //We are in the first 1/4 of the RPM range
                if (CurrentRPM > 600 && CurrentRPM < 850)
                {
                    if (SoundManager.IsPlaying(EngineLoopSound1))
                    {
                        SoundManager.Stop(EngineLoopSound1);
                    }
                    var Pitch = 1.0 + ((CurrentRPM - 600)/250.0);
                    if (SoundManager.GetLastPitch(EngineLoopSound) != Pitch)
                    {
                        SoundManager.Play(EngineLoopSound, 1.0, Pitch, true);
                    }
                }
                //We are in the second 1/4 of the RPM range
                else if (CurrentRPM >= 850 && CurrentRPM < 1100)
                {
                    if (SoundManager.IsPlaying(EngineLoopSound))
                    {
                        //Stop the idle- 25% loop sound and play the fade-up sound
                        SoundManager.Stop(EngineLoopSound);
                        SoundManager.Play(EngineFadeUpSound1, 1.0, 1.0, false);
                    }
                    else if (SoundManager.IsPlaying(EngineLoopSound2))
                    {
                        SoundManager.Stop(EngineLoopSound2);
                    }
                    else if (!SoundManager.IsPlaying(EngineFadeUpSound1))
                    {
                        //Finally, trigger the new loop sound
                        var Pitch = ((CurrentRPM - 850)/250.0) - 0.5;
                        if (SoundManager.GetLastPitch(EngineLoopSound1) != Pitch)
                        {
                            SoundManager.Play(EngineLoopSound1, 1.0, Pitch, true);
                        }
                    }
                }
                else if (CurrentRPM >= 1100 && CurrentRPM < 1350)
                {
                    if (SoundManager.IsPlaying(EngineLoopSound1))
                    {
                        SoundManager.Stop(EngineLoopSound1);
                        SoundManager.Play(EngineFadeUpSound2, 1.0, 1.0, false);
                    }
                    else if (SoundManager.IsPlaying(EngineLoopSound3))
                    {
                        SoundManager.Stop(EngineLoopSound3);
                    }
                    else if (!SoundManager.IsPlaying(EngineFadeUpSound2))
                    {
                        var Pitch = ((CurrentRPM - 1100)/250.0) - 0.5;
                        if (SoundManager.GetLastPitch(EngineLoopSound2) != Pitch)
                        {
                            SoundManager.Play(EngineLoopSound2, 1.0, Pitch, true);
                        }
                    }
                }
                else if (CurrentRPM >= 1350 && CurrentRPM < 1600)
                {
                    if (SoundManager.IsPlaying(EngineLoopSound1))
                    {
                        SoundManager.Stop(EngineLoopSound2);
                        SoundManager.Play(EngineFadeUpSound3, 1.0, 1.0, false);
                    }
                    else if (!SoundManager.IsPlaying(EngineFadeUpSound3))
                    {
                        var Pitch = ((CurrentRPM - 1350)/250.0) - 0.5;
                        if (SoundManager.GetLastPitch(EngineLoopSound3) != Pitch)
                        {
                            SoundManager.Play(EngineLoopSound3, 1.0, Pitch, true);
                        }
                    }
                }
                else if (CurrentRPM == 600)
                {
                    SoundManager.Play(EngineLoopSound, 1.0, 1.0, true);
                }
            }
            PreviousRPM = CurrentRPM;
            //Temporarily pass the startup self-test manager state out to string
            //Remove this later....
            if (StartupManager.StartupState != WesternStartupManager.SequenceStates.ReadyToStart)
            {
                data.DebugMessage = StartupManager.StartupState.ToString();
            }
            else
            {
                data.DebugMessage = Engine2Starter.StarterMotorState.ToString();
            }
        }

        internal void ToggleAWS()
        {
            //If we are the locomotive is ready to start, then switch the AWS to online
            //This will begin the standard BVE startup self-test sequence
            if (StartupManager.StartupState == WesternStartupManager.SequenceStates.ReadyToStart)
            {
                Train.DebugLogger.LogMessage("Western Diesel- AWS System energized.");
                StartupManager.StartupState = WesternStartupManager.SequenceStates.AWSOnline;
                return;
            }
            //If the AWS safety state is clear or none, then we may isolate it
            if (Train.AWS.SafetyState == AWS.SafetyStates.Clear || Train.AWS.SafetyState == AWS.SafetyStates.None)
            {
                Train.DebugLogger.LogMessage("Western Diesel- AWS System Isolated.");
                Train.tractionmanager.isolatetpwsaws();
                return;
            }
            //If the AWS has been isolated by the driver, issue an unconditional reset
            if (Train.AWS.SafetyState == AWS.SafetyStates.Isolated)
            {
                Train.DebugLogger.LogMessage("Western Diesel- AWS System reset.");
                Train.tractionmanager.reenabletpwsaws();
            }
        }
    }
}