namespace Plugin
{
    /// <summary>Represents a generic part affected by heat.</summary>
    class Temperature
    {
        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        /// <summary>The current temperature of this part.</summary>
        internal double InternalTemperature;
        /// <summary>The rate at which this part heats up in temperature units/ second.</summary>
        internal int HeatingRate;
        /// <summary>The maximum temperature of this part.</summary>
        internal int MaximumTemperature;
        /// <summary>The overheat temperature of this part (Must be less than or equal to the maxiumum temperature).</summary>
        internal int OverheatTemperature;
        /// <summary>The temperature at which overheating resets.</summary>
        internal int ResetTemperature;
        /// <summary>The floor below which the temperature does not drop whilst heat is applied.</summary>
        internal int FloorTemperature;
        /// <summary>Whether this temperature is overheated.</summary>
        internal bool Overheated;
        /// <summary>**INTERNAL LOGGING USE**</summary>
        internal bool Logged;

        /// <summary>Returns whether this part is currently overheated.</summary>
        /// <param name="ElapsedTime">The elapsed time since the last function call</param>
        /// <param name="Multiplier">The multiplier to be applied to the result of the overheat calculation</param>
        /// <param name="Increase">Whether the temperature is to increase or decrease</param>
        /// <param name="Active">Whether this part is currently active</param>
        internal void Update(double ElapsedTime, int Multiplier, bool Increase, bool Active)
        {
            //Calculate the temperature
            double TemperatureChange = ((HeatingRate/1000.0) * ElapsedTime) * Multiplier;
            //Change the temperature value
            if (Increase && InternalTemperature < MaximumTemperature)
            {
                InternalTemperature += TemperatureChange;
            }
            else if (Active && !Increase && InternalTemperature > FloorTemperature)
            {
                InternalTemperature -= TemperatureChange;
            }
            else if (!Active && !Increase && InternalTemperature > 0)
            {
                InternalTemperature -= TemperatureChange;
            }

            if (InternalTemperature > OverheatTemperature)
            {
                //If internal temperature is greater than the overheat, trip
                Overheated = true;
            }

            if (InternalTemperature < ResetTemperature && Overheated)
            {
                //Our temperature is now less than the reset temperature, so return to normal
                Overheated = false;
            }
        }
    }
}
