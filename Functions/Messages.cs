using OpenBveApi.Colors;
using OpenBveApi.Runtime;

namespace Plugin
{

	/// <summary>Manages the playback of sounds.</summary>
	internal static class MessageManager
	{
		/// <summary>The callback function for adding interface messages.</summary>
		private static AddInterfaceMessageDelegate AddMessage;

		/// <summary>Initialises the message manager. Call this method via the Load() method.</summary>
		/// <param name="addMessage">The callback function for adding interface messages.</param>
		internal static void Initialise(AddInterfaceMessageDelegate addMessage)
		{
			AddMessage = addMessage;
		}

		/// <summary>Prints a new message to the in-game display</summary>
		/// <param name="Message">A string representing the message to print</param>
		/// <param name="Color">The color of the message</param>
		/// <param name="Time">The time in seconds the message is to be displayed for</param>
		internal static void PrintMessage(string Message, MessageColor Color, double Time)
		{
			AddMessage(Message, Color, Time);
		}

	}
}