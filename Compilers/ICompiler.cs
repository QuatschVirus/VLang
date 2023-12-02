using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VLang.InternalTypes;

namespace VLang.Compilers
{
    interface ICompiler
    {
        abstract InstructionTree Compile(string code);


    }
}
