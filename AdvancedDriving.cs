using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Plugin
{
    public partial class AdvancedDriving : Form
    {
        private UserControl SteamPanel;
        private UserControl ElectricPanel;
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

        private AdvancedDriving()
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

        internal void Elapse(string[] debuginformation, int tractiontype)
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
                SteamControl.debuglabel.Text = debuginformation[0];
                SteamControl.trainspeed.Text = debuginformation[13];
                SteamControl.pressure.Text = debuginformation[1];
                SteamControl.genrate.Text = debuginformation[2];
                SteamControl.userate.Text = debuginformation[3];
                //If we're using more pressure than we're generating, change the usage rate text color to red
                if (Int32.Parse(debuginformation[3]) > Int32.Parse(debuginformation[2]))
                {
                    SteamControl.userate.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    SteamControl.userate.ForeColor = System.Drawing.Color.Black;
                }
                SteamControl.currentcutoff.Text = debuginformation[4];
                //If the current cutoff is greater than the optimum cutoff, set the text color to red
                if (double.Parse(debuginformation[4]) > double.Parse(debuginformation[5]))
                {
                    SteamControl.currentcutoff.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    SteamControl.currentcutoff.ForeColor = System.Drawing.Color.Black;
                }
                SteamControl.optimalcutoff.Text = debuginformation[5];
                SteamControl.firemass.Text = debuginformation[6];
                SteamControl.firetemp.Text = debuginformation[7];
                SteamControl.injectors.Text = debuginformation[8];
                SteamControl.blowers.Text = debuginformation[9];
                SteamControl.boilerlevel.Text = debuginformation[10];
                SteamControl.fuellevel.Text = debuginformation[11];
                SteamControl.automatic.Text = debuginformation[12];
                SteamControl.cylindercocks.Text = debuginformation[14];
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
                ElectricControl.debuglabel.Text = debuginformation[0];
                ElectricControl.trainspeed.Text = debuginformation[13];
                ElectricControl.frontpanto.Text = debuginformation[14];
                ElectricControl.rearpanto.Text = debuginformation[15];
                ElectricControl.vcb.Text = debuginformation[16];
                ElectricControl.linecurrent.Text = debuginformation[17];
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
