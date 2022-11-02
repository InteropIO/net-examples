
namespace WindowsFormsDemo
{
    partial class Form1
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
            this.StateBox = new System.Windows.Forms.TextBox();
            this.btnTaskMgr = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // StateBox
            // 
            this.StateBox.Location = new System.Drawing.Point(113, 122);
            this.StateBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.StateBox.Name = "StateBox";
            this.StateBox.Size = new System.Drawing.Size(245, 20);
            this.StateBox.TabIndex = 0;
            this.StateBox.Text = "This text will be saved and restored";
            // 
            // btnTaskMgr
            // 
            this.btnTaskMgr.Location = new System.Drawing.Point(144, 38);
            this.btnTaskMgr.Name = "btnTaskMgr";
            this.btnTaskMgr.Size = new System.Drawing.Size(75, 23);
            this.btnTaskMgr.TabIndex = 1;
            this.btnTaskMgr.Text = "TaskMgr";
            this.btnTaskMgr.UseVisualStyleBackColor = true;
            this.btnTaskMgr.Click += new System.EventHandler(this.btnTaskMgr_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(533, 292);
            this.Controls.Add(this.btnTaskMgr);
            this.Controls.Add(this.StateBox);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox StateBox;
        private System.Windows.Forms.Button btnTaskMgr;
    }
}

