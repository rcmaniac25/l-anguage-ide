using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanguageDebugger.Elements
{
    public class Variable
    {
        public string Name { get; private set; }

        public virtual ulong Value { get; internal set; }

        public Variable(string name)
        {
            this.Name = name;
        }

        public Variable(string name, ulong value)
            : this(name)
        {
            this.Value = value;
        }

        public static Variable FindVar(string name, IEnumerable<Variable> vars)
        {
            foreach(Variable v in vars)
            {
                if (v.Name.Equals(name))
                {
                    return v;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
