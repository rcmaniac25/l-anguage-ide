using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LanguageDebugger.Elements;

namespace LanguageDebugger
{
    public partial class VariableEditor : Form
    {
        private Variable var;

        public VariableEditor(Variable var)
        {
            InitializeComponent();

            this.var = var;
            this.variableNameLabel.Text = var.Name;
            this.variableValueBox.Text = var.Value.ToString();
        }

        private void setButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            ulong ul;
            if (ulong.TryParse(this.variableValueBox.Text, out ul))
            {
                this.var.Value = ul;
            }
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void variableValueBox_TextChanged(object sender, EventArgs e)
        {
            ulong ul;
            if (ulong.TryParse(this.variableValueBox.Text, out ul))
            {
                this.variableValueBox.BackColor = SystemColors.Window;
            }
            else
            {
                this.variableValueBox.BackColor = Color.LightCoral;
            }
        }
    }
}
