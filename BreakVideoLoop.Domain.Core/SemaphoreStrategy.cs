using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreakVideoLoop.Domain.Core
{
    public class SemaphoreStrategy
    {
        private bool semaphore;
        public SemaphoreStrategy()
        {
            semaphore = true;
        }

        public void Reset()
        {
            semaphore = true;
        }

        public void ExecSemaphore(Action func)
        {
            while (!semaphore)
                Thread.Sleep(1);

            semaphore = false;
            func();
            semaphore = true;
        }
    }
}
