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
        internal double CurrentRPM;
        /// <summary>This stores the previous frames RPM.</summary>
        internal double PreviousRPM;
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

        /// <summary>Stores the current temperature value for engine 1</summary>
        internal double Engine1Temperature = 0;
        /// <summary>Stores whether engine 1 has overheated</summary>
        internal bool Engine1Overheated;
        /// <summary>Stores the current temperature value for engine 2</summary>
        internal double Engine2Temperature = 0;
        /// <summary>Stores whether engine 2 has overheated</summary>
        internal bool Engine2Overheated;
        /// <summary>The basic rate at which engine temperature changes</summary>
        internal double TemperatureChangeRate = 1;
        /// <summary>Stores the current transmission temperature</summary>
        internal double TransmissionTemperature = 0;
        /// <summary>Stores whether the transmission has overheated</summary>
        internal bool TransmissionOverheated;
        /// <summary>Whether the radiator shutters are open</summary>
        internal bool RadiatorShuttersOpen = true;
        /// <summary>The panel index for the radiator shutters</summary>
        internal int RadiatorShutters = -1;



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
                    if (Engine1Starter.StarterMotorState != StarterMotor.StarterMotorStates.None)
                    {
                        switch (Engine1Starter.StarterMotorState)
                        {
                            case StarterMotor.StarterMotorStates.RunUp:
                                MaximumRPM = 150;
                                break;
                            case StarterMotor.StarterMotorStates.Active:
                                MaximumRPM = 300;
                                break;
                            case StarterMotor.StarterMotorStates.EngineFire:
                                MaximumRPM = 500;
                                break;
                            case StarterMotor.StarterMotorStates.RunDown:
                                MaximumRPM = 150;
                                break;
                            default:
                                MaximumRPM = 0;
                                break;
                        }
                    }
                    else if (Engine2Starter.StarterMotorState != StarterMotor.StarterMotorStates.None)
                    {
                        switch (Engine2Starter.StarterMotorState)
                        {
                            case StarterMotor.StarterMotorStates.RunUp:
                                MaximumRPM = 150;
                                break;
                            case StarterMotor.StarterMotorStates.Active:
                                MaximumRPM = 300;
                                break;
                            case StarterMotor.StarterMotorStates.EngineFire:
                                MaximumRPM = 500;
                                break;
                            case StarterMotor.StarterMotorStates.RunDown:
                                MaximumRPM = 150;
                                break;
                            default:
                                MaximumRPM = 0;
                                break;
                        }
                    }
                    else
                    {
                        MaximumRPM = 0;
                    }

                }

                
                if (CurrentRPM < MaximumRPM)
                {
                    if (Turbocharger.TurbochargerState != Turbocharger.TurbochargerStates.Running)
                    {
                        //The standard RPM increase
                        CurrentRPM += (RPMChange/1000.0)*data.ElapsedTime.Milliseconds;
                    }
                    else
                    {
                        //The engine spools up twice as fast when the turbocharger is running
                        CurrentRPM += ((RPMChange / 1000.0) * data.ElapsedTime.Milliseconds) *2;
                    }
                }
                else if (CurrentRPM > MaximumRPM)
                {
                    CurrentRPM -= (RPMChange / 1000.0) * data.ElapsedTime.Milliseconds;
                }
                
                

            }

            /* 
             * Calculate the final power notch applied by OpenBVE
             * Power notches 1-5 represent increases of 20% power with one engine running
             * Power notches 6-10 represent increases of 20% power with two engines running
             */
            if (Train.Handles.PowerNotch != 0 && Train.tractionmanager.powercutoffdemanded == false)
            {
                //An engine may be running but not providing power
                var EnginesProvidingPower = NumberOfEnginesRunning;
                switch (NumberOfEnginesRunning)
                {
                    case 1:
                        if (Engine1Running && Engine1Overheated || Engine2Running && Engine2Overheated)
                        {
                            EnginesProvidingPower = 0;
                        }
                        break;
                    case 2:
                        if (Engine1Overheated)
                        {
                            EnginesProvidingPower -= 1;
                        }
                        if (Engine2Overheated)
                        {
                            EnginesProvidingPower -= 1;
                        }
                        break;
                }
                data.Handles.PowerNotch = Train.WesternDiesel.GearBox.PowerNotch((int)CurrentRPM,EnginesProvidingPower,Turbocharger.RunTurbocharger(data.ElapsedTime.Milliseconds, (int)CurrentRPM));
            }
            //This section of code handles engine & transmission temperatures
            {
                if (Engine1Running)
                {
                    //Only increase temperature if it's less than 500 or the current RPM is greater than 1000
                    if (Engine1Temperature < 500 || CurrentRPM > 1000)
                    {
                        //If the turbocharger is running, then temperature increases twice as fast
                        if (Turbocharger.TurbochargerState != Turbocharger.TurbochargerStates.None)
                        {
                            //If the radiator shutters are closed or we are travelling at under 20km/h, temperature increases twice as fast
                            if (RadiatorShuttersOpen || Train.trainspeed < 20)
                            {
                                Engine1Temperature += ((TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds)*2;
                            }
                            else
                            {
                                Engine1Temperature += (((TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds)*2)* 2;
                            }
                        }
                        else
                        {
                            //If the radiator shutters are closed, temperature increases twice as fast
                            if (RadiatorShuttersOpen || Train.trainspeed < 20)
                            {
                                Engine1Temperature += (TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds;
                            }
                            else
                            {
                                Engine1Temperature += ((TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds)*2;
                            }
                        }
                    }
                    else
                    {
                        //If the radiator shutters are closed, temperature decreases half as fast
                        if (RadiatorShuttersOpen || Train.trainspeed < 20)
                        {
                            Engine1Temperature -= (TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds;
                        }
                        else
                        {
                            Engine1Temperature -= ((TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds)/2;
                        }
                    }

                }
                else
                {
                    if (Engine1Temperature > 0)
                    {
                        //If the radiator shutters are open and we are travelling at over 20km/h, then temperature decreases twice as fast
                        if (RadiatorShuttersOpen && Train.trainspeed > 20)
                        {
                            Engine1Temperature -= (TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds;
                        }
                        else
                        {
                            Engine1Temperature -= ((TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds) /2;
                        }
                    }
                }
                if (Engine2Running)
                {
                    if (Engine2Temperature < 500 || CurrentRPM > 1000)
                    {
                        //If the turbocharger is running, then temperature increases twice as fast
                        if (Turbocharger.TurbochargerState != Turbocharger.TurbochargerStates.None)
                        {
                            //If the radiator shutters are closed, temperature increases twice as fast
                            if (RadiatorShuttersOpen && Train.trainspeed < 20)
                            {
                                Engine2Temperature += ((TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds)*2;
                            }
                            else
                            {
                                Engine2Temperature += (((TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds)*2)* 2;
                            }
                        }
                        else
                        {
                            //If the radiator shutters are closed, temperature increases twice as fast
                            if (RadiatorShuttersOpen && Train.trainspeed < 20)
                            {
                                Engine2Temperature += (TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds;
                            }
                            else
                            {
                                Engine2Temperature += ((TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds)*2;
                            }
                        }
                    }
                    else
                    {
                        //If the radiator shutters are closed, temperature decreases half as fast
                        if (RadiatorShuttersOpen || Train.trainspeed < 20)
                        {
                            Engine2Temperature -= (TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds;
                        }
                        else
                        {
                            Engine2Temperature -= ((TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds) / 2;
                        }
                    }
                }
                else
                {
                    if (Engine2Temperature > 0)
                    {
                        //If the radiator shutters are open, then temperature decreases twice as fast
                        if (RadiatorShuttersOpen && Train.trainspeed > 20)
                        {
                            Engine2Temperature -= (TemperatureChangeRate/1000.0)*data.ElapsedTime.Milliseconds;
                        }
                        else
                        {
                            Engine2Temperature -= ((TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds) /2;
                        }
                    }
                }
                //Transmission temperatures
                switch (NumberOfEnginesRunning)
                {
                    case 0:
                        //No engines running- Decrease at full rate
                        if (TransmissionTemperature > 0)
                        {
                            TransmissionTemperature -= (TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds;
                        }
                        break;
                    case 1:
                        //Less than 1k RPM- Decrease at full rate
                        if (CurrentRPM < 1000 && TransmissionTemperature > 0)
                        {
                            TransmissionTemperature -= (TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds;
                        }
                        //Increase at half rate
                        else if (CurrentRPM > 1200)
                        {
                            TransmissionTemperature += ((TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds) /2;
                        }
                        break;
                    case 2:
                        //Less than 1k RPM- Decrease at full rate
                        if (CurrentRPM > 1000 && TransmissionTemperature > 0)
                        {
                            TransmissionTemperature -= (TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds;
                        }
                        //Increase at full rate
                        else if (CurrentRPM > 1300)
                        {
                            TransmissionTemperature += (TemperatureChangeRate / 1000.0) * data.ElapsedTime.Milliseconds;
                        }
                        break;
                }
                //Consequences
                if (Engine1Temperature > 600)
                {
                    if (Engine1Overheated == false)
                    {
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 1 overheated");
                        Engine1Overheated = true;
                        if (Engine2Running == false)
                        {
                            Train.tractionmanager.demandpowercutoff();
                            Train.DebugLogger.LogMessage("Western Diesel- Traction power was cutoff due to engine 1 overheating, and engine 2 being stopped");
                        }
                    }
                }
                else if(Engine1Temperature < 400)
                {
                    if (Engine1Overheated)
                    {
                        Engine1Overheated = false;
                        Train.tractionmanager.resetpowercutoff();
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 1 returned to normal operating temperature");
                    }
                }
                if (Engine2Temperature > 600)
                {
                    if (Engine2Overheated == false)
                    {
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 2 overheated");
                        Engine2Overheated = true;
                        if (Engine1Running == false)
                        {
                            Train.tractionmanager.demandpowercutoff();
                            Train.DebugLogger.LogMessage("Western Diesel- Traction power was cutoff due to engine 2 overheating, and engine 1 being stopped");
                        }
                    }
                }
                else if (Engine2Temperature < 400)
                {
                    if (Engine2Overheated)
                    {
                        Engine2Overheated = false;
                        Train.tractionmanager.resetpowercutoff();
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 2 returned to normal operating temperature");
                    }
                }
                if (TransmissionTemperature > 600)
                {
                    if (TransmissionOverheated == false)
                    {
                        TransmissionOverheated = true;
                        Train.tractionmanager.demandpowercutoff();
                        Train.DebugLogger.LogMessage("Western Diesel- Traction power was cutoff due to the transmission overheating");
                    }

                }
                else if (TransmissionTemperature < 400)
                {
                    if (TransmissionOverheated)
                    {
                        TransmissionOverheated = false;
                        if ((Engine1Running && !Engine1Overheated) || (Engine2Running && !Engine2Overheated))
                        {
                            Train.tractionmanager.resetpowercutoff();
                        }
                        Train.DebugLogger.LogMessage("Western Diesel- Transmission returned to normal operating temperature");
                    }

                }
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
                        else
                        {
                            //Something has overheated
                            if (Engine1Overheated || Engine2Overheated && TransmissionOverheated)
                            {

                                if (RadiatorShuttersOpen)
                                {
                                    //The radiator shutters are open, so the high oil temp light should be lit
                                    if (!TransmissionOverheated)
                                    {
                                        
                                        this.Train.Panel[ILCluster2] = 3;
                                    }
                                    else
                                    {
                                        this.Train.Panel[ILCluster2] = 6;
                                    }
                                }
                                else
                                {
                                    //The radiator shutters are closed, so the high oil and water temp lights should be lit
                                    if (!TransmissionOverheated)
                                    {
                                        this.Train.Panel[ILCluster2] = 4;
                                    }
                                    else
                                    {
                                        this.Train.Panel[ILCluster2] = 5;
                                    }
                                }
                            }
                            else if (TransmissionOverheated)
                            {
                                //Just the transmission
                                this.Train.Panel[ILCluster2] = 7;
                            }
                            //Otherwise all lights blue
                            else
                            {
                                this.Train.Panel[ILCluster2] = 2;
                            }
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
                            //Engine 1 IL lit blue
                            //If the torque convertor fill sequence is active, or we have overheated the general alarm light should be lit
                            if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress || Engine1Overheated)
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
                            //Engine 2 IL lit blue
                            //If the torque convertor fill sequence is active, or we have overheated the general alarm light should be lit
                            if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress || Engine2Overheated)
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
                            //If the torque convertor fill sequence is active, or we have overheated the general alarm light should be lit
                            if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress || (Engine1Overheated || Engine2Overheated))
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
                        this.Train.Panel[RPMGauge1] = (int)CurrentRPM;
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
                        this.Train.Panel[RPMGauge2] = (int)CurrentRPM;
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
                    //Pitch increases by 25%, volume by 10%
                    var Pitch = 1.0 + (((CurrentRPM - 600)/250.0) * 0.25);
                    var Volume = 1.0 + (((CurrentRPM - 600) / 250.0) * 0.1);
                    if (SoundManager.GetLastPitch(EngineLoopSound) != Pitch && SoundManager.GetLastVolume(EngineLoopSound) != Volume)
                    {
                        SoundManager.Play(EngineLoopSound, 1.0, Volume, true);
                    }
                }
                //We are in the second 1/4 of the RPM range
                else if (CurrentRPM >= 850 && CurrentRPM < 1100)
                {
                    var Pitch = 1.25 + (((CurrentRPM - 600) / 250.0) * 0.25);
                    var Volume = 1.1 + (((CurrentRPM - 600) / 250.0) * 0.25);
                    if (SoundManager.GetLastPitch(EngineLoopSound) != Pitch && SoundManager.GetLastVolume(EngineLoopSound) != Volume)
                    {
                        SoundManager.Play(EngineLoopSound, 1.0, Volume, true);
                    }
                }

                else if (CurrentRPM >= 1100 && CurrentRPM < 1350)
                {
                    var Pitch = 1.5 + (((CurrentRPM - 600) / 250.0) * 0.25);
                    var Volume = 1.35 + (((CurrentRPM - 600) / 250.0) * 0.1);
                    if (SoundManager.GetLastPitch(EngineLoopSound) != Pitch && SoundManager.GetLastVolume(EngineLoopSound) != Volume)
                    {
                        SoundManager.Play(EngineLoopSound, 1.0, Volume, true);
                    }
                }
                else if (CurrentRPM >= 1350 && CurrentRPM < 1600)
                {
                    var Pitch = 1.75 + (((CurrentRPM - 600) / 250.0) * 0.25);
                    var Volume = 1.45 + (((CurrentRPM - 600) / 250.0) * 0.1);
                    if (SoundManager.GetLastPitch(EngineLoopSound) != Pitch && SoundManager.GetLastVolume(EngineLoopSound) != Volume)
                    {
                        SoundManager.Play(EngineLoopSound, 1.0, Volume, true);
                    }
                }
                else if (CurrentRPM == 600)
                {
                    SoundManager.Play(EngineLoopSound, 1.0, 1.0, true);
                }
            }
            PreviousRPM = CurrentRPM;
            //Pass out the debug data
            if (StartupManager.StartupState != WesternStartupManager.SequenceStates.ReadyToStart)
            {
                data.DebugMessage = StartupManager.StartupState.ToString();
            }
            else
            {
                if (StarterKeyPressed)
                {
                    if (EngineSelector == 1)
                    {
                        data.DebugMessage = Engine1Starter.StarterMotorState.ToString();
                    }
                    else
                    {
                        data.DebugMessage = Engine1Starter.StarterMotorState.ToString();
                    }
                }
                else
                {
                    data.DebugMessage = StartupManager.StartupState.ToString();
                }
            }

            if (AdvancedDriving.CheckInst != null)
            {
                this.Train.tractionmanager.DebugWindowData.WesternEngine.CurrentRPM = ((int)CurrentRPM).ToString();
                if (Engine1Running)
                {
                    this.Train.tractionmanager.DebugWindowData.WesternEngine.RearEngineState = "Running";
                }
                else
                {
                    this.Train.tractionmanager.DebugWindowData.WesternEngine.RearEngineState = Engine2Starter.StarterMotorState.ToString();
                        //"Stopped";
                }
                if (Engine2Running)
                {
                    this.Train.tractionmanager.DebugWindowData.WesternEngine.FrontEngineState = "Running";
                }
                else
                {
                    this.Train.tractionmanager.DebugWindowData.WesternEngine.FrontEngineState = "Stopped";
                }

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