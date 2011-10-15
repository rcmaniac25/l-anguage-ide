using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanguageDebugger.Elements
{
    public class Macro : Operation
    {
        private static List<Macro> macros = new List<Macro>();

        //Macro variables
        private List<Operation> operations;
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
            this.operations = mac.operations;
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
            this.operations = new List<Operation>();

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

        public Macro(string format, IEnumerable<Operation> ops)
            : this(format)
        {
            this.operations.AddRange(ops);
        }

        public void AddOperation(Operation op)
        {
            this.operations.Add(op);
        }

        public Operation[] GetOperations()
        {
            return operations.ToArray();
        }

        public string GlobalCommit()
        {
            if (!globalSave)
            {
                if (FindMacro(this.ToString()) == null)
                {
                    macros.Add(this);
                    globalSave = true;
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
                    return new Macro(m);
                }
            }
            return null;
        }

        public bool MatchFormat(string line)
        {
            StringBuilder bu = new StringBuilder(line);
            int index = 0;
            int tI = 0;
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
            return index == format.Length;
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
            Run(opVars, 0);
        }

        //Need to think about this, by starting at a different point we could mess up something
        internal void Run(List<Variable> opVars, int start)
        {
            gotoLabel = null;

            WrapVars(opVars);

            bool isGoto;
            int prevSize = 0;
            for (int i = start; i < this.operations.Count; i++)
            {
                if (isGoto = this.operations[i] is GotoOperation)
                {
                    prevSize = opVars.Count;
                }
                this.operations[i].Run(opVars);
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
                this.operations[prevSize].Cleanup(opVars);
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
            for (int i = 0; i < this.operations.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(this.operations[i].GotoLabel))
                {
                    if (this.operations[i].GotoLabel.Equals(name))
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
                        if (m.operations.Count == this.operations.Count)
                        {
                            if (m.format.SequenceEqual(this.format))
                            {
                                return m.operations.SequenceEqual(this.operations);
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
