using System;
using System.Collections;
using System.Management.Instrumentation;
using OpenBveApi.Runtime;

namespace Plugin
{
    /// <summary>Represents a German PZB Device.</summary>
    internal partial class PZB : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        internal bool enabled;

        /// <summary>The aspect of the signal passed over at the last beacon.</summary>
        internal int BeaconAspect;
        /// <summary>Stores whether the Stop Override key is currently pressed (To pass red signal under authorisation).</summary>
        internal bool StopOverrideKeyPressed;
        /// <summary>The warning tone played continuosly when a red signal has been passed under authorisation.</summary>
        internal int RedSignalWarningSound = -1;
        /// <summary>The light lit continuosly when a red signal has been passed under authorisation.</summary>
        internal int RedSignalWarningLight = -1;
        /// <summary>The warning tone played continuosly whilst waiting for the driver to acknowledge a PZB DistantProgram Warning.</summary>
        internal int PZBDistantProgramWarningSound = -1;
        /// <summary>The light lit continuosly whilst waiting for the driver to acknowledge that a home signal has been passed.</summary>
        internal int HomeSignalWarningLight = -1;
        /// <summary>The sound played continuously when waiting for the driver to acknowledge a PZB HomeProgram Warning.</summary>
        internal int PZBHomeProgramWarningSound = -1;
        /// <summary>The light lit continuosly whilst waiting for the driver to acknowledge that a distant signal has been passed.</summary>
        internal int DistantSignalWarningLight = -1;
        /// <summary>The sound played continuously when waiting for the driver to acknowledge an overspeed warning.</summary>
        internal int OverSpeedWarningSound = -1;
        /// <summary>The light lit when an EB application has been triggered.</summary>
        internal int EBLight = -1;
        /// <summary>Stores whether a permenant speed restriction is currently active</summary>
        internal bool SpeedRestrictionActive;
        /// <summary>The location of the last inductor.</summary>
        internal double InductorLocation;

        internal double PZBDistantProgramMaxSpeed;
        internal double PZBHomeProgramMaxSpeed;
        internal double PZBBefehelMaxSpeed;
        /// <summary>Stores whether we've entered the switch mode of the brake curve.</summary>
        internal bool BrakeCurveSwitchMode;

        //These define the paramaters of the train
        /// <summary>Holds the classification of the train- 0 for Higher, 1 for Medium & 2 for Lower.</summary>
        internal int trainclass = 0;
        //1000hz Inductors
        /// <summary>The maximum permissable speed for this classification of train.</summary>
        internal int MaximumSpeed_1000hz;
        /// <summary>The length of the brake curve program.</summary>
        internal int BrakeCurveTime_1000hz;
        /// <summary>The target speed for the brake curve program.</summary>
        internal int BrakeCurveTargetSpeed_1000hz;
        //500hz Inductors
        /// <summary>The maximum permissable speed for this classification of train.</summary>
        internal int MaximumSpeed_500hz;
        /// <summary>The target speed for this brake curve program.</summary>
        internal double BrakeCurveTargetSpeed_500hz;


        //Timers
        internal double DistantAcknowledgementTimer;
        internal double HomeAcknowledgementTimer;
        internal double SpeedRestrictionTimer;
        internal double BrakeCurveTimer;
        internal double SwitchTimer;
        internal double BrakeReleaseTimer;

        //Internal Variables
        internal double MaxBrakeCurveSpeed;

        private PZBProgramStates PZBHomeProgramState;
        /// <summary>Gets the current warning state of the PZB System.</summary>
        internal PZBProgramStates HomeProgramState
        {
            get { return this.PZBHomeProgramState; }
        }

        private PZBProgramStates PZBDistantProgramState;
        /// <summary>Gets the current warning state of the PZB System.</summary>
        internal PZBProgramStates DistantProgramState
        {
            get { return this.PZBDistantProgramState; }
        }

        private PZBBefehelStates PZBBefehelState;
        /// <summary>Gets the current warning state of the PZB System.</summary>
        internal PZBBefehelStates BefehelState
        {
            get { return this.PZBBefehelState; }
        }

        internal PZB(Train train)
        {
            this.Train = train;
        }

        internal override void Initialize(InitializationModes mode)
        {
            PZBDistantProgramState = PZBProgramStates.None;
            PZBHomeProgramState = PZBProgramStates.None;
            PZBBefehelState = PZBBefehelStates.None;
            InductorLocation = 0;
            switch (trainclass)
            {
                case 0:
                    MaximumSpeed_1000hz = 165;
                    BrakeCurveTime_1000hz = 23000;
                    BrakeCurveTargetSpeed_1000hz = 85;
                break;
                case 1:
                    MaximumSpeed_1000hz = 125;
                    BrakeCurveTime_1000hz = 29000;
                    BrakeCurveTargetSpeed_1000hz = 70;
                break;
                case 2:
                    MaximumSpeed_1000hz = 105;
                    BrakeCurveTime_1000hz = 38000;
                    BrakeCurveTargetSpeed_1000hz = 55;
                break;
            }
        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (this.enabled)
            {
                //The distance from the last inductor
                double InductorDistance = Train.trainlocation - InductorLocation;
                //PZB DistantProgram- Distant Signal Program
                {
                    switch (PZBDistantProgramState)
                    {
                        case PZBProgramStates.SignalPassed:
                            DistantAcknowledgementTimer += data.ElapsedTime.Milliseconds;
                            if (DistantAcknowledgementTimer > 4000)
                            {
                                //If the driver fails to acknowledge the warning within 4 secs, apply EB
                                PZBDistantProgramState = PZBProgramStates.EBApplication;
                            }
                            if (PZBDistantProgramWarningSound != -1)
                            {
                                SoundManager.Play(PZBDistantProgramWarningSound, 1.0, 1.0, true);
                            }
                            if (DistantSignalWarningLight != -1)
                            {
                                this.Train.Panel[DistantSignalWarningLight] = 1;
                            }
                            break;
                        case PZBProgramStates.BrakeCurveActive:
                            //Elapse the timer first
                            BrakeCurveTimer += data.ElapsedTime.Milliseconds;

                            //First let's figure out whether the brake curve has expired
                            if (InductorDistance > 1250)
                            {
                                PZBDistantProgramState = PZBProgramStates.BrakeCurveExpired;
                                BrakeCurveTimer = 0.0;
                            }

                            //We are in the brake curve, so work out maximum speed
                            if (BrakeCurveSwitchMode == true)
                            {
                                //If we're in the switch mode, maximum speed is always 45km/h
                                PZBDistantProgramMaxSpeed = 45;
                            }
                            else
                            {
                                //Otherwise, work it out based upon the train paramaters
                                PZBDistantProgramMaxSpeed = Math.Max(BrakeCurveTargetSpeed_1000hz,
                                    MaximumSpeed_1000hz -
                                    (((MaximumSpeed_1000hz - BrakeCurveTargetSpeed_1000hz)/
                                      (double) BrakeCurveTime_1000hz)*
                                     BrakeCurveTimer));
                            }

                            //If we exceed the maximum brake curve speed, then apply EB brakes
                            if (Train.trainspeed > PZBDistantProgramMaxSpeed)
                            {
                                PZBDistantProgramState = PZBProgramStates.EBApplication;
                                BrakeCurveTimer = 0.0;
                            }

                            //If we've dropped below the switch speed, then switch to alternate speed restriction
                            if (Train.trainspeed < 10)
                            {
                                SwitchTimer += data.ElapsedTime.Milliseconds;
                                if (SwitchTimer > 15000)
                                {
                                    BrakeCurveSwitchMode = true;
                                }
                            }
                            else
                            {
                                SwitchTimer = 0.0;
                            }
                            break;
                        case PZBProgramStates.EBApplication:
                            if (PZBDistantProgramWarningSound != -1)
                            {
                                SoundManager.Stop(PZBDistantProgramWarningSound);
                            }
                            if (PZBHomeProgramWarningSound != -1)
                            {
                                SoundManager.Stop(PZBHomeProgramWarningSound);
                            }
                            //Apply EB brakes
                            Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);

                            if (Train.trainspeed < 30)
                            {
                                BrakeReleaseTimer += data.ElapsedTime.Milliseconds;
                                if (BrakeReleaseTimer > 3000)
                                {
                                    BrakeReleaseTimer = 0.0;
                                    PZBDistantProgramState = PZBProgramStates.PenaltyReleasable;
                                }
                            }
                            break;
                       case PZBProgramStates.PenaltyReleasable:
                            BrakeCurveSwitchMode = true;
                            break;
                    }
                    
                }
                //PZB HomeProgram- Home Signal Program
                {
                    switch (PZBHomeProgramState)
                    {
                        case PZBProgramStates.SignalPassed:
                            HomeAcknowledgementTimer += data.ElapsedTime.Milliseconds;
                            if (HomeAcknowledgementTimer > 4000)
                            {
                                //If the driver fails to acknowledge the warning within 4 secs, apply EB
                                PZBHomeProgramState = PZBProgramStates.EBApplication;
                            }
                            if (PZBHomeProgramWarningSound != -1)
                            {
                                SoundManager.Play(PZBHomeProgramWarningSound, 1.0, 1.0, true);
                            }
                            if (HomeSignalWarningLight != -1)
                            {
                                this.Train.Panel[HomeSignalWarningLight] = 1;
                            }
                            break;
                        case PZBProgramStates.BrakeCurveActive:
                            //First let's figure out whether the brake curve has expired
                            if (InductorDistance > 250)
                            {
                                PZBHomeProgramState = PZBProgramStates.BrakeCurveExpired;
                            }

                            //We need to work out the maximum target speed first, as this isn't fixed unlike DistantProgram
                            if (trainclass == 0)
                            {
                                BrakeCurveTargetSpeed_500hz = Math.Max(25, 45 -((20.0/153)*InductorDistance));
                            }
                            else
                            {
                                BrakeCurveTargetSpeed_500hz = 25;
                            }

                            //We are in the brake curve, so work out maximum speed
                            if (BrakeCurveSwitchMode == true)
                            {
                                if (trainclass == 0)
                                {
                                    //Fast trains have a dropping maximum speed in the switch mode
                                    PZBHomeProgramMaxSpeed =Math.Max(45 - ((20.0/153)*InductorDistance),25);
                                }
                                else
                                {
                                    PZBHomeProgramMaxSpeed = 25;
                                }
                            }
                            else
                            {
                                PZBHomeProgramMaxSpeed = Math.Max(BrakeCurveTargetSpeed_500hz,
                                MaximumSpeed_500hz -
                                (((MaximumSpeed_500hz - BrakeCurveTargetSpeed_500hz) / (double)153) * InductorDistance));
                            }
                            

                            if (Train.trainspeed > PZBHomeProgramMaxSpeed)
                            {
                                PZBHomeProgramState = PZBProgramStates.EBApplication;
                            }
                            
                            //If we've dropped below the switch speed, then switch to alternate speed restriction
                            //Fast trains require a different switch speed calculation
                            double SwitchSpeed;
                            if (trainclass == 0)
                            {
                                SwitchSpeed = 30 - ((20.0 / 153) * InductorDistance);
                            }
                            else
                            {
                                SwitchSpeed = 10;
                            }

                            if (Train.trainspeed < SwitchSpeed)
                            {
                                SwitchTimer += data.ElapsedTime.Milliseconds;
                                if (SwitchTimer > 15000)
                                {
                                    BrakeCurveSwitchMode = true;
                                }
                            }
                            else
                            {
                                SwitchTimer = 0.0;
                            }
                            break;
                        case PZBProgramStates.EBApplication:
                            if (PZBDistantProgramWarningSound != -1)
                            {
                                SoundManager.Stop(PZBDistantProgramWarningSound);
                            }
                            if (PZBHomeProgramWarningSound != -1)
                            {
                                SoundManager.Stop(PZBHomeProgramWarningSound);
                            }
                            //Apply EB brakes
                            Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                            if (Train.trainspeed < 30)
                            {
                                BrakeReleaseTimer += data.ElapsedTime.Milliseconds;
                                if (BrakeReleaseTimer > 3000)
                                {
                                    BrakeReleaseTimer = 0.0;
                                    PZBHomeProgramState = PZBProgramStates.PenaltyReleasable;
                                }
                            }
                            break;
                    }
                }
                //PZB Befehel- Allows passing of red signals under authorisation
                {
                    switch (PZBBefehelState)
                    {
                        case PZBBefehelStates.HomeStopPassed:
                            //We've passed a home stop signal which it is possible to override-
                            //Check if the override key is currently pressed
                            if (StopOverrideKeyPressed == true)
                            {
                                PZBBefehelState = PZBBefehelStates.HomeStopPassedAuthorised;
                            }
                            else
                            {
                                PZBBefehelState = PZBBefehelStates.EBApplication;
                            }
                            break;
                        case PZBBefehelStates.HomeStopPassedAuthorised:
                            if (RedSignalWarningSound != -1)
                            {
                                SoundManager.Play(RedSignalWarningSound, 1.0, 1.0, true);
                            }
                            //If the speed is not zero and less than 40km/h, check that the stop override key
                            //remains pressed
                            if ((Train.trainspeed != 0 && !StopOverrideKeyPressed) || Train.trainspeed > 40)
                            {
                                PZBBefehelState = PZBBefehelStates.EBApplication;
                            }
                            break;
                        case PZBBefehelStates.EBApplication:
                            if (RedSignalWarningSound != -1)
                            {
                                SoundManager.Stop(RedSignalWarningSound);
                            }
                            //Demand EB application
                            Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                            break;
                    }
                }

                //Panel Lights
                {
                    if (EBLight != -1)
                    {
                        if (PZBDistantProgramState == PZBProgramStates.EBApplication || PZBBefehelState == PZBBefehelStates.EBApplication || PZBHomeProgramState == PZBProgramStates.EBApplication)
                        {
                            this.Train.Panel[EBLight] = 1;
                        }
                        else
                        {
                            this.Train.Panel[EBLight] = 0;
                        }
                    }
                    if (RedSignalWarningLight != -1)
                    {
                        if (PZBBefehelState == PZBBefehelStates.HomeStopPassedAuthorised)
                        {
                            this.Train.Panel[RedSignalWarningLight] = 1;
                        }
                        else
                        {
                            this.Train.Panel[RedSignalWarningLight] = 0;
                        }
                    }
                }
                if (AdvancedDriving.CheckInst != null)
                {
                    tractionmanager.debuginformation[18] = Convert.ToString(PZBDistantProgramState);
                    tractionmanager.debuginformation[19] = Convert.ToString(PZBHomeProgramState);
                    tractionmanager.debuginformation[20] = Convert.ToString(PZBBefehelState);
                    //Work out the maximum permitted speed for all programs to pass to the debug window
                    if (PZBDistantProgramState != PZBProgramStates.None)
                    {
                        MaxBrakeCurveSpeed = PZBDistantProgramMaxSpeed;
                    }
                    if (PZBHomeProgramState != PZBProgramStates.None)
                    {
                        MaxBrakeCurveSpeed = Math.Min(MaxBrakeCurveSpeed, PZBHomeProgramMaxSpeed);
                    }
                    if (PZBBefehelState != PZBBefehelStates.None)
                    {
                        MaxBrakeCurveSpeed = Math.Min(MaxBrakeCurveSpeed, 45);
                    }
                    tractionmanager.debuginformation[21] = Convert.ToString((int)MaxBrakeCurveSpeed + " km/h");
                    tractionmanager.debuginformation[22] = Convert.ToString((int)InductorDistance + " m");
                    tractionmanager.debuginformation[23] = Convert.ToString(BrakeCurveSwitchMode);
                }
            }
           
        }

        /// <summary>Call this function to trigger a PZB alert.</summary>
        internal void Trigger(int frequency, int data)
        {
            switch (frequency)
            {
                case 1000:
                    if (BeaconAspect != 6)
                    {
                        PZBDistantProgramState = PZBProgramStates.SignalPassed;
                        DistantAcknowledgementTimer = 0.0;
                        BrakeCurveTimer = 0.0;
                        InductorLocation = Train.trainlocation;
                    }
                    break;
                case 500:
                    if (BeaconAspect != 6)
                    {
                        PZBHomeProgramState = PZBProgramStates.SignalPassed;
                        HomeAcknowledgementTimer = 0.0;
                        BrakeCurveTimer = 0.0;
                        InductorLocation = Train.trainlocation;
                    }
                    break;
                case 2000:
                    //First check if the signal is red
                    if (BeaconAspect == 0)
                    {
                        //If we're red, check if we can pass this beacon under authorisation
                        if (data == 1)
                        {
                            PZBBefehelState = PZBBefehelStates.HomeStopPassedAuthorised;
                        }
                        else
                        {
                            PZBBefehelState = PZBBefehelStates.EBApplication;
                        }
                    }
                        //The signal is clear and showing no speed restrictive aspects, so drop back to standby
                    else if (BeaconAspect == 6)
                    {
                        PZBHomeProgramState = PZBProgramStates.None;
                    }
                    else
                    {
                        //Otherwise, start the acknowledgement phase
                        PZBHomeProgramState = PZBProgramStates.SignalPassed;
                        //HomeAcknowledgementTimer = 0.0;
                    }
                    InductorLocation = Train.trainlocation;
                    break;
                case 2001:
                    break;
            }
        }

        /// <summary>Call this function to attempt to acknowledge a PZB alert.</summary>
        internal void Acknowledge()
        {
            if (PZBDistantProgramState == PZBProgramStates.SignalPassed)
            {
                if (PZBDistantProgramWarningSound != -1)
                {
                    SoundManager.Stop(PZBDistantProgramWarningSound);
                }
                if (PZBHomeProgramWarningSound != -1)
                {
                    SoundManager.Stop(PZBHomeProgramWarningSound);
                }
                PZBDistantProgramState = PZBProgramStates.BrakeCurveActive;
            }
            if (PZBHomeProgramState == PZBProgramStates.SignalPassed)
            {
                if (PZBHomeProgramWarningSound != -1)
                {
                    SoundManager.Stop(PZBHomeProgramWarningSound);
                }
                PZBHomeProgramState = PZBProgramStates.BrakeCurveActive;
            }
        }

        /// <summary>Call this function to attempt to attempt to release the current PZB restrictions.</summary>
        internal void Release()
        {
            //PZB DistantProgram
            //The current brake curve has expired
            if (PZBDistantProgramState == PZBProgramStates.BrakeCurveExpired)
            {
                BrakeCurveSwitchMode = false;
                PZBDistantProgramState = PZBProgramStates.None;
            }
            //Reset the PZB system after an EB application
            if (PZBDistantProgramState == PZBProgramStates.PenaltyReleasable)
            {
                //We can release a distant EB application, so drop back into the main program
                PZBDistantProgramState = PZBProgramStates.BrakeCurveActive;
                //Check whether any other program is holding the brakes before we call the tractionmanager to release them
                if (PZBHomeProgramState != PZBProgramStates.EBApplication && PZBBefehelState != PZBBefehelStates.EBApplication)
                {
                    Train.tractionmanager.resetbrakeapplication();
                }
            }
            //PZB HomeProgram
            //The current brake curve has expired
            if (PZBHomeProgramState == PZBProgramStates.BrakeCurveExpired)
            {
                BrakeCurveSwitchMode = false;
                PZBHomeProgramState = PZBProgramStates.None;
            }
            if (PZBHomeProgramState == PZBProgramStates.PenaltyReleasable)
            {
                //We can release a distant EB application, so drop back into the main program
                PZBHomeProgramState = PZBProgramStates.BrakeCurveActive;
                //Check whether any other program is holding the brakes before we call the tractionmanager to release them
                if (PZBDistantProgramState != PZBProgramStates.EBApplication && PZBBefehelState != PZBBefehelStates.EBApplication)
                {
                    Train.tractionmanager.resetbrakeapplication();
                }
            }
            
        }
    }


    }

