using System;

namespace VSolver.Default
{
    public class PropCustomerBLL
    {
        [Import]
        public ICustomerDAL Dal { get; set; }
        
        [Import]
        public Logger logger { get; set; }

        public void PrintInformation()
        {
            Console.WriteLine("PropCustomerBLL information:");
            Dal.PrintInfo();
        }
    }
}