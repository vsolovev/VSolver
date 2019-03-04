using System;

namespace VSolver.Default
{
    public class CustomerNoAttributes
    {
        private ICustomerDAL _customerDal;
        public CustomerNoAttributes(ICustomerDAL dal, Logger logger)
        {
            _customerDal = dal;
        }

        public void PrintInformation()
        {
            Console.WriteLine("Customer N_A information:");
            _customerDal.PrintInfo();
        }
    }
}