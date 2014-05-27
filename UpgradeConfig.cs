﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OpenBveApi.Runtime;

namespace Plugin
{
    class UpgradeOSATS
    {
        //This class will upgrade an existing OS_ATS configuration file.
        //Only supported values will be upgradeed.
        	internal static void UpgradeConfigurationFile(string file, string trainpath) {
            List<string> diesel = new List<string>();
            List<string> electric = new List<string>();
            List<string> steam = new List<string>();
            List<string> common = new List<string>();
            List<string> vigilance = new List<string>();
            List<string> AWS = new List<string>();
            List<string> TPWS = new List<string>();
            List<string> interlocks = new List<string>();
            List<string> windscreen = new List<string>();
            bool steamtype = false;
            bool dieseltype = false;
            //Read all lines of existing OS_ATS configuration file and add to appropriate arrays
			string[] lines = File.ReadAllLines(file, Encoding.UTF8);
			string section = string.Empty;
			for (int i = 0; i < lines.Length; i++) {
				string line = lines[i];
				int semicolon = line.IndexOf(';');
				if (semicolon >= 0) {
					line = line.Substring(0, semicolon).Trim();
				} else {
					line = line.Trim();
				}
				
			int equals = line.IndexOf('=');
			if (equals >= 0) {
			string key = line.Substring(0, equals).Trim().ToLowerInvariant();
            string value = line.Substring(equals + 1).Trim();
				switch (key) {
                case "traction":
                        if (Convert.ToInt32(value) == 1)
                        {
                            steamtype = true;
                        }
                        else if (Convert.ToInt32(value) == 2)
                        {
                            dieseltype = true;
                        }
                break;
                //VALUES COMMON TO ALL SECTIONS
                case "automatic":
                common.Add(line);
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
                case "ammetervalues":
                electric.Add(line);
                break;
                //VALUES TO BE ADDED TO VIGILANCE SECTION OF CONFIGURATION FILE
                case "vigilance":
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
                case "reminderkey":
                vigilance.Add("draenabled="+value);
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
                case "klaxonindicator":
                interlocks.Add(line);
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
                case "numberofdrops":
                windscreen.Add(line);
                break;
                case "dropstartindex":
                windscreen.Add(line);
                break;
                throw new InvalidDataException("The parameter " + key + " is not supported.");

						}
					}
				}
            //Create new configuration file and cycle through the newly created arrays to upgrade the original configuration file.
            using (StreamWriter sw = File.CreateText(Path.Combine(trainpath, "BVEC_Ats.cfg")))
            {
                //Traction Type First
                if (electric.Count > 0 && steamtype != true && dieseltype != true)
                {
                    sw.WriteLine("[Electric]");
                    if (common.Count > 0)
                    {
                        {
                            foreach (string item in common)
                            {
                                sw.WriteLine(item);
                            }
                        }
                    }

                    {
                        foreach (string item in electric)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }

                if (steamtype == true)
                {
                    sw.WriteLine("[Steam]");
                    if (common.Count > 0)
                    {
                        {
                            foreach (string item in common)
                            {
                                sw.WriteLine(item);
                            }
                        }
                    }
                    if (steam.Count > 0)
                    {
                        {
                            foreach (string item in steam)
                            {
                                sw.WriteLine(item);
                            }
                        }
                    }
                }

                if (dieseltype == true)
                {
                    sw.WriteLine("[Diesel]");
                    if (common.Count > 0)
                    {
                        {
                            foreach (string item in common)
                            {
                                sw.WriteLine(item);
                            }
                        }
                    }
                    if (diesel.Count > 0)
                    {
                        {
                            foreach (string item in diesel)
                            {
                                sw.WriteLine(item);
                            }
                        }
                    }
                }

                if (interlocks.Count > 0)
                {
                    sw.WriteLine("[Interlocks]");
                    {
                        foreach (string item in interlocks)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }

                if (vigilance.Count > 0)
                {
                    sw.WriteLine("[Vigilance]");
                    {
                        foreach (string item in vigilance)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }
                if (AWS.Count > 0)
                {
                    sw.WriteLine("[AWS]");
                    {
                        foreach (string item in AWS)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }
                if (TPWS.Count > 0)
                {
                    sw.WriteLine("[TPWS]");
                    {
                        foreach (string item in TPWS)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }
                if (windscreen.Count > 0)
                {
                    sw.WriteLine("[Windscreen]");
                    {
                        foreach (string item in windscreen)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }
            }
			}
    }
}
