using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Plugin
{
	class UpgradeOSATS
	{
		//This class will upgrade an existing OS_ATS configuration file.
		//Only supported values will be upgradeed.
			internal static string[] UpgradeConfigurationFile(string file, string trainpath) {
			var diesel = new List<string>();
			var electric = new List<string>();
			var steam = new List<string>();
			var common = new List<string>();
			var vigilance = new List<string>();
			var AWS = new List<string>();
			var TPWS = new List<string>();
			var interlocks = new List<string>();
			var windscreen = new List<string>();
			var keys = new List<string>();
			var errors = new List<string>();
			int mySystem = 0;

			bool steamtype = false;
			bool dieseltype = false;
			//Read all lines of existing OS_ATS configuration file and add to appropriate arrays
			string[] lines = File.ReadAllLines(file, Encoding.UTF8);
			foreach (string t in lines)
			{
				string parsedKey = "";
				string line = t;
				int semicolon = line.IndexOf(';');
				if (semicolon >= 0) {
					line = line.Substring(0, semicolon).Trim();
				} else {
					line = line.Trim();
				}
				//Trim extra commas from the end of strings
				//Stops the parser from attempting to read in blank values and causing issues
				line = line.TrimEnd(',');
				int equals = line.IndexOf('=');
				if (@equals >= 0) {
					string key = line.Substring(0, @equals).Trim().ToLowerInvariant();
					string value = line.Substring(@equals + 1).Trim();
					switch (key) {
						case "traction":
							//Set the traction type
							if (Convert.ToInt32(value) == 1)
							{
								steamtype = true;
							}
							else if (Convert.ToInt32(value) == 2)
							{
								dieseltype = true;
							}
							break;
							//VALUES COMMON TO ALL TRACTION TYPE SECTIONS
						case "automatic":
							common.Add(line);
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add("automatickey=" + parsedKey);
							break;
						case "fuelindicator":
							common.Add(line);
							break;
						case "fuelcapacity":
							common.Add(line);
							break;
						case "fuelstartamount":
							common.Add(line);
							break;
						case "fuelfillspeed":
							common.Add(line);
							break;
						case "fuelfillindicator":
							common.Add(line);
							break;
						case "automaticindicator":
							common.Add(line);
							break;
						case "heatingpart":
							common.Add(line);
							break;
						case "heatingrate":
							common.Add(line);
							break;
						case "overheatwarn":
							common.Add(line);
							break;
						case "overheat":
							common.Add(line);
							break;
						case "overheatresult":
							common.Add(line);
							break;
						case "thermometer":
							common.Add(line);
							break;
						case "overheatindicator":
							common.Add(line);
							break;
						case "overheatalarm":
							common.Add(line);
							break;
							//VALUES TO BE ADDED TO THE DIESEL SECTION OF THE CONFIGURATION FILE
						case "gearratios":
							diesel.Add(line);
							break;
						case "gearfadeinrange":
							diesel.Add(line);
							break;
						case "gearfadeoutrange":
							diesel.Add(line);
							break;
						case "gearindicator":
							diesel.Add(line);
							break;
						case "tachometer":
							diesel.Add(line);
							break;
						case "fuelconsumption":
							diesel.Add(line);
							break;
						case "gearloopsound":
							diesel.Add(line);
							break;
						case "gearchangesound":
							//Handle trains with an additional paramater on gearchange sound
							string[] gearchangefix = line.Split(',');
							diesel.Add(gearchangefix[0]);
							break;
						case "gearupkey":
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add(key + "=" + parsedKey);
							break;
						case "geardownkey":
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add(key +"=" + parsedKey);
							break;
							//VALUES TO BE ADDED TO THE STEAM SECTION OF THE CONFIGURATION FILE
						case "cutoffmax":
							steam.Add(line);
							break;
						case "cutoffmin":
							steam.Add(line);
							break;
						case "cutoffineffective":
							steam.Add(line);
							break;
						case "cutoffdecreasekey":
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add("cutoffdownkey=" + parsedKey);
							break;
						case "cutoffincreasekey":
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add("cutoffupkey=" + parsedKey);
							break;
						case "cutoffdeviation":
							steam.Add(line);
							break;
						case "cutoffratio":
							steam.Add(line);
							break;
						case "cutoffratiobase":
							steam.Add(line);
							break;
						case "cutoffindicator":
							steam.Add(line);
							break;
						case "injectorrate":
							steam.Add(line);
							break;
						case "injectorindicator":
							steam.Add(line);
							break;
						case "injectortogglekey":
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add("injectorkey=" + parsedKey);
							break;
						case "boilermaxpressure":
							steam.Add(line);
							break;
						case "boilerminpressure":
							steam.Add(line);
							break;
						case "boilerpressureindicator":
							steam.Add(line);
							break;
						case "boilerwaterlevelindicator":
							steam.Add(line);
							break;
						case "boilermaxwaterlevel":
							steam.Add(line);
							break;
						case "boilerwatertosteamrate":
							steam.Add(line);
							break;
							//VALYES TO BE ADDED TO THE ELECTRIC SECTION OF THE CONFIGURATION FILE
						case "powergapbehaviour":
							electric.Add(line);
							break;
						case "powerpickuppoints":
							electric.Add(line);
							break;
						case "powerindicator":
							electric.Add(line);
							break;
						case "ammeter":
							electric.Add(line);
							break;
						case "ammetervalues":
							//Handle negative values in ammeter string
							string ammeterfix = line.Replace("-", "");
							electric.Add(ammeterfix);
							break;
							//VALUES TO BE ADDED TO VIGILANCE SECTION OF CONFIGURATION FILE
						case "vigilance":
							//Change vigilance to deadmanshandle (Internal)
							vigilance.Add("deadmanshandle=" + value);
							break;
						case "vigilancekey":
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add("safetykey=" + parsedKey);
							break;
						case "overspeedcontrol":
							vigilance.Add(line);
							break;
						case "warningspeed":
							vigilance.Add(line);
							break;
						case "overspeed":
							vigilance.Add(line);
							break;
						case "overspeedalarm":
							vigilance.Add(line);
							break;
						case "safespeed":
							vigilance.Add(line);
							break;
						case "overspeedindicator":
							vigilance.Add(line);
							break;
						case "vigilanceinterval":
							vigilance.Add(line);
							break;
						case "vigilanceautorelease":
							vigilance.Add(line);
							break;
						case "vigilancecancellable":
							vigilance.Add(line);
							break;
						case "vigilancelamp":
							vigilance.Add(line);
							break;
						case "vigilancealarm":
							vigilance.Add(line);
							break;
						case "independentvigilance":
							vigilance.Add(line);
							break;
						case "vigilancedelay1":
							vigilance.Add(line);
							break;
						case "vigilancedelay2":
							vigilance.Add(line);
							break;
						case "vigilanceinactivespeed":
							vigilance.Add(line);
							break;
						case "reminderkey":
							vigilance.Add("draenabled="+value);
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add("drakey=" + parsedKey);
							break;
						case "reminderindicator":
							vigilance.Add("draindicator="+value);
							break;
							//VALUES TO BE ADDED TO AWS SECTION OF THE CONFIGURATION FILE
						case "awsindicator":
							AWS.Add(line);
							break;
						case "awswarningsound":
							AWS.Add(line);
							break;
						case "awsclearsound":
							AWS.Add(line);
							break;
						case "awsdelay":
							AWS.Add(line);
							break;
						case "tpwswarningsound":
							AWS.Add(line);
							break;
							//VALUES TO BE ADDED TO TPWS SECTION OF CONFIGURATION FILE
						case "tpwsindicator":
							TPWS.Add(line);
							break;
						case "tpwsindicator2":
							TPWS.Add(line);
							break;

							//VALUES TO BE ADDED TO THE INTERLOCKS SECTION OF THE CONFIGURATION FILE
						case "doorpowerlock":
							interlocks.Add(line);
							break;
						case "doorapplybrake":
							interlocks.Add(line);
							break;
						case "neutralrvrbrake":
							interlocks.Add(line);
							break;
						case "directionindicator":
							interlocks.Add(line);
							break;
						case "reverserindex":
							interlocks.Add(line);
							break;
						case "travelmetermode":
							interlocks.Add(line);
							break;
						case "travelmeter100":
							interlocks.Add(line);
							break;
						case "travelmeter10":
							interlocks.Add(line);
							break;
						case "travelmeter1":
							interlocks.Add(line);
							break;
						case "travelmeter01":
							interlocks.Add(line);
							break;
						case "travelmeter001":
							interlocks.Add(line);
							break;
						case "effectivervrindex":
							interlocks.Add("directionindicator=" + value);
							break;
						case "klaxonindicator":
							//Remove key assignments from klaxonindicator
							string[] splitklaxonindicator = value.Split(',');
							var splitarray = new int[splitklaxonindicator.Length + 1];
							for (int j = 0; j < splitklaxonindicator.Length; j++)
							{
								if (j <= 1)
								{
									splitarray[j] = Int32.Parse(splitklaxonindicator[j], NumberStyles.Number, CultureInfo.InvariantCulture);
								}
								else
								{
									splitarray[2] = Int32.Parse(splitklaxonindicator[0], NumberStyles.Number, CultureInfo.InvariantCulture);
									splitarray[3] = Int32.Parse(splitklaxonindicator[j], NumberStyles.Number, CultureInfo.InvariantCulture);
								}
							}
							string finishedvalue = "klaxonindicator=" + string.Join(",", Array.ConvertAll(splitarray, Convert.ToString));
							interlocks.Add(finishedvalue);
							break;
						case "customindicators":
							string[] splitcustomindicators = value.Split(',');
							var splitarray1 = new int[splitcustomindicators.Length / 2];
							int k = 0;
							for (int j = 0; j < splitcustomindicators.Length; j++)
							{
								if (j % 2 == 0)
								{
									InternalFunctions.UpgradeKey(splitcustomindicators[j], ref parsedKey, key + j / 2);
									keys.Add("customindicatorkey" + (j /2 + 1) + "=" + parsedKey);
								}
								else
								{
									splitarray1[k] = Int32.Parse(splitcustomindicators[j]);
									k++;
								}
							}
							string finishedvalue1 = "customindicators=" + string.Join(",", Array.ConvertAll(splitarray1, Convert.ToString));
							interlocks.Add(finishedvalue1);
							break;
							//VALUES TO BE ADDED TO THE WINDSCREEN SECTION OF THE CONFIGURATION FILE
						case "wiperindex":
							windscreen.Add(line);
							break;
						case "wiperrate":
							windscreen.Add(line);
							break;
						case "wiperdelay":
							windscreen.Add(line);
							break;
						case "wiperholdposition":
							windscreen.Add(line);
							break;
						case "wiperonkey":
						case "wiperoffkey":
							InternalFunctions.UpgradeKey(value, ref parsedKey, key);
							keys.Add(key + "=" + parsedKey);
							break;
						case "numberofdrops":
							windscreen.Add(line);
							break;
						case "dropstartindex":
							windscreen.Add(line);
							break;
						case "dropsound":
							windscreen.Add(line);
							break;
						case "wipersound":
							//Convert to drywipe & wipersoundbehaviour
							string[] splitwipersound = value.Split(',');
							for (int j = 0; j < splitwipersound.Length; j++)
							{
								if (j == 0)
								{
									windscreen.Add("drywipesound="+Convert.ToString(Int32.Parse(splitwipersound[j], NumberStyles.Number, CultureInfo.InvariantCulture)));
								}
								if (j == 1)
								{
									windscreen.Add("wipersoundbehaviour=0");
								}
							}
							break;
						case "system":
							//Ignore this one, and don't add as an error
							//Safety systems are only instanciated as necessary
							mySystem = Int32.Parse(value);
							break;
						case "awsacknowledgekey":
							string[] splitKey = value.Split(',');
							for (int j = 0; j < splitKey.Length; j++)
							{
								if (j == 0)
								{
									InternalFunctions.UpgradeKey(splitKey[0], ref parsedKey, key);
									keys.Add("awskey=" + parsedKey);
								}
								if (j == 1)
								{
									AWS.Add("cancelbuttonindex="+Convert.ToString(Int32.Parse(splitKey[j], NumberStyles.Number, CultureInfo.InvariantCulture)));
								}
							}
							break;
						default:
							errors.Add(line);
							break;
					}
				}
			}

			List<string> newLines = new List<string>();
			//Create new configuration file and cycle through the newly created arrays to upgrade the original configuration file.
				/*Write out the generator version and warning
				 *      Version 1 files handle OS_ATS only
				 *      Version 2 files handle OS_SZ_ATS files
				 *      Version 3 files twiddle with key assignments slightly
				 *      Version 4 fixes custom OS_ATS keys
				 * TODO: Re-generate files if a version 1 file is detected with OS_SZ_ATS present
				*/
				newLines.Add(";GenVersion=4");
				newLines.Add(";DELETE THE ABOVE LINE IF YOU MODIFY THIS FILE");
				newLines.Add("");
				//Traction Type First
				if (electric.Count > 0 && steamtype != true && dieseltype != true)
				{
					newLines.Add("[Electric]");
					newLines.AddRange(common);
					newLines.AddRange(electric);
				}

				if (steamtype)
				{
					newLines.Add("[Steam]");
					newLines.AddRange(common);
					newLines.AddRange(steam);
				}

				if (dieseltype)
				{
					newLines.Add("[Diesel]");
					newLines.AddRange(common);
					newLines.AddRange(diesel);
				}
				newLines.Add("[Interlocks]");
				newLines.AddRange(interlocks);
				newLines.Add("[Vigilance]");
				newLines.AddRange(vigilance);
				if (mySystem == 1 || mySystem == 2)
				{
					newLines.Add("[AWS]");
					newLines.AddRange(AWS);
				}
				if (mySystem == 2)
				{
					newLines.Add("[TPWS]");
					newLines.AddRange(TPWS);
				}

				if (mySystem == 3)
				{

				}

				newLines.Add("[Windscreen]");
				newLines.AddRange(windscreen);
				//Use the legacy set key assignments
				newLines.Add("[LegacyKeyAssignments]");
				newLines.AddRange(keys);



			using (StreamWriter sw = File.AppendText(Path.Combine(trainpath, "error.log")))
			{
				//Write out upgrade errors to log file
				sw.WriteLine("The following unsupported paramaters were detected whilst attempting to upgrade the existing configuration file:");
				foreach (string item in errors)
				{
					sw.WriteLine(item);
				}
			}
			return newLines.ToArray();

			}
	}

	class UpgradeOSSZATS
	{
		internal static string[] UpgradeConfigurationFile(string file, string trainpath)
		{
			var SCMT = new List<string>();
			var vigilance = new List<string>();
			var keys = new List<string>();
			var errors = new List<string>();
			string[] lines = File.ReadAllLines(file, Encoding.UTF8);
			foreach (string t in lines)
			{
				string line = t;
				int semicolon = line.IndexOf(';');
				if (semicolon >= 0)
				{
					line = line.Substring(0, semicolon).Trim();
				}
				else
				{
					line = line.Trim();
				}
				//Trim extra commas from the end of strings
				//Stops the parser from attempting to read in blank values and causing issues
				line = line.TrimEnd(',');
				int equals = line.IndexOf('=');
				if (@equals >= 0)
				{
					string key = line.Substring(0, @equals).Trim().ToLowerInvariant();
					string value = line.Substring(@equals + 1).Trim();
					string keyassignment = "";
					string failingvalue = "";
					switch (key)
					{
						case "spiablu":
							SCMT.Add(line);
							break;
						case "spiarossi":
							SCMT.Add(line);
							break;
						case "spiascmt":
							SCMT.Add(line);
							break;
						case "indsr":
							SCMT.Add(line);
							break;
						case "traintrip":
							SCMT.Add(line);
							break;
						case "testscmt":
							SCMT.Add(line);
							break;
						case "testpulsanti":
							SCMT.Add(line);
							break;
						case "suonoscmton":
							SCMT.Add(line);
							break;
						case "suonoconfdati":
							SCMT.Add(line);
							break;
						case "suonoinsscmt":
							SCMT.Add(line);
							break;
						case "indlcm":
							SCMT.Add(line);
							break;
						case "indimpvelpressed":
							SCMT.Add(line);
							break;
						case "indimpvelpressedsu":
							SCMT.Add(line);
							break;
						case "indimpvelpressedgiu":
							SCMT.Add(line);
							break;
						case "suonoimpvel":
							SCMT.Add(line);
							break;
						case "indabbanco":
							SCMT.Add(line);
							break;
						case "suonoconsavv":
							SCMT.Add(line);
							break;
						case "indconsavv":
							SCMT.Add(line);
							break;
						case "indavv":
							SCMT.Add(line);
							break;
						case "suonoavv":
							SCMT.Add(line);
							break;
						case "indarr":
							SCMT.Add(line);
							break;
						case "suonoarr":
							SCMT.Add(line);
							break;
						case "indattesa":
							SCMT.Add(line);
							break;
						case "indacarrfren":
							SCMT.Add(line);
							break;
						case "accensionemot":
							SCMT.Add(line);
							break;
						case "indcontgiri":
							SCMT.Add(line);
							break;
						case "indgas":
							SCMT.Add(line);
							break;
						case "inddinamometro":
							SCMT.Add(line);
							break;
						case "indvoltbatt":
							SCMT.Add(line);
							break;
						case "indspegnmon":
							SCMT.Add(line);
							break;
						case "suonosottofondo":
							SCMT.Add(line);
							break;
						case "tpwsstopdelay":
							SCMT.Add(line);
							break;
						//Key Assignments
						case "scmtkey":
							InternalFunctions.UpgradeKey(value, ref keyassignment, failingvalue);
							keys.Add(key + "=" + keyassignment);
							break;
						case "lcmupkey":
							InternalFunctions.UpgradeKey(value, ref keyassignment, failingvalue);
							keys.Add(key + "=" + keyassignment);
							break;
						case "lcmdownkey":
							InternalFunctions.UpgradeKey(value, ref keyassignment, failingvalue);
							keys.Add(key + "=" + keyassignment);
							break;
						case "abbancokey":
							InternalFunctions.UpgradeKey(value, ref keyassignment, failingvalue);
							keys.Add(key + "=" + keyassignment);
							break;
						case "consavvkey":
							InternalFunctions.UpgradeKey(value, ref keyassignment, failingvalue);
							keys.Add(key + "=" + keyassignment);
							break;
						case "avvkey":
							InternalFunctions.UpgradeKey(value, ref keyassignment, failingvalue);
							keys.Add(key + "=" + keyassignment);
							break;
						case "spegnkey":
							InternalFunctions.UpgradeKey(value, ref keyassignment, failingvalue);
							keys.Add(key + "=" + keyassignment);
							break;
						case "impvel":
							var splits = value.Split(',');
							try
							{
								InternalFunctions.UpgradeKey(splits[0], ref keyassignment, failingvalue);
								keys.Add("impvelsukey" + "=" + keyassignment);
								InternalFunctions.UpgradeKey(splits[1], ref keyassignment, failingvalue);
								keys.Add("impvelgiukey" + "=" + keyassignment);
							}
							catch (Exception)
							{
								errors.Add("The key assignment impvel contains invalid data");
							}
							break;
						case "vigilante":
							vigilance.Add(line);
							break;
						//VALUES TO BE ADDED TO VIGILANCE SECTION OF CONFIGURATION FILE
						case "vigilance":
							//Change vigilance to deadmanshandle (Internal)
							vigilance.Add("deadmanshandle=" + value);
							break;
						case "overspeedcontrol":
							vigilance.Add(line);
							break;
						case "warningspeed":
							vigilance.Add(line);
							break;
						case "overspeed":
							vigilance.Add(line);
							break;
						case "overspeedalarm":
							vigilance.Add(line);
							break;
						case "safespeed":
							vigilance.Add(line);
							break;
						case "overspeedindicator":
							vigilance.Add(line);
							break;
						case "vigilanceinterval":
							vigilance.Add(line);
							break;
						case "vigilanceautorelease":
							vigilance.Add(line);
							break;
						case "vigilancecancellable":
							vigilance.Add(line);
							break;
						case "vigilancelamp":
							vigilance.Add(line);
							break;
						case "vigilancealarm":
							vigilance.Add(line);
							break;
						case "independentvigilance":
							vigilance.Add(line);
							break;
						case "vigilancedelay1":
							vigilance.Add(line);
							break;
						case "vigilancedelay2":
							vigilance.Add(line);
							break;
						case "vigilanceinactivespeed":
							vigilance.Add(line);
							break;
						case "reminderkey":
							vigilance.Add("draenabled=" + value);
							break;
						case "reminderindicator":
							vigilance.Add("draindicator=" + value);
							break;
						default:
							errors.Add(line);
							break;
					}

				}

			}

			List<string> Lines = new List<string>();

			/*Write out the generator version and warning
			 *      Version 1 files handle OS_ATS only
			 *      Version 2 files handle OS_SZ_ATS files
			 * TODO: Re-generate files if a version 1 file is detected with OS_SZ_ATS present
			 */
			Lines.Add(";GenVersion=2");
			Lines.Add(";DELETE THE ABOVE LINE IF YOU MODIFY THIS FILE");
			Lines.Add("");
			if (SCMT.Count > 0)
			{
				Lines.Add("[SCMT]");
				Lines.AddRange(SCMT);
			}
			if (vigilance.Count > 0)
			{
				Lines.Add("[Vigilance]");
				Lines.AddRange(vigilance);
			}
			Lines.Add("[KeyAssignmentsLegacy]");
			if (keys.Count > 0)
			{
				Lines.AddRange(keys);
			}
			
			using (StreamWriter sw = File.AppendText(Path.Combine(trainpath, "error.log")))
			{
				//Write out upgrade errors to log file
				sw.WriteLine("The following unsupported paramaters were detected whilst attempting to upgrade the existing configuration file:");
				foreach (string item in errors)
				{
					sw.WriteLine(item);
				}
			}
			return Lines.ToArray();
		



		
		}

	}
}
