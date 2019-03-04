using System;

namespace VSolver.Default
{
    [Export(typeof(ICustomerDAL))]
    public class CustomerDAL : ICustomerDAL
    {
        public void PrintInfo()
        {
            Console.WriteLine($"Customer DAL information: {this.GetHashCode()}");
        }
    }
}