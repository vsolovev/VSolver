using System;

namespace VSolver
{
    public class ImportConstructor : Attribute
    {
    }

    public class Import : Attribute
    {
    }

    public class Export : Attribute
    {
        public Export()
        {
        }

        public Export(Type type)
        {
            BaseType = type;
        }

        public Type BaseType { get; set; }
    }
}