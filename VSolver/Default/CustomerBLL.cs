using System;

namespace VSolver.Default
{
    [ImportConstructor]
    public class CustomerBLL
    {
        private ICustomerDAL _customerDal; 
        public CustomerBLL(ICustomerDAL dal, Logger logger)
        {
            _customerDal = dal;
        }

        public void PrintInformation()
        {
            Console.WriteLine("Customer BLL information:");
            _customerDal.PrintInfo();
        }
    }
}