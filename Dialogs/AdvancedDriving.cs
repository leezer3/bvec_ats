using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Plugin
{
    public partial class AdvancedDriving : Form
    {
        internal SteamControl SteamPanel;
        internal ElectricControl ElectricPanel;
        internal PZBControl PZBPanel;
        internal WesternControl WesternPanel;

        //The Advanced Driving form provides an independant debug window
        //Intended to show things such as the current steam production rate versus usage (WIP)
        protected override void OnClosed(EventArgs e)
        {
            tractionmanager.debugwindowshowing = false;
            mInst = null;
            base.OnClosed(e);   // Always call the base of OnClose !
        }

        //FOCUS ME NOT???
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                //make sure Top Most property on form is set to false
                //otherwise this doesn't work
                const int WS_EX_TOPMOST = 0x00000008;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST;
                return cp;
            }
        }

        public static string debugmessage;
        private static AdvancedDriving mInst;
        readonly AdvancedDrivingMask mask;
        // Create a public static property that returns the state of the instance
        public static AdvancedDriving CheckInst
        {
            get
            {
                return mInst;
            }
        }

        // Create a public static property that will create an instance of the form and return it
        public static AdvancedDriving CreateInst
        {
            get
            {
                if (mInst == null)
                    mInst = new AdvancedDriving();
                return mInst;
            }
        }

        public AdvancedDriving()
        {
            //Window to show advanced driving debug information
            InitializeComponent();
            this.LocationChanged += new EventHandler(AdvancedDriving_LocationChanged);
            this.SizeChanged += new EventHandler(AdvancedDriving_SizeChanged);
            this.FormClosed += new FormClosedEventHandler(AdvancedDriving_FormClosed);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            mask = new AdvancedDrivingMask();
            mask.Show();

        }

        internal void Elapse(string[] debuginformation, int tractiontype, AdvancedDrivingData DebugData)
        {

            //Only attempt to display steam related debug information if this is a steam locomotive
            if (tractiontype == 0)
            {
                if (SteamPanel == null)
                {
                    SteamPanel = new SteamControl();
                    Controls.Add(SteamPanel);
                    SteamPanel.Location = new Point(5,5);
                    mask.Size = this.Size;
                }
                SteamPanel.debuglabel.Text = debuginformation[0];
                SteamPanel.trainspeed.Text = debuginformation[13];

                SteamPanel.pressure.Text = Convert.ToString(DebugData.SteamEngine.BoilerPressure);
                SteamPanel.genrate.Text = Convert.ToString(DebugData.SteamEngine.PressureGenerationRate);
                SteamPanel.userate.Text = Convert.ToString(DebugData.SteamEngine.PressureUsageRate);
                //If we're using more pressure than we're generating, change the usage rate text color to red
                if (DebugData.SteamEngine.PressureUsageRate > DebugData.SteamEngine.PressureGenerationRate)
                {
                    SteamPanel.userate.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    SteamPanel.userate.ForeColor = System.Drawing.Color.Black;
                }
                SteamPanel.currentcutoff.Text = Convert.ToString(DebugData.SteamEngine.CurrentCutoff);
                //If the current cutoff is greater than the optimum cutoff, set the text color to red
                if (DebugData.SteamEngine.CurrentCutoff > DebugData.SteamEngine.OptimalCutoff)
                {
                    SteamPanel.currentcutoff.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    SteamPanel.currentcutoff.ForeColor = System.Drawing.Color.Black;
                }
                SteamPanel.optimalcutoff.Text = Convert.ToString(DebugData.SteamEngine.OptimalCutoff);
                SteamPanel.firemass.Text = Convert.ToString(DebugData.SteamEngine.FireMass);
                SteamPanel.firetemp.Text = Convert.ToString(DebugData.SteamEngine.FireTemperature);
                SteamPanel.injectors.Text = Convert.ToString(DebugData.SteamEngine.Injectors);
                SteamPanel.blowers.Text = Convert.ToString(DebugData.SteamEngine.Blowers);
                SteamPanel.boilerlevel.Text = DebugData.SteamEngine.BoilerWaterLevel;
                SteamPanel.fuellevel.Text = DebugData.SteamEngine.TanksWaterLevel;
                SteamPanel.automatic.Text = Convert.ToString(DebugData.SteamEngine.AutoCutoff);
                SteamPanel.cylindercocks.Text = DebugData.SteamEngine.CylinderCocks;
            }
            else if (tractiontype == 2)
            {
                if (ElectricPanel == null)
                {
                    ElectricPanel = new ElectricControl();
                    Controls.Add(ElectricPanel);
                    ElectricPanel.Location = new Point(5,5);
                    mask.Size = this.Size;
                }
                ElectricPanel.debuglabel.Text = debuginformation[0];
                ElectricPanel.trainspeed.Text = debuginformation[13];
                ElectricPanel.frontpanto.Text = debuginformation[14];
                ElectricPanel.rearpanto.Text = debuginformation[15];
                ElectricPanel.vcb.Text = debuginformation[16];
                ElectricPanel.linecurrent.Text = debuginformation[17];
            }
            else if (tractiontype == 3)
            {
                if (WesternPanel == null)
                {
                    WesternPanel = new WesternControl();
                    Controls.Add(WesternPanel);
                    WesternPanel.Location = new Point(5, 5);
                    mask.Size = this.Size;
                }
                WesternPanel.debuglabel.Text = debuginformation[0];
                WesternPanel.RPM.Text = DebugData.WesternEngine.CurrentRPM;
                WesternPanel.Engine1Status.Text = DebugData.WesternEngine.RearEngineState;
                WesternPanel.Engine2Status.Text = DebugData.WesternEngine.FrontEngineState;

            }
            else
            {
                if (PZBPanel == null)
                {
                    PZBPanel = new PZBControl();
                    Controls.Add(PZBPanel);
                    PZBPanel.Location = new Point(5, 5);
                    mask.Size = this.Size;

                }
                PZBPanel.debuglabel.Text = debuginformation[0];
                PZBPanel.trainspeed.Text = debuginformation[13];

                //Base System Information
                PZBPanel.AwaitingAcknowledgement.Text = debuginformation[26];
                PZBPanel.SwitchMode.Text = debuginformation[23];
                PZBPanel.EnforcedSpeed.Text = debuginformation[21];

                //Distant Program Information
                PZBPanel.ActiveDistantBrakeCurves.Text = debuginformation[25];
                PZBPanel.NewestDistantSpeed.Text = debuginformation[27];
                PZBPanel.DistantInductorDistance.Text = debuginformation[28];

                //Home Program Information
                PZBPanel.ActiveHomeBrakeCurves.Text = debuginformation[29];
                PZBPanel.NewestHomeSpeed.Text = debuginformation[24];
                PZBPanel.HomeInductorDistance.Text = debuginformation[22];

                //Befehl
                PZBPanel.PZBBefehl.Text = debuginformation[20];
            }
        }

        void AdvancedDriving_FormClosed(object sender, FormClosedEventArgs e)
        {
            mask.Close();
            //Write Out Location
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\BVEC_ATS", true))
            {
                if (key != null)
                {
                    key.SetValue("Left", this.Left);
                    key.SetValue("Top", this.Top);
                }
                else
                {
                    using (var key2 = Registry.CurrentUser.CreateSubKey(@"Software\BVEC_ATS"))
                    {
                        key2.SetValue("Left", this.Left);
                        key2.SetValue("Top", this.Top);
                    }
                }
            }

        }

        void AdvancedDriving_SizeChanged(object sender, EventArgs e)
        {
            mask.Size = this.Size;
        }

        void AdvancedDriving_LocationChanged(object sender, EventArgs e)
        {
            mask.Location = this.Location;
        }

    }

}
