using System;
using System.Collections.Generic;
using System.Globalization;


namespace Plugin
{
    class GearBox
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        /// <summary>Stores the current gear</summary>
        internal int CurrentGear;
        /// <summary>Stores the total number of gears</summary>
        private int TotalGears;
        /// <summary>Stores the current RPM of the engine</summary>
        private int CurrentRPM;
        /// <summary>The sound played once when the gear is changed</summary>
        private int GearChangeSound = -1;

        /// <summary>A comma separated list storing the current gear ratios.</summary>
        internal string GearRatios;
        /// <summary>A comma separated list storing the current gear fade in ranges.</summary>
        internal string GearFadeInRanges;
        /// <summary>A comma separated list storing the current gear fade out ranges.</summary>
        internal string GearFadeOutRanges;

        /// <summary>Call this function to initialise the gearbox</summary>
        internal void Initialize()
        {
            var geararray = new int[] { };
            var gearfadeinarray = new int[] { };
            var gearfadeoutarray = new int[] { };
            //First, test whether we have a correctly setup gearbox
            if (GearRatios != null && GearFadeInRanges != null && GearFadeOutRanges != null)
            {

                try
                {
                    //Split gear ratios into an array
                    var splitgearratios = GearRatios.Split(',');
                    geararray = new int[splitgearratios.Length];
                    for (var i = 0; i < geararray.Length; i++)
                    {
                        geararray[i] = (int)(double.Parse(splitgearratios[i], CultureInfo.InvariantCulture));
                    }
                }
                catch
                {
                    InternalFunctions.LogError("gearratios",0);
                }
                try
                {
                    //Split gear fade in range into an array
                    var splitgearfade = GearFadeOutRanges.Split(',');
                    gearfadeinarray = new int[splitgearfade.Length];
                    for (var i = 0; i < gearfadeinarray.Length; i++)
                    {
                        gearfadeinarray[i] =
                            (int)double.Parse(splitgearfade[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                }
                catch
                {
                    InternalFunctions.LogError("gearfadeinrange",0);
                }
                try
                {
                    //Split gear fade out range into an array
                    var splitgearfade1 = GearFadeOutRanges.Split(',');
                    gearfadeoutarray = new int[splitgearfade1.Length];
                    for (var i = 0; i < gearfadeoutarray.Length; i++)
                    {
                        gearfadeoutarray[i] =
                            (int)double.Parse(splitgearfade1[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                }
                catch
                {
                    InternalFunctions.LogError("gearfadeoutrange",0);
                }

                if (geararray.Length != gearfadeinarray.Length && geararray.Length != gearfadeoutarray.Length)
                {
                    var i = 0;
                    while (i < geararray.Length)
                    {
                        //Add the gear to the gearbox
                        Gear[i].GearRatio = geararray[i];
                        Gear[i].FadeInRange = gearfadeinarray[i];
                        Gear[i].FadeOutRange = gearfadeoutarray[i];
                        i++;
                    }
                }
                else
                {
                    InternalFunctions.LogError("gearratios, gearfadeinrange and gearfadeoutrange should be of identical lengths.",6);
                }
            }
            //Return the total number of gears plus one [Neutral]
            TotalGears = Gear.Count + 1;
        }

        internal class GearSpecification
        {
            internal int GearRatio;
            internal int FadeInRange;
            internal int FadeOutRange;
        }

        internal List<GearSpecification> Gear = new List<GearSpecification>();

        /// <summary>This method runs the gearbox power calculations and returns the current power notch</summary>
        internal int RunGearBox()
        {
            int CalculatedPowerNotch;
            if (CurrentGear == 0)
            {
                /* We are currently in neutral gear-
                 * Set the current RPM to zero
                 * Set the max power notch to zero */
                CurrentRPM = 0;
                CalculatedPowerNotch = 0;
            }
            else
            {
                /* We are currently in a gear-
                 * First calculate the current RPM
                 * Then check the fade in/ out ranges & return the max power notch */
                CurrentRPM = Math.Max(0, Math.Min(1000, Train.trainspeed * Gear[CurrentGear].GearRatio));
                if (CurrentRPM < Gear[CurrentGear].FadeInRange)
                {
                    CalculatedPowerNotch = (int)((float)CurrentRPM / Gear[CurrentGear].FadeInRange * this.Train.Specs.PowerNotches);
                }
                else if (CurrentRPM > 1000 - Gear[CurrentGear].FadeOutRange)
                {
                    CalculatedPowerNotch = (int)(this.Train.Specs.PowerNotches - (float)(CurrentRPM - (1000 - Gear[CurrentGear].FadeOutRange)) / Gear[CurrentGear].FadeOutRange * this.Train.Specs.PowerNotches);
                }
                else
                {
                    CalculatedPowerNotch = this.Train.Specs.PowerNotches;
                }
            }
            
            return CalculatedPowerNotch;
        }

        /// <summary>Call to attempt to increase the current gear</summary>
        internal void GearUp()
        {
            if (CurrentGear < TotalGears)
            {
                SoundManager.Play(GearChangeSound, 1.0, 1.0, false);
                CurrentGear++;
            }

        }

        /// <summary>Call to attempt to decrease the current gear</summary>
        internal void GearDown()
        {
            if (CurrentGear > 1)
            {
                SoundManager.Play(GearChangeSound, 1.0, 1.0, false);
                CurrentGear--;
            }
        }
    }
}
