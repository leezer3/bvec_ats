namespace Plugin
{
	/// <summary>Defines a basic ammeter</summary>
	class Ammeter : Component
	{
		/// <summary>An array storing the ammeter values for each power notch</summary>
		internal int[] NotchValues;

		internal Ammeter(Train train)
		{
			this.Train = train;
		}

		internal void Initialize(string ammeterValues)
		{
			InternalFunctions.ParseStringToIntArray(ammeterValues, ref NotchValues, "ammetervalues");
			if (NotchValues.Length == 0 || NotchValues == null)
			{
				NotchValues = new int[] { 0 };
			}
		}

		/// <summary>Gets the current ammeter value</summary>
		internal int GetCurrentValue()
		{
			if (Train.Handles.Reverser == 0 || Train.Handles.BrakeNotch != 0 || Train.Handles.PowerNotch == 0 || NotchValues == null)
			{
				return 0;
			}
			if (Train.Handles.PowerNotch < NotchValues.Length)
			{
				return NotchValues[Train.Handles.PowerNotch];
			}
			return NotchValues[NotchValues.Length - 1];
		}
	}
}
