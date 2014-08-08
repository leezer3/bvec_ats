using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenBveApi.Runtime;

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
        public static string debugmessage;
        private static AdvancedDriving mInst;
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
            
            
        }
        internal void Elapse(string[] debuginformation)
        {
            debuglabel.Text = debuginformation[0];
            
        }

        
    }
    
}
