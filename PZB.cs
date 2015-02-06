﻿using System.Collections;
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
        /// <summary>The warning tone played continuosly whilst waiting for the driver to acknowledge that a restrictive speed home signal has been passed.</summary>
        internal int HomeSignalWarningSound = -1;
        /// <summary>The light lit continuosly whilst waiting for the driver to acknowledge that a restrictive speed home signal has been passed.</summary>
        internal int HomeSignalWarningLight = -1;
        /// <summary>The sound played continuously when waiting for the driver to acknowledge an overspeed warning.</summary>
        internal int OverSpeedWarningSound = -1;
        /// <summary>The light lit when an EB application has been triggered.</summary>
        internal int EBLight = -1;
        /// <summary>Stores whether a permenant speed restriction is currently active</summary>
        internal bool SpeedRestrictionActive;
        /// <summary>The current restricted speed.</summary>
        internal int RestrictedSpeed;
        /// <summary>The location of the last inductor.</summary>
        internal double InductorLocation;

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
        internal int BrakeCurveTargetSpeed_500hz;


        //Timers
        internal double HomeAcknowledgementTimer;
        internal double SpeedRestrictionTimer;

        private SafetyStates MySafetyState;
        /// <summary>Gets the current warning state of the PZB System.</summary>
        internal SafetyStates SafetyState
        {
            get { return this.MySafetyState; }
        }

        internal PZB(Train train)
        {
            this.Train = train;
        }

        internal override void Initialize(InitializationModes mode)
        {
            MySafetyState = SafetyStates.None;
            RestrictedSpeed = 0;
            InductorLocation = 0;
            switch (trainclass)
            {
                case 0:
                    MaximumSpeed_1000hz = 165;
                    BrakeCurveTime_1000hz = 23;
                    BrakeCurveTargetSpeed_1000hz = 85;
                break;
                case 1:
                    MaximumSpeed_1000hz = 125;
                    BrakeCurveTime_1000hz = 29;
                    BrakeCurveTargetSpeed_1000hz = 70;
                break;
                case 2:
                    MaximumSpeed_1000hz = 105;
                    BrakeCurveTime_1000hz = 38;
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
                

                if (MySafetyState == SafetyStates.HomePassed)
                {
                    HomeAcknowledgementTimer += data.ElapsedTime.Milliseconds;
                    if (HomeAcknowledgementTimer > 4000)
                    {
                        //If the driver fails to acknowledge the warning within 4 secs, apply EB
                        MySafetyState = SafetyStates.HomeStopEBApplication;
                    }
                    if (HomeSignalWarningSound != -1)
                    {
                        SoundManager.Play(HomeSignalWarningSound, 1.0, 1.0, true);
                    }
                    if (HomeSignalWarningLight != -1)
                    {
                        this.Train.Panel[HomeSignalWarningLight] = 1;
                    }
                }
                else if (MySafetyState == SafetyStates.HomeBrakeCurveActive)
                {
                    //The distance from the last inductor
                    double InductorDistance = Train.trainlocation - InductorLocation;

                    //First let's figure out whether the brake curve has expired
                    if (InductorDistance > 1250)
                    {
                        MySafetyState = SafetyStates.HomeBrakeCurveExpired;
                    }

                    //We are in the brake curve, so work out the speed
                    //double TargetBrakeCurveSpeed 

                }
                else if (MySafetyState == SafetyStates.HomeStopPassed)
                {
                    //We've passed a home stop signal which it is possible to override-
                    //Check if the override key is currently pressed
                    if (StopOverrideKeyPressed == true)
                    {
                        MySafetyState = SafetyStates.HomeStopPassedAuthorised;
                    }
                    else
                    {
                        MySafetyState = SafetyStates.HomeStopEBApplication;
                    }
                }
                else if (MySafetyState == SafetyStates.HomeStopPassedAuthorised)
                {
                    if (RedSignalWarningSound != -1)
                    {
                        SoundManager.Play(RedSignalWarningSound, 1.0, 1.0, true);
                    }
                    //If the speed is not zero and less than 40km/h, check that the stop override key
                    //remains pressed
                    if ((Train.trainspeed != 0 && !StopOverrideKeyPressed) || Train.trainspeed > 40)
                    {
                        MySafetyState = SafetyStates.HomeStopEBApplication;
                    }
                }
                else if (MySafetyState == SafetyStates.HomeStopEBApplication)
                {
                    if (RedSignalWarningSound != -1)
                    {
                        SoundManager.Stop(RedSignalWarningSound);
                    }
                    //Demand EB application
                    Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                }
                else if (MySafetyState == SafetyStates.SpeedRestrictionAcknowledgement)
                {
                    SpeedRestrictionTimer += data.ElapsedTime.Milliseconds;
                    if (SpeedRestrictionTimer > 4000)
                    {
                        MySafetyState = SafetyStates.SpeedRestrictionBrake;
                        SpeedRestrictionTimer = 0.0;
                    }
                }
                else if (MySafetyState == SafetyStates.SpeedRestrictionBrakeCurve)
                {

                    double InductorDistance = Train.trainlocation - InductorLocation;

                }
                else if (MySafetyState == SafetyStates.SpeedRestrictionBrake)
                {
                    Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches +1);
                }

                //Panel Lights
                {
                    if (EBLight != -1)
                    {
                        if (MySafetyState == SafetyStates.DistantEBApplication || MySafetyState == SafetyStates.HomeStopEBApplication)
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
                        if (MySafetyState == SafetyStates.HomeStopPassedAuthorised)
                        {
                            this.Train.Panel[RedSignalWarningLight] = 1;
                        }
                        else
                        {
                            this.Train.Panel[RedSignalWarningLight] = 0;
                        }
                    }
                }
            }
        }

        /// <summary>Call this function to trigger a PZB alert.</summary>
        internal void Trigger(int frequency, int data)
        {
            switch (frequency)
            {
                case 1000:
                    //Permenant Speed Reduction
                    RestrictedSpeed = data;
                    InductorLocation = Train.trainlocation;
                    break;
                case 500:
                    //Home signal speed control
                    break;
                case 2000:
                    //First check if the signal is red
                    if (BeaconAspect == 0)
                    {
                        //If we're red, check if we can pass this beacon under authorisation
                        if (data == 1)
                        {
                            MySafetyState = SafetyStates.HomeStopPassed;
                        }
                        else
                        {
                            MySafetyState = SafetyStates.HomeStopEBApplication;
                        }
                    }
                        //The signal is clear and showing no speed restrictive aspects, so drop back to standby
                    else if (BeaconAspect == 6)
                    {
                        MySafetyState = SafetyStates.None;
                    }
                    break;
                case 2001:
                    //Home signal showing a potentially speed restrictive aspect
                    RestrictedSpeed = data;
                    if (BeaconAspect == 0)
                    {
                        //If this signal is RED, trigger an EB application immediately
                        MySafetyState = SafetyStates.HomeStopEBApplication;
                    }
                    else
                    {
                        //Otherwise, start the acknowledgement phase
                        MySafetyState = SafetyStates.HomePassed;
                        HomeAcknowledgementTimer = 0.0;
                    }
                    InductorLocation = Train.trainlocation;
                    break;
            }
        }

        /// <summary>Call this function to attempt to acknowledge a PZB alert.</summary>
        internal void Acknowledge()
        {
            if (MySafetyState == SafetyStates.HomePassed)
            {
                MySafetyState = SafetyStates.HomeBrakeCurveActive;
            }
            if (MySafetyState == SafetyStates.SpeedRestrictionAcknowledgement)
            {
                MySafetyState = SafetyStates.SpeedRestrictionBrakeCurve;
            }
        }
    }


    }

