using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanguageDebugger.Elements
{
    public class Macro : Instruction
    {
        private static List<Macro> macros = new List<Macro>();

        //Macro variables
        private List<Instruction> instructions;
        private bool globalSave;
        private object[] format;
        private bool setVar;

        //Execution variables
        private string gotoLabel;
        private string[] args;
        private string[] wrappedArgs;
        private Variable[] replacedArgs;
        private bool wrappedVars;

        private Macro(Macro mac)
        {
            this.instructions = mac.instructions;
            this.globalSave = mac.globalSave;
            this.format = mac.format;
            this.setVar = mac.setVar;

            this.gotoLabel = mac.gotoLabel;
            this.args = new string[mac.args.Length];
            this.wrappedArgs = new string[mac.wrappedArgs.Length];
            this.replacedArgs = new Variable[mac.replacedArgs.Length];
            this.wrappedVars = mac.wrappedVars;
        }

        public Macro(string format)
        {
            this.instructions = new List<Instruction>();

            int argC = 0;
            StringBuilder bu = new StringBuilder(format.Trim());
            List<object> components = new List<object>();
            bool onArg = false;
            while (bu.Length > 0)
            {
                int i = bu.ToString().IndexOf('%');
                string sub = i == -1 ? bu.ToString() : bu.ToString().Substring(0, i);
                if (i == -1)
                {
                    bu.Clear();
                }
                else
                {
                    bu.Remove(0, i + 1);
                }
                if (sub.Length > 0)
                {
                    if (onArg)
                    {
                        components.Add(new Variable(sub));
                        argC++;
                    }
                    else
                    {
                        components.Add(sub);
                    }
                }
                onArg = !onArg;
            }
            this.format = components.ToArray();
            if (this.format.Length == 0)
            {
                if (string.IsNullOrWhiteSpace(format))
                {
                    throw new ArgumentException();
                }
                else
                {
                    this.format = new string[] { format.Trim() };
                }
            }

            this.args = new string[argC];
            this.wrappedArgs = new string[argC];
            this.replacedArgs = new Variable[argC];
            this.wrappedVars = false;
            //We don't want a macro saying "you have overlapping variables" when it's just the variable set...
            if (this.format.Length > 2 && this.format[1] is string && ((string)this.format[1]).IndexOf(" <- ") == 0)
            {
                this.setVar = true;
            }
        }

        public Macro(string format, IEnumerable<Instruction> ops)
            : this(format)
        {
            List<string> labels = new List<string>();
            foreach (Instruction i in ops)
            {
                if (i.GotoLabel != null)
                {
                    if (labels.Contains(i.GotoLabel))
                    {
                        throw new ArgumentException(string.Format("Duplicate goto labels: {0}", i.GotoLabel));
                    }
                    labels.Add(i.GotoLabel);
                }
            }
            this.instructions.AddRange(ops);
        }

        public string AddInstruction(Instruction op)
        {
            List<string> labels = new List<string>();
            foreach (Instruction i in this.instructions)
            {
                if (i.GotoLabel != null && i.GotoLabel.Equals(op.GotoLabel))
                {
                    return i.GotoLabel;
                }
            }
            this.instructions.Add(op);
            return null;
        }

        public Instruction[] GetInstructions()
        {
            return instructions.ToArray();
        }

        public override string[] GetArguments()
        {
            return this.args;
        }

        public string GlobalCommit()
        {
            if (!globalSave)
            {
                if (FindMacro(this.ToString()) == null)
                {
                    //We don't want macros that could override instructions
                    string[] msg;
                    if (Instruction.GetInstruction(this.ToString(), out msg) == null)
                    {
                        macros.Add(this);
                        globalSave = true;
                    }
                    else
                    {
                        string msgs = string.Empty;
                        if (msg != null && msg.Length > 0)
                        {
                            StringBuilder bu = new StringBuilder();
                            foreach (string s in msg)
                            {
                                bu.AppendFormat("{0} ", s);
                            }
                            msgs = bu.ToString();
                        }
                        return string.Format("Macro has same name as instruction: \"{0}\".{1}", this, msgs);
                    }
                }
                else
                {
                    return string.Format("Macro of the same name: \"{0}\", already exists.", this);
                }
            }
            return null;
        }

        public static void ClearMacros()
        {
            macros.Clear();
        }

        public static Macro FindMacro(string line)
        {
            foreach (Macro m in macros)
            {
                if (m.MatchFormat(line))
                {
                    if (m.setVar)
                    {
                        //We need to check to see what set-variable is used
                        int index = -1;
                        for (int i = 1; i < m.format.Length; i++)
                        {
                            if (m.format[i] is Variable)
                            {
                                if (((Variable)m.format[i]).Name.Equals(((Variable)m.format[0]).Name))
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }
                        //We only want to process variables that exist already within arguments (otherwise the macro could create the argument)
                        if (index >= 0)
                        {
                            //Using the macro's format (since we had to compare to it to see if it is the same macro), create a macro format for the passed in "line"
                            object[] format = BreakFormat(line, m.format);
                            bool continueSearch = false;
                            //Do the same loop as before and compare indexes
                            for (int i = 1; i < format.Length; i++)
                            {
                                if (format[i] is Variable)
                                {
                                    if (((Variable)format[i]).Name.Equals(((Variable)format[0]).Name))
                                    {
                                        //Continue looking, if not the same match
                                        continueSearch = index != i;
                                        break;
                                    }
                                }
                            }
                            if (continueSearch)
                            {
                                continue;
                            }
                        }
                    }
                    return new Macro(m);
                }
            }
            return null;
        }

        private bool MatchFormat(string line)
        {
            StringBuilder bu = new StringBuilder(line);
            int index = 0;
            int tI = 0;
            string rem = null;
            while (bu.Length > 0)
            {
                if (format[index] is Variable)
                {
                    if (++index != format.Length)
                    {
                        if ((tI = bu.ToString().IndexOf(format[index] as string)) <= 0)
                        {
                            return false;
                        }
                        bu.Remove(0, tI);
                    }
                    else
                    {
                        rem = bu.ToString();
                        bu.Clear();
                    }
                    continue;
                }
                else if ((tI = bu.ToString().IndexOf(format[index++] as string)) != 0)
                {
                    return false;
                }
                bu.Remove(0, (format[index - 1] as string).Length);
            }
            return index == format.Length && (rem == null || rem.IndexOf(' ') == -1);
        }

        private static object[] BreakFormat(string line, object[] baseFormat)
        {
            object[] format = new object[baseFormat.Length];

            StringBuilder bu = new StringBuilder(line);
            int index = 0;
            int tI = 0;
            while (bu.Length > 0)
            {
                string part;
                if (baseFormat[index] is Variable)
                {
                    if (++index != baseFormat.Length)
                    {
                        part = bu.ToString();
                        tI = part.IndexOf(baseFormat[index] as string);
                        format[index - 1] = new Variable(part.Substring(0, tI));
                        bu.Remove(0, tI);
                    }
                    else
                    {
                        format[index - 1] = new Variable(bu.ToString());
                        bu.Clear();
                    }
                    continue;
                }
                part = bu.ToString();
                tI = part.IndexOf(baseFormat[index++] as string);
                format[index - 1] = part.Substring(0, (baseFormat[index - 1] as string).Length);
                bu.Remove(0, (baseFormat[index - 1] as string).Length);
            }
            return format;
        }

        protected override void Parse(string[] components)
        {
            if (components.Length == format.Length)
            {
                wrappedVars = false;
                int pos = 0;
                for (int i = 0; i < format.Length; i++)
                {
                    if (format[i] is Variable)
                    {
                        wrappedArgs[pos] = null;
                        if (!components[i].Equals(((Variable)format[i]).Name))
                        {
                            if (i > 0 || !setVar)
                            {
                                wrappedVars = true;
                                wrappedArgs[pos] = ((Variable)format[i]).Name;
                            }
                        }
                        args[pos++] = components[i];
                    }
                }
                return;
            }
            throw new ArgumentException();
        }

        internal void WrapVars(List<Variable> opVars)
        {
            //Wrap variables
            if (wrappedVars)
            {
                for (int i = 0; i < wrappedArgs.Length; i++)
                {
                    string find;
                    if ((find = wrappedArgs[i]) != null)
                    {
                        Variable var = null;
                        //These need to be seperate loops because we don't know the order they could be in

                        //Find the variable to wrap
                        for (int k = 0; k < opVars.Count; k++)
                        {
                            if (opVars[k].Name.Equals(args[i]))
                            {
                                var = opVars[k];
                                opVars.RemoveAt(k);
                                break;
                            }
                        }
                        //Find any conflicting variables
                        for (int k = 0; k < opVars.Count; k++)
                        {
                            if (opVars[k].Name.Equals(find))
                            {
                                replacedArgs[i] = opVars[k];
                                opVars.RemoveAt(k);
                                break;
                            }
                        }

                        opVars.Add(new WrappedVariable(find, var));
                    }
                }
            }
        }

        public override void Run(List<Variable> opVars)
        {
            gotoLabel = null;

            WrapVars(opVars);

            bool isGoto;
            int prevSize = 0;
            for (int i = 0; i < this.instructions.Count; i++)
            {
                if (isGoto = this.instructions[i] is GotoInstruction)
                {
                    prevSize = opVars.Count;
                }
                this.instructions[i].Run(opVars);
                if (isGoto && prevSize != opVars.Count)
                {
                    string gl = opVars[opVars.Count - 1].Name;
                    int jump = JumpIndex(gl);
                    if (jump == -1)
                    {
                        //We don't want to cleanup. Instead we will do that when the macro is called
                        gotoLabel = gl;
                        break;
                    }
                    prevSize = i;
                    i = jump - 1;
                }
                else
                {
                    prevSize = i;
                }
                this.instructions[prevSize].Cleanup(opVars);
            }
        }

        public override void Cleanup(List<Variable> opVars)
        {
            if (gotoLabel != null)
            {
                opVars.Remove(Variable.FindVar(gotoLabel, opVars));
            }
            if (wrappedVars)
            {
                //Unwrap variables
                for (int i = 0; i < wrappedArgs.Length; i++)
                {
                    string find;
                    if ((find = wrappedArgs[i]) != null)
                    {
                        WrappedVariable var = null;
                        //These need to be seperate loops because we don't know the order they could be in

                        //Find the variable to wrap
                        for (int k = 0; k < opVars.Count; k++)
                        {
                            if (opVars[k].Name.Equals(find))
                            {
                                var = opVars[k] as WrappedVariable;
                                opVars.RemoveAt(k);
                                break;
                            }
                        }

                        //Add it as it was before
                        opVars.Add(var.GetVariable());

                        //Re-add conflicting variables
                        if (replacedArgs[i] != null)
                        {
                            opVars.Add(replacedArgs[i]);
                            replacedArgs[i] = null;
                        }
                    }
                }
            }
        }

        internal int JumpIndex(string name)
        {
            for (int i = 0; i < this.instructions.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(this.instructions[i].GotoLabel))
                {
                    if (this.instructions[i].GotoLabel.Equals(name))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public override bool Equals(object obj)
        {
            if (obj is Macro)
            {
                Macro m = (Macro)obj;
                if (m.setVar == this.setVar && m.globalSave == this.globalSave)
                {
                    if (m.format.Length == this.format.Length)
                    {
                        if (m.instructions.Count == this.instructions.Count)
                        {
                            if (m.format.SequenceEqual(this.format))
                            {
                                return m.instructions.SequenceEqual(this.instructions);
                            }
                        }
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            StringBuilder bu = new StringBuilder(base.ToString());
            foreach (object obj in format)
            {
                bu.Append(obj);
            }
            return bu.ToString();
        }
    }
}
