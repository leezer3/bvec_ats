using System;

namespace Plugin
{
	internal partial class TractionManager
	{
		internal class TravelMeter
		{
			/// <summary>The total distance travelled</summary>
			internal double TotalDistance;

			internal TravelMeterUnits Units = TravelMeterUnits.Kilometers;
			/// <summary>The 1000 unit digit reading</summary>
			internal int Digit1000;
			/// <summary>The 100 unit digit reading</summary>
			internal int Digit100;
			/// <summary>The 10 unit digit reading</summary>
			internal int Digit10;
			/// <summary>The single unit digit reading</summary>
			internal int Digit1;
			/// <summary>The .10 unit digit reading</summary>
			internal int Decimal10;
			/// <summary>The .01 unit digit reading</summary>
			internal int Decimal1;

			internal TravelMeterMode Mode = TravelMeterMode.IncreaseForwards;

			/// <summary>Updates the travel meter</summary>
			/// <param name="DistanceTravelled">The distance travelled in m</param>
			internal void Update(double DistanceTravelled)
			{
				if (Units == TravelMeterUnits.Miles)
				{
					//Convert to miles
					DistanceTravelled /= 0.621;
				}

				switch (Mode)
				{
					case TravelMeterMode.IncreaseForwards:
						//Increment the total counter
						TotalDistance += DistanceTravelled;
						break;
					case TravelMeterMode.IncreaseBackwards:
						if (DistanceTravelled > 0)
						{
							return;
						}
						//Invert total distance
						DistanceTravelled = Math.Abs(TotalDistance);
						TotalDistance += DistanceTravelled;
						break;
					case TravelMeterMode.IncreaseBoth:
						//Invert total distance
						DistanceTravelled = Math.Abs(TotalDistance);
						//Increment the total counter
						TotalDistance += DistanceTravelled;
						break;
				}
				Digit1000 = (int)Math.Abs(TotalDistance / 100000 % 10);
				Digit100 = (int)Math.Abs(TotalDistance / 10000 % 10);
				Digit10 = (int)Math.Abs(TotalDistance / 1000 % 10);
				Digit1 = (int)Math.Abs(TotalDistance / 100 % 10);
				Decimal10 = (int)Math.Abs(TotalDistance / 10 % 10);
				Decimal1 = (int)Math.Abs(TotalDistance / 1 % 10);
			}
		}

		internal enum TravelMeterUnits
		{
			Kilometers = 0,
			Miles = 1
		}

		internal enum TravelMeterMode
		{
			/// <summary>The travel meter increases whilst travelling in the forwards direction</summary>
			IncreaseForwards = 0,
			/// <summary>The travel meter increases whilst travelling in the backwards direction</summary>
			IncreaseBackwards = 1,
			/// <summary>The travel meter increases whilst travelling in either direction</summary>
			IncreaseBoth = 2,
		}
	}
}
