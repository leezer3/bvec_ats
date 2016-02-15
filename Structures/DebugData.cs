namespace Plugin
{
    public class AdvancedDrivingData
    {

        /* <para>1. Plugin debug message</para>
         * <para>2. Steam locomotive boiler pressure</para>
         * <para>3. Steam locomotive pressure generation rate</para>
         * <para>4. Steam locomotice pressure usage rate</para>
         * <para>5. Steam locomotive current cutoff</para>
         * <para>6. Steam locomotive optimal cutoff</para>
         * <para>7. Steam locomotive fire mass</para>
         * <para>8. Steam locomotive fire temperature</para>
         * <para>9. Steam locomotive injectors state</para>
         * <para>10. Steam locomotive blowers state</para>
         * <para>11. Steam locomotive boiler water levels</para>
         * <para>12. Steam locomotive tanks water levels</para>
         * <para>13. Steam locomotive automatic cutoff state</para>
         * <para>14. Train speed</para>
         * <para>15. Front pantograph state</para>
         * <para>16. Rear pantograph state</para>
         * <para>17. ACB/VCB state</para>
         * <para>18. Line Volts</para></summary> */
        public double TrainSpeed;
        public string DebugMessage;
        
        public SteamLocomotive SteamEngine = new SteamLocomotive();
        public ElectricLocomotive ElectricEngine = new ElectricLocomotive();
        public WesternDebug WesternEngine = new WesternDebug();
        public class SteamLocomotive
        {
            public int BoilerPressure;
            public int PressureGenerationRate;
            public int PressureUsageRate;
            public bool AutoCutoff;
            public int CurrentCutoff;
            public int OptimalCutoff;
            public int FireMass;
            public int FireTemperature;
            public bool Injectors;
            public bool Blowers;
            public string CylinderCocks;
            //Strings as they show both the current and the max levels...
            public string BoilerWaterLevel;
            public string TanksWaterLevel;
        }

        public class ElectricLocomotive
        {
            public string FrontPantographState;
            public string RearPantographState;
            public bool VCBState;
            public bool LineVolts;
        }

        public class WesternDebug
        {
            public string FrontEngineState;
            public string RearEngineState;
            public string CurrentRPM;
            public string Engine1Temperature;
            public string Engine2Temperature;
            public string TransmissionTemperature;
            public string TorqueConverterState;
            public string TurbochargerState;
        }
    }
}
