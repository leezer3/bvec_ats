namespace Plugin
{
	class CustomIndicator
	{
		/// <summary>The panel index for this custom indicator</summary>
		internal int PanelIndex = -1;
		/// <summary>The sound index played when this custom indicator is toggled</summary>
		internal int SoundIndex = -1;
		/// <summary>Stores whether this custom indicator is active</summary>
		internal bool Active = false;
		/// <summary>The key to toggle this custom indicator</summary>
		internal string Key = "";
		/// <summary>Stores whether this is a push-to-make switch (Off by default)</summary>
		internal bool PushToMake = false;
	}
}
