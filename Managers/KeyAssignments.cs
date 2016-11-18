using OpenBveApi.Runtime;

namespace Plugin
{
	internal class KeyConfiguration
	{
		/*
		 * Common Keys
		 */
		internal VirtualKeys? SafetyKey = VirtualKeys.A1;
		internal VirtualKeys? AutomaticGearsCutoff = VirtualKeys.A2;
		internal VirtualKeys? IncreaseWiperSpeed = VirtualKeys.WiperSpeedUp;
		internal VirtualKeys? DecreaseWiperSpeed = VirtualKeys.WiperSpeedDown;
		internal VirtualKeys? IsolateSafetySystems = VirtualKeys.L;
		internal VirtualKeys? FillFuel = VirtualKeys.FillFuel;
		internal VirtualKeys? DRA = VirtualKeys.S;
		internal VirtualKeys? CustomIndicatorKey1 = VirtualKeys.D;
		internal VirtualKeys? CustomIndicatorKey2 = VirtualKeys.E;
		internal VirtualKeys? CustomIndicatorKey3 = VirtualKeys.F;
		internal VirtualKeys? CustomIndicatorKey4 = VirtualKeys.G;
		internal VirtualKeys? CustomIndicatorKey5 = VirtualKeys.H;
		internal VirtualKeys? CustomIndicatorKey6;
		internal VirtualKeys? CustomIndicatorKey7;
		internal VirtualKeys? CustomIndicatorKey8;
		internal VirtualKeys? CustomIndicatorKey9;
		internal VirtualKeys? CustomIndicatorKey10;
		internal VirtualKeys? HeadCode;

		/*
		 * Electric Engine
		 */
		internal VirtualKeys? FrontPantograph = VirtualKeys.RaisePantograph;
		internal VirtualKeys? RearPantograph = VirtualKeys.RaisePantograph;
		
		/*
		 * Steam Engine
		 */
		internal VirtualKeys? ShovelFuel;
		internal VirtualKeys? Blowers = VirtualKeys.Blowers;
		internal VirtualKeys? ShowAdvancedDrivingWindow;
		internal VirtualKeys? IncreaseSteamHeat;
		internal VirtualKeys? DecreaseSteamHeat;
		internal VirtualKeys? CylinderCocks;
		internal VirtualKeys? SteamInjector = VirtualKeys.LiveSteamInjector;
		internal VirtualKeys? CutoffDecrease = VirtualKeys.DecreaseCutoff;
		internal VirtualKeys? CutoffIncrease = VirtualKeys.IncreaseCutoff;

		/*
		 * Diesel Engine
		 */
		internal VirtualKeys? EngineStartKey = VirtualKeys.EngineStart;
		internal VirtualKeys? EngineStopKey = VirtualKeys.EngineStop;
		internal VirtualKeys? GearUp = VirtualKeys.GearUp;
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
	}
}
