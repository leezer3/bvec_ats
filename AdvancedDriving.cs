using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Plugin
{
    public partial class AdvancedDriving : Form
    {
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

            steambox.Visible = true;
            
            mask = new AdvancedDrivingMask();
            mask.Show();
            
        }

        internal void Elapse(string[] debuginformation, int tractiontype)
        {
            debuglabel.Text = debuginformation[0];
            //Only attempt to display steam related debug information if this is a steam locomotive
            if (tractiontype == 0)
            {
                pressure.Text = debuginformation[1];
                genrate.Text = debuginformation[2];
                userate.Text = debuginformation[3];
                //If we're using more pressure than we're generating, change the usage rate text color to red
                if (Int32.Parse(debuginformation[3]) > Int32.Parse(debuginformation[2]))
                {
                    userate.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    userate.ForeColor = System.Drawing.Color.Black;
                }
                currentcutoff.Text = debuginformation[4];
                //If the current cutoff is greater than the optimum cutoff, set the text color to red
                if (double.Parse(debuginformation[4]) > double.Parse(debuginformation[5]))
                {
                    currentcutoff.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    currentcutoff.ForeColor = System.Drawing.Color.Black;
                }
                optimalcutoff.Text = debuginformation[5];
                firemass.Text = debuginformation[6];
                firetemp.Text = debuginformation[7];
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
