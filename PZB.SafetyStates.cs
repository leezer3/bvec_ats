using OpenBveApi.Runtime;

namespace Plugin
{
    internal partial class PZB : Device
    {
        internal enum SafetyStates
        {
            /// <summary>Set this state when no processing or action is to be taken.</summary>
            None = 0,
            /// <summary>A distant signal has been passed and the warning horn is playing.</summary>
            DistantPassed = 1,
            /// <summary>The distant signal braking curve is active.</summary>
            DistantBrakeCurveActive = 2,
            /// <summary>The distant signal braking curve has expired, and the brake curve may now be released.</summary>
            DistantBrakeCurveExpired = 3,
            /// <summary>The speed has dropped below 10km/h during the distant braking curve and is now limited to the low speed value.</summary>
            DistantBrakeCurveLowSpeed = 4,
            /// <summary>A home signal has been passed and the warning horn is playing.</summary>
            HomePassed = 5,
            /// <summary>The home signal braking curve is active.</summary>
            HomeBrakeCurveActive = 6,
            /// <summary>The home signal braking curve has expired, and the brake curve may now be released.</summary>
            HomeBrakeCurveExpired = 7,
            /// <summary>The speed has dropped below 10km/h during the home braking curve and is now limited to the low speed value.</summary>
            HomeBrakeCurveLowSpeed = 8,
            /// <summary>Failure to keep within the brake curve has resulted in an EB application.</summary>
            HomeBrakeCurveEB = 9,
            /// <summary>Failure to acknowledge the distant signal warning horn has resulted in an EB application.</summary>
            DistantEBApplication = 10,
            /// <summary>A home signal inductor has been .</summary>
            HomeStopPassed = 11,
            /// <summary>A home signal has been passed under authorised conditions.</summary>
            HomeStopPassedAuthorised = 12,
            /// <summary>A home signal has been passed and the EB has been applied.</summary>
            HomeStopEBApplication = 13,
            /// <summary>The train is currently exceeding a permenant speed restriction.</summary>
            SpeedRestrictionAcknowledgement = 14,
            /// <summary>The speed restriction brake curve is active.</summary>
            SpeedRestrictionBrakeCurve = 15,
            /// <summary>The train brakes have been applied due to overspeed.</summary>
            SpeedRestrictionBrake = 16,
        }
    }
}
