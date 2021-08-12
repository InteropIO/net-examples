
namespace WindowsFormsChildAppsDemo
{
    partial class ChildForm
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
            this.btnRndColor = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnRndColor
            // 
            this.btnRndColor.Location = new System.Drawing.Point(292, 141);
            this.btnRndColor.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRndColor.Name = "btnRndColor";
            this.btnRndColor.Size = new System.Drawing.Size(152, 46);
            this.btnRndColor.TabIndex = 0;
            this.btnRndColor.Text = "Fill the background with Random color";
            this.btnRndColor.UseVisualStyleBackColor = true;
            this.btnRndColor.Click += new System.EventHandler(this.BtnRndColorClick);
            // 
            // ChildForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(711, 360);
            this.Controls.Add(this.btnRndColor);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "ChildForm";
            this.Text = "ChildForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnRndColor;
    }
}