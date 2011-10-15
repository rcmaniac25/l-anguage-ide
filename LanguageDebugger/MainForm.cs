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
    public partial class MainForm : Form
    {
        private class MacroGUI
        {
            public Macro macro;
            public int macroLine;
            public int[] opIndexes;
            public List<int> breakpoints;

            public int lastLine;

            public MacroGUI(Macro m, int line, int[] opIndexes)
            {
                this.macro = m;
                this.macroLine = line;

                this.opIndexes = opIndexes;

                this.breakpoints = new List<int>();
            }

            public override string ToString()
            {
                return macro.ToString();
            }
        }

        private class VariableGUI
        {
            public Variable var;

            public VariableGUI(Variable var)
            {
                this.var = var;
            }

            public override string ToString()
            {
                return string.Format("{0} = {{{1}}}", var.Name, var.Value);
            }
        }

        private List<MacroGUI> macros;
        private string file;
        private bool saved;
        private bool cancelProcess, exit;
        private int debugLine;
        private int debugMacro;

        public MainForm()
        {
            InitializeComponent();

            dataTabs.Controls.Remove(debugTabPage);

            ConsoleMessage("Starting up");

            macros = new List<MacroGUI>();

            exit = false;
            debugLine = -1;
            debugMacro = -1;
            saved = false;
        }

        private void ConsoleMessage(string msg)
        {
            consoleOutputList.Items.Insert(0, msg);
        }

        #region Fields

        private void codeBox_TextChanged(object sender, EventArgs e)
        {
            saved = false;

            int messageCount = consoleOutputList.Items.Count;
            consoleOutputList.BeginUpdate();
            try
            {
                cancelProcess = true; //Cancel any previous process
                compileToolStripMenuItem_Click(sender, e); //Could probably run on another thread
            }
            catch
            {
            }
            //Don't add any new console messages
            while (consoleOutputList.Items.Count > messageCount)
            {
                consoleOutputList.Items.RemoveAt(0);
            }
            consoleOutputList.EndUpdate();
        }

        private void callStackListBox_DoubleClick(object sender, EventArgs e)
        {
            MacroGUI mac = callStackListBox.SelectedItem as MacroGUI;
            GotoLine(mac.opIndexes[mac.lastLine]);
        }

        private void variableListBox_DoubleClick(object sender, EventArgs e)
        {
            VariableGUI vg = variableListBox.SelectedItem as VariableGUI;
            if (new VariableEditor(vg.var).ShowDialog() == DialogResult.OK)
            {
                //Really bad way of getting the ListBox to redraw but invalidate, update, and invalidate followed by update didn't do anything
                variableListBox.BeginUpdate();

                int index = variableListBox.Items.IndexOf(vg);
                variableListBox.Items.Remove(vg);
                variableListBox.Items.Insert(index, vg);

                variableListBox.SelectedIndex = index;

                variableListBox.EndUpdate();
            }
        }

        private void macroListBox_DoubleClick(object sender, EventArgs e)
        {
            MacroGUI mac = macroListBox.SelectedItem as MacroGUI;
            GotoLine(mac.macroLine);
        }

        private void GotoLine(int line)
        {
            //Not a very efficient way to do it, but it prevents jumping to the first item in the text
            int pos = 0;
            for (int i = 0; i < line; i++)
            {
                pos += codeBox.Lines[i].Length + 1;
            }
            //Now jump to the line
            codeBox.Select(pos, 0);
            codeBox.Focus();
        }

        #endregion

        #region Menu

        #region File

        private bool CheckForOldWork(object sender, EventArgs e)
        {
            foreach (string line in codeBox.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    ConsoleMessage("Old work still exists");
                    DialogResult res = MessageBox.Show("Would you like to save your current work?", "Unsaved work", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    if (res == DialogResult.Yes)
                    {
                        saveToolStripMenuItem_Click(sender, e);
                    }
                    else if (res == DialogResult.Cancel)
                    {
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        private void EnableWindow()
        {
            //Clear stuff
            codeBox.Clear();
            macros.Clear();
            macroListBox.Items.Clear();

            //Enable stuff
            codeBox.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            debugToolStripMenuItem.Enabled = true;
            buildToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConsoleMessage("");
            ConsoleMessage("Creating new code");

            if (!saved && !CheckForOldWork(sender, e))
            {
                if (debugLine != -1)
                {
                    stopDebuggerToolStripMenuItem_Click(sender, e);
                }
            }

            EnableWindow();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckForOldWork(sender, e))
            {
                if (debugLine != -1)
                {
                    stopDebuggerToolStripMenuItem_Click(sender, e);
                }

                ConsoleMessage("Open code");
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    EnableWindow();

                    file = openFileDialog1.FileName;
                    this.codeBox.LoadFile(file, RichTextBoxStreamType.PlainText);

                    saved = true;
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (file == null)
            {
                saveAsToolStripMenuItem_Click(sender, e);
            }
            else
            {
                saved = true;
                ConsoleMessage("Save code");
                this.codeBox.SaveFile(file, RichTextBoxStreamType.PlainText);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConsoleMessage("Save As code");
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                saved = true;
                file = saveFileDialog1.FileName;
                this.codeBox.SaveFile(file, RichTextBoxStreamType.PlainText);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!exit)
            {
                e.Cancel = true;
                exitToolStripMenuItem_Click(sender, e);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConsoleMessage("Exiting");
            if (!saved && CheckForOldWork(sender, e))
            {
                ConsoleMessage("Skip exit");
            }
            else
            {
                exit = true;
                this.Close();
            }
        }

        #endregion

        #region Debug

        private void startDebuggerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (debugToolStripMenuItem.Enabled && startDebuggerToolStripMenuItem.Enabled)
            {
                compileToolStripMenuItem_Click(sender, e);

                dataTabs.Controls.Add(debugTabPage);
                debugTabPage.Focus();

                startDebuggerToolStripMenuItem.Enabled = false;
                stepIntoToolStripMenuItem.Enabled = true;
                stepOverToolStripMenuItem.Enabled = true;
                stopDebuggerToolStripMenuItem.Enabled = true;

                buildToolStripMenuItem.Enabled = false;
                codeBox.ReadOnly = true;

                MacroGUI main = null;
                foreach (MacroGUI mg in macros)
                {
                    if (mg.ToString().StartsWith("main", StringComparison.InvariantCultureIgnoreCase))
                    {
                        main = mg;
                        debugMacro = macros.IndexOf(mg);
                        break;
                    }
                }

                if (main != null)
                {
                    ConsoleMessage("");
                    ConsoleMessage("Starting debugger");

                    //Setup main
                    debugLine = main.opIndexes.Length > 0 ? main.opIndexes[0] : -1;
                    callStackListBox.Items.Insert(0, main);

                    if (debugLine != -1)
                    {
                        //Make sure we are at the first line
                        main.lastLine = 0;
                        GotoLine(debugLine);
                    }
                }
                else
                {
                    stopDebuggerToolStripMenuItem_Click(sender, e);
                }
            }
        }

        private void debugStepping(object sender, EventArgs e, bool stepInto)
        {
            //Create the variable list
            List<Variable> opVars = new List<Variable>();
            foreach (VariableGUI vg in variableListBox.Items)
            {
                opVars.Add(vg.var);
            }

            if (debugLine == -1)
            {
                MacroGUI omac = callStackListBox.Items[0] as MacroGUI;
                callStackListBox.Items.RemoveAt(0);
                if (callStackListBox.Items.Count == 0)
                {
                    //No more code
                    stopDebuggerToolStripMenuItem_Click(sender, e);
                }
                else
                {
                    //Unwrap variables
                    omac.macro.Cleanup(opVars);

                    ReloadVars(opVars);

                    //Go to parent macro
                    MacroGUI mac = callStackListBox.Items[0] as MacroGUI;
                    debugMacro = macros.IndexOf(mac);
                    GotoLine(debugLine = mac.opIndexes[mac.lastLine++]);
                    if (mac.lastLine >= mac.opIndexes.Length)
                    {
                        debugLine = -1;
                    }
                }
            }
            else
            {
                MacroGUI mac = callStackListBox.Items[0] as MacroGUI;
                Operation[] ops = mac.macro.GetOperations();

                //We always want to be on the current debug line when we run
                GotoLine(debugLine);

                //Invoke
                if (ops[mac.lastLine] is Macro && stepInto)
                {
                    //We want to cut off execution right away so we can jump to the macro

                    Macro macro = ops[mac.lastLine] as Macro;

                    //Find the MacroGUI
                    debugMacro = -1;
                    for (int i = 0; i < macros.Count; i++)
                    {
                        if (macros[i].macro.Equals(macro))
                        {
                            debugMacro = i;
                            break;
                        }
                    }
                    if (debugMacro == -1)
                    {
                        throw new InvalidOperationException("Macro not found");
                    }

                    //Get the macro, push it onto the call stack, reset it's position, then go to the line
                    mac = macros[debugMacro];
                    callStackListBox.Items.Insert(0, mac);
                    mac.lastLine = 0;
                    GotoLine(debugLine = mac.opIndexes[mac.lastLine]);

                    //Wrap variables
                    mac.macro.WrapVars(opVars);
                }
                else
                {
                    string gotoLabel = null;
                    bool isGoto, increment = true;
                    int prevSize = 0;

                    if (isGoto = ops[mac.lastLine] is Operation.GotoOperation)
                    {
                        prevSize = opVars.Count;
                    }

                    //Invoke the operation
                    ops[mac.lastLine].Run(opVars);
                    //Handle any gotos
                    if (isGoto && prevSize != opVars.Count)
                    {
                        gotoLabel = opVars[opVars.Count - 1].Name;
                        int jump = mac.macro.JumpIndex(gotoLabel);
                        increment = false;
                        if (jump == -1)
                        {
                            opVars.Remove(Variable.FindVar(gotoLabel, opVars));
                            debugLine = -1;
                            stepOverToolStripMenuItem_Click(sender, e); //It does exactly what we want, so why copy and paste?
                        }
                        else
                        {
                            prevSize = mac.lastLine;
                            mac.lastLine = jump;
                            GotoLine(debugLine = mac.opIndexes[mac.lastLine]);
                        }
                    }
                    else
                    {
                        prevSize = mac.lastLine;
                    }
                    if (increment)
                    {
                        //We need to manually go to the next line
                        mac.lastLine++;
                        if (mac.lastLine >= mac.opIndexes.Length)
                        {
                            debugLine = -1;
                            GotoLine(mac.opIndexes[mac.opIndexes.Length - 1] + 1); //Jump to the end of the macro
                        }
                        else
                        {
                            GotoLine(debugLine = mac.opIndexes[mac.lastLine]);
                        }
                    }
                    ops[prevSize].Cleanup(opVars);
                }

                ReloadVars(opVars);
            }
        }

        private void ReloadVars(List<Variable> opVars)
        {
            variableListBox.BeginUpdate();
            variableListBox.Items.Clear();
            foreach (Variable v in opVars)
            {
                variableListBox.Items.Add(new VariableGUI(v));
            }
            variableListBox.EndUpdate();
        }

        //These is really just a glorified, and manual version of Macro.Run
        private void stepIntoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (debugToolStripMenuItem.Enabled && stepIntoToolStripMenuItem.Enabled)
            {
                debugStepping(sender, e, true);
            }
        }

        private void stepOverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (debugToolStripMenuItem.Enabled && stepOverToolStripMenuItem.Enabled)
            {
                debugStepping(sender, e, false);
            }
        }

        private void stopDebuggerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (debugToolStripMenuItem.Enabled && stopDebuggerToolStripMenuItem.Enabled)
            {
                ConsoleMessage("Stopping debugger");

                dataTabs.Controls.Remove(debugTabPage);

                startDebuggerToolStripMenuItem.Enabled = true;
                stepIntoToolStripMenuItem.Enabled = false;
                stepOverToolStripMenuItem.Enabled = false;
                stopDebuggerToolStripMenuItem.Enabled = false;

                buildToolStripMenuItem.Enabled = true;
                codeBox.ReadOnly = false;

                debugLine = -1;
                debugMacro = -1;

                variableListBox.Items.Clear();
                callStackListBox.Items.Clear();
            }
        }

        #endregion

        #region Build

        private void compileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (buildToolStripMenuItem.Enabled && compileToolStripMenuItem.Enabled)
            {
                ConsoleMessage("");
                ConsoleMessage("Compiling started");

                macros.Clear();
                macroListBox.Items.Clear();

                cancelProcess = false;

                Macro.ClearMacros();

                string[] messages;

                bool onMacro = false;
                Macro mac = null;
                int mLine = -1;
                List<int> opLines = new List<int>();

                for (int i = 0; i < codeBox.Lines.Length && !cancelProcess; i++)
                {
                    Operation op = Operation.GetOperation(codeBox.Lines[i], out messages);
                    if (op != null && onMacro)
                    {
                        //Got the operation
                        opLines.Add(i);
                        mac.AddOperation(op);
                    }
                    else if (!string.IsNullOrWhiteSpace(codeBox.Lines[i]))
                    {
                        //Got a non-whitespace line
                        if (onMacro)
                        {
                            if (codeBox.Lines[i].Trim().StartsWith("**"))
                            {
                                //End of a macro
                                string res = mac.GlobalCommit();
                                if (res == null)
                                {
                                    macros.Add(new MacroGUI(mac, mLine, opLines.ToArray()));
                                }
                                else
                                {
                                    if (messages != null && messages.Length > 0)
                                    {
                                        List<string> m = new List<string>(messages);
                                        m.Add(res);
                                        messages = m.ToArray();
                                    }
                                    else
                                    {
                                        messages = new string[] { res };
                                    }
                                }
                                opLines.Clear();
                                mac = null;
                                onMacro = false;
                                //Remove unknown command message
                                if (messages != null && messages.Length > 0)
                                {
                                    List<string> m = new List<string>(messages);
                                    for (int c = 0; c < m.Count; c++)
                                    {
                                        if (m[c].StartsWith("Unknown command \"**"))
                                        {
                                            m.RemoveAt(c);
                                            c--;
                                        }
                                    }
                                    messages = m.ToArray();
                                }
                            }
                        }
                        else if (codeBox.Lines[i].TrimStart().StartsWith("**"))
                        {
                            //Start of a macro
                            mac = new Macro(codeBox.Lines[i].TrimStart().Substring(2));
                            onMacro = true;
                            mLine = i;
                            //Remove unknown command message
                            if (messages != null && messages.Length > 0)
                            {
                                List<string> m = new List<string>(messages);
                                for (int c = 0; c < m.Count; c++)
                                {
                                    if (m[c].StartsWith("Unknown command \"**"))
                                    {
                                        m.RemoveAt(c);
                                        c--;
                                    }
                                }
                                messages = m.ToArray();
                            }
                        }
                    }
                    //Any messages
                    if (messages != null && messages.Length > 0)
                    {
                        foreach (string msg in messages)
                        {
                            ConsoleMessage(string.Format("Line {0}: {1}", i + 1, msg));
                        }
                    }
                }

                if (cancelProcess)
                {
                    ConsoleMessage("Compiling canceled");
                    macros.Clear();
                }
                else
                {
                    ConsoleMessage("Compiling complete");
                    bool gotMain = false;
                    if (macros.Count > 0)
                    {
                        macroListBox.BeginUpdate();
                        foreach (MacroGUI mg in macros)
                        {
                            if (mg.ToString().StartsWith("main", StringComparison.InvariantCultureIgnoreCase))
                            {
                                gotMain = true;
                            }
                            macroListBox.Items.Add(mg);
                        }
                        macroListBox.EndUpdate();
                    }
                    if (!gotMain)
                    {
                        ConsoleMessage("No \"main\" function found");
                    }
                }
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO (remeber, if a shortcut is used to check if enabled. Same with it's menu too)
        }

        #endregion

        #endregion
    }
}
