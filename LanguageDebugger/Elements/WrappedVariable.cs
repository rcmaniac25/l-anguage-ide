using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanguageDebugger.Elements
{
    public class WrappedVariable : Variable
    {
        private Variable wrappedVariable;

        public override ulong Value
        {
            get
            {
                return wrappedVariable.Value;
            }
            internal set
            {
                wrappedVariable.Value = value;
            }
        }

        public WrappedVariable(string name, Variable wrappedVariable)
            : base(name)
        {
            this.wrappedVariable = wrappedVariable;
        }

        public Variable GetVariable()
        {
            return wrappedVariable;
        }
    }
}
