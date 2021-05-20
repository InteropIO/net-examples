
namespace WindowsFormsChildAppsDemo
{
    partial class ChildForm2
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
            this.StartDateLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // StartDateLabel
            // 
            this.StartDateLabel.AutoSize = true;
            this.StartDateLabel.Location = new System.Drawing.Point(358, 196);
            this.StartDateLabel.Name = "StartDateLabel";
            this.StartDateLabel.Size = new System.Drawing.Size(92, 20);
            this.StartDateLabel.TabIndex = 0;
            this.StartDateLabel.Text = "Started on: ";
            // 
            // ChildForm2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.StartDateLabel);
            this.Name = "ChildForm2";
            this.Text = "ChildForm2";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label StartDateLabel;
    }
}