using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenBveApi.Runtime;
using Microsoft.Win32;

namespace Plugin
{
    public partial class AdvancedDriving : Form
    {

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
                int WS_EX_TOPMOST = 0x00000008;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST;
                return cp;
            }
        }

        public static string debugmessage;
        private static AdvancedDriving mInst;
        AdvancedDrivingMask mask;
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

        internal void Elapse(string[] debuginformation)
        {
            debuglabel.Text = debuginformation[0];
            
        }

        void AdvancedDriving_FormClosed(object sender, FormClosedEventArgs e)
        {
            mask.Close();
            Point location = Location;
            string initLocation = string.Join(",", location.X, location.Y);
            
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
