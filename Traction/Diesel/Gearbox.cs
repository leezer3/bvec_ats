using System;
using System.Collections.Generic;
using System.Globalization;


namespace Plugin
{
    /// <summary>Represents a generic OS_ATS gearbox.</summary>
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
        /// <summary>The sound played once when the gear is changed up</summary>
        internal int GearUpSound = -1;
	    /// <summary>The sound played once when the gear is changed down</summary>
	    internal int GearDownSound = -1;
        /// <summary>A comma separated list storing the current gear ratios.</summary>
        internal string GearRatios;
        /// <summary>A comma separated list storing the current gear fade in ranges.</summary>
        internal string GearFadeInRanges;
        /// <summary>A comma separated list storing the current gear fade out ranges.</summary>
        internal string GearFadeOutRanges;
		/// <summary>Whether gear changes are currently blocked</summary>
	    internal bool Blocked = false;

	    internal bool Loop = false;

	    internal double LoopTimer;
		/// <summary>Whether this gearbox is currently in automatic mode</summary>
	    internal bool Automatic = false;

	    internal GearBox(Train train)
	    {
		    this.Train = train;
	    }

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
                        gearfadeinarray[i] = (int)double.Parse(splitgearfade[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
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
                        Gears[i].GearRatio = geararray[i];
                        Gears[i].FadeInRange = gearfadeinarray[i];
                        Gears[i].FadeOutRange = gearfadeoutarray[i];
                        i++;
                    }
                }
                else
                {
                    InternalFunctions.LogError("gearratios, gearfadeinrange and gearfadeoutrange should be of identical lengths.",6);
                }
            }
            //Return the total number of gears plus one [Neutral]
            TotalGears = Gears.Count + 1;
        }

        internal class GearSpecification
        {
            internal int GearRatio;
            internal int FadeInRange;
            internal int FadeOutRange;
        }

        internal List<GearSpecification> Gears = new List<GearSpecification>();

        /// <summary>This method runs the gearbox power calculations and returns the current power notch</summary>
        internal int Run()
        {
			UpdateAutomaticGears();
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
                CurrentRPM = Math.Max(0, Math.Min(1000, Train.CurrentSpeed * Gears[CurrentGear].GearRatio));
                if (CurrentRPM < Gears[CurrentGear].FadeInRange)
                {
                    CalculatedPowerNotch = (int)((float)CurrentRPM / Gears[CurrentGear].FadeInRange * this.Train.Specs.PowerNotches);
                }
                else if (CurrentRPM > 1000 - Gears[CurrentGear].FadeOutRange)
                {
                    CalculatedPowerNotch = (int)(this.Train.Specs.PowerNotches - (float)(CurrentRPM - (1000 - Gears[CurrentGear].FadeOutRange)) / Gears[CurrentGear].FadeOutRange * this.Train.Specs.PowerNotches);
                }
                else
                {
                    CalculatedPowerNotch = this.Train.Specs.PowerNotches;
                }
            }
			if (Blocked)
	        {
		        CalculatedPowerNotch = 0;
	        }
            return CalculatedPowerNotch;
        }

	    private void UpdateAutomaticGears()
	    {
		    if (Blocked)
		    {
			    if (Train.CurrentSpeed == 0 && Train.Handles.Reverser == 0 && Train.Handles.PowerNotch == 0)
			    {
				    //Stop, drop to N with no power applied and the gears will unblock
				    Blocked = false;
			    }
		    }

		    if (Train.Handles.Reverser != 0 && Train.Handles.PowerNotch != 0 && Train.Handles.BrakeNotch != 0)
		    {
			    if (CurrentGear == 0 && Blocked == false)
			    {
				    GearUp();
			    }

			    if (CurrentRPM > Math.Min((2000 - Gears[CurrentGear].FadeOutRange) / 2, 800) && CurrentGear < Gears.Count - 1)
			    {
				    GearUp();
			    }
			    //Change down
			    else if (CurrentRPM < Math.Max(Gears[CurrentGear].FadeInRange / 2, 200) && CurrentGear > 1)
			    {
				    GearDown();
			    }
			    else if (Train.Handles.Reverser == 0 && Train.Handles.PowerNotch == 0 && CurrentGear != 0)
			    {
				    SetGear(0);
			    }
		    }
	    }

        /// <summary>Call to attempt to increase the current gear</summary>
        internal void GearUp()
        {
            if (CurrentGear < TotalGears)
            {
                SoundManager.Play(GearUpSound, 1.0, 1.0, false);
                CurrentGear++;
	            Loop = false;
	            LoopTimer = 0.0;
            }

        }

        /// <summary>Call to attempt to decrease the current gear</summary>
        internal void GearDown()
        {
            if (CurrentGear > 1)
            {
                SoundManager.Play(GearDownSound, 1.0, 1.0, false);
                CurrentGear--;
            }
        }

		/// <summary>Call to set a specific gear</summary>
		/// <param name="Gear">The gear to set</param>
	    internal void SetGear(int Gear)
	    {
		    if (Gear < CurrentGear)
		    {
			    SoundManager.Play(GearDownSound, 1.0, 1.0, false);
		    }
		    else
		    {
			    SoundManager.Play(GearUpSound, 1.0, 1.0, false);
		    }
		    CurrentGear = Gear;
	    }
	}
}
