using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin
{
	enum PowerGapBehaviour
	{
		/// <summary>The power gap has no effect on the train</summary>
		NoEffect = 0,
		/// <summary>The effective throttle notch is reduced proportionally by the ratio of available pickup points</summary>
		ProportionalReduction = 1,
		/// <summary>Power is cutoff if any pickup points are in the power gap</summary>
		InactiveAny = 2,
		/// <summary>Power is cutoff if all of the pickup points are in the power gap</summary>
		InactiveAll = 3
	}
}
