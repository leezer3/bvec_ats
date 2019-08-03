using OpenBveApi.Runtime;

namespace Plugin
{
	internal class KeyConfiguration
	{
		/*
		 * Common Keys
		 */
		/// <summary>Acknowledge a warning from the AWS</summary>
		internal VirtualKeys? AWSKey = VirtualKeys.A1;

		internal VirtualKeys? TPWSOverride = VirtualKeys.A2;
		/// <summary>Acknowledge a safety system warning</summary>
		internal VirtualKeys? SafetyKey = VirtualKeys.A1;
		/// <summary>Toggle automatic gearbox/ cutoff</summary>
		internal VirtualKeys? AutomaticGearsCutoff = VirtualKeys.A2;
		/// <summary>Increase speed of windscreen wipers</summary>
		/// TODO: Move to main program rather than plugin
		internal VirtualKeys? IncreaseWiperSpeed = VirtualKeys.WiperSpeedUp;
		/// <summary>Decrease speed of windscreen wipers</summary>
		internal VirtualKeys? DecreaseWiperSpeed = VirtualKeys.WiperSpeedDown;
		/// <summary>Isolates the current safety system (If available)</summary>
		internal VirtualKeys? IsolateSafetySystems = VirtualKeys.L;
		/// <summary>Fills fuel</summary>
		internal VirtualKeys? FillFuel = VirtualKeys.FillFuel;
		/// <summary>Toggles the DRA</summary>
		internal VirtualKeys? DRA = VirtualKeys.S;
		/// <summary>Toggles Custom Indicator #1</summary>
		internal VirtualKeys? CustomIndicatorKey1 = VirtualKeys.D;
		/// <summary>Toggles Custom Indicator #2</summary>
		internal VirtualKeys? CustomIndicatorKey2 = VirtualKeys.E;
		/// <summary>Toggles Custom Indicator #3</summary>
		internal VirtualKeys? CustomIndicatorKey3 = VirtualKeys.F;
		/// <summary>Toggles Custom Indicator #4</summary>
		internal VirtualKeys? CustomIndicatorKey4 = VirtualKeys.G;
		/// <summary>Toggles Custom Indicator #5</summary>
		internal VirtualKeys? CustomIndicatorKey5 = VirtualKeys.H;
		/// <summary>Toggles Custom Indicator #6</summary>
		internal VirtualKeys? CustomIndicatorKey6;
		/// <summary>Toggles Custom Indicator #7</summary>
		internal VirtualKeys? CustomIndicatorKey7;
		/// <summary>Toggles Custom Indicator #8</summary>
		internal VirtualKeys? CustomIndicatorKey8;
		/// <summary>Toggles Custom Indicator #9</summary>
		internal VirtualKeys? CustomIndicatorKey9;
		/// <summary>Toggles Custom Indicator #10</summary>
		internal VirtualKeys? CustomIndicatorKey10;
		/// <summary>Toggles Custom Indicator #11</summary>
		internal VirtualKeys? CustomIndicatorKey11;
		/// <summary>Toggles Custom Indicator #12</summary>
		internal VirtualKeys? CustomIndicatorKey12;
		/// <summary>Toggles Custom Indicator #13</summary>
		internal VirtualKeys? CustomIndicatorKey13;
		/// <summary>Toggles Custom Indicator #14</summary>
		internal VirtualKeys? CustomIndicatorKey14;
		/// <summary>Toggles Custom Indicator #15</summary>
		internal VirtualKeys? CustomIndicatorKey15;
		/// <summary>Toggles Custom Indicator #16</summary>
		internal VirtualKeys? CustomIndicatorKey16;
		/// <summary>Rotates the headcode display</summary>
		internal VirtualKeys? HeadCode;
		/// <summary>Shows the advanced window, with info on pressure generation etc.</summary>
		internal VirtualKeys? ShowAdvancedDrivingWindow;

		/*
		 * Electric Engine
		 */
		/// <summary>Raises or lowers the front pantograph</summary>
		internal VirtualKeys? FrontPantograph = VirtualKeys.RaisePantograph;
		/// <summary>Raises or lowrers the rear pantograph</summary>
		internal VirtualKeys? RearPantograph = VirtualKeys.RaisePantograph;

		
		/*
		 * Steam Engine
		 */
		/// <summary>Shovels coal into the firebox</summary>
		internal VirtualKeys? ShovelFuel;
		/// <summary>Toggles the blowers</summary>
		internal VirtualKeys? Blowers = VirtualKeys.Blowers;
		/// <summary>Increases the steam heating level</summary>
		internal VirtualKeys? IncreaseSteamHeat;
		/// <summary>Decreases the steam heating level</summary>
		internal VirtualKeys? DecreaseSteamHeat;
		/// <summary>Toggles the cylinder cocks</summary>
		internal VirtualKeys? CylinderCocks;
		/// <summary>Toggles the live steam injector</summary>
		internal VirtualKeys? LiveSteamInjector = VirtualKeys.LiveSteamInjector;
		/// <summary>Increases the cutoff</summary>
		internal VirtualKeys? CutoffDecrease = VirtualKeys.DecreaseCutoff;
		/// <summary>Decreases the cutoff</summary>
		internal VirtualKeys? CutoffIncrease = VirtualKeys.IncreaseCutoff;

		/*
		 * Diesel Engine
		 */
		/// <summary>Starts the engine</summary>
		internal VirtualKeys? EngineStartKey = VirtualKeys.EngineStart;
		/// <summary>Stops the engine</summary>
		internal VirtualKeys? EngineStopKey = VirtualKeys.EngineStop;
		/// <summary>Changes gear up</summary>
		internal VirtualKeys? GearUp = VirtualKeys.GearUp;
		/// <summary>Changes gear down</summary>
		internal VirtualKeys? GearDown = VirtualKeys.GearDown;

		/*
		 * Western (Custom traction type)
		 */
		internal VirtualKeys? WesternBatterySwitch;
		internal VirtualKeys? WesternMasterKey;
		internal VirtualKeys? WesternTransmissionResetButton;
		internal VirtualKeys? WesternEngineSwitchKey;
		internal VirtualKeys? WesternAWSIsolationKey;
		internal VirtualKeys? WesternFireBellKey;
		internal VirtualKeys? WesternEngineOnlyKey;
		internal VirtualKeys? WesternFuelPumpSwitch;

		/*
		 * OS_SZ_ATS
		 */
		internal VirtualKeys? SCMTincreasespeed;
		internal VirtualKeys? SCMTdecreasespeed;
		internal VirtualKeys? AbilitaBancoKey;
		internal VirtualKeys? ConsensoAvviamentoKey;
		internal VirtualKeys? AvviamentoKey;
		internal VirtualKeys? SpegnimentoKey;
		internal VirtualKeys? LCMupKey;
		internal VirtualKeys? LCMdownkey;
		internal VirtualKeys? TestSCMTKey;
		//These used to use safetykey && tpwsresetkey
		//tpwsreset doesn't exist, so move to their own key assignments
		internal VirtualKeys? vigilantekey;
		internal VirtualKeys? vigilanteresetkey;

		/*
		 * CAWS
		 */
		internal VirtualKeys? CAWSKey = VirtualKeys.S;

		/*
		 * PZB
		 */
		internal VirtualKeys? PZBKey;
		internal VirtualKeys? PZBReleaseKey;
		internal VirtualKeys? PZBStopOverrideKey;

		internal KeyConfiguration(bool Legacy)
		{
			//Sets various keys to use the legacy OS_ATS key assignments
			if (Legacy)
			{
				GearUp = VirtualKeys.C1;
				GearDown = VirtualKeys.C2;
				CutoffIncrease = VirtualKeys.C1;
				CutoffDecrease = VirtualKeys.C2;
				LiveSteamInjector = VirtualKeys.B2;
				FillFuel = VirtualKeys.I;
				IncreaseWiperSpeed = VirtualKeys.J;
				DecreaseWiperSpeed = VirtualKeys.K;
				FrontPantograph = null;
				RearPantograph = null;
			}
		}
	}
}
