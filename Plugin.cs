using System;
using System.Text.RegularExpressions;
using System.IO;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>The interface to be implemented by the plugin.</summary>
	public class Plugin : IRuntime {

        // --- members ---

        /// <summary>The train that is simulated by this plugin.</summary>
        private Train Train = null;
		/// <summary>Holds the array of panel variables.</summary>
		
		
		/// <summary>Is called when the plugin is loaded.</summary>
		/// <param name="properties">The properties supplied to the plugin on loading.</param>
		/// <returns>Whether the plugin was loaded successfully.</returns>
        public bool Load(LoadProperties properties)
        {
            properties.Panel = new int[272];
            SoundManager.Initialise(properties.PlaySound, 256);
            properties.AISupport = AISupport.None;
            this.Train = new Train(properties.Panel);
            //No AI Support
            properties.AISupport = AISupport.None;

            this.Train = new Train(properties.Panel);
            string configFile = Path.Combine(properties.TrainFolder, "BVEC_Ats.cfg");
            string OS_ATSDLL = Path.Combine(properties.TrainFolder, "OS_ATS1.dll");
            string OS_ATSconfigFile = Path.Combine(properties.TrainFolder, "OS_ATS1.cfg");
            if (File.Exists(configFile))
            {
                //Check for the automatic generator version
                string generatorversion;
                using (StreamReader reader = new StreamReader(configFile))
                {
                    //Read in first line
                    generatorversion = reader.ReadLine();
                }
                //If it exists
                if(generatorversion.StartsWith(";GenVersion="))
                {
                    string versiontext = Regex.Match(generatorversion, @"\d+").Value;
                    int version = Int32.Parse(versiontext);
                    //If we're below the current version, try to upgrade again
                    if (version < 1)
                    {
                        if (File.Exists(OS_ATSDLL) && File.Exists(OS_ATSconfigFile))
                        {
                            try
                            {
                                UpgradeOSATS.UpgradeConfigurationFile(OS_ATSconfigFile, properties.TrainFolder);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                    
                //Now try loading
                try
                {
                    this.Train.LoadConfigurationFile(configFile);
                    return true;
                }
                catch (Exception ex)
                {
                    properties.FailureReason = "Error loading the configuration file: " + ex.Message;
                    return false;
                }


            }
            else if (!File.Exists(configFile) && File.Exists(OS_ATSDLL) && File.Exists(OS_ATSconfigFile))
            {
                if (Regex.IsMatch(properties.TrainFolder, @"\\F92_en(\\)?", RegexOptions.IgnoreCase))
                {
                    properties.FailureReason = "The F92_en is not currently a supported train.";
                    using (StreamWriter sw = File.CreateText(Path.Combine(properties.TrainFolder, "error.log")))
                    {
                        sw.WriteLine("The F92_en is not currently a supported train");
                    }
                    return false;
                }
                else
                {
                    //If there is no existing BVEC_ATS configuration file, but OS_ATS and the appropriate
                    //configuration file exist, then attempt to upgrade the existing file to BVEC_ATS
                    try
                    {
                        UpgradeOSATS.UpgradeConfigurationFile(OS_ATSconfigFile, properties.TrainFolder);
                    }
                    catch (Exception)
                    {
                        properties.FailureReason = "Error upgrading the existing OS_ATS configuration.";
                        return false;
                    }

                    try
                    {
                        this.Train.LoadConfigurationFile(configFile);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        properties.FailureReason = "Error loading the configuration file: " + ex.Message;
                        return false;
                    }
                }
            }
            else
            {
                properties.FailureReason = "No supported configuration files exist.";
                //Create new configuration file and cycle through the newly created arrays to upgrade the original configuration file.
                using (StreamWriter sw = File.CreateText(Path.Combine(properties.TrainFolder, "error.log")))
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
		/// <param name="data">Signal information per section. In the array, index 0 is the current section, index 1 the upcoming section, and so on.</param>
		/// <remarks>The signal array is guaranteed to have at least one element. When accessing elements other than index 0, you must check the bounds of the array first.</remarks>
		public void SetSignal(SignalData[] signal) {
			/*
             int aspect = signal[0].Aspect;
			if (aspect != this.LastAspect) {
				// TODO: Your old SetSignal code goes here.
			}
             */
		}
		

		/// <summary>Is called when the train passes a beacon.</summary>
		/// <param name="beacon">The beacon data.</param>
		public void SetBeacon(BeaconData beacon) {
            this.Train.SetBeacon(beacon);
		}
		
		/// <summary>Is called when the plugin should perform the AI.</summary>
		/// <param name="data">The AI data.</param>
		public void PerformAI(AIData data) {
			// TODO: Implement this function if you want your plugin to support the AI.
			//       Be to set properties.AISupport to AISupport.Basic in the Load call if you do.
		}
		
	}
}