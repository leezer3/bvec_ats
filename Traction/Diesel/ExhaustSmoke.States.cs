namespace Plugin
{
    partial class DieselExhaust
    {
        internal enum SmokeType
        {
            /// <summary>No smoke is being generated</summary>
            None = 0,
            /// <summary>Medium white- Engine is being cranked</summary>
            MediumWhite = 1,
            /// <summary>Thick white- Engine is being cranked</summary>
            ThickWhite = 2,
            /// <summary>Thin black- Constant engine RPM</summary>
            ThinBlack = 3,
            /// <summary>Medium black- Engine RPM going up or down</summary>
            MediumBlack = 4,
            /// <summary>Thick black- Engine firing or turbo activating</summary>
            ThickBlack = 2,
        }
    }
}
