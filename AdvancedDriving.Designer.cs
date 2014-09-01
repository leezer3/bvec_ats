namespace Plugin
{
    partial class AdvancedDriving
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.debuglabel = new System.Windows.Forms.Label();
            this.steambox = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.userate = new System.Windows.Forms.Label();
            this.genrate = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.currentcutoff = new System.Windows.Forms.Label();
            this.optimalcutoff = new System.Windows.Forms.Label();
            this.steambox.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Current Debug Message:";
            // 
            // debuglabel
            // 
            this.debuglabel.AutoSize = true;
            this.debuglabel.Location = new System.Drawing.Point(16, 30);
            this.debuglabel.Name = "debuglabel";
            this.debuglabel.Size = new System.Drawing.Size(35, 13);
            this.debuglabel.TabIndex = 1;
            this.debuglabel.Text = "label2";
            // 
            // steambox
            // 
            this.steambox.Controls.Add(this.optimalcutoff);
            this.steambox.Controls.Add(this.currentcutoff);
            this.steambox.Controls.Add(this.label7);
            this.steambox.Controls.Add(this.label6);
            this.steambox.Controls.Add(this.label5);
            this.steambox.Controls.Add(this.label4);
            this.steambox.Controls.Add(this.label3);
            this.steambox.Controls.Add(this.userate);
            this.steambox.Controls.Add(this.genrate);
            this.steambox.Controls.Add(this.label2);
            this.steambox.Location = new System.Drawing.Point(16, 46);
            this.steambox.Name = "steambox";
            this.steambox.Size = new System.Drawing.Size(263, 198);
            this.steambox.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(3, 32);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Usage Rate:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(4, 19);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Generation Rate:";
            // 
            // userate
            // 
            this.userate.AutoSize = true;
            this.userate.Location = new System.Drawing.Point(114, 32);
            this.userate.Name = "userate";
            this.userate.Size = new System.Drawing.Size(35, 13);
            this.userate.TabIndex = 2;
            this.userate.Text = "label4";
            // 
            // genrate
            // 
            this.genrate.AutoSize = true;
            this.genrate.Location = new System.Drawing.Point(114, 19);
            this.genrate.Name = "genrate";
            this.genrate.Size = new System.Drawing.Size(35, 13);
            this.genrate.TabIndex = 1;
            this.genrate.Text = "label3";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(4, 4);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(208, 15);
            this.label2.TabIndex = 0;
            this.label2.Text = "Steam Generation and Usage Rates:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(4, 45);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 15);
            this.label5.TabIndex = 5;
            this.label5.Text = "Cutoff:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(3, 60);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(90, 13);
            this.label6.TabIndex = 6;
            this.label6.Text = "Current Cutoff:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(3, 73);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(91, 13);
            this.label7.TabIndex = 7;
            this.label7.Text = "Optimal Cutoff:";
            // 
            // currentcutoff
            // 
            this.currentcutoff.AutoSize = true;
            this.currentcutoff.Location = new System.Drawing.Point(117, 60);
            this.currentcutoff.Name = "currentcutoff";
            this.currentcutoff.Size = new System.Drawing.Size(35, 13);
            this.currentcutoff.TabIndex = 8;
            this.currentcutoff.Text = "label8";
            // 
            // optimalcutoff
            // 
            this.optimalcutoff.AutoSize = true;
            this.optimalcutoff.Location = new System.Drawing.Point(117, 73);
            this.optimalcutoff.Name = "optimalcutoff";
            this.optimalcutoff.Size = new System.Drawing.Size(35, 13);
            this.optimalcutoff.TabIndex = 9;
            this.optimalcutoff.Text = "label8";
            // 
            // AdvancedDriving
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(294, 271);
            this.Controls.Add(this.steambox);
            this.Controls.Add(this.debuglabel);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "AdvancedDriving";
            this.TransparencyKey = System.Drawing.SystemColors.Control;
            this.steambox.ResumeLayout(false);
            this.steambox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label debuglabel;
        private System.Windows.Forms.Panel steambox;
        private System.Windows.Forms.Label userate;
        private System.Windows.Forms.Label genrate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label optimalcutoff;
        private System.Windows.Forms.Label currentcutoff;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
    }
}