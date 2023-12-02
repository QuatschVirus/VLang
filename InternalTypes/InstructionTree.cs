using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VLang.InternalTypes
{
    class InstructionTree
    {
        public InstructionKey Key {  get; protected set; }
        public InstructionTree[] Branches { get; protected set; }

        public InstructionTree(InstructionKey key, InstructionTree[] branches)
        {
            this.Key = key;
            this.Branches = branches;
        }
    }

    class InstructionTree<T> : InstructionTree where T : InstructionMeta
    {
        public T Meta { get; protected set; }

        public InstructionTree(InstructionKey key, InstructionTree[] branches, T meta) : base(key, branches)
        {
            this.Meta = meta;
        }
    }

    enum InstructionKey
    {
        Root,
        Meta,

    }

    abstract class InstructionMeta { }


}
