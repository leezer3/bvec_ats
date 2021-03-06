﻿using System.Windows.Forms;


namespace Plugin
{
    public partial class AdvancedDrivingMask : Form
    {

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

        //AdvancedDriving AdvancedDriving;
        public AdvancedDrivingMask()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
        }

    }
}
