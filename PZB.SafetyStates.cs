using OpenBveApi.Runtime;

namespace Plugin
{
    internal partial class PZB : Device
    {
        internal enum PZBProgramStates
        {
            /// <summary>Set this state when no processing or action is to be taken.</summary>
            None = 0,
            /// <summary>A signal has been passed and the warning horn is playing.</summary>
            SignalPassed = 1,
            /// <summary>The home signal braking curve is active.</summary>
            BrakeCurveActive = 2,
            /// <summary>The home signal braking curve has expired, and the brake curve may now be released.</summary>
            BrakeCurveExpired = 3,
            /// <summary>The speed has dropped below 10km/h during the home braking curve and is now limited to the low speed value.</summary>
            HomeBrakeCurveLowSpeed = 4,
            /// <summary>Failure to keep within the brake curve has resulted in an EB application.</summary>
            EBApplication = 5,
            /// <summary>The home penalty brakes are now releaseable.</summary>
            PenaltyReleasable = 6,
        }

        internal enum PZBBefehelStates
        {
            /// <summary>Set this state when no processing or action is to be taken.</summary>
            None = 0,
            /// <summary>A home signal inductor has been triggered.</summary>
            HomeStopPassed = 1,
            /// <summary>A home signal has been passed under authorised conditions.</summary>
            HomeStopPassedAuthorised = 2,
            /// <summary>A home signal has been passed and the EB has been applied.</summary>
            EBApplication = 3,
            /// <summary>The home stop penalty brakes are now releaseable.</summary>
            PenaltyReleasable = 4,
        }
    }
}
