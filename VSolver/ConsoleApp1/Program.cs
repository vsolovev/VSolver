using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSolver.Implementations;

namespace ConsoleApp1
{

    public class TestClass : ITestInterface
    {
        private Dummy1 _d1;
        private Dummy2 _d2;
        public TestClass(Dummy1 d1, Dummy2 d2)
        {
            _d1 = d1;
            _d2 = d2;
        }
        public void DumpMe()
        {
            _d1.DumpMe();
            _d2.DumpMe();
        }
    }

    public class Dummy1
    {
        public void DumpMe() { Console.WriteLine("d1"); }
        public Dummy1() { }
    }

    public class Dummy2
    {
        public void DumpMe() { Console.WriteLine("d2"); }
        public Dummy2() { }
    }

    public interface ITestInterface
    {
        void DumpMe();
    }

    class Program
    {
        static void Main(string[] args)
        {

            var cont = new Container();
            cont.Register<Dummy1>();
            cont.Register<Dummy2>();
            cont.Register<ITestInterface, TestClass>();

            var inst = cont.Resolve<ITestInterface>();
            inst.DumpMe();

            Console.ReadKey();

        }
    }
}
