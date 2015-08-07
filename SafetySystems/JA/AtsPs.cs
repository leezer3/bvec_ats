using System;
using System.Text;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>Represents ATS-Ps.</summary>
	internal class AtsPs : Device {
		
		// --- enumerations ---
		
		/// <summary>Represents different states of ATS-Ps.</summary>
		internal enum States {
			/// <summary>The system is disabled.</summary>
			Disabled = 0,
			/// <summary>The system is enabled, but currently suppressed because ATS-Sx is unavailable.</summary>
			Suppressed = 1,
			/// <summary>The system is available but no ATS-Ps signal was yet picked up.</summary>
			Standby = 2,
			/// <summary>The system is operating normally.</summary>
			Normal = 3,
			/// <summary>At least one brake pattern is set up.</summary>
			Pattern = 4,
			/// <summary>The system is approaching a brake pattern.</summary>
			Approaching = 5,
			/// <summary>The system applies the emergency brakes.</summary>
			Emergency = 6
		}
		
		
		// --- function selector ---
		
		/// <summary>Represents a function selector used to determine what purpose a particular combination of beacons serves.</summary>
		private class FunctionSelector {
			/// <summary>The frequency of the first beacon, or 0 if not available yet.</summary>
			internal int FirstBeaconFrequency;
			/// <summary>The frequency of the second beacon, or 0 if not available yet.</summary>
			internal int SecondBeaconFrequency;
			/// <summary>The frequency of the third beacon, or 0 if not available yet.</summary>
			internal int ThirdBeaconFrequency;
			/// <summary>The distance accumulated since the last reported beacon.</summary>
			internal double DistanceAccumulator;
			/// <summary>Creates a new function selector.</summary>
			internal FunctionSelector() {
				this.FirstBeaconFrequency = 0;
				this.SecondBeaconFrequency = 0;
				this.ThirdBeaconFrequency = 0;
				this.DistanceAccumulator = 0.0;
			}
			/// <summary>Adds another beacon to the function selector.</summary>
			/// <param name="frequency">The frequency of the beacon.</param>
			internal void Add(int frequency) {
				if (frequency != 0) {
					if (this.FirstBeaconFrequency == 0) {
						this.FirstBeaconFrequency = frequency;
					} else if (this.SecondBeaconFrequency == 0) {
						this.SecondBeaconFrequency = frequency;
					} else if (this.ThirdBeaconFrequency == 0) {
						this.ThirdBeaconFrequency = frequency;
					}
				}
			}
			/// <summary>Clears the state of the function selector.</summary>
			internal void Clear() {
				this.FirstBeaconFrequency = 0;
				this.SecondBeaconFrequency = 0;
				this.ThirdBeaconFrequency = 0;
				this.DistanceAccumulator = 0.0;
			}
			/// <summary>Tries to perform the function. If the function was performed successfully, the state of the function selector is cleared.</summary>
			/// <param name="system">A reference to the current ATS-Ps system.</param>
			/// <param name="data">The elapse data.</param>
			internal void Perform(AtsPs system, ElapseData data) {
				if (this.FirstBeaconFrequency != 0) {
					if (this.FirstBeaconFrequency == 73) {
						// --- activate ATS-Ps ---
						system.SwitchToPs();
						this.Clear();
					} else if (this.FirstBeaconFrequency == 80) {
						// --- Ps1 (pattern A) ---
						system.SignalAPattern.SetPs1();
						system.SwitchToPs();
						this.Clear();
					} else if (this.FirstBeaconFrequency == 85) {
						// --- ignore subsequent Ps combinations ---
						if (this.SecondBeaconFrequency != 0) {
							if (this.SecondBeaconFrequency == 108) {
								if (this.ThirdBeaconFrequency != 0) {
									this.Clear();
								}
							} else {
								this.Clear();
							}
						}
					} else if (this.FirstBeaconFrequency == 90) {
						if (this.SecondBeaconFrequency != 0) {
							if (this.SecondBeaconFrequency == 80) {
								// --- slope limit ---
								double speed;
								if (this.DistanceAccumulator > 1.0 & this.DistanceAccumulator < 2.7) {
									speed = double.MaxValue;
								} else if (this.DistanceAccumulator > 3.3 & this.DistanceAccumulator < 4.8) {
									speed = 95.0 / 3.6;
								} else if (this.DistanceAccumulator > 5.7 & this.DistanceAccumulator < 7.2) {
									speed = 85.0 / 3.6;
								} else if (this.DistanceAccumulator > 8.3 & this.DistanceAccumulator < 9.8) {
									speed = 75.0 / 3.6;
								} else if (this.DistanceAccumulator > 11.5 & this.DistanceAccumulator < 13.0) {
									speed = 65.0 / 3.6;
								} else if (this.DistanceAccumulator > 14.4 & this.DistanceAccumulator < 15.9) {
									speed = 55.0 / 3.6;
								} else if (this.DistanceAccumulator > 17.9 & this.DistanceAccumulator < 19.4) {
									speed = 45.0 / 3.6;
								} else if (this.DistanceAccumulator > 21.8 & this.DistanceAccumulator < 23.3) {
									speed = 35.0 / 3.6;
								} else {
									speed = 0.0;
								}
								if (speed != 0.0) {
									system.SlopePattern.SetUpcomingLimit(speed + 10 / 3.6, false);
								} else {
									system.State = States.Emergency;
								}
								system.SwitchToPs();
								this.Clear();
							} else if (this.SecondBeaconFrequency == 85) {
								// --- curve limit ---
								double speed;
								if (this.DistanceAccumulator > 1.0 & this.DistanceAccumulator < 2.7) {
									speed = double.MaxValue;
								} else if (this.DistanceAccumulator > 3.3 & this.DistanceAccumulator < 4.8) {
									speed = 100.0 / 3.6;
								} else if (this.DistanceAccumulator > 5.7 & this.DistanceAccumulator < 7.2) {
									speed = 90.0 / 3.6;
								} else if (this.DistanceAccumulator > 8.3 & this.DistanceAccumulator < 9.8) {
									speed = 80.0 / 3.6;
								} else if (this.DistanceAccumulator > 11.5 & this.DistanceAccumulator < 13.0) {
									speed = 70.0 / 3.6;
								} else if (this.DistanceAccumulator > 14.4 & this.DistanceAccumulator < 15.9) {
									speed = 60.0 / 3.6;
								} else if (this.DistanceAccumulator > 17.9 & this.DistanceAccumulator < 19.4) {
									speed = 50.0 / 3.6;
								} else if (this.DistanceAccumulator > 21.8 & this.DistanceAccumulator < 23.3) {
									speed = 40.0 / 3.6;
								} else {
									speed = 0.0;
								}
								if (speed != 0.0) {
									system.CurvePattern.SetUpcomingLimit(speed + 10 / 3.6, false);
								} else {
									system.State = States.Emergency;
								}
								system.SwitchToPs();
								this.Clear();
							} else if (this.SecondBeaconFrequency == 90) {
								// --- temporary limit ---
								double speed;
								if (this.DistanceAccumulator > 1.0 & this.DistanceAccumulator < 2.7) {
									speed = double.MaxValue;
								} else if (this.DistanceAccumulator > 3.3 & this.DistanceAccumulator < 4.8) {
									speed = 55.0 / 3.6;
								} else if (this.DistanceAccumulator > 5.7 & this.DistanceAccumulator < 7.2) {
									speed = 50.0 / 3.6;
								} else if (this.DistanceAccumulator > 8.3 & this.DistanceAccumulator < 9.8) {
									speed = 45.0 / 3.6;
								} else if (this.DistanceAccumulator > 11.5 & this.DistanceAccumulator < 13.0) {
									speed = 40.0 / 3.6;
								} else if (this.DistanceAccumulator > 14.4 & this.DistanceAccumulator < 15.9) {
									speed = 35.0 / 3.6;
								} else if (this.DistanceAccumulator > 17.9 & this.DistanceAccumulator < 19.4) {
									speed = 30.0 / 3.6;
								} else if (this.DistanceAccumulator > 21.8 & this.DistanceAccumulator < 23.3) {
									speed = 25.0 / 3.6;
								} else {
									speed = 0.0;
								}
								if (speed != 0.0) {
									system.TemporaryPattern.SetUpcomingLimit(speed + 10 / 3.6, false);
								} else {
									system.State = States.Emergency;
								}
								system.SwitchToPs();
								this.Clear();
							} else if (this.SecondBeaconFrequency == 95) {
								// --- divergence limit ---
								double speed;
								if (this.DistanceAccumulator > 1.0 & this.DistanceAccumulator < 2.7) {
									speed = double.MaxValue;
								} else if (this.DistanceAccumulator > 3.3 & this.DistanceAccumulator < 4.8) {
									speed = 60.0 / 3.6;
								} else if (this.DistanceAccumulator > 5.7 & this.DistanceAccumulator < 7.2) {
									speed = 55.0 / 3.6;
								} else if (this.DistanceAccumulator > 8.3 & this.DistanceAccumulator < 9.8) {
									speed = 50.0 / 3.6;
								} else if (this.DistanceAccumulator > 11.5 & this.DistanceAccumulator < 13.0) {
									speed = 45.0 / 3.6;
								} else if (this.DistanceAccumulator > 14.4 & this.DistanceAccumulator < 15.9) {
									speed = 40.0 / 3.6;
								} else if (this.DistanceAccumulator > 17.9 & this.DistanceAccumulator < 19.4) {
									speed = 35.0 / 3.6;
								} else if (this.DistanceAccumulator > 21.8 & this.DistanceAccumulator < 23.3) {
									speed = 25.0 / 3.6;
								} else {
									speed = 0.0;
								}
								if (speed != 0.0) {
									system.DivergencePattern.SetUpcomingLimit(speed + 10 / 3.6, true);
								} else {
									system.State = States.Emergency;
								}
								system.SwitchToPs();
								this.Clear();
							} else if (this.SecondBeaconFrequency == 108) {
								// --- Ps -> Sx ---
								system.SwitchToSx();
								this.Clear();
							} else {
								// --- unsupported ---
								this.Clear();
							}
						}
					} else if (this.FirstBeaconFrequency == 95) {
						if (this.SecondBeaconFrequency != 0) {
							if (this.SecondBeaconFrequency == 80) {
								// --- Ps1 (pattern B) --
								system.SignalBPattern.SetPs1();
								system.SwitchToPs();
								this.Clear();
							} else if (this.SecondBeaconFrequency == 103) {
								// --- clear (pattern B) ---
								system.SignalBPattern.Clear();
								system.SwitchToPs();
								this.Clear();
							} else if (this.SecondBeaconFrequency == 108) {
								if (this.ThirdBeaconFrequency != 0) {
									if (this.ThirdBeaconFrequency == 103) {
										// --- clear (pattern B) ---
										system.SignalBPattern.Clear();
										system.SwitchToPs();
										this.Clear();
									} else if (this.ThirdBeaconFrequency == 108) {
										// --- Ps2 (pattern B) ---
										system.SignalBPattern.SetPs2();
										system.SwitchToPs();
										this.Clear();
									} else {
										// --- unsupported ---
										this.Clear();
									}
								}
							} else if (this.SecondBeaconFrequency == 123) {
								// --- 15km/h (pattern B) ---
								system.SignalBPattern.Set15Kmph();
								system.SwitchToPs();
								this.Clear();
							} else if (this.SecondBeaconFrequency == 129 | this.SecondBeaconFrequency == 130) {
								// --- downslope (pattern B) ---
								double gradient;
								if (this.DistanceAccumulator >= 1.0 & this.DistanceAccumulator <= 2.7) {
									gradient = -0.015;
								} else if (this.DistanceAccumulator >= 3.3 & this.DistanceAccumulator <= 4.8) {
									gradient = -0.025;
								} else if (this.DistanceAccumulator >= 5.7 & this.DistanceAccumulator <= 7.2) {
									gradient = -0.035;
								} else {
									gradient = 0.0;
								}
								system.SignalBPattern.SetGradient(gradient);
								system.SwitchToPs();
								this.Clear();
							} else {
								// --- unsupported ---
								this.Clear();
							}
						}
					} else if (this.FirstBeaconFrequency == 103) {
						// --- clear (pattern A) ---
						if (system.State != States.Standby) {
							system.SignalAPattern.Clear();
							system.IrekaePattern.Clear();
						}
						this.Clear();
					} else if (this.FirstBeaconFrequency == 108) {
						if (this.SecondBeaconFrequency != 0) {
							if (this.SecondBeaconFrequency == 80) {
								// --- Ps3 (pattern A) ---
								system.SignalAPattern.SetPs3();
								system.SwitchToPs();
								this.Clear();
							} else if (this.SecondBeaconFrequency == 85) {
								// --- yuudou limit ---
								if (this.DistanceAccumulator >= 1.0 & this.DistanceAccumulator <= 2.7) {
									system.YuudouPattern.SetImmediateLimit((15 + 10) / 3.6);
									system.SwitchToPs();
								} else if (this.DistanceAccumulator >= 3.3 & this.DistanceAccumulator <= 4.8) {
									system.YuudouPattern.Clear();
									system.SwitchToPs();
								}
								this.Clear();
							} else if (this.SecondBeaconFrequency == 90 | this.SecondBeaconFrequency == 95) {
								// --- irekae limit ---
								if (this.DistanceAccumulator >= 1.0 & this.DistanceAccumulator <= 2.7) {
									system.IrekaePattern.SetImmediateLimit((25 + 5) / 3.6);
									system.SwitchToPs();
								} else if (this.DistanceAccumulator >= 3.3 & this.DistanceAccumulator <= 4.8) {
									system.IrekaePattern.SetImmediateLimit((45 + 5) / 3.6);
									system.SwitchToPs();
								}
								this.Clear();
							} else if (this.SecondBeaconFrequency == 103) {
								// --- clear (pattern A) ---
								if (system.State != States.Standby) {
									system.SignalAPattern.Clear();
								}
								this.Clear();
							} else if (this.SecondBeaconFrequency == 108) {
								// --- Ps2 (pattern A) ---
								if (system.State != States.Standby) {
									system.SignalAPattern.SetPs2();
								}
								this.Clear();
							} else if (this.SecondBeaconFrequency == 129 | this.SecondBeaconFrequency == 130) {
								// --- downslope (pattern A) ---
								if (system.State != States.Standby) {
									double gradient;
									if (this.DistanceAccumulator >= 1.0 & this.DistanceAccumulator <= 2.7) {
										gradient = -0.015;
									} else if (this.DistanceAccumulator >= 3.3 & this.DistanceAccumulator <= 4.8) {
										gradient = -0.025;
									} else if (this.DistanceAccumulator >= 5.7 & this.DistanceAccumulator <= 7.2) {
										gradient = -0.035;
									} else {
										gradient = 0.0;
									}
									system.SignalAPattern.SetGradient(gradient);
								}
								this.Clear();
							} else {
								// --- unsupported ---
								this.Clear();
							}
						}
					} else if (this.FirstBeaconFrequency == 123) {
						// --- 15km/h (pattern A) ---
						if (system.State != States.Standby) {
							system.SignalAPattern.Set15Kmph();
							system.IrekaePattern.Clear();
						}
						this.Clear();
					} else {
						// --- unsupported ---
						this.Clear();
					}
				}
				if (this.FirstBeaconFrequency != 0) {
					this.DistanceAccumulator += data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
				}
			}
		}
		
		
		// --- pattern ---
		
		/// <summary>Represents a pattern.</summary>
		internal class Pattern {
			// --- members ---
			/// <summary>The distance to the point of danger in meters, or System.Double.MinValue, or System.Double.MaxValue.</summary>
			internal double Distance;
			/// <summary>The speed limit at the point of danger, or System.DoubleMaxValue.</summary>
			internal double TargetSpeed;
			/// <summary>The release speed. The brake pattern will not drop below this value.</summary>
			internal double ReleaseSpeed;
			/// <summary>The distance to the point where the pattern should be automatically cleared, or System.Double.MaxValue.</summary>
			internal double ReleaseDistance;
			/// <summary>The speed limit imposed by this pattern, or System.Double.MaxValue.</summary>
			internal double SpeedPattern;
			/// <summary>The gradient currently underlying the speed pattern. The gradient is negative for downslopes.</summary>
			internal double CurrentGradient;
			/// <summary>The gradient received through beacons but not yet affecting the speed pattern. The gradient is negative for downslopes.</summary>
			internal double UpcomingGradient;
			/// <summary>Whether the pattern is persistent, i.e. cannot be cleared.</summary>
			internal bool Persistent;
			// --- constructors ---
			/// <summary>Creates a new pattern.</summary>
			internal Pattern() {
				this.Distance = double.MaxValue;
				this.TargetSpeed = double.MaxValue;
				this.ReleaseSpeed = 0.0;
				this.ReleaseDistance = double.MaxValue;
				this.SpeedPattern = double.MaxValue;
				this.CurrentGradient = 0.0;
				this.UpcomingGradient = 0.0;
			}
			// --- functions ---
			/// <summary>Updates the pattern.</summary>
			/// <param name="data">The elapse data.</param>
			internal void Perform(ElapseData data) {
				if (this.Distance == double.MaxValue) {
					this.SpeedPattern = double.MaxValue;
				} else if (this.TargetSpeed == double.MaxValue) {
					this.SpeedPattern = double.MaxValue;
				} else {
					double value = data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
					if (this.ReleaseDistance != double.MaxValue) {
						this.ReleaseDistance -= value;
					}
					if (this.ReleaseDistance <= 0.0) {
						this.Clear();
					} else {
						if (this.Distance != double.MinValue & this.Distance != double.MaxValue) {
							this.Distance -= value;
						}
						this.SpeedPattern = GetPatternSpeed(this.Distance);
						if (this.SpeedPattern < this.ReleaseSpeed) {
							this.SpeedPattern = this.ReleaseSpeed;
						}
					}
				}
			}
			/// <summary>Gets the speed of the pattern at the specified distance, not incorporating the release speed.</summary>
			/// <param name="distance">The distance.</param>
			/// <returns>The speed of the pattern at the specified distance.</returns>
			private double GetPatternSpeed(double distance) {
				if (distance == double.MaxValue) {
					return double.MaxValue;
				} else if (distance <= 0.0) {
					return this.TargetSpeed;
				} else {
					const double designDeceleration = 4.0 / 3.6;
					const double earthGravity = 9.81;
					const double delay = 2.0;
					const double offset = 20.0;
					double deceleration = designDeceleration + earthGravity * this.CurrentGradient;
					double sqrtTerm = 2.0 * deceleration * (this.Distance - offset) + deceleration * deceleration * delay * delay + this.TargetSpeed * this.TargetSpeed;
					if (sqrtTerm <= 0.0) {
						return this.TargetSpeed;
					} else {
						double speed = Math.Sqrt(sqrtTerm) - deceleration * delay;
						if (speed < this.TargetSpeed) {
							return this.TargetSpeed;
						} else {
							return speed;
						}
					}
				}
			}
			/// <summary>Sets the pattern to Ps1.</summary>
			internal void SetPs1() {
				if (this.UpcomingGradient < -0.025) {
					this.ReleaseDistance = 1350.0;
				} else if (this.UpcomingGradient < -0.015) {
					this.ReleaseDistance = 970.0;
				} else if (this.UpcomingGradient < -0.005) {
					this.ReleaseDistance = 775.0;
				} else {
					this.ReleaseDistance = 655.0;
				}
				this.Distance = this.ReleaseDistance;
				this.TargetSpeed = 0.0;
				this.ReleaseSpeed = 65.0 / 3.6;
				this.CurrentGradient = this.UpcomingGradient;
			}
			/// <summary>Sets the pattern to Ps2.</summary>
			internal void SetPs2() {
				if (this.UpcomingGradient < -0.025) {
					this.ReleaseDistance = 765.0;
				} else if (this.UpcomingGradient < -0.015) {
					this.ReleaseDistance = 560.0;
				} else if (this.UpcomingGradient < -0.005) {
					this.ReleaseDistance = 455.0;
				} else {
					this.ReleaseDistance = 390.0;
				}
				this.Distance = this.ReleaseDistance;
				this.TargetSpeed = 0.0;
				this.ReleaseSpeed = 10.0 / 3.6;
				this.CurrentGradient = this.UpcomingGradient;
			}
			/// <summary>Sets the pattern to Ps3.</summary>
			internal void SetPs3() {
				this.ReleaseDistance = 100.0;
				this.Distance = this.ReleaseDistance;
				this.TargetSpeed = 0.0;
				this.ReleaseSpeed = 10.0 / 3.6;
				this.CurrentGradient = 0.0;
			}
			/// <summary>Sets the pattern to 15 km/h for 80 meters.</summary>
			internal void Set15Kmph() {
				this.ReleaseDistance = 80.0;
				this.Distance = 0.0;
				this.TargetSpeed = 0.0;
				this.ReleaseSpeed = 15.0 / 3.6;
			}
			/// <summary>Sets the pattern to the specified speed limit in 555m.</summary>
			/// <param name="speed">The speed limit. The value of System.Double.MaxValue clears the pattern.</param>
			/// <param name="clear">Whether to automatically clear the pattern after 50 meters behind the point of danger.</param>
			internal void SetUpcomingLimit(double speed, bool clear) {
				if (speed == double.MaxValue) {
					this.Clear();
				} else {
					this.Distance = 555.0;
					this.TargetSpeed = speed;
					this.ReleaseDistance = clear ? this.Distance + 50.0 : double.MaxValue;
					this.ReleaseSpeed = this.TargetSpeed;
				}
			}
			/// <summary>Sets the pattern to the specified speed limit, effective immediately.</summary>
			/// <param name="speed">The speed limit. The value of System.Double.MaxValue clears the pattern.</param>
			internal void SetImmediateLimit(double speed) {
				if (speed == double.MaxValue) {
					this.Clear();
				} else {
					this.Distance = double.MinValue;
					this.TargetSpeed = speed;
					this.ReleaseDistance = double.MaxValue;
					this.ReleaseSpeed = this.TargetSpeed;
				}
			}
			/// <summary>Sets the train-specific permanent speed limit.</summary>
			/// <param name="speed">The speed limit. The value of System.Double.MaxValue clears the pattern.</param>
			internal void SetPersistentLimit(double speed) {
				if (speed == double.MaxValue) {
					this.Clear();
				} else {
					this.Distance = double.MinValue;
					this.TargetSpeed = speed;
					this.ReleaseSpeed = 0.0;
					this.ReleaseDistance = double.MaxValue;
					this.Persistent = true;
				}
			}
			/// <summary>Sets the gradient. This will only affect the current pattern once passing Ps1, Ps2, or Ps3.</summary>
			/// <param name="gradient"></param>
			internal void SetGradient(double gradient) {
				this.UpcomingGradient = gradient;
			}
			/// <summary>Clears the pattern.</summary>
			internal void Clear() {
				if (!this.Persistent) {
					this.Distance = double.MaxValue;
					this.TargetSpeed = double.MaxValue;
					this.ReleaseSpeed = 0.0;
					this.ReleaseDistance = 0.0;
					this.SpeedPattern = double.MaxValue;
					this.CurrentGradient = 0.0;
					this.UpcomingGradient = 0.0;
				}
			}
			/// <summary>Adds a textual representation to the specified string builder if this pattern is not clear.</summary>
			/// <param name="prefix">The textual prefix.</param>
			/// <param name="builder">The string builder.</param>
			internal void AddToStringBuilder(string prefix, StringBuilder builder) {
				if (this.Distance <= 0.0) {
					string text = prefix + (3.6 * this.SpeedPattern).ToString("0");
					if (builder.Length != 0) {
						builder.Append(", ");
					}
					builder.Append(text);
				} else if (this.Distance != double.MaxValue) {
					string text = prefix + (3.6 * this.TargetSpeed).ToString("0") + "(" + (3.6 * this.SpeedPattern).ToString("0") + ")@" + this.Distance.ToString("0");
					if (builder.Length != 0) {
						builder.Append(", ");
					}
					builder.Append(text);
				} else {
					// do nothing
				}
			}
		}
		
		
		// --- members ---
		
		/// <summary>The underlying train.</summary>
		private Train Train;
		
		/// <summary>The current state of the system.</summary>
		internal States State;
		
		/// <summary>The function selector.</summary>
		private FunctionSelector Selector;
		
		/// <summary>The frequency of the last IIYAMA-style Ps marker beacon. This can be 90, 95 or 108. For picking up Ps2 on the second 108.5 KHz beacon, this can be 108 (pattern A) or 109 (pattern B).</summary>
		private int CompatibilitySelector;
		
		/// <summary>The distance traveled since the last IIYAMA-style Ps marker beacon.</summary>
		private double CompatibilityDistanceAccumulator;
		
		/// <summary>The train-specific permanent pattern.</summary>
		internal Pattern TrainPermanentPattern;
		
		/// <summary>The signal A pattern.</summary>
		private Pattern SignalAPattern;
		
		/// <summary>The signal B pattern.</summary>
		private Pattern SignalBPattern;
		
		/// <summary>The divergence (95 KHz) limit pattern.</summary>
		private Pattern DivergencePattern;

		/// <summary>The temporary (90 KHz) limit pattern.</summary>
		private Pattern TemporaryPattern;

		/// <summary>The curve (85 KHz) limit pattern.</summary>
		private Pattern CurvePattern;

		/// <summary>The slope (80 KHz) limit pattern.</summary>
		private Pattern SlopePattern;
		
		/// <summary>The irekae limit pattern.</summary>
		private Pattern IrekaePattern;

		/// <summary>The yuudou limit pattern.</summary>
		private Pattern YuudouPattern;
		
		/// <summary>An array of all patterns.</summary>
		internal Pattern[] Patterns;

	    internal int ATSPatternEstablishment = -1;
	    internal int ATSPatternRelease = -1;
	    internal int ATSPSChime = -1;

		
		// --- constructors ---
		
		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal AtsPs(Train train) {
			this.Train = train;
			this.State = States.Disabled;
			this.Selector = new FunctionSelector();
			this.CompatibilitySelector = 0;
			this.CompatibilityDistanceAccumulator = 0.0;
			this.SignalAPattern = new Pattern();
			this.SignalBPattern = new Pattern();
			this.DivergencePattern = new Pattern();
			this.TemporaryPattern = new Pattern();
			this.CurvePattern = new Pattern();
			this.SlopePattern = new Pattern();
			this.IrekaePattern = new Pattern();
			this.YuudouPattern = new Pattern();
			this.TrainPermanentPattern = new Pattern();
			this.Patterns = new Pattern[] {
				this.SignalAPattern, this.SignalBPattern,
				this.DivergencePattern, this.TemporaryPattern, this.CurvePattern, this.SlopePattern,
				this.IrekaePattern, this.YuudouPattern,
				this.TrainPermanentPattern
			};
		}
		
		
		// --- functions ---
		
		/// <summary>Switches to ATS-Ps.</summary>
		private void SwitchToPs() {
			if (this.State == States.Standby) {
				this.State = States.Normal;
                SoundManager.Play(ATSPatternEstablishment, 1.0, 1.0, false);
			}
		}
		
		/// <summary>Switches to ATS-Sx.</summary>
		private void SwitchToSx() {
			if (this.State == States.Emergency) {
				this.State = States.Standby;
				this.Train.AtsSx.State = AtsSx.States.Emergency;
			} else if (this.State != States.Standby) {
				this.State = States.Standby;
                SoundManager.Play(ATSPatternRelease, 1.0, 1.0, false);
			}
			this.Selector.Clear();
			foreach (Pattern pattern in this.Patterns) {
				pattern.Clear();
			}
		}
		
		// --- inherited functions ---
		
		/// <summary>Is called when the system should initialize.</summary>
		/// <param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
			if (this.Train.AtsSx.State == AtsSx.States.Disabled) {
				this.State = States.Disabled;
			} else {
				if (mode == InitializationModes.OffEmergency) {
					this.State = States.Suppressed;
				} else {
					this.State = States.Standby;
				}
			}
			this.Selector.Clear();
			foreach (Pattern pattern in this.Patterns) {
				pattern.Clear();
			}
		}

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking) {
			// --- behavior ---
			if (this.State == States.Suppressed) {
				if (this.Train.AtsSx.State != AtsSx.States.Disabled & this.Train.AtsSx.State != AtsSx.States.Suppressed & this.Train.AtsSx.State != AtsSx.States.Initializing) {
					this.State = States.Standby;
				}
			} else if (this.State != States.Disabled) {
				if (this.Train.AtsSx.State == AtsSx.States.Disabled | this.Train.AtsSx.State == AtsSx.States.Suppressed | this.Train.AtsSx.State == AtsSx.States.Initializing) {
					this.State = States.Suppressed;
				}
			}
			if (blocking) {
				if (this.State != States.Disabled & this.State != States.Suppressed) {
					this.State = States.Standby;
				}
			} else if (this.State != States.Disabled & this.State != States.Suppressed) {
				this.Selector.Perform(this, data);
				if (this.CompatibilitySelector != 0) {
					this.CompatibilityDistanceAccumulator += this.Train.State.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
					if (this.CompatibilityDistanceAccumulator > 27.7) {
						this.CompatibilitySelector = 0;
					}
				}
				if (this.State != States.Standby) {
					foreach (Pattern pattern in this.Patterns) {
						pattern.Perform(data);
					}
					double limit = double.MaxValue;
					bool establishment = false;
					foreach (Pattern pattern in this.Patterns) {
						if (pattern.SpeedPattern < limit) {
							limit = pattern.SpeedPattern;
						}
						if (pattern != this.TrainPermanentPattern & pattern.SpeedPattern != double.MaxValue) {
							establishment = true;
						}
					}
					if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= limit) {
						if (this.State != States.Emergency) {
							this.State = States.Emergency;
                            SoundManager.Play(ATSPSChime, 1.0, 1.0, false);
						}
					} else if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= limit - 10.0 / 3.6) {
						if (this.State == States.Normal) {
                            SoundManager.Play(ATSPatternEstablishment, 1.0, 1.0, false);
						} else if (this.State == States.Pattern) {
                            SoundManager.Play(ATSPSChime, 1.0, 1.0, false);
						}
						if (this.State == States.Normal | this.State == States.Pattern) {
							this.State = States.Approaching;
						}
					} else if (establishment) {
						if (this.State == States.Normal) {
                            SoundManager.Play(ATSPatternEstablishment, 1.0, 1.0, false);
						} else if (this.State == States.Approaching) {
							SoundManager.Play(ATSPSChime, 1.0, 1.0, false);
						}
						if (this.State == States.Normal | this.State == States.Approaching) {
							this.State = States.Pattern;
						}
					} else {
						if (this.State == States.Pattern | this.State == States.Approaching) {
							this.State = States.Normal;
                            SoundManager.Play(ATSPatternRelease, 1.0, 1.0, false);
						}
					}
					if (this.State == States.Emergency) {
						this.Train.Sounds.AtsBell.Play();
						Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
					}
					else
					{
					    Train.tractionmanager.resetbrakeapplication();
					}
				}
			}
			// --- panel ---
			if (this.State != States.Disabled & this.State != States.Suppressed & this.State != States.Standby) {
				double limit = double.MaxValue;
				if (this.State == States.Normal | this.State == States.Pattern | this.State == States.Approaching | this.State == States.Emergency) {
					foreach (Pattern pattern in this.Patterns) {
						if (pattern.SpeedPattern < limit) {
							limit = pattern.SpeedPattern;
						}
					}
				}
				if (this.State == States.Pattern | this.State == States.Approaching | this.State == States.Emergency) {
					this.Train.Panel[35] = 1;
				}
				if (this.State == States.Approaching) {
					this.Train.Panel[36] = 1;
				}
				if (this.State == States.Emergency) {
					this.Train.Panel[37] = 1;
				}
				if (blocking) {
					this.Train.Panel[41] = 60;
				} else {
					this.Train.Panel[40] = (int)Math.Min(Math.Floor(3.0 / 7.0 * Math.Abs(data.Vehicle.Speed.KilometersPerHour)), 60.0);
					this.Train.Panel[41] = (int)Math.Min(Math.Floor(3.0 / 7.0 * 3.6 * limit), 60.0);
				}
			} else {
				this.Train.Panel[41] = 60;
			}
			if (this.State == States.Suppressed & (this.Train.AtsSx.State == AtsSx.States.Disabled | this.Train.AtsSx.State == AtsSx.States.Initializing)) {
				this.Train.Panel[39] = 1;
			}
			if (this.State != States.Disabled & this.State != States.Suppressed) {
				this.Train.Panel[42] = 1;
			}
			if (this.State == States.Disabled) {
				this.Train.Panel[54] = 1;
			}
			// --- panel (debug) ---
			if (this.SignalAPattern.ReleaseSpeed == 15.0 / 3.6) {
				this.Train.Panel[60] = 1;
				this.Train.Panel[61] = 1;
				this.Train.Panel[64] = 1;
			} else if (this.SignalAPattern.ReleaseSpeed == 10.0 / 3.6) {
				this.Train.Panel[60] = 1;
				this.Train.Panel[61] = 1;
			} else if (this.SignalAPattern.TargetSpeed != double.MaxValue) {
				this.Train.Panel[60] = 1;
			}
			if (this.SignalBPattern.ReleaseSpeed == 15.0 / 3.6) {
				this.Train.Panel[62] = 1;
				this.Train.Panel[63] = 1;
				this.Train.Panel[64] = 1;
			} else if (this.SignalBPattern.ReleaseSpeed == 10.0 / 3.6) {
				this.Train.Panel[62] = 1;
				this.Train.Panel[63] = 1;
			} else if (this.SignalBPattern.TargetSpeed != double.MaxValue) {
				this.Train.Panel[62] = 1;
			}
			if (this.DivergencePattern.TargetSpeed != double.MaxValue) {
				this.Train.Panel[65] = 1;
			}
			if (this.CurvePattern.TargetSpeed != double.MaxValue) {
				this.Train.Panel[66] = 1;
			}
			if (this.SlopePattern.TargetSpeed != double.MaxValue) {
				this.Train.Panel[67] = 1;
			}
			if (this.TemporaryPattern.TargetSpeed != double.MaxValue) {
				this.Train.Panel[68] = 1;
			}
			if (this.IrekaePattern.TargetSpeed != double.MaxValue) {
				this.Train.Panel[69] = 1;
			}
			if (this.YuudouPattern.TargetSpeed != double.MaxValue) {
				this.Train.Panel[70] = 1;
			}
			if (this.SignalAPattern.UpcomingGradient != 0.0) {
				this.Train.Panel[71] = 1;
			}
			if (this.SignalBPattern.UpcomingGradient != 0.0) {
				this.Train.Panel[72] = 1;
			}
			// --- debug ---
			if (this.State == States.Normal | this.State == States.Pattern | this.State == States.Approaching | this.State == States.Emergency) {
				StringBuilder builder = new StringBuilder();
				this.SignalAPattern.AddToStringBuilder("A:", builder);
				this.SignalBPattern.AddToStringBuilder("B:", builder);
				this.DivergencePattern.AddToStringBuilder("分岐/D:", builder);
				this.TemporaryPattern.AddToStringBuilder("臨時/T:", builder);
				this.CurvePattern.AddToStringBuilder("曲線/C:", builder);
				this.SlopePattern.AddToStringBuilder("勾配/S:", builder);
				this.IrekaePattern.AddToStringBuilder("入替/I:", builder);
				this.YuudouPattern.AddToStringBuilder("誘導/Y:", builder);
				this.TrainPermanentPattern.AddToStringBuilder("M:", builder);
				if (builder.Length == 0) {
					data.DebugMessage = this.State.ToString();
				} else {
					data.DebugMessage = this.State.ToString() + " - " + builder.ToString();
				}
			}
		}
		
		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal override void KeyDown(VirtualKeys key) {
			switch (key) {
				case VirtualKeys.B1:
					// --- reset the system ---
					if (this.State == States.Emergency & this.Train.Handles.Reverser == 0 & this.Train.Handles.PowerNotch == 0 & this.Train.Handles.BrakeNotch == this.Train.Specs.BrakeNotches + 1) {
						this.State = States.Normal;
						this.Selector.Clear();
						if (this.SignalAPattern.SpeedPattern <= 0.0 & this.SignalAPattern.Distance <= 0.0) {
							this.SignalAPattern.Clear();
						}
						if (this.SignalBPattern.SpeedPattern <= 0.0 & this.SignalBPattern.Distance <= 0.0) {
							this.SignalBPattern.Clear();
						}
					}
					break;
				case VirtualKeys.F:
					// --- activate or deactivate the system ---
					if (this.State == States.Disabled) {
						this.State = States.Suppressed;
					} else {
						this.State = States.Disabled;
					}
					break;
			}
		}
		
		/// <summary>Is called when a beacon is passed.</summary>
		/// <param name="beacon">The beacon data.</param>
		internal override void SetBeacon(BeaconData beacon) {
			if (this.State != States.Disabled) {
				switch (beacon.Type) {
					case 0:
						// --- S long / Ps downslope ---
						if (this.CompatibilitySelector == 90 & beacon.Optional < -1) {
							this.SignalAPattern.SetGradient(0.001 * (double)beacon.Optional);
							this.SignalBPattern.SetGradient(0.001 * (double)beacon.Optional);
							this.CompatibilitySelector = 0;
							this.SwitchToPs();
						}
						break;
					case 1:
						// --- SN immediate stop / Ps clear ---
						if (this.State != States.Standby) {
							if (this.CompatibilitySelector == 95) {
								if (beacon.Signal.Aspect == 0) {
									this.SignalBPattern.Set15Kmph();
								} else {
									this.SignalBPattern.Clear();
								}
								this.CompatibilitySelector = 0;
							} else {
								if (beacon.Signal.Aspect == 0) {
									this.SignalAPattern.Set15Kmph();
								} else {
									this.SignalAPattern.Clear();
								}
							}
							this.IrekaePattern.Clear();
						}
						break;
					case 11:
						// --- Ps1 ---
						if (this.CompatibilitySelector == 0) {
							if (beacon.Signal.Aspect == 0) {
								this.SignalAPattern.SetPs1();
							} else {
								this.SignalAPattern.Clear();
							}
							this.CompatibilitySelector = 0;
						} else if (this.CompatibilitySelector == 95) {
							if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) {
								this.SignalBPattern.SetPs1();
							} else {
								this.SignalBPattern.Clear();
							}
							this.CompatibilitySelector = 0;
						}
						this.SwitchToPs();
						break;
					case 12:
						// --- Ps2 ---
						if (this.CompatibilitySelector == 0) {
							this.CompatibilitySelector = 108;
							this.CompatibilityDistanceAccumulator = 0.0;
						} else if (this.CompatibilitySelector == 95) {
							if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) {
								this.CompatibilitySelector = 109;
								this.CompatibilityDistanceAccumulator = 0.0;
							} else {
								this.SignalBPattern.Clear();
								this.CompatibilitySelector = 0;
							}
						} else if (this.CompatibilitySelector == 108) {
							if (beacon.Signal.Aspect == 0) {
								this.SignalAPattern.SetPs2();
								this.CompatibilitySelector = 0;
							} else {
								this.SignalAPattern.Clear();
							}
						} else if (this.CompatibilitySelector == 109) {
							if (beacon.Signal.Aspect == 0) {
								this.SignalBPattern.SetPs2();
								this.CompatibilitySelector = 0;
							} else {
								this.SignalBPattern.Clear();
							}
						}
						this.SwitchToPs();
						break;
					case 13:
						// --- Ps pattern elimination ---
						if (beacon.Signal.Aspect != 0 & beacon.Signal.Aspect <= 100) {
							this.SignalAPattern.Clear();
							this.SignalBPattern.Clear();
						}
						this.SwitchToPs();
						break;
					case 14:
						// --- Ps marker ---
						if (beacon.Optional == 90 | beacon.Optional == 95 | beacon.Optional == 108) {
							this.CompatibilitySelector = beacon.Optional;
							this.CompatibilityDistanceAccumulator = 0.0;
							this.SwitchToPs();
						}
						break;
					case 15:
						// --- Ps divergence speed limit ---
						if (this.CompatibilitySelector == 90) {
							if (beacon.Optional == 0) {
								this.DivergencePattern.Clear();
							} else if (beacon.Optional > 0) {
								this.DivergencePattern.SetUpcomingLimit(beacon.Optional / 3.6, true);
							}
							this.CompatibilitySelector = 0;
							this.SwitchToPs();
						}
						break;
					case 16:
						// --- Ps curve speed limit ---
						if (this.CompatibilitySelector == 90) {
							if (beacon.Optional == 0) {
								this.CurvePattern.Clear();
							} else if (beacon.Optional > 0) {
								this.CurvePattern.SetUpcomingLimit(beacon.Optional / 3.6, false);
							}
							this.CompatibilitySelector = 0;
							this.SwitchToPs();
						}
						break;
					case 17:
						// --- Ps slope speed limit ---
						if (this.CompatibilitySelector == 90) {
							if (beacon.Optional == 0) {
								this.SlopePattern.Clear();
							} else if (beacon.Optional > 0) {
								this.SlopePattern.SetUpcomingLimit(beacon.Optional / 3.6, false);
							}
							this.CompatibilitySelector = 0;
							this.SwitchToPs();
						}
						break;
					case 18:
						// --- Ps temporary speed limit ---
						if (this.CompatibilitySelector == 90) {
							if (beacon.Optional == 0) {
								this.TemporaryPattern.Clear();
							} else if (beacon.Optional > 0) {
								this.TemporaryPattern.SetUpcomingLimit(beacon.Optional / 3.6, false);
							}
							this.CompatibilitySelector = 0;
							this.SwitchToPs();
						}
						break;
					case 19:
						// --- Ps irekae speed limit ---
						if (this.CompatibilitySelector == 108) {
							if (beacon.Optional > 0) {
								this.IrekaePattern.SetImmediateLimit(beacon.Optional / 3.6);
							}
							this.CompatibilitySelector = 0;
							this.SwitchToPs();
						}
						break;
					case 20:
						// --- Ps yuudou speed limit ---
						if (this.CompatibilitySelector == 108) {
							if (beacon.Optional == 0) {
								this.YuudouPattern.SetImmediateLimit((15 + 10) / 3.6);
							}
							this.CompatibilitySelector = 0;
							this.SwitchToPs();
						}
						break;
					default:
						// --- frequency-based beacons ---
						int frequency = Train.GetFrequencyFromBeacon(beacon);
						this.Selector.Add(frequency);
						break;
				}
			}
		}
		
	}
}