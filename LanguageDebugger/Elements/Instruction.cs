using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanguageDebugger.Elements
{
    public abstract class Instruction
    {
        public abstract void Run(List<Variable> opVars);

        public abstract string[] GetArguments();

        protected virtual void Parse(string[] components)
        {
        }

        public virtual void Cleanup(List<Variable> opVars)
        {
        }

        public string GotoLabel { get; internal set; }

        public static Instruction GetInstruction(string line, out string[] messages)
        {
            List<string> mes = new List<string>();
            if (!string.IsNullOrWhiteSpace(line))
            {
                Instruction op = null;
                string gotoTxt = null;

                line = line.Trim();

                //Get the GOTO label
                if (line.StartsWith("[") && line.IndexOf(']') > 0)
                {
                    line = line.Substring(1);
                    gotoTxt = line.Substring(0, line.IndexOf(']'));
                    line = line.Substring(gotoTxt.Length + 1).Trim();
                }
                //Find the instruction
                if (line.StartsWith("goto "))
                {
                    if (line.Substring(5).IndexOf(' ') == -1)
                    {
                        op = new GotoDirectInstruction();
                    }
                    else
                    {
                        mes.Add("Not valid GOTO 'x' instruction");
                    }
                }
                else if (line.IndexOf(" <- ") > 0)
                {
                    string strLine = line.Substring(line.IndexOf(" <- ") + 4);
                    if (strLine.EndsWith("-1"))
                    {
                        op = new SubOneInstruction();
                    }
                    else if (strLine.EndsWith("+1"))
                    {
                        op = new AddOneInstruction();
                    }
                    else if (strLine.IndexOf(' ') == -1)
                    {
                        //Could be a do nothing instruction
                        if (line.StartsWith(strLine + ' '))
                        {
                            op = new DoNothingInstruction();
                        }
                    }
                }
                else if (line.StartsWith("if", StringComparison.InvariantCultureIgnoreCase) && line.IndexOf(" != 0 GOTO ", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    op = new GotoInstruction();
                }
                if (op == null)
                {
                    op = Macro.FindMacro(line);
                }

                if (op != null)
                {
                    op.GotoLabel = gotoTxt;
                    op.Parse(line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

                    if (op is GotoInstruction && ((GotoInstruction)op).gotoLabel.Equals(op.GotoLabel))
                    {
                        //Infinite loop possible ([a] ... goto a)
                        if (op is GotoDirectInstruction)
                        {
                            mes.Add(string.Format("Infinite loop occurance \"{0}\"", op));
                            op = null;
                        }
                        else
                        {
                            mes.Add(string.Format("Infinite loop possible \"{0}\"", op));
                        }
                    }
                }
                else
                {
                    mes.Add(string.Format("Unknown command \"{0}\". Remember, instructions are case, \"space\", and \"argument order\" sensitive.", line));
                }

                messages = mes.ToArray();
                return op;
            }
            messages = null;
            return null;
        }

        public override string ToString()
        {
            return DirectToString();
        }

        protected string DirectToString()
        {
            return GotoLabel == null ? string.Empty : string.Format("[{0}] ", GotoLabel);
        }

        private class AddOneInstruction : Instruction
        {
            private string argName;

            public override void Run(List<Variable> opVars)
            {
                Variable var = Variable.FindVar(argName, opVars);
                if (var == null)
                {
                    opVars.Add(new Variable(argName, 1));
                }
                else
                {
                    if (var.Value == ulong.MaxValue)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    var.Value++;
                }
            }

            public override string[] GetArguments()
            {
                return new string[] { argName };
            }

            protected override void Parse(string[] components)
            {
                if (components.Length != 4)
                {
                    throw new ArgumentException();
                }
                argName = components[2];
            }

            public override string ToString()
            {
                return string.Format("{0}{1} <- {1} +1", base.ToString(), argName);
            }
        }

        private class SubOneInstruction : Instruction
        {
            private string argName;

            public override void Run(List<Variable> opVars)
            {
                Variable var = Variable.FindVar(argName, opVars);
                if (var.Value == 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                var.Value--;
            }

            public override string[] GetArguments()
            {
                return new string[] { argName };
            }

            protected override void Parse(string[] components)
            {
                if (components.Length != 4)
                {
                    throw new ArgumentException();
                }
                argName = components[2];
            }

            public override string ToString()
            {
                return string.Format("{0}{1} <- {1} -1", base.ToString(), argName);
            }
        }

        private class DoNothingInstruction : Instruction
        {
            private string argName;

            public override void Run(List<Variable> opVars)
            {
                Variable var = Variable.FindVar(argName, opVars);
                if (var == null)
                {
                    opVars.Add(new Variable(argName));
                }
            }

            public override string[] GetArguments()
            {
                return new string[] { argName };
            }

            protected override void Parse(string[] components)
            {
                if (components.Length != 3)
                {
                    throw new ArgumentException();
                }
                argName = components[2];
            }

            public override string ToString()
            {
                return string.Format("{0}{1} <- {1}", base.ToString(), argName);
            }
        }

        internal class GotoInstruction : Instruction
        {
            protected internal string gotoLabel;
            private string argName;

            //Do some fancy, under-the-hood work to do goto
            public override void Run(List<Variable> opVars)
            {
                Variable var = Variable.FindVar(argName, opVars);
                if (var.Value != 0)
                {
                    opVars.Add(new Variable(gotoLabel, 1));
                }
            }

            protected override void Parse(string[] components)
            {
                if (components.Length != 6)
                {
                    throw new ArgumentException();
                }
                argName = components[1];
                gotoLabel = components[5];
            }

            public override string[] GetArguments()
            {
                return new string[] { argName };
            }

            public override void Cleanup(List<Variable> opVars)
            {
                opVars.Remove(Variable.FindVar(gotoLabel, opVars));
            }

            public override string ToString()
            {
                return string.Format("{0}IF {1} != 0 GOTO {2}", base.ToString(), argName, gotoLabel);
            }
        }

        //Not a "standard" instruction, but a necessery one because it is a goto
        internal class GotoDirectInstruction : GotoInstruction
        {
            /* Quivilant of:
             * **goto 'x'
               w <- w +1
               if w != 0 goto 'x'
             * **
             */

            public override void Run(List<Variable> opVars)
            {
                opVars.Add(new Variable(gotoLabel, 1));
            }

            protected override void Parse(string[] components)
            {
                if (components.Length != 2)
                {
                    throw new ArgumentException();
                }
                gotoLabel = components[1];
            }

            public override string ToString()
            {
                return string.Format("{0}GOTO {1}", this.DirectToString(), gotoLabel);
            }
        }
    }
}
