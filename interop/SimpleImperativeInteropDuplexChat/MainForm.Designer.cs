namespace SimpleImperativeInteropDuplexChat
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
            this.txtMsg_ = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // txtLog_
            // 
            this.txtLog_.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.txtLog_.Location = new System.Drawing.Point(12, 12);
            this.txtLog_.Name = "txtLog_";
            this.txtLog_.ReadOnly = true;
            this.txtLog_.Size = new System.Drawing.Size(776, 365);
            this.txtLog_.TabIndex = 0;
            this.txtLog_.Text = "";
            // 
            // txtMsg_
            // 
            this.txtMsg_.Enabled = false;
            this.txtMsg_.Location = new System.Drawing.Point(12, 383);
            this.txtMsg_.Name = "txtMsg_";
            this.txtMsg_.Size = new System.Drawing.Size(776, 55);
            this.txtMsg_.TabIndex = 1;
            this.txtMsg_.Text = "";
            this.txtMsg_.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtMsgKeyDown);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtMsg_);
            this.Controls.Add(this.txtLog_);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtLog_;
        private System.Windows.Forms.RichTextBox txtMsg_;
    }
}

