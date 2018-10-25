namespace AdvancedImperativeInterop
{
    partial class MainForm
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
            this.txtLog_ = new System.Windows.Forms.RichTextBox();
            this.targets_ = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // txtLog_
            // 
            this.txtLog_.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.txtLog_.Location = new System.Drawing.Point(12, 320);
            this.txtLog_.Name = "txtLog_";
            this.txtLog_.ReadOnly = true;
            this.txtLog_.Size = new System.Drawing.Size(776, 118);
            this.txtLog_.TabIndex = 1;
            this.txtLog_.Text = "";
            // 
            // targets_
            // 
            this.targets_.FullRowSelect = true;
            this.targets_.Location = new System.Drawing.Point(12, 12);
            this.targets_.Name = "targets_";
            this.targets_.Size = new System.Drawing.Size(776, 302);
            this.targets_.TabIndex = 2;
            this.targets_.UseCompatibleStateImageBehavior = false;
            this.targets_.View = System.Windows.Forms.View.Details;
            this.targets_.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.TargetsMouseDoubleClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.targets_);
            this.Controls.Add(this.txtLog_);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "AdvancedImperativeInterop";
            this.Shown += new System.EventHandler(this.MainFormShown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtLog_;
        private System.Windows.Forms.ListView targets_;
    }
}

