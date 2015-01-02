using System.Collections;
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
        /// <summary>The light lit when an EB application has been triggered.</summary>
        internal int EBLight = -1;
        /// <summary>The current restricted speed.</summary>
        internal int RestrictedSpeed;
        /// <summary>The location of the last inductor.</summary>
        internal double InductorLocation;

        //Timers
        internal double HomeAcknowledgementTimer;

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
                    if ((Train.trainlocation - InductorLocation) > 1250)
                    {
                        MySafetyState = SafetyStates.HomeBrakeCurveExpired;
                    }
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
                    //Home signal showing a speed restrictive aspect
                    RestrictedSpeed = data;
                    MySafetyState = SafetyStates.HomePassed;
                    HomeAcknowledgementTimer = 0.0;
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
        }
    }


    }

