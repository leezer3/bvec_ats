/* This file contains code originally derived from that developed by Stefano Zilocchi & licenced under the GPL.
 * Relicenced under BSD 2-Clause with permission
 */

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
        internal bool spiarossi_act;
        /// <summary>The SCMT self-test sequence state.</summary>
        internal static int testscmt;
        /// <summary>The SCMT self-test sequence panel variable.</summary>
        internal int testscmt_variable;
        //Default from OS_ATS??
        //Appears to be in seconds
        internal int tpwsstopdelay;
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

        internal SCMT_Traction.Indicator BrakeDemandIndicator;
        internal SCMT_Traction.Indicator TrainstopOverrideIndicator;
        internal SCMT_Traction.Indicator TraintripIndicator;
        internal SCMT_Traction.Indicator srIndicator;

        internal bool awsStop;
        internal bool awsRelease;
        internal bool tpwsRelease;
        internal double tpwstopdelay;
        internal double tpwsoverridelifetime;

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
                                        BrakeDemandIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Flashing;
                                        
                                    }
                                    else
                                    {
                                        //Stop brake demand indicator blinking?
                                        BrakeDemandIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Off;
                                    }
                                    riarmoTimer.TimeElapsed = 0;
                                }
                                StopTimer.TimerActive = true;
                                StopTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                                if ((Train.trainspeed < beacon_speed + 2 && beacon_speed > 30 && flagbrake == false) || (Train.trainspeed == 0 && StopTimer.TimeElapsed > (tpwsstopdelay*1000)) && trainstop == true)
                                {
                                    riarmoTimer.TimerActive = false;
                                    //Start brake demand indicator blinking?
                                    BrakeDemandIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Flashing;
                                    flagriarmo = true;
                                }
                            }
                            if (trainstop == false)
                            {
                                SrTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                                if (SrTimer.TimeElapsed > 5000)
                                {
                                    SrTimer.TimerActive = false;
                                    srIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Solid;
                                    srIndicator.Lit = false;
                                }
                                if (OverrideTimer.TimerActive == true)
                                {
                                    if (beacon_type == 4403)
                                    {
                                        OverrideTimer.TimerActive = false;
                                        TrainstopOverrideIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Solid;
                                        TrainstopOverrideIndicator.Lit = false;
                                        srIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Solid;
                                        srIndicator.Lit = true;
                                        TrainstopOverrideIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Solid;
                                        beacon_type = 4402;
                                        beacon_speed = 30;
                                        maxspeed = beacon_speed;
                                        SrTimer.TimeElapsed = 0;
                                    }
                                }
                                else if ((beacon_type == 4402 && Train.trainspeed > alertspeed) || (beacon_type == 4403 && beacon_signal.Aspect == 0))
                                {
                                    if (Train.trainspeed > alertspeed && beacon_type == 4403)
                                    {
                                        if (AlarmTimer.TimerActive == false)
                                        {
                                            AlarmTimer.TimerActive = true;
                                            AlarmTimer.TimeElapsed = 0;
                                        }
                                        //Play looped overspeed alarm- Probably need to disable the base BVEC_ATS overspeed alarm
                                        spiarossi_act = true;
                                        if (Train.Handles.BrakeNotch == 0 && brakeNotchDemanded == 0 && testscmt == 4)
                                        {
                                            data.Handles.PowerNotch = 0;
                                            brakeNotchDemanded = 1;
                                            tgtraz_active = true;
                                        }
                                        if (Train.trainspeed > maxspeed + 4)
                                        {
                                            //Play looped TPWSWarningSound
                                            EBDemanded = true;
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
                                }
                                else
                                {
                                    //Blink traintrip indicator
                                    TraintripIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Flashing;
                                    //Play tpwswarningsound looping
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

                if (Train.trainspeed == 0)
                {
                    if (StopTimer.TimerActive == false)
                    {
                        StopTimer.TimerActive = true;
                        StopTimer.TimeElapsed = 0;
                    }
                    else
                    {
                        if (awsRelease == true)
                        {
                            //Set AWS indicator to -1
                            awsStop = false;
                            awsRelease = false;
                        }
                        if (tpwsRelease == true && StopTimer.TimeElapsed > tpwstopdelay*1000)
                        {
                            tractionmanager.resetbrakeapplication();
                            BrakeDemandIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Solid;
                            BrakeDemandIndicator.Lit = false;
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
                        TrainstopOverrideIndicator.IndicatorState = SCMT_Traction.IndicatorStates.Solid;
                        TrainstopOverrideIndicator.Lit = false;
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
                }
                //Brake Demand Indicator
                if (BrakeDemandIndicator.PanelIndex != -1)
                {
                    if (BrakeDemandIndicator.IndicatorState == SCMT_Traction.IndicatorStates.Solid)
                    {
                        BrakeDemandIndicator.Lit = true;
                    }
                    else if (BrakeDemandIndicator.IndicatorState == SCMT_Traction.IndicatorStates.Flashing)
                    {
                        BrakeDemandIndicator.TimeElapsed += data.ElapsedTime.Milliseconds;
                        if (BrakeDemandIndicator.TimeElapsed > 500)
                        {
                            if (BrakeDemandIndicator.Lit == true)
                            {
                                BrakeDemandIndicator.Lit = false;
                            }
                            else
                            {
                                BrakeDemandIndicator.Lit = true;
                            }
                            BrakeDemandIndicator.TimeElapsed = 0.0;
                        }
                    }
                }

                //Trainstop Override Indicator
                //Repurposed by SCMT
                if (TrainstopOverrideIndicator.PanelIndex != -1)
                {
                    if (TrainstopOverrideIndicator.IndicatorState == SCMT_Traction.IndicatorStates.Solid)
                    {
                        TrainstopOverrideIndicator.Lit = true;
                    }
                    else if (TrainstopOverrideIndicator.IndicatorState == SCMT_Traction.IndicatorStates.Flashing)
                    {
                        TrainstopOverrideIndicator.TimeElapsed += data.ElapsedTime.Milliseconds;
                        if (TrainstopOverrideIndicator.TimeElapsed > 500)
                        {
                            if (TrainstopOverrideIndicator.Lit == true)
                            {
                                TrainstopOverrideIndicator.Lit = false;
                            }
                            else
                            {
                                TrainstopOverrideIndicator.Lit = true;
                            }
                            TrainstopOverrideIndicator.TimeElapsed = 0.0;
                        }
                    }
                }

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
    }
}
