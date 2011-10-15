namespace LanguageDebugger
{
    partial class VariableEditor
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
            this.setButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.variableLabel = new System.Windows.Forms.Label();
            this.variableNameLabel = new System.Windows.Forms.Label();
            this.valueLabel = new System.Windows.Forms.Label();
            this.variableValueBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // setButton
            // 
            this.setButton.Location = new System.Drawing.Point(54, 51);
            this.setButton.Name = "setButton";
            this.setButton.Size = new System.Drawing.Size(75, 23);
            this.setButton.TabIndex = 0;
            this.setButton.Text = "Set";
            this.setButton.UseVisualStyleBackColor = true;
            this.setButton.Click += new System.EventHandler(this.setButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(154, 51);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cacnel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // variableLabel
            // 
            this.variableLabel.AutoSize = true;
            this.variableLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.variableLabel.Location = new System.Drawing.Point(12, 9);
            this.variableLabel.Name = "variableLabel";
            this.variableLabel.Size = new System.Drawing.Size(61, 13);
            this.variableLabel.TabIndex = 2;
            this.variableLabel.Text = "Variable: ";
            // 
            // variableNameLabel
            // 
            this.variableNameLabel.AutoSize = true;
            this.variableNameLabel.Location = new System.Drawing.Point(79, 9);
            this.variableNameLabel.Name = "variableNameLabel";
            this.variableNameLabel.Size = new System.Drawing.Size(35, 13);
            this.variableNameLabel.TabIndex = 3;
            this.variableNameLabel.Text = "label2";
            // 
            // valueLabel
            // 
            this.valueLabel.AutoSize = true;
            this.valueLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.valueLabel.Location = new System.Drawing.Point(26, 28);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(47, 13);
            this.valueLabel.TabIndex = 4;
            this.valueLabel.Text = "Value: ";
            // 
            // variableValueBox
            // 
            this.variableValueBox.Location = new System.Drawing.Point(79, 25);
            this.variableValueBox.Name = "variableValueBox";
            this.variableValueBox.Size = new System.Drawing.Size(203, 20);
            this.variableValueBox.TabIndex = 5;
            this.variableValueBox.TextChanged += new System.EventHandler(this.variableValueBox_TextChanged);
            // 
            // VariableEditor
            // 
            this.AcceptButton = this.setButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(294, 85);
            this.Controls.Add(this.variableValueBox);
            this.Controls.Add(this.valueLabel);
            this.Controls.Add(this.variableNameLabel);
            this.Controls.Add(this.variableLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.setButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VariableEditor";
            this.Text = "Edit Variable";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button setButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label variableLabel;
        private System.Windows.Forms.Label variableNameLabel;
        private System.Windows.Forms.Label valueLabel;
        private System.Windows.Forms.TextBox variableValueBox;
    }
}