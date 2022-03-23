using System.Windows.Forms;
using OpenBveApi.Runtime;

namespace Plugin
{
	/// <summary>Represents a LU Trainstop device</summary>
	internal class Trainstop : Device
	{
		/// <summary>The underlying train.</summary>
		private readonly Train Train;
		internal int awsIndicator;
		internal int brakeDemandIndicator;
		internal int stopOverrideIndicator;
		internal double awsDelay;
		internal double tpwsStopDelay;
		internal double tpwsOverrideLifetime;
		private bool awsStop;
		private bool awsRelease;
		private bool tpwsTrainstop;
		private bool tpwsRelease;
		private Timer awsTimer;
		private Timer overrideTimer;
		private Timer stopTimer;

		internal override void Elapse(ElapseData data, ref bool blocking)
		{
			if(awsTimer.TimerActive && awsTimer.TimeElapsed != -2)
			{
				awsTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
				if(awsTimer.TimeElapsed > awsDelay && awsStop == false)
				{
					awsTimer.TimeElapsed = -2;
					awsStop = true;
					Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the LU Trainstop system.");
					awsRelease = false;
				}
			}

			this.Train.Panel[brakeDemandIndicator] = 1;
			this.Train.Panel[stopOverrideIndicator] = 1;
	
			if(data.Vehicle.Speed.KilometersPerHour == 0)
			{
				if (stopTimer.TimerActive == false)
				{
					stopTimer.TimerActive = true;
					stopTimer.TimeElapsed = 0;
				}
					
					
				if(awsRelease)
				{
					Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the LU Trainstop system.");
					if (awsIndicator != -1)
					{
						this.Train.Panel[awsIndicator] = 0;
					}

					awsTimer.TimerActive = false;
					awsStop = false;
					awsRelease = false;
				}

				stopTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
				if(tpwsRelease && stopTimer.TimeElapsed > tpwsStopDelay * 1000)
				{
					Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the LU Trainstop system.");
					this.Train.Panel[brakeDemandIndicator] = 1;
					tpwsTrainstop = false;
					tpwsRelease = false;
				}
			}

			if (overrideTimer.TimerActive)
			{
				overrideTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
				if(overrideTimer.TimeElapsed > tpwsOverrideLifetime * 1000)
				{
					overrideTimer.TimerActive = false;
					this.Train.Panel[stopOverrideIndicator] = 0;
				}
			}
				
		}
	}
}
