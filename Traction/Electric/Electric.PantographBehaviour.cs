
namespace Plugin
{
	/// <summary>Represents an electric locomotive.</summary>
	internal partial class Electric : Device
	{
		internal enum AutomaticPantographLoweringModes
		{
			/// <summary>No action will be taken when the set speed is hit</summary>
			NoAction = 0,
			/// <summary>All pantographs will be lowered when the set speed is hit</summary>
			LowerAll = 1,
			/// <summary>The front pantograph will be lowered *if* the rear pantograph is on service</summary>
			LowerFront = 2,
			/// <summary>The rear pantograph will be lowered *if* the front pantograph is on service</summary>
			LowerRear = 3,
			/// <summary>The front pantograph will be lowered regardless of the rear pantograph state</summary>
			LowerFrontRegardless = 4,
			/// <summary>The rear pantograph will be lowered regardless of the front pantograph state</summary>
			LowerRearRegardless = 5
			
		}
	}
}
