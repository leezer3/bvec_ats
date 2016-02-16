using System;

namespace Plugin
{
    partial class DieselExhaust
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        /// <summary>The panel index for exhaust smoke</summary>
        internal int SmokeIndex = -1;
        /// <summary>The panel index for exhaust sparks</summary>
        internal int SparksIndex = -1;

        /*
         * Internal variables
         */
        internal double Timer;
        internal bool Startup;
        internal bool Startup2;

        internal Random randomGenerator = new Random();
        
        internal void Update(double ElapsedTime, int CurrentRPM, int PreviousRPM, bool StarterActive, bool EngineRunning, bool TurbochargerActive)
        {
            if (SmokeIndex == -1 && SparksIndex == -1)
            {
                //Don't bother running any calculations if no panel indices set
                return;
            }
            SmokeType currentSmoke = SmokeType.None;
            if (SmokeIndex != -1)
            {
                if (!EngineRunning && !StarterActive)
                {
                    //Nothing happening- No smoke
                    currentSmoke = SmokeType.None;
                }
                else if (!EngineRunning)
                {
                    //Starter is cranking, generate a random smoke between medium and full white
                    if (currentSmoke == SmokeType.None)
                    {
                        //Generate a random smoke type
                        currentSmoke = (SmokeType) randomGenerator.Next(1,2);
                    }
                    else
                    {
                        Timer += ElapsedTime;
                        if (Timer > 1500)
                        {
                            //Reset timer and generate another random type
                            Timer = 0;
                            currentSmoke = (SmokeType)randomGenerator.Next(1, 2);
                        }
                    }
                }
                else
                {
                    //Engine has fired
                    if (!Startup)
                    {
                        //In the startup phase
                        if (!Startup2)
                        {
                            Timer = 0;
                            Startup2 = true;
                        }
                        currentSmoke = SmokeType.MediumBlack;
                        Timer += ElapsedTime;
                        if (Timer > 2500 && Timer < 10000)
                        {
                            currentSmoke = SmokeType.ThickBlack;
                        }
                        else if (Timer > 10000 && Timer < 12500)
                        {
                            currentSmoke = SmokeType.MediumBlack;
                        }
                        else
                        {
                            //Startup phase complete
                            Startup = true;
                            Timer = 0;
                        }
                    }
                    else
                    {
                        if (CurrentRPM - PreviousRPM > 10 && !TurbochargerActive)
                        {
                            //RPM is currently changing
                            currentSmoke = SmokeType.MediumBlack;
                        }
                        else if (CurrentRPM == PreviousRPM && !TurbochargerActive)
                        {
                            //Steady RPM, no turbo
                            currentSmoke = SmokeType.ThinBlack;
                        }
                        else
                        {
                            //Turbo is in run-up phase
                            Timer += ElapsedTime;
                            if (Timer > 2000)
                            {
                                currentSmoke = SmokeType.ThickBlack;
                            }
                            else
                            {
                                currentSmoke = SmokeType.ThinBlack;
                            }
                        }
                    }
                }
                this.Train.Panel[SmokeIndex] = (int)currentSmoke;
            }
            if (SparksIndex != -1)
            {
                this.Train.Panel[SmokeIndex] = 0;
            }
        }
    }
}
