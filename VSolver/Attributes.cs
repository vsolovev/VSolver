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
        public Type BaseType { get; set; }
        public Export()
        {
        }

        public Export(Type type)
        {
            BaseType = type;
        }
    }


}