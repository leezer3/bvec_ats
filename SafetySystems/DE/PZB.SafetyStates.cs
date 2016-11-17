namespace Plugin
{
    internal partial class PZB
    {
        internal enum PZBProgramStates
        {
            /// <summary>Set this state when no processing or action is to be taken.</summary>
            None = 0,
            /// <summary>A signal has been passed and the warning horn is playing.</summary>
            SignalPassed = 1,
            /// <summary>The home signal braking curve is active.</summary>
            BrakeCurveActive = 2,
            /// <summary>The speed has dropped below 10km/h during the home braking curve and is now limited to the low speed value.</summary>
            HomeBrakeCurveLowSpeed = 3,
            /// <summary>Failure to keep within the brake curve has resulted in an EB application.</summary>
            EBApplication = 4,
            /// <summary>The penalty brakes are now releaseable.</summary>
            PenaltyReleasable = 5,
            /// <summary>The distant program is in suppression mode, and will be pushed back to active by any home inductor.</summary>
            Suppressed = 6,
        }

        internal enum PZBBefehlStates
        {
            /// <summary>Set this state when no processing or action is to be taken.</summary>
            None = 0,
            /// <summary>Befehl has been applied.</summary>
            Applied = 1,
            /// <summary>A home stop inductor has been passed.</summary>
            HomeStopPassed = 2,
            /// <summary>A home signal has been passed under authorised conditions.</summary>
            HomeStopPassedAuthorised = 3,
            /// <summary>A home signal has been passed and the EB has been applied.</summary>
            EBApplication = 4,
            /// <summary>The home stop penalty brakes are now releaseable.</summary>
            PenaltyReleasable = 5,
        }
    }
}
