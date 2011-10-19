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

        public MainForm()
        {
            InitializeComponent();

            dataTabs.Controls.Remove(debugTabPage);

            ConsoleMessage("Starting up");

            macros = new List<MacroGUI>();

            exit = false;
            debugLine = -1;
            saved = false;
        }

        private void ConsoleMessage(string msg)
        {
            consoleOutputList.Items.Insert(0, msg);
        }

        #region Fields

        private void codeBox_TextChanged(object sender, EventArgs e)
        {
            if (!startDebuggerToolStripMenuItem.Text.Equals("Continue"))
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
        }

        private void codeBox_MouseHover(object sender, EventArgs e)
        {
            if (startDebuggerToolStripMenuItem.Text.Equals("Continue"))
            {
                //We first need to see if we are over a piece of text (perferrably a variable)
                Point mp = codeBox.PointToClient(MousePosition);
                int tpos = codeBox.GetCharIndexFromPosition(mp);
                Point tp = codeBox.GetPositionFromCharIndex(tpos);
                if (Math.Abs(mp.X - tp.X) <= 10)
                {
                    if (Math.Abs(mp.Y - tp.Y) <= 10)
                    {
                        //Now get the line
                        int line = codeBox.GetLineFromCharIndex(tpos);
                        string sline = codeBox.Lines[line];

                        //Adjust the position to be relitive to the line
                        tpos -= codeBox.GetFirstCharIndexFromLine(line);

                        string[] messages;
                        Instruction op = Instruction.GetInstruction(sline, out messages);
                        if (op != null)
                        {
                            string[] args = op.GetArguments();

                            if (args != null && args.Length > 0)
                            {
                                //Figure out what variable we are over
                                int i = 0;
                                int index = 0;
                                bool found = false;
                                if (op.GotoLabel == null)
                                {
                                    index = sline.IndexOf(args[i]);
                                    if (index > tpos)
                                    {
                                        //Not found, end
                                        return;
                                    }
                                    else if (tpos >= index && tpos <= (index + args[i].Length))
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        i++;
                                    }
                                }
                                if (!found)
                                {
                                    for (; i < args.Length; i++)
                                    {
                                        index = sline.IndexOf(' ' + args[i], index + 1);
                                        if (index == -1)
                                        {
                                            //Nothing
                                            break;
                                        }
                                        if (index > tpos)
                                        {
                                            //Not found, end
                                            return;
                                        }
                                        else if (tpos >= index && tpos <= (index + args[i].Length))
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                }
                                //Display the tool tip
                                if (found)
                                {
                                    foreach (VariableGUI gui in variableListBox.Items)
                                    {
                                        if (gui.var.Name.Equals(args[i]))
                                        {
                                            variableToolTip.Show(gui.var.Value.ToString(), codeBox, tp);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
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

        private void GotoLineAbs(int pos, int len)
        {
            codeBox.Select(pos, len);
            codeBox.Focus();
        }

        private int GotoLine(int line)
        {
            //Not a very efficient way to do it, but it prevents jumping to the first item in the text
            int pos = 0;
            for (int i = 0; i < line; i++)
            {
                pos += codeBox.Lines[i].Length + 1;
            }
            //Now jump to the line
            GotoLineAbs(pos, 0);
            return pos;
        }

        private void HighlighLine(int line, Color col)
        {
            int pos = GotoLine(line);

            //Reset the text formatting
            codeBox.Text = codeBox.Text;

            //Select the whole line
            GotoLineAbs(pos, codeBox.Lines[line].Length);

            //Set the color
            codeBox.SelectionBackColor = col;

            //Return to the line position
            GotoLineAbs(pos, 0);
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
            if (debugToolStripMenuItem.Enabled)
            {
                if (!startDebuggerToolStripMenuItem.Text.Equals("Continue"))
                {
                    compileToolStripMenuItem_Click(sender, e);

                    //TODO: How do we figure out if an error occured?

                    dataTabs.Controls.Add(debugTabPage);
                    debugTabPage.Focus();

                    startDebuggerToolStripMenuItem.Text = "Continue";
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
                            HighlighLine(debugLine, Color.Red);
                        }
                    }
                    else
                    {
                        stopDebuggerToolStripMenuItem_Click(sender, e);
                    }
                }
                else
                {
                    //TODO: Continue
                }
            }
        }

        //This is really just a glorified, and manual version of Macro.Run
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
                    if (mac.lastLine + 1 >= mac.opIndexes.Length)
                    {
                        //Just go to the next line
                        debugLine = mac.opIndexes[mac.lastLine++] + 1;
                    }
                    else
                    {
                        //We have another line we can go to
                        debugLine = mac.opIndexes[++mac.lastLine];
                    }
                    HighlighLine(debugLine, Color.Red);
                    if (mac.lastLine >= mac.opIndexes.Length)
                    {
                        debugLine = -1;
                    }
                }
            }
            else
            {
                MacroGUI mac = callStackListBox.Items[0] as MacroGUI;
                Instruction[] ops = mac.macro.GetInstructions();

                //We always want to be on the current debug line when we run
                HighlighLine(debugLine, Color.Red);

                //Invoke
                if (ops[mac.lastLine] is Macro && stepInto)
                {
                    //We want to cut off execution right away so we can jump to the macro

                    Macro macro = ops[mac.lastLine] as Macro;

                    //Find the MacroGUI
                    int debugMacro = -1;
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
                    HighlighLine(debugLine = mac.opIndexes[mac.lastLine], Color.Red);

                    //Wrap variables
                    mac.macro.WrapVars(opVars);
                }
                else
                {
                    string gotoLabel = null;
                    bool isGoto, increment = true;
                    int prevSize = 0;

                    if (isGoto = ops[mac.lastLine] is Instruction.GotoInstruction)
                    {
                        prevSize = opVars.Count;
                    }

                    //Invoke the instruction
                    ops[mac.lastLine].Run(opVars);
                    //TODO: Figure out how to handle break points within a macro

                    //Handle any gotos
                    if (isGoto && prevSize != opVars.Count)
                    {
                        gotoLabel = opVars[opVars.Count - 1].Name;
                        int jump = mac.macro.JumpIndex(gotoLabel);
                        increment = false;
                        if (jump == -1)
                        {
                            //We know how to cleanup a goto, we can ignore it
                            opVars.Remove(Variable.FindVar(gotoLabel, opVars));
                            debugLine = -1;
                            stepOverToolStripMenuItem_Click(sender, e); //It does exactly what we want, so why copy and paste?
                        }
                        else
                        {
                            prevSize = mac.lastLine;
                            mac.lastLine = jump;
                            HighlighLine(debugLine = mac.opIndexes[mac.lastLine], Color.Red);
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
                            HighlighLine(mac.opIndexes[mac.opIndexes.Length - 1] + 1, Color.Red); //Jump to the end of the macro
                        }
                        else
                        {
                            HighlighLine(debugLine = mac.opIndexes[mac.lastLine], Color.Red);
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

                stepIntoToolStripMenuItem.Enabled = false;
                stepOverToolStripMenuItem.Enabled = false;
                stopDebuggerToolStripMenuItem.Enabled = false;

                buildToolStripMenuItem.Enabled = true;

                //Reset the text formatting
                codeBox.Text = codeBox.Text;

                if (debugLine >= 0)
                {
                    GotoLine(debugLine);
                }
                else
                {
                    //Find main
                    foreach (MacroGUI mac in this.macros)
                    {
                        if (mac.macro.ToString().Equals("main", StringComparison.InvariantCultureIgnoreCase))
                        {
                            GotoLine(mac.opIndexes[mac.opIndexes.Length - 1] + 1);
                            break;
                        }
                    }
                }

                debugLine = -1;

                variableListBox.Items.Clear();
                callStackListBox.Items.Clear();

                //Do this last to prevent "you need to save"
                startDebuggerToolStripMenuItem.Text = "Start Debugger";
                codeBox.ReadOnly = false;
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
                    Instruction op = Instruction.GetInstruction(codeBox.Lines[i], out messages);
                    if (op != null && onMacro)
                    {
                        //Got the instruction
                        opLines.Add(i);
                        string label;
                        if ((label = mac.AddInstruction(op)) != null)
                        {
                            label = string.Format("Goto label already exists: {0}", label);
                            if (messages != null && messages.Length > 0)
                            {
                                List<string> m = new List<string>(messages);
                                m.Add(label);
                                messages = m.ToArray();
                            }
                            else
                            {
                                messages = new string[] { label };
                            }
                        }
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
