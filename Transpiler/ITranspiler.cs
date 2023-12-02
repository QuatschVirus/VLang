using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VLang.InternalTypes;

namespace VLang.Transpiler
{
    interface ITranspiler
    {
        public abstract string Transpile(InstructionTree tree);
    }
}
