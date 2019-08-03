using System;
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
        /// <summary>Stores whether the fuel pump is currently isolated.</summary>
        internal bool FuelPumpIsolated = false;
        /// <summary>Stores whether the fire bell is currently ringing.</summary>
        internal bool FireBell = false;
        /// <summary>Stores whether we are currently in engine-only mode.</summary>
        internal bool EngineOnly = false;

        /// <summary>The panel variable for the current state of the instrument cluster lights (Drivers).</summary>
        internal int ILCluster1 = -1;
        /// <summary>The panel variable for the current state of the instrument cluster lights (Secondmans).</summary>
        internal int ILCluster2 = -1;
        /// <summary>The panel variable for the master key.</summary>
        internal int MasterKeyIndex = -1;
        /// <summary>The panel variable for the fuel pump isolation switch.</summary>
        internal int FuelPumpSwitchIndex = -1;
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

        /// <summary>The sound index played when the engine is stopped.</summary>
        internal int EngineStopSound = -1;

        internal bool EngineLoop = false;
        /*
         * This bool should be toggled when an engine starts-
         * It allows us to hold-on the brakes until the air compressors prototypically start
         * 
         * NOTE: This hack also means that it is not possible to coast with no engines running.
         */
        internal bool CompressorsRunning = false;
        internal double RPMTimer;
        /// <summary>The sound index for the DSD Buzzer.</summary>
        internal int DSDBuzzer = -1;
        /// <summary>The sound index for the DSD Buzzer.</summary>
        internal int NeutralSelectedSound = -1;
        /// <summary>The sound index for the battery isolation switch & master switch.</summary>
        internal int SwitchSound = -1;
        /// <summary>The sound index for the master key insertion/ removal.</summary>
        internal int MasterKeySound = -1;
        /// <summary>The sound index for the fire bell test.</summary>
        internal int FireBellSound = -1;
		/// <summary>The panel index for the fire bell test.</summary>
	    internal int FireBellIndex = -1;
        /// <summary>Whether the radiator shutters are open</summary>
        internal bool RadiatorShuttersOpen = true;
        /// <summary>The panel index for the radiator shutters</summary>
        internal int RadiatorShutters = -1;

        internal int Engine1Smoke = -1;

        internal int Engine1Sparks = -1;

        internal int Engine2Smoke = -1;

        internal int Engine2Sparks = -1;


        /*
         * 
         * Internal engine classes
         * 
         */
        internal readonly StarterMotor Engine1Starter = new StarterMotor();
        internal readonly StarterMotor Engine2Starter = new StarterMotor();
        internal readonly WesternStartupManager StartupManager = new WesternStartupManager();
        internal readonly WesternGearBox GearBox = new WesternGearBox();
        internal readonly Turbocharger Turbocharger = new Turbocharger();
        internal readonly Temperature Engine1Temperature = new Temperature();
        internal readonly Temperature Engine2Temperature = new Temperature();
        internal readonly Temperature TransmissionTemperature = new Temperature();
        internal readonly DieselExhaust Engine1Exhaust = new DieselExhaust();
        internal readonly DieselExhaust Engine2Exhaust = new DieselExhaust();

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
                if (EngineSelector == 1 && StartupManager.StartupState >= WesternStartupManager.SequenceStates.ReadyToStart)
                {
                    //If this method returns true, then our engine is now running
                    if (Engine1Starter.RunComplexStarter(data.ElapsedTime.Milliseconds, StarterKeyPressed, !FuelPumpIsolated))
                    {
                        Engine1Running = true;
                        //Reset the power cutoff
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 1 started.");
                        Train.TractionManager.ResetPowerCutoff();
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
                if (EngineSelector == 2 && StartupManager.StartupState >= WesternStartupManager.SequenceStates.ReadyToStart)
                {
                    if (Engine2Starter.RunComplexStarter(data.ElapsedTime.Milliseconds, StarterKeyPressed, !FuelPumpIsolated))
                    {
                        Engine2Running = true;
                        //Reset the power cutoff
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 2 started.");
                        Train.TractionManager.ResetPowerCutoff(); 
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
                if (Train.TractionManager.PowerCutoffDemanded == false)
                {
	                Train.TractionManager.DemandPowerCutoff("Western Diesel- Traction power was cutoff due to no available engines.");
                }
                if (Train.TractionManager.BrakeInterventionDemanded == false)
                {
	                Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches, "Western Diesel- Brakes applied due to no running compressors.");
                }
            }
            else
            {
                if (CompressorsRunning == false)
                {
                    CompressorsRunning = true;
                    if (Train.TractionManager.BrakeInterventionDemanded == true)
                    {
                        Train.DebugLogger.LogMessage("Western Diesel- An attempt was made to reset the current brake application due to the compressors starting.");
                        Train.TractionManager.ResetBrakeApplication();
                    }
                }
                if (GearBox.TorqueConvertorState != WesternGearBox.TorqueConvertorStates.OnService)
                {
                    //If the torque convertor is not on service, then cut power
                    if (Train.TractionManager.PowerCutoffDemanded == false)
                    {
	                    Train.TractionManager.DemandPowerCutoff("Western Diesel- Traction power was cutoff due the torque convertor being out of service.");
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
                            Train.TractionManager.ResetPowerCutoff();
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
            bool TurboBoost = Turbocharger.RunTurbocharger(data.ElapsedTime.Milliseconds, (int) CurrentRPM);
            if (Train.Handles.PowerNotch != 0 && Train.TractionManager.PowerCutoffDemanded == false)
            {
                //An engine may be running but not providing power
                var EnginesProvidingPower = NumberOfEnginesRunning;
                if (EngineOnly != true)
                {
                    switch (NumberOfEnginesRunning)
                    {
                        case 1:
                            if (Engine1Running && Engine1Temperature.Overheated || Engine2Running && Engine2Temperature.Overheated)
                            {
                                EnginesProvidingPower = 0;
                            }
                            break;
                        case 2:
                            if (Engine1Temperature.Overheated)
                            {
                                EnginesProvidingPower -= 1;
                            }
                            if (Engine2Temperature.Overheated)
                            {
                                EnginesProvidingPower -= 1;
                            }
                            break;
                    }
                }
                else
                {
                    //If in engine only mode, no engines are providing power, but we still want the nice sound effects!
                    EnginesProvidingPower = 0;
                }
                data.Handles.PowerNotch = Train.WesternDiesel.GearBox.PowerNotch((int)CurrentRPM,EnginesProvidingPower,TurboBoost);
            }
            //Update exhaust smoke
            {
                bool Starter1 = EngineSelector == 1 && StarterKeyPressed == true;
                Engine1Exhaust.Update(data.ElapsedTime.Milliseconds,CurrentRPM,Starter1,Engine1Running,Turbocharger.TurbochargerState);
                bool Starter2 = EngineSelector == 2 && StarterKeyPressed == true;
                Engine2Exhaust.Update(data.ElapsedTime.Milliseconds, CurrentRPM, Starter2, Engine2Running,Turbocharger.TurbochargerState);
            }
            //This section of code handles engine & transmission temperatures
            {
                if (Engine1Running)
                {
                    //Increase rates:
                    // 0x - Shutters open & speed above 20kph
                    // 1x - Shutters open, speed above 20kph, turbo
                    // 2x - Shutters closed, speed above 20kph & turbo OR speed below 20mph
                    //Decrease rates:
                    // 1x- RPM below 1000, speed below 20kph or shutters closed
                    // 2x- RPM below 1000, speed above 20kph & shutters open
                    //Only increase temperature if it's less than 500 or the current RPM is greater than 1000
                    
	                var Multiplier = 0;
	                if (Train.CurrentSpeed > 20)
	                {
						//Greater than 20kph one notch on the cooling
		                Multiplier -= 1;
	                }
	                else
	                {
		                Multiplier += 1;
	                }
	                if (RadiatorShuttersOpen)
	                {
						//Radiator shutters are open, therefore cool faster
		                Multiplier -= 1;
	                }
	                else
	                {
						//Closed, so cool slower
		                Multiplier += 1;
	                }
	                if (Turbocharger.TurbochargerState == Turbocharger.TurbochargerStates.None)
	                {
		                Multiplier -= 1;
	                }
	                else
	                {
		                Multiplier += 1;
	                }
					//Current RPM is less than 650
					if (CurrentRPM < 650)
					{
						Multiplier -= 1;
					}
					//Above 1000, therefore we must be heating
					if (CurrentRPM > 1000)
					{
						Multiplier += 1;
					}
                    var Increase = Multiplier > 0;
	                if (Increase && Engine1Temperature.InternalTemperature < Engine1Temperature.FloorTemperature)
	                {
						//Increase faster upto the floor temperature
		                Multiplier *= 2;
	                }
					else if (!Increase &&Engine1Temperature.InternalTemperature > Engine1Temperature.MaximumTemperature - Engine1Temperature.FloorTemperature)
					{
						//Faster decrease when we've overheated
						Multiplier *= 2;
					}
                    Engine1Temperature.Update(data.ElapsedTime.Milliseconds,Math.Abs(Multiplier),Increase,true);
                }
                else
                {
                    var Multiplier = 1;
                    if (Train.CurrentSpeed > 20)
                    {
                        Multiplier += 1;
                    }
                    if (RadiatorShuttersOpen)
                    {
                        Multiplier += 1;
                    }
                    Engine1Temperature.Update(data.ElapsedTime.Milliseconds,Multiplier, false, false);
                }
                if (Engine2Running)
                {
                    //Increase rates:
                    // 0x - Shutters open & speed above 20kph
                    // 1x - Shutters open, speed above 20kph, turbo
                    // 2x - Shutters closed, speed above 20kph & turbo OR speed below 20mph
                    //Decrease rates:
                    // 1x- RPM below 1000, speed below 20kph or shutters closed
                    // 2x- RPM below 1000, speed above 20kph & shutters open
                    //Only increase temperature if it's less than 500 or the current RPM is greater than 1000
					var Multiplier = 0;
					if (Train.CurrentSpeed > 20)
					{
						//Greater than 20kph one notch on the cooling
						Multiplier -= 1;
					}
					else
					{
						Multiplier += 1;
					}
					if (RadiatorShuttersOpen)
					{
						//Radiator shutters are open, therefore cool faster
						Multiplier -= 1;
					}
					else
					{
						//Closed, so cool slower
						Multiplier += 1;
					}
					if (Turbocharger.TurbochargerState == Turbocharger.TurbochargerStates.None)
					{
						Multiplier -= 1;
					}
					else
					{
						Multiplier += 1;
					}
					//Current RPM is less than 650
					if (CurrentRPM < 650)
					{
						Multiplier -= 1;
					}
					//Above 1000, therefore we must be heating
					if (CurrentRPM > 1000)
					{
						Multiplier += 1;
					}
					var Increase = Multiplier > 0;
					if (Increase && Engine2Temperature.InternalTemperature < Engine2Temperature.FloorTemperature)
					{
						//Increase faster upto the floor temperature
						Multiplier *= 2;
					}
					else if (!Increase && Engine2Temperature.InternalTemperature > Engine2Temperature.MaximumTemperature - Engine2Temperature.FloorTemperature)
					{
						//Faster decrease when we've overheated
						Multiplier *= 2;
					}
					Engine2Temperature.Update(data.ElapsedTime.Milliseconds, Math.Abs(Multiplier), Increase, true);
                }
                else
                {
                    var Multiplier = 1;
                    if (Train.CurrentSpeed > 20)
                    {
                        Multiplier += 1;
                    }
                    if (RadiatorShuttersOpen)
                    {
                        Multiplier += 1;
                    }
                    Engine1Temperature.Update(data.ElapsedTime.Milliseconds, Multiplier, false, false);
                }
                var TransmissionMultiplier = 0;
                if (CurrentRPM < 1000)
                {
                    TransmissionMultiplier -= 1;
                }
                else if (NumberOfEnginesRunning == 1 && CurrentRPM > 1300)
                {
                    TransmissionMultiplier += 1;
                }
                else if (NumberOfEnginesRunning == 2 && CurrentRPM > 1200)
                {
                    TransmissionMultiplier += 1;
                }
                var TransmissionIncrease = TransmissionMultiplier > 0;
                //Last paramater for transmission temp is always false, as it does not have a floor temperature
                TransmissionTemperature.Update(data.ElapsedTime.Milliseconds, Math.Abs(TransmissionMultiplier),TransmissionIncrease, false);
                //Consequences
                /*
                 *  Engine 1
                 */
                if (Engine1Temperature.Overheated == true)
                {
                    if (Engine1Temperature.Logged == false)
                    {
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 1 overheated");
                        if (Engine2Running == false)
                        {
                            Train.TractionManager.DemandPowerCutoff("Western Diesel- Traction power was cutoff due to engine 1 overheating, and engine 2 being stopped");
                        }
                        else if (Engine2Temperature.Overheated)
                        {
                            Train.TractionManager.DemandPowerCutoff("Western Diesel- Traction power was cutoff due to both engines overheating");
                        }
                        Engine1Temperature.Logged = true;
                    }
                }
                else if(Engine1Temperature.Logged == true && Engine1Temperature.Overheated == false)
                {
                    Train.DebugLogger.LogMessage("Western Diesel- Engine 1 returned to normal operating temperature");
                    Train.TractionManager.ResetPowerCutoff();
                    Engine1Temperature.Logged = false;
                }
                /*
                 *  Engine 2
                 */
                if (Engine2Temperature.Overheated == true)
                {
                    if (Engine2Temperature.Logged == false)
                    {
                        Train.DebugLogger.LogMessage("Western Diesel- Engine 2 overheated");
                        if (Engine1Running == false)
                        {
                            Train.TractionManager.DemandPowerCutoff("Western Diesel- Traction power was cutoff due to engine 2 overheating, and engine 1 being stopped");
                        }
                        else if (Engine1Temperature.Overheated)
                        {
                            Train.TractionManager.DemandPowerCutoff("Western Diesel- Traction power was cutoff due to both engines overheating");
                        }
                        Engine2Temperature.Logged = true;
                    }
                }
                else if(Engine2Temperature.Logged == true)
                {
                    Train.DebugLogger.LogMessage("Western Diesel- Engine 2 returned to normal operating temperature");
                    Train.TractionManager.ResetPowerCutoff();
                    Engine2Temperature.Logged = false;
                }
                /*
                 *  Transmission
                 */
                if (TransmissionTemperature.Overheated == true)
                {
                    if (TransmissionTemperature.Logged == false)
                    {
	                    Train.TractionManager.DemandPowerCutoff("Western Diesel- Transmission overheated");
                        TransmissionTemperature.Logged = true;
                    }
                }
                else if (TransmissionTemperature.Logged == true && TransmissionTemperature.Overheated == false)
                {
                    if ((Engine1Running && !Engine1Temperature.Overheated) || (Engine2Running && !Engine2Temperature.Overheated))
                    {
                        Train.TractionManager.ResetPowerCutoff();
                    }
                    Train.DebugLogger.LogMessage("Western Diesel- Transmission returned to normal operating temperature");
                    TransmissionTemperature.Logged = false;
                }
                
            }
            //This section of code handles the startup self-test routine
            if (StartupManager.StartupState != WesternStartupManager.SequenceStates.ReadyToStart || StartupManager.StartupState != WesternStartupManager.SequenceStates.AWSOnline)
            {
                switch (StartupManager.StartupState)
                {
                    case WesternStartupManager.SequenceStates.Pending:
                        if (FireBell == true)
                        {
                            //We have isolated the battery, but the fire bell is currently ringing- Stop
                            FireBell = false;
                            SoundManager.Stop(FireBellSound);
                        }
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
                            //Play 3 clicks sound effect
                            SoundManager.Play(NeutralSelectedSound, 0.5, 1.0, false);
                        }
                        break;
                    case WesternStartupManager.SequenceStates.NeutralSelected:
                        StartupManager.StartupState = WesternStartupManager.SequenceStates.ReadyToStart;
                        break;
                    case WesternStartupManager.SequenceStates.MasterKeyRemoved:
                        if (BatteryIsolated)
                        {
                            StartupManager.StartupState = WesternStartupManager.SequenceStates.Pending;
                        }
                        else
                        {
                            StartupManager.StartupState = WesternStartupManager.SequenceStates.BatteryEnergized;
                        }
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
                        //Minor hack- Torque convertor cannot come off service....
                        if (GearBox.TorqueConvertorState != WesternGearBox.TorqueConvertorStates.OnService)
                        {
                            this.Train.Panel[ILCluster2] = 1;
                        }
                        else
                        {
                            //Something has overheated
                            if ((Engine1Running && Engine1Temperature.Overheated) || (Engine2Running && Engine2Temperature.Overheated) && TransmissionTemperature.Overheated)
                            {
                                if (RadiatorShuttersOpen)
                                {
                                    //The radiator shutters are open, so the high oil temp light should be lit
                                    this.Train.Panel[ILCluster2] = 6;
                                }
                                else
                                {
                                    //The radiator shutters are closed, so the high oil and water temp lights should be lit
                                    this.Train.Panel[ILCluster2] = 5;
                                }
                            }
                            else if ((Engine1Running && Engine1Temperature.Overheated) || (Engine2Running && Engine2Temperature.Overheated) && !TransmissionTemperature.Overheated)
                            {
                                //High water temp light lit
                                if (RadiatorShuttersOpen)
                                {
                                    this.Train.Panel[ILCluster2] = 3;
                                }
                                else
                                {
                                    this.Train.Panel[ILCluster2] = 4;
                                }
                            }
                            else if (TransmissionTemperature.Overheated)
                            {
                                //Just the transmission
                                this.Train.Panel[ILCluster2] = 7;
                            }
                            else
                            {
                                //All blue
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
                        if (FuelPumpIsolated)
                        {
                            this.Train.Panel[ILCluster1] = 8;
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
                                if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress ||
                                    Engine1Temperature.Overheated)
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
                                if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress ||
                                    Engine2Temperature.Overheated)
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
                                if (GearBox.TorqueConvertorState == WesternGearBox.TorqueConvertorStates.FillInProgress ||
                                    (Engine1Temperature.Overheated || Engine2Temperature.Overheated))
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
                }
                if (MasterKeyIndex != -1)
                {
                    //If the startup sequence is greater than or equal to 3, then the master key has been inserted
                    if ((int)StartupManager.StartupState >= 3)
                    {
                        this.Train.Panel[MasterKeyIndex] = 1;
                    }
                    else
                    {
                        this.Train.Panel[MasterKeyIndex] = 0;   
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
                if (FuelPumpSwitchIndex != -1)
                {
                    if (FuelPumpIsolated == true)
                    {
                        this.Train.Panel[FuelPumpSwitchIndex] = 1;
                    }
                    else
                    {
                        this.Train.Panel[FuelPumpSwitchIndex] = 0;
                    }
                }
                if (Engine1Smoke != -1)
                {
                    this.Train.Panel[Engine1Smoke] = (int) Engine1Exhaust.currentSmoke;
                }
                if (Engine1Sparks != -1)
                {
                    this.Train.Panel[Engine1Sparks] = Engine1Exhaust.Sparks;
                }
                if (Engine2Smoke != -1)
                {
                    this.Train.Panel[Engine2Smoke] = (int)Engine2Exhaust.currentSmoke;
                }
                if (Engine2Sparks != -1)
                {
                    this.Train.Panel[Engine2Sparks] = Engine2Exhaust.Sparks;
                }
	            if (FireBellIndex != -1)
	            {
		            if (FireBell)
		            {
			            this.Train.Panel[FireBellIndex] = 1;
		            }
		            else
		            {
						this.Train.Panel[FireBellIndex] = 0;
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
            if (AdvancedDriving.CheckInst != null)
            {
                this.Train.TractionManager.DebugWindowData.WesternEngine.CurrentRPM = ((int)CurrentRPM).ToString();
                if (Engine1Running)
                {
                    this.Train.TractionManager.DebugWindowData.WesternEngine.RearEngineState = "Running";
                }
                else
                {
                    this.Train.TractionManager.DebugWindowData.WesternEngine.RearEngineState = Engine2Starter.StarterMotorState.ToString();
                }
                if (Engine2Running)
                {
                    this.Train.TractionManager.DebugWindowData.WesternEngine.FrontEngineState = "Running";
                }
                else
                {
                    this.Train.TractionManager.DebugWindowData.WesternEngine.RearEngineState = Engine1Starter.StarterMotorState.ToString();
                }
                this.Train.TractionManager.DebugWindowData.WesternEngine.TorqueConverterState = GearBox.TorqueConvertorState.ToString();
                this.Train.TractionManager.DebugWindowData.WesternEngine.Engine1Temperature = (int)Engine1Temperature.InternalTemperature + " of " + Engine1Temperature.MaximumTemperature;
                this.Train.TractionManager.DebugWindowData.WesternEngine.Engine2Temperature = (int)Engine2Temperature.InternalTemperature + " of " + Engine2Temperature.MaximumTemperature;
                this.Train.TractionManager.DebugWindowData.WesternEngine.TransmissionTemperature = (int)TransmissionTemperature.InternalTemperature + " of " + TransmissionTemperature.MaximumTemperature;
                this.Train.TractionManager.DebugWindowData.WesternEngine.TurbochargerState = Turbocharger.TurbochargerState.ToString();
            }
        }

        /// <summary>This method should be called when the battery switch is toggled</summary>
        internal void BatterySwitch()
        {
            SoundManager.Play(SwitchSound, 1.0, 1.0, false);
            //Toggle isolation state
            if (BatteryIsolated == true)
            {
                BatteryIsolated = false;
            }
            else
            {
                BatteryIsolated = true;
                StartupManager.StartupState = WesternStartupManager.SequenceStates.Pending;
            }
        }

        /// <summary>This method should be called when the master key is inserted or removed</summary>
        internal void MasterKey()
        {
            SoundManager.Play(MasterKeySound, 1.0, 1.0, false);
            if (StartupManager.StartupState == WesternStartupManager.SequenceStates.BatteryEnergized)
            {
                StartupManager.StartupState = WesternStartupManager.SequenceStates.MasterKeyInserted;
            }
            else
            {
                StartupManager.StartupState = WesternStartupManager.SequenceStates.MasterKeyRemoved;
            }
        }

        /// <summary>This method should be called to isolate or enable the fuel pump</summary>
        internal void FuelPumpSwitch()
        {
            SoundManager.Play(SwitchSound, 1.0, 1.0, false);
            if (FuelPumpIsolated == true)
            {
                FuelPumpIsolated = false;
            }
            else
            {
                FuelPumpIsolated = true;
                EngineStop(0);
            }
        }

        /// <summary>This method should be called to attempt to stop one or more engines</summary>
        internal void EngineStop(int type)
        {
            switch (type)
            {
                case 0:
                    //Stop all running engines
                    SoundManager.Stop(EngineLoopSound);
                    switch (NumberOfEnginesRunning)
                    {
                        case 1:
                            SoundManager.Play(EngineStopSound, 0.5, 1.0, false);
                            break;
                        case 2:
                            SoundManager.Play(EngineStopSound, 1.0, 1.0, false);
                            break;
                    }
                    Engine1Running = false;
                    Engine2Running = false;
                    //We have stopped the engine, so the starter motor state must be reset to none, otherwise we can't restart.....
                    Engine1Starter.StarterMotorState = StarterMotor.StarterMotorStates.None;
                    Engine2Starter.StarterMotorState = StarterMotor.StarterMotorStates.None;
                    break;
                case 1:
                    //Stop near engine
                    switch (NumberOfEnginesRunning)
                    {
                        case 1:
                            SoundManager.Stop(EngineLoopSound);
                            SoundManager.Play(EngineStopSound, 0.5, 1.0, false);
                            break;
                        case 2:
                            SoundManager.Play(EngineLoopSound, 0.5, 1.0, false);
                            SoundManager.Play(EngineStopSound, 1.0, 1.0, false);
                            break;
                    }
                    Engine1Running = false;
                    break;
                case 2:
                    //Stop far engine
                    switch (NumberOfEnginesRunning)
                    {
                        case 1:
                            SoundManager.Stop(EngineLoopSound);
                            SoundManager.Play(EngineStopSound, 0.5, 1.0, false);
                            break;
                        case 2:
                            SoundManager.Play(EngineLoopSound, 0.5, 1.0, false);
                            SoundManager.Play(EngineStopSound, 1.0, 1.0, false);
                            break;
                    }
                    Engine2Running = false;
                    break;
            }
            if (!Engine1Running && !Engine2Running)
            {
                EngineLoop = false;
            }
        }

        /// <summary>This method should be called to attempt to initialise or cancel a fire-bell test</summary>
        internal void FireBellTest()
        {
            if (StartupManager.StartupState == WesternStartupManager.SequenceStates.Pending || FireBell == true)
            {
                FireBell = false;
                SoundManager.Stop(FireBellSound);
            }
            else
            {
                FireBell = true;
                SoundManager.Play(FireBellSound, 1.0, 1.0, true);
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
                Train.TractionManager.IsolateTPWSAWS();
                return;
            }
            //If the AWS has been isolated by the driver, issue an unconditional reset
            if (Train.AWS.SafetyState == AWS.SafetyStates.Isolated)
            {
                Train.DebugLogger.LogMessage("Western Diesel- AWS System reset.");
                Train.TractionManager.reenabletpwsaws();
            }
        }
    }
}