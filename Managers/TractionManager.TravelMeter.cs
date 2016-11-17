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

			private double Counter;

			internal TravelMeterMode Mode = TravelMeterMode.IncreaseForwards;

			/// <summary>Updates the travel meter</summary>
			/// <param name="DistanceTravelled">The distance travelled in m</param>
			internal void Update(double DistanceTravelled)
			{
				//BUG: This doesn't actally work if we travel more than 1m in a frame
				//Issue with the original OS_ATS code, not going to fix just at the minute
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
						Counter += DistanceTravelled;
						if (Counter > 1)
						{
							Decimal1++;
							Counter = 0.0;
						}
						if (Decimal1 > 9)
						{
							Decimal10++;
							Decimal1 = 0;
						}
						if (Decimal10 > 9)
						{
							Digit1++;
							Decimal10 = 0;
						}
						if (Digit10 > 9)
						{
							Digit10++;
							Digit1 = 0;
						}
						if (Digit10 > 9)
						{
							Digit100++;
							Digit10 = 0;
						}
						break;
					case TravelMeterMode.IncreaseBackwards:
						if (DistanceTravelled > 0)
						{
							return;
						}
						//Invert total distance
						DistanceTravelled = Math.Abs(TotalDistance);
						//Increment the total counter
						TotalDistance += DistanceTravelled;
						Counter += DistanceTravelled;
						if (DistanceTravelled > 1)
						{
							Decimal1++;
							Counter = 0.0;
						}
						if (Decimal1 > 9)
						{
							Decimal10++;
							Decimal1 = 0;
						}
						if (Decimal10 > 9)
						{
							Digit1++;
							Decimal10 = 0;
						}
						if (Digit10 > 9)
						{
							Digit10++;
							Digit1 = 0;
						}
						if (Digit10 > 9)
						{
							Digit100++;
							Digit10 = 0;
						}
						break;
					case TravelMeterMode.IncreaseBoth:
						//Invert total distance
						DistanceTravelled = Math.Abs(TotalDistance);
						//Increment the total counter
						TotalDistance += DistanceTravelled;
						Counter += DistanceTravelled;
						if (DistanceTravelled > 1)
						{
							Decimal1++;
							Counter = 0.0;
						}
						if (Decimal1 > 9)
						{
							Decimal10++;
							Decimal1 = 0;
						}
						if (Decimal10 > 9)
						{
							Digit1++;
							Decimal10 = 0;
						}
						if (Digit10 > 9)
						{
							Digit10++;
							Digit1 = 0;
						}
						if (Digit10 > 9)
						{
							Digit100++;
							Digit10 = 0;
						}
						break;
				}	
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
