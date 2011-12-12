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
            this.ctrlSolutions = new ColoredCheckedListBox();
            this.cmdSelectAll = new System.Windows.Forms.Button();
            this.cmdUnselectAll = new System.Windows.Forms.Button();
            this.cmdRunTests = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ctrlSolutions
            // 
            this.ctrlSolutions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ctrlSolutions.CheckOnClick = true;
            this.ctrlSolutions.FormattingEnabled = true;
            this.ctrlSolutions.Location = new System.Drawing.Point(12, 12);
            this.ctrlSolutions.Name = "ctrlSolutions";
            this.ctrlSolutions.Size = new System.Drawing.Size(696, 259);
            this.ctrlSolutions.Sorted = true;
            this.ctrlSolutions.TabIndex = 0;
            // 
            // cmdSelectAll
            // 
            this.cmdSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdSelectAll.Location = new System.Drawing.Point(12, 276);
            this.cmdSelectAll.Name = "cmdSelectAll";
            this.cmdSelectAll.Size = new System.Drawing.Size(97, 29);
            this.cmdSelectAll.TabIndex = 1;
            this.cmdSelectAll.Text = "Select all";
            this.cmdSelectAll.UseVisualStyleBackColor = true;
            this.cmdSelectAll.Click += new System.EventHandler(this.cmdSelectAll_Click);
            // 
            // cmdUnselectAll
            // 
            this.cmdUnselectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmdUnselectAll.Location = new System.Drawing.Point(115, 277);
            this.cmdUnselectAll.Name = "cmdUnselectAll";
            this.cmdUnselectAll.Size = new System.Drawing.Size(97, 29);
            this.cmdUnselectAll.TabIndex = 2;
            this.cmdUnselectAll.Text = "Unselect all";
            this.cmdUnselectAll.UseVisualStyleBackColor = true;
            this.cmdUnselectAll.Click += new System.EventHandler(this.cmdUnselectAll_Click);
            // 
            // cmdRunTests
            // 
            this.cmdRunTests.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdRunTests.Location = new System.Drawing.Point(611, 277);
            this.cmdRunTests.Name = "cmdRunTests";
            this.cmdRunTests.Size = new System.Drawing.Size(97, 29);
            this.cmdRunTests.TabIndex = 3;
            this.cmdRunTests.Text = "Run tests";
            this.cmdRunTests.UseVisualStyleBackColor = true;
            this.cmdRunTests.Click += new System.EventHandler(this.cmdRunTests_Click);
            // 
            // TestRunner
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(720, 313);
            this.Controls.Add(this.cmdRunTests);
            this.Controls.Add(this.cmdUnselectAll);
            this.Controls.Add(this.cmdSelectAll);
            this.Controls.Add(this.ctrlSolutions);
            this.Name = "TestRunner";
            this.Text = "TestRunner";
            this.Load += new System.EventHandler(this.TestRunner_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private ColoredCheckedListBox ctrlSolutions;
        private System.Windows.Forms.Button cmdSelectAll;
        private System.Windows.Forms.Button cmdUnselectAll;
        private System.Windows.Forms.Button cmdRunTests;

    }
}

