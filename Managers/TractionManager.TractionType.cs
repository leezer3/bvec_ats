namespace Plugin
{
	internal partial class TractionManager: Device
	{
		/// <summary>Defines the available traction types simulated by this plugin</summary>
		internal enum TractionType
		{
			Steam = 0,
			Diesel = 1,
			Electric = 2,
			WesternDiesel = 3,
			Unknown = 99
		}
	}
}
