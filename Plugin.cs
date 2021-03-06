﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using OpenBveApi.Runtime;
using Path = OpenBveApi.Path;
namespace Plugin {
	/// <summary>The interface to be implemented by the plugin.</summary>
	public class Plugin : IRuntime {

		// --- members ---

		/// <summary>The train that is simulated by this plugin.</summary>
		private Train Train = null;

		/// <summary>The random number generator used by the plugin.</summary>
		public static Random Random = new Random();

		public static string TrainFolder;

		internal static bool FolderWriteAccess = true;
		/// <summary>Is called when the plugin is loaded.</summary>
		/// <param name="properties">The properties supplied to the plugin on loading.</param>
		/// <returns>Whether the plugin was loaded successfully.</returns>
		public bool Load(LoadProperties properties)
		{
			properties.Panel = new int[512];
			SoundManager.Initialise(properties.PlaySound, 512);
			MessageManager.Initialise(properties.AddMessage);
			properties.AISupport = AISupport.Basic;
			this.Train = new Train(properties.Panel);
			//No AI Support
			//properties.AISupport = AISupport.None;

			this.Train = new Train(properties.Panel);
			string configFile = Path.CombineFile(properties.TrainFolder, "BVEC_Ats.cfg");
			string OS_ATSDLL = Path.CombineFile(properties.TrainFolder, "OS_ATS1.dll");
			string SZ_ATSDLL = Path.CombineFile(properties.TrainFolder, "OS_SZ_ATS1.dll");
			string SZ_ATSDLL_2 = Path.CombineFile(properties.TrainFolder, "OS_SZ_Ats2_0.dll");
			string OS_ATSconfigFile = Path.CombineFile(properties.TrainFolder, "OS_ATS1.cfg");
			string SZ_ATSconfigFile = Path.CombineFile(properties.TrainFolder, "OS_SZ_ATS1.cfg");
			string SZ_ATS_2configFile = Path.CombineFile(properties.TrainFolder, "OS_SZ_Ats2_0.cfg");
			string ODF_ATSconfigFile = Path.CombineFile(properties.TrainFolder, "OdakyufanAts.cfg");
			TrainFolder = properties.TrainFolder;
			//Delete error.log from previous run
			if (File.Exists(Path.CombineFile(properties.TrainFolder, "error.log")))
			{
				try
				{
					File.Delete(Path.CombineFile(properties.TrainFolder, "error.log"));
				}
				catch
				{
					FolderWriteAccess = false;
				}
			}
			if (File.Exists(configFile))
			{
				//Check for the automatic generator version
				string generatorversion;
				using (var reader = new StreamReader(configFile))
				{
					//Read in first line
					generatorversion = reader.ReadLine();
				}
				//If it exists
				try
				{
					if (generatorversion != null && generatorversion.StartsWith(";GenVersion="))
					{
						string versiontext = Regex.Match(generatorversion, @"\d+").Value;
						int version = Int32.Parse(versiontext, NumberStyles.Number, CultureInfo.InvariantCulture);
						//If we're below the current version, try to upgrade again
						if (version < 1)
						{
							if (File.Exists(OS_ATSDLL) && File.Exists(OS_ATSconfigFile))
							{
								try
								{
									string[] Lines = UpgradeOSATS.UpgradeConfigurationFile(OS_ATSconfigFile, properties.TrainFolder);

								}
								catch (Exception)
								{
									properties.FailureReason = "An error occured whilst attempting to upgrade the OS_ATS configuration.";
									return false;
								}
							}
						}
					}
				}
				catch (Exception)
				{
					properties.FailureReason = "Empty configuration file detected.";
					return false;
				}

				//Now try loading
				try
				{
					string[] Lines = File.ReadAllLines(configFile, Encoding.UTF8);
					this.Train.LoadConfigurationFile(Lines);
					return true;
				}
				catch (Exception ex)
				{
					properties.FailureReason = "Error loading the configuration file: " + ex.Message;
					return false;
				}


			}
			if (!File.Exists(configFile) && File.Exists(OS_ATSDLL) && File.Exists(OS_ATSconfigFile))
			{
				//The F92_en is blacklisted due to a custom OS_ATS version
				if (Regex.IsMatch(properties.TrainFolder, @"\\F92_en(\\)?", RegexOptions.IgnoreCase))
				{
					properties.FailureReason = "The F92_en is not currently a supported train.";
					try
					{
						using (StreamWriter sw = File.CreateText(Path.CombineFile(properties.TrainFolder, "error.log")))
						{
							sw.WriteLine("The F92_en is not currently a supported train");
						}
						return false;
					}
					catch
					{
						return false;
					}
				}
				//If there is no existing BVEC_ATS configuration file, but OS_ATS and the appropriate
				//configuration files exist, then attempt to upgrade the existing file to BVEC_ATS
				try
				{
					string[] Lines = UpgradeOSATS.UpgradeConfigurationFile(OS_ATSconfigFile, properties.TrainFolder);
					try
					{
						File.WriteAllLines(Path.CombineFile(TrainFolder, "BVEC_ATS.cfg"), Lines);
					}
					catch
					{
						//Error writing the new configuration file
					}
					this.Train.LoadConfigurationFile(Lines);
					return true;
				}
				catch (Exception)
				{
					properties.FailureReason = "Error upgrading the existing OS_ATS configuration.";
					using (StreamWriter sw = File.CreateText(Path.CombineFile(properties.TrainFolder, "error.log")))
					{
						sw.WriteLine("An existing OS_ATS configuration was found.");
						sw.WriteLine("However, an error occurred upgrading the existing OS_ATS configuration.");
					}
					return false;
				}
			}
			if (File.Exists(SZ_ATSDLL))
			{
				//We've found an OS_SZ_ATS equipped train
				//Upgrade for this is in alpha
				try
				{
					string[] Lines = UpgradeOSSZATS.UpgradeConfigurationFile(SZ_ATSconfigFile, properties.TrainFolder);
					try
					{
						File.WriteAllLines(Path.CombineFile(TrainFolder, "BVEC_ATS.cfg"), Lines);
					}
					catch
					{
						//Error writing the new configuration file
					}
					this.Train.LoadConfigurationFile(Lines);
					return true;
				}
				catch (Exception)
				{
					properties.FailureReason = "Error upgrading the existing OS_SZ_ATS configuration.";
					using (StreamWriter sw = File.CreateText(Path.CombineFile(properties.TrainFolder, "error.log")))
					{
						sw.WriteLine("An existing OS_SZ_ATS configuration was found.");
						sw.WriteLine("However, an error occurred upgrading the existing OS_SZ_ATS configuration.");
					}
					return false;
				}
			}
			else
			{
				if (File.Exists(SZ_ATSDLL_2))
				{
					//We've found an OS_SZ_ATS equipped train
					//Upgrade for this is in alpha
					try
					{
						string[] Lines = UpgradeOSSZATS.UpgradeConfigurationFile(SZ_ATS_2configFile, properties.TrainFolder);
						try
						{
							File.WriteAllLines(Path.CombineFile(TrainFolder, "BVEC_ATS.cfg"), Lines);
						}
						catch
						{
							//Error writing the new configuration file
						}
						this.Train.LoadConfigurationFile(Lines);
						return true;
					}
					catch (Exception)
					{
						properties.FailureReason = "Error upgrading the existing OS_SZ_ATS configuration.";
						using (StreamWriter sw = File.CreateText(Path.CombineFile(properties.TrainFolder, "error.log")))
						{
							sw.WriteLine("An existing OS_SZ_ATS configuration was found.");
							sw.WriteLine("However, an error occurred upgrading the existing OS_SZ_ATS configuration.");
						}
						return false;
					}

				}
				if (File.Exists(ODF_ATSconfigFile))
				{
					//We've found an OdyakufanATS equipped train
					//Upgrade for this is in alpha
					try
					{
						File.Copy(ODF_ATSconfigFile, configFile);
						string[] Lines = File.ReadAllLines(configFile);
						this.Train.LoadConfigurationFile(Lines);
						return true;
					}
					catch (Exception ex)
					{
						properties.FailureReason = "Error loading the configuration file: " + ex.Message;
						return false;
					}
				}
				else
				{
					properties.FailureReason = "No supported configuration files exist.";
					//Write out error.log with details of what it thinks was found and missing
					using (StreamWriter sw = File.CreateText(Path.CombineFile(properties.TrainFolder, "error.log")))
					{
						sw.WriteLine("Plugin location " + Convert.ToString(properties.TrainFolder));
						if (File.Exists(OS_ATSDLL))
						{
							sw.WriteLine("OS_ATS DLL found");
						}
						else
						{
							sw.WriteLine("No OS_ATS DLL found");
						}

						if (File.Exists(OS_ATSconfigFile))
						{
							sw.WriteLine("OS_ATS configuration file found");
						}
						else
						{
							sw.WriteLine("No OS_ATS configuration file found");
						}
					}
					return false;
				}
			}
		}
		
		/// <summary>Is called when the plugin is unloaded.</summary>
		public void Unload() {
			// TODO: Your old Dispose code goes here.
		}
		
		/// <summary>Is called after loading to inform the plugin about the specifications of the train.</summary>
		/// <param name="specs">The specifications of the train.</param>
		public void SetVehicleSpecs(VehicleSpecs specs) {
			this.Train.Specs = specs;
		}
		
		/// <summary>Is called when the plugin should initialize or reinitialize.</summary>
		/// <param name="mode">The mode of initialization.</param>
		public void Initialize(InitializationModes mode) {
			this.Train.Initialize(mode);
		}
		
		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data passed to the plugin.</param>
		public void Elapse(ElapseData data) {
			this.Train.Elapse(data);
		}
		
		/// <summary>Is called when the driver changes the reverser.</summary>
		/// <param name="reverser">The new reverser position.</param>
		public void SetReverser(int reverser) {
			this.Train.SetReverser(reverser);
		}
		
		/// <summary>Is called when the driver changes the power notch.</summary>
		/// <param name="powerNotch">The new power notch.</param>
		public void SetPower(int powerNotch) {
			this.Train.SetPower(powerNotch);
		}
		
		/// <summary>Is called when the driver changes the brake notch.</summary>
		/// <param name="brakeNotch">The new brake notch.</param>
		public void SetBrake(int brakeNotch) {
			this.Train.SetBrake(brakeNotch);
		}
		
		/// <summary>Is called when a virtual key is pressed.</summary>
		/// <param name="key">The virtual key that was pressed.</param>
		public void KeyDown(VirtualKeys key) {
			this.Train.KeyDown(key);
		}
		
		/// <summary>Is called when a virtual key is released.</summary>
		/// <param name="key">The virtual key that was released.</param>
		public void KeyUp(VirtualKeys key) {
			this.Train.KeyUp(key);
		}
		
		/// <summary>Is called when a horn is played or when the music horn is stopped.</summary>
		/// <param name="type">The type of horn.</param>
		public void HornBlow(HornTypes type) {
			this.Train.HornBlow(type);
		}
		
		/// <summary>Is called when the state of the doors changes.</summary>
		/// <param name="oldState">The old state of the doors.</param>
		/// <param name="newState">The new state of the doors.</param>
		public void DoorChange(DoorStates oldState, DoorStates newState) {
			this.Train.Doors = newState;
			this.Train.DoorChange(oldState, newState);
			
		}

		
		/// <summary>Is called when the aspect in the current or in any of the upcoming sections changes, or when passing section boundaries.</summary>
		/// <param name="signal">Signal information per section. In the array, index 0 is the current section, index 1 the upcoming section, and so on.</param>
		/// <remarks>The signal array is guaranteed to have at least one element. When accessing elements other than index 0, you must check the bounds of the array first.</remarks>
		public void SetSignal(SignalData[] signal) {
			this.Train.SetSignal(signal);
		}
		

		/// <summary>Is called when the train passes a beacon.</summary>
		/// <param name="beacon">The beacon data.</param>
		public void SetBeacon(BeaconData beacon) {
			this.Train.SetBeacon(beacon);
		}

		/// <summary>Is called when the plugin should perform the AI.</summary>
		/// <param name="data">The AI data.</param>
		public void PerformAI(AIData data)
		{
			this.Train.PerformAI(data);
		}
	}
}