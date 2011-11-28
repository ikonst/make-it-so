namespace TestRunner
{
    partial class TestRunner
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
            this.ctrlSolutions = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // ctrlSolutions
            // 
            this.ctrlSolutions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ctrlSolutions.FullRowSelect = true;
            this.ctrlSolutions.Location = new System.Drawing.Point(12, 12);
            this.ctrlSolutions.Name = "ctrlSolutions";
            this.ctrlSolutions.ShowItemToolTips = true;
            this.ctrlSolutions.Size = new System.Drawing.Size(682, 227);
            this.ctrlSolutions.TabIndex = 1;
            this.ctrlSolutions.UseCompatibleStateImageBehavior = false;
            this.ctrlSolutions.View = System.Windows.Forms.View.List;
            // 
            // TestRunner
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(706, 251);
            this.Controls.Add(this.ctrlSolutions);
            this.Name = "TestRunner";
            this.Text = "TestRunner";
            this.Load += new System.EventHandler(this.TestRunner_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView ctrlSolutions;
    }
}

