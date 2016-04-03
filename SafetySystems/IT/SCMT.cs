/* This file contains code originally derived from that developed by Stefano Zilocchi & licenced under the GPL.
 * Relicenced under BSD 2-Clause with permission
 */

using System.IO;
using OpenBveApi.Runtime;

namespace Plugin
{
    /// <summary>Represents an Italian SCMT Device.</summary>
    internal class SCMT : Device
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        // --- members ---
        internal bool enabled;

        //Internal Status
        /// <summary>Whether a SCMT alert has been triggered</summary>
        internal static bool SCMT_Alert;
        /// <summary>The data type of the last beacon recieved</summary>
        internal int beacon_type;
        /// <summary>The signal data of the last beacon recieved</summary>
        internal SignalData beacon_signal;
        /// <summary>The signal aspect for the last beacon 44005 recieved</summary>
        /// This beacon is a SCMT addition
        internal int beacon_44005;
        /// <summary>The speed set by the last beacon recieved</summary>
        internal int beacon_speed;
        /// <summary>The set speed</summary>
        internal int speed;
        /// <summary>The maximum permissable speed</summary>
        internal int maxspeed;
        /// <summary>The speed at which an alert will be triggered</summary>
        internal int alertspeed;
        /// <summary>Used to set the braking curve flag</summary>
        internal bool curveflag;
        /// <summary>The distance to the signal controlled by the last beacon recieved</summary>
        internal double beacon_distance;

        //Stores whether something is active??
        internal bool tgtraz_active;
        //The variable for this
        internal int trgraz;


        internal int phase;
        //Stores the state value of the SCMT self-test [Move to ENUM later]
        //internal int testscmt_state;

        //Panel Variables
        /// <summary>SCMT Safety Intervention Light.</summary>
        internal static int spiaSCMT = -1;
        /// <summary>Blue light for SCMT safety device.</summary>
        internal int spiablue = -1;
        //Stores whether the blue light has been triggered
        internal bool spiablue_act;
        /// <summary>Red light for SCMT safety device.</summary>
        internal int spiarossi = -1;
        //Stores whether the red light has been triggered
        internal static bool spiarossi_act;
        /// <summary>The SCMT self-test sequence state.</summary>
        internal static int testscmt;
        /// <summary>The SCMT self-test sequence panel variable.</summary>
        internal int testscmt_variable = -1;
        //Default from OS_ATS??
        //Appears to be in seconds
        internal double tpwsstopdelay;
        //Brake flag has been triggered
        internal bool flagbrake;
        //We can now rearm I think
        internal bool flagriarmo;

        internal static int brakeNotchDemanded;
        internal SCMT_Traction.Timer SpiabluTimer;
        internal SCMT_Traction.Timer SpiaRossiTimer;
        internal SCMT_Traction.Timer StopTimer;
        //Rearm timer?
        internal SCMT_Traction.Timer riarmoTimer;
        internal SCMT_Traction.Timer SrTimer;
        internal SCMT_Traction.Timer OverrideTimer;
        internal SCMT_Traction.Timer AlarmTimer;
        internal SCMT_Traction.Timer campionamentoTimer;

        internal SCMT_Traction.Indicator BrakeDemandIndicator;
        internal SCMT_Traction.Indicator TrainstopOverrideIndicator;
        internal SCMT_Traction.Indicator TraintripIndicator;
        internal SCMT_Traction.Indicator srIndicator;

        internal bool awsStop;
        internal bool awsRelease;
        internal bool tpwsRelease;
        internal bool flagtgtraz;
        internal double tpwstopdelay;
        internal double tpwsoverridelifetime;
        internal static int tpwswarningsound = -1;
        internal int overspeedalarm = -1;
        internal int awsindicator = -1;

        internal static bool EBDemanded;
        
        //Sound Variables
        /// <summary>Trigger sound for SCMT safety device.</summary>
        internal int sound_scmt = -1;
        internal bool trainstop;
        

        internal SCMT(Train train)
        {
            this.Train = train;
        }

        

        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            //Load indicators default values
            BrakeDemandIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Off;
            BrakeDemandIndicator.Lit = false;
            BrakeDemandIndicator.FlashInterval = 1000;
            TrainstopOverrideIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Off;
            TrainstopOverrideIndicator.Lit = false;
            TrainstopOverrideIndicator.FlashInterval = 1000;
            TraintripIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Off;
            TraintripIndicator.Lit = false;
            TraintripIndicator.FlashInterval = 1000;
            srIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Off;
            srIndicator.Lit = false;
            srIndicator.FlashInterval = 1000;
            //Initialise Default Paramaters
            beacon_speed = 500;
            speed = 500;
            alertspeed = 500;
            beacon_type = 44004;
            beacon_signal = new SignalData(4,0);
            beacon_44005 = 4;
            beacon_distance = 0;
            SCMT_Alert = true;
            flagbrake = false;
            tgtraz_active = false;

        }

        internal void Reinitialise(InitializationModes mode)
        {

        }

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (this.enabled)
            {
                //First, we need to sort out the distance from our beacon. This should be easier in OpenBVE...
                //Assume that below zero values won't cause issues just for the moment
                if (beacon_distance > 0)
                {
                    beacon_distance -= (Train.trainlocation - Train.previouslocation);
                }

                //Update the beacon distance values with those calculated from the current speed
                if (beacon_distance > 0)
                {
                    if (campionamentoTimer.TimerActive == false)
                    {
                        campionamentoTimer.TimerActive = true;
                    }
                    campionamentoTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                    if (campionamentoTimer.TimeElapsed > 2000)
                    {
                        beacon_distance = beacon_distance - speed * 2;
                        campionamentoTimer.TimerActive = false;
                    }
                }

                //If we're in the alert phase
                if (SCMT_Alert == true)
                {
                    //We now need to check if the signal of the last beacon 4405 is at danger or the SCMT braking curve has been triggered
                    if (beacon_44005 == 0 || curveflag == true)
                    {
                        //Split this into it's own function for ease of reading
                        brakecurve(data.ElapsedTime.Milliseconds);
                    }
                    //The last code block figured out the maxiumum permissable speeds
                    //Now run the interventions
                    if (testscmt != 0 && SCMT_Alert == true)
                    {
                        //First check the trainspeed and reset the overspeed trip if applicable
                        if (Train.trainspeed < alertspeed + 1)
                        {
                            //If we're no longer over the alert speed, reset the overspeed device
                            Train.overspeedtripped = false;

                            AlarmTimer.TimerActive = false;
                            //If the tgtraz is active, the red light is not lit and we're in state 4
                            if (tgtraz_active == true && spiarossi_act == false && testscmt == 4)
                            {
                                if (SCMT_Traction.indlcm >= 0 && Train.Handles.PowerNotch == 0)
                                {
                                    brakeNotchDemanded = Train.Handles.BrakeNotch;
                                    data.Handles.PowerNotch = SCMT_Traction.indlcm;
                                }
                                else
                                {
                                    brakeNotchDemanded = Train.Handles.BrakeNotch;
                                    data.Handles.PowerNotch = Train.Handles.PowerNotch;
                                }
                                tgtraz_active = false;
                            }
                        }
                        if (SpiabluTimer.TimerActive == true)
                        {
                            SpiabluTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (SpiabluTimer.TimeElapsed > 500)
                            {
                                if (spiablue_act == true)
                                {
                                    spiablue_act = false;
                                }
                                else
                                {
                                    spiablue_act = true;
                                }
                                SpiabluTimer.TimeElapsed = 0;
                            }
                        }
                        if (SpiaRossiTimer.TimerActive == true)
                        {
                            SpiaRossiTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (SpiaRossiTimer.TimeElapsed > 500)
                            {
                                if (spiarossi_act == true)
                                {
                                    spiarossi_act = false;
                                }
                                else
                                {
                                    spiarossi_act = true;
                                }
                                SpiaRossiTimer.TimeElapsed = 0;
                            }
                        }
                        if (riarmoTimer.TimerActive == true)
                        {
                            riarmoTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (riarmoTimer.TimeElapsed > 300)
                            {
                                if (brakeNotchDemanded == 0)
                                {
                                    //Start brake demand indicator blinking?
                                    //Or does it want the indicator state toggled?
                                    BlinkIndicator(ref BrakeDemandIndicator, 1, data.ElapsedTime.Milliseconds);

                                }
                                else
                                {
                                    //Stop brake demand indicator blinking?
                                    BlinkIndicator(ref BrakeDemandIndicator, 0, data.ElapsedTime.Milliseconds);
                                }
                                riarmoTimer.TimeElapsed = 0;
                            }
                            StopTimer.TimerActive = true;
                            StopTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if ((Train.trainspeed < beacon_speed + 2 && beacon_speed > 30 && flagbrake == false) ||
                                (Train.trainspeed == 0 && StopTimer.TimeElapsed > (tpwsstopdelay*1000)) &&
                                trainstop == true)
                            {
                                riarmoTimer.TimerActive = false;
                                BlinkIndicator(ref BrakeDemandIndicator, 1, data.ElapsedTime.Milliseconds);
                                flagriarmo = true;
                            }
                        }
                        if (trainstop == false)
                        {
                            SrTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (SrTimer.TimeElapsed > 5000)
                            {
                                SrTimer.TimerActive = false;
                                SetIndicator(ref srIndicator, 0);
                            }
                            if (OverrideTimer.TimerActive == true)
                            {
                                if (beacon_type == 44003)
                                {
                                    OverrideTimer.TimerActive = false;
                                    SetIndicator(ref TrainstopOverrideIndicator, 0);
                                    SetIndicator(ref srIndicator, 1);
                                    beacon_type = 44002;
                                    beacon_speed = 30;
                                    maxspeed = beacon_speed;
                                    SrTimer.TimeElapsed = 0;
                                }
                            }
                            else if ((beacon_type == 4402 && Train.trainspeed > alertspeed) ||
                                     (beacon_type == 4403 && beacon_signal.Aspect == 0))
                            {
                                if (Train.trainspeed > alertspeed && beacon_type == 44003)
                                {
                                    if (AlarmTimer.TimerActive == false)
                                    {
                                        AlarmTimer.TimerActive = true;
                                        AlarmTimer.TimeElapsed = 0;
                                    }
                                    if (overspeedalarm != -1)
                                    {
                                        SoundManager.Play(overspeedalarm, 1.0, 1.0, true);
                                    }
                                    spiarossi_act = true;
                                    if (Train.Handles.BrakeNotch == 0 && brakeNotchDemanded == 0 &&
                                        testscmt == 4)
                                    {
                                        data.Handles.PowerNotch = 0;
                                        brakeNotchDemanded = 1;
                                        tgtraz_active = true;
                                    }
                                    if (Train.trainspeed > maxspeed + 4)
                                    {
                                        if (tpwswarningsound != -1)
                                        {
                                            SoundManager.Play(tpwswarningsound, 1.0, 1.0, true);
                                        }
                                        EBDemanded = true;
                                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                                        trainstop = true;
                                        StopTimer.TimerActive = false;
                                        tpwsRelease = false;
                                        AlarmTimer.TimerActive = false;
                                        if (riarmoTimer.TimerActive == false)
                                        {
                                            riarmoTimer.TimerActive = true;
                                        }
                                        if (SpiaRossiTimer.TimerActive == false && spiarossi_act == false)
                                        {
                                            SpiaRossiTimer.TimerActive = true;
                                            //maybe needs to be a separate whatsit, come back to this
                                            spiarossi_act = true;
                                        }
                                    }

                                }

                                else
                                {
                                    BlinkIndicator(ref TraintripIndicator, 1, data.ElapsedTime.Milliseconds);
                                    if (tpwswarningsound != -1)
                                    {
                                        SoundManager.Play(tpwswarningsound, 1.0, 1.0, true);
                                    }
                                    EBDemanded = true;
                                    trainstop = true;
                                    StopTimer.TimerActive = false;
                                    tpwsRelease = false;
                                    flagbrake = true;
                                    if (riarmoTimer.TimerActive == false)
                                    {
                                        riarmoTimer.TimerActive = true;
                                    }
                                    if (SpiaRossiTimer.TimerActive == false && spiarossi_act == false)
                                    {
                                        SpiaRossiTimer.TimerActive = true;
                                        spiarossi_act = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (Train.trainspeed == 0)
            {
                if (StopTimer.TimerActive == false)
                {
                    StopTimer.TimerActive = true;
                    StopTimer.TimeElapsed = 0;
                }
                else
                {
                    /*
                         * 
                         * TODO:
                         * Do we need the AWS release? Appears to only be triggered by the AWS startup test
                         * 
                         */
                    if (awsRelease == true)
                    {
                        if (awsindicator != -1)
                        {
                            awsindicator = 0;
                        }
                        awsStop = false;
                        awsRelease = false;
                    }
                    if (tpwsRelease == true && StopTimer.TimeElapsed > tpwstopdelay*1000)
                    {
                        EBDemanded = false;
                        Train.tractionmanager.resetbrakeapplication();
                        SetIndicator(ref BrakeDemandIndicator, 0);
                        tpwsRelease = false;
                        trainstop = false;
                    }

                }
            }
            if (OverrideTimer.TimerActive == true)
            {
                OverrideTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                if (OverrideTimer.TimeElapsed > tpwsoverridelifetime*1000)
                {
                    SetIndicator(ref TrainstopOverrideIndicator, 0);
                }
            }

            //Set Panel Lights
            if (spiaSCMT != -1)
            {
                if (SCMT_Alert == true)
                {
                    this.Train.Panel[spiaSCMT] = 1;
                }
                else
                {
                    this.Train.Panel[spiaSCMT] = 0;
                }
                if (spiarossi != -1)
                {
                    if (spiarossi_act == false && (phase == 0 || phase == 2))
                    {
                        this.Train.Panel[spiarossi] = 0;
                    }
                    else
                    {
                        this.Train.Panel[spiarossi] = 1;
                    }
                }
                if (spiablue != -1)
                {
                    if (spiablue_act == true)
                    {
                        this.Train.Panel[spiablue] = 1;
                    }
                    else
                    {
                        this.Train.Panel[spiablue] = 0;
                    }
                }
                if (testscmt_variable != -1)
                {
                    this.Train.Panel[testscmt_variable] = testscmt;
                }
            }
            //Brake Demand Indicator
            if (BrakeDemandIndicator.PanelIndex != -1)
            {
                
            }

            //Trainstop Override Indicator
            //Repurposed by SCMT
            if (TrainstopOverrideIndicator.PanelIndex != -1)
            {
                
            }
            if (TraintripIndicator.PanelIndex != -1)
            {
                
            }
            if (srIndicator.PanelIndex != -1)
            {
                
            }

        }


        //Call this function from the main beacon manager
        internal void trigger()
        {
            if (SCMT_Alert == false)
            {
                SCMT_Alert = true;
                //Trigger audible alert if this is set
                if (sound_scmt != -1)
                {
                    SoundManager.Play(sound_scmt, 1.0, 1.0, false);
                }
            }
        }

        /// <summary>This function is called from the traction manager when the TPWS reset key is pressed.</summary>
        internal void tpwsresetkey()
        {
            if (trainstop == true && this.enabled == true)
            {
                if (flagbrake == true)
                {
                    SetIndicator(ref TraintripIndicator, 1);
                    SetIndicator(ref BrakeDemandIndicator, 1);
                    Train.tractionmanager.resetbrakeapplication();

                    //Something is a little funnny here
                    //This would appear to reset the lights instantly?
                    SetIndicator(ref TraintripIndicator, 0);
                    SetIndicator(ref BrakeDemandIndicator, 0);
                    if (beacon_type == 44003)
                    {
                        beacon_type = 44004;
                    }
                    if (tpwswarningsound != -1)
                    {
                        SoundManager.Stop(tpwswarningsound);
                    }
                    flagbrake = false;
                    flagriarmo = false;
                    SpiaRossiTimer.TimerActive = false;
                    spiarossi_act = false;
                    //flagspiarossi = false; ???
                }
                else
                {
                    SetIndicator(ref BrakeDemandIndicator, 1);
                    if (flagriarmo == true)
                    {
                        flagriarmo = false;
                        Train.tractionmanager.resetbrakeapplication();
                        SetIndicator(ref BrakeDemandIndicator, 0);
                        trainstop = false;
                        if (beacon_type == 44003)
                        {
                            beacon_type = 44004;
                        }
                        if (tpwswarningsound != -1)
                        {
                            SoundManager.Stop(tpwswarningsound);
                        }
                        SpiaRossiTimer.TimerActive = false;
                        spiarossi_act = false;
                    }
                }
            }
        }

        /// <summary>This function is called from the traction manager when the TPWS override key is pressed.</summary>
        internal void TPWSOverrideKey()
        {
            if (OverrideTimer.TimerActive == false && Train.trainspeed <= 30)
            {
                OverrideTimer.TimeElapsed = 0;
                //TODO: BLINKINDICATOR
            }
            else
            {
                if (Train.trainspeed <= 30)
                {
                    OverrideTimer.TimeElapsed = 0;
                }
            }
        }

        /// <summary>This function is called by the main elapse function to calculate the current requireed braking curve.</summary>
        internal void brakecurve(double time)
        {
            //Run through the braking curve
            if (beacon_distance < 1200 && beacon_distance > 1000)
            {
                alertspeed = 117;
                if (AlarmTimer.TimerActive == true)
                {
                    AlarmTimer.TimeElapsed += time;
                    if (AlarmTimer.TimeElapsed > 5000)
                    {
                        maxspeed = 115;
                    }
                }
            }
            else if (beacon_distance < 1000 && beacon_distance > 800)
            {
                alertspeed = 102;
                if (AlarmTimer.TimerActive == true)
                {
                    AlarmTimer.TimeElapsed += time;
                    if (AlarmTimer.TimeElapsed > 5000)
                    {
                        maxspeed = 100;
                    }
                }
            }
            if (beacon_distance < 800 && beacon_distance > 700)
            {
                alertspeed = 92;
                if (AlarmTimer.TimerActive == true)
                {
                    AlarmTimer.TimeElapsed += time;
                    if (AlarmTimer.TimeElapsed > 5000)
                    {
                        maxspeed = 90;
                    }
                }
            }
            if (beacon_distance < 700 && beacon_distance > 600)
            {
                alertspeed = 82;
                if (AlarmTimer.TimerActive == true)
                {
                    AlarmTimer.TimeElapsed += time;
                    if (AlarmTimer.TimeElapsed > 5000)
                    {
                        maxspeed = 80;
                    }
                }
            }
            if (beacon_distance < 600 && beacon_distance > 500)
            {
                alertspeed = 72;
                if (AlarmTimer.TimerActive == true)
                {
                    AlarmTimer.TimeElapsed += time;
                    if (AlarmTimer.TimeElapsed > 5000)
                    {
                        maxspeed = 70;
                    }
                }
            }
            if (beacon_distance < 500 && beacon_distance > 400)
            {
                alertspeed = 62;
                if (AlarmTimer.TimerActive == true)
                {
                    AlarmTimer.TimeElapsed += time;
                    if (AlarmTimer.TimeElapsed > 5000)
                    {
                        maxspeed = 60;
                    }
                }
            }
            if (beacon_distance < 400 && beacon_distance > 300)
            {
                alertspeed = 45;
                if (AlarmTimer.TimerActive == true)
                {
                    AlarmTimer.TimeElapsed += time;
                    if (AlarmTimer.TimeElapsed > 5000)
                    {
                        maxspeed = 43;
                    }
                }
            }
            if (beacon_distance < 300 && beacon_distance > 220)
            {
                alertspeed = 33;
                if (AlarmTimer.TimerActive == true)
                {
                    AlarmTimer.TimeElapsed += time;
                    if (AlarmTimer.TimeElapsed > 5000)
                    {
                        maxspeed = 31;
                    }
                }
            }
        }

        internal void BlinkIndicator(ref SCMT_Traction.Indicator i, int x, double time)
        {
            i.Value = x;
            i.Timer.TimeElapsed = time;
            i.Active = true;
        }

        internal void SetIndicator(ref SCMT_Traction.Indicator i, int x)
        {
            i.Value = x;
            i.Timer.TimerActive = false;
            i.Timer.TimeElapsed = 0;
        }
    }
}
