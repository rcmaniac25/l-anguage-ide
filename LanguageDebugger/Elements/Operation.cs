using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanguageDebugger.Elements
{
    public abstract class Operation
    {
        public abstract void Run(List<Variable> opVars);

        protected virtual void Parse(string[] components)
        {
        }

        public virtual void Cleanup(List<Variable> opVars)
        {
        }

        public string GotoLabel { get; internal set; }

        public static Operation GetOperation(string line, out string[] messages)
        {
            List<string> mes = new List<string>();
            if (!string.IsNullOrWhiteSpace(line))
            {
                Operation op = null;
                string gotoTxt = null;

                line = line.Trim();

                //Get the GOTO label
                if (line.StartsWith("[") && line.IndexOf(']') > 0)
                {
                    line = line.Substring(1);
                    gotoTxt = line.Substring(0, line.IndexOf(']'));
                    line = line.Substring(gotoTxt.Length + 1).Trim();
                }
                //Find the operation
                if (line.StartsWith("goto "))
                {
                    if (line.Substring(5).IndexOf(' ') == -1)
                    {
                        op = new GotoDirectOperation();
                    }
                    else
                    {
                        mes.Add("Not valid GOTO 'x' operation");
                    }
                }
                else if (line.IndexOf(" <- ") > 0)
                {
                    string strLine = line.Substring(line.IndexOf(" <- ") + 4);
                    if (strLine.EndsWith("-1"))
                    {
                        op = new SubOneOperation();
                    }
                    else if (strLine.EndsWith("+1"))
                    {
                        op = new AddOneOperation();
                    }
                }
                else if (line.StartsWith("if", StringComparison.InvariantCultureIgnoreCase) && line.IndexOf(" != 0 GOTO ", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    op = new GotoOperation();
                }
                if (op == null)
                {
                    op = Macro.FindMacro(line);
                }

                if (op != null)
                {
                    op.GotoLabel = gotoTxt;
                    op.Parse(line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                }
                else
                {
                    mes.Add(string.Format("Unknown command \"{0}\". Remember, operations are case and \"space\" sensitive.", line));
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

        private class AddOneOperation : Operation
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

        private class SubOneOperation : Operation
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

        internal class GotoOperation : Operation
        {
            protected string gotoLabel;
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

            public override void Cleanup(List<Variable> opVars)
            {
                opVars.Remove(Variable.FindVar(gotoLabel, opVars));
            }

            public override string ToString()
            {
                return string.Format("{0}IF {1} != 0 GOTO {2}", base.ToString(), argName, gotoLabel);
            }
        }

        //Not a "standard" operation, but a necessery one because it is a goto
        internal class GotoDirectOperation : GotoOperation
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
