using System;

namespace Plugin
{
    partial class DieselExhaust
    {
        internal SmokeType currentSmoke { get; set; }
        /*
         * Internal variables
         */
        private double Timer;
        private bool StartupComplete;
        internal bool Sparks;

        private readonly Random randomGenerator = new Random();
        
        internal void Update(double ElapsedTime, double CurrentRPM, bool StarterActive, bool EngineRunning, Turbocharger.TurbochargerStates TurbochargerState = Turbocharger.TurbochargerStates.None)
        {
            Sparks = false;
            Timer += ElapsedTime;
            if (!EngineRunning && !StarterActive)
            {
                currentSmoke = SmokeType.None;
            }
            else if (!EngineRunning)
            {
                //Starter is cranking, generate a random smoke between medium and full white
                if (currentSmoke == SmokeType.None)
                {
                    //Generate a random smoke type
                    currentSmoke = (SmokeType) randomGenerator.Next(1, 3);
                }
                
                if (Timer > 300)
                {
                    //Reset timer and generate another random type
                    Timer = 0;
                    currentSmoke = (SmokeType) randomGenerator.Next(1, 3);
                }

            }
            else
            {
                //Engine has fired
                if (!StartupComplete)
                {
                    if (Timer < 2500)
                    {
                        currentSmoke = SmokeType.ThickBlack;
                        Sparks = true;
                    }
                    else if (Timer > 2500 && Timer < 5000)
                    {
                        currentSmoke = SmokeType.MediumBlack;
                    }
                    else
                    {
                        //Startup phase complete
                        StartupComplete = true;
                        Timer = 0;
                    }
                }
                //Startup smoke burst complete
                else
                {
                    if (CurrentRPM < 800)
                    {
                        //Current RPM is less than 800
                        currentSmoke = SmokeType.ThinBlack;
                        Timer = 0;
                    }
                    else if (CurrentRPM < 1100 && TurbochargerState != Turbocharger.TurbochargerStates.Running && TurbochargerState != Turbocharger.TurbochargerStates.RunDown)
                    {
                        //Current RPM is less than 1100, turbo is not active/ spooling down
                        currentSmoke = SmokeType.MediumBlack;
                        Timer = 0;
                    }
                    else
                    {
                        switch (TurbochargerState)
                        {
                                case Turbocharger.TurbochargerStates.None:
                                    currentSmoke = SmokeType.MediumBlack;
                                break;
                                case Turbocharger.TurbochargerStates.RunUp:
                                    currentSmoke = SmokeType.ThickBlack;
                                Sparks = true;
                                break;
                                case Turbocharger.TurbochargerStates.Running:
                                    currentSmoke = SmokeType.ThinBlack;
                                break;
                                case Turbocharger.TurbochargerStates.RunDown:
                                    currentSmoke = SmokeType.MediumBlack;
                                break;
                        }
                    }
                }
            }
        }
    }
}
