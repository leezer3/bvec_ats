using System.IO;

namespace Plugin
{
	partial class InternalFunctions
	{
		//Call this function to log an occured error in parsing a string to an array of values
		internal static void LogError(string FailingValue, int ErrorCase)
		{
			try
			{
				using (StreamWriter sw = File.AppendText(Path.Combine(Plugin.TrainFolder, "error.log")))
				{
					switch (ErrorCase)
					{
						case 0:
							sw.WriteLine("The paramater " + FailingValue + " contains invalid data. This should be a comma separated list of integers.");
							break;
						case 1:
							sw.WriteLine("The paramater " + FailingValue + " is not an integer between -1 & 255");
							break;
						case 2:
							sw.WriteLine("The paramater " + FailingValue +
										 " is not an integer between -2 & 3 [See the documentation for available values for each setting]");
							break;
						case 3:
							sw.WriteLine("The paramater " + FailingValue + " is not a valid number");
							break;
						case 4:
							sw.WriteLine("The paramater " + FailingValue + " failed to parse correctly");
							break;
						case 5:
							sw.WriteLine("The paramater " + FailingValue + " is not a valid key assignment to be upgraded.");
							break;
						case 6:
							sw.WriteLine(FailingValue);
							break;
						case 7:
							sw.WriteLine("The paramater " + FailingValue + " is not a valid key assignment.");
							break;
					}

				}
			}
			catch
			{
			}
		}
	}
}
