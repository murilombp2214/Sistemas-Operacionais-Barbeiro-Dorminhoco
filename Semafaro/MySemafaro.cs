using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Semafaro
{
    public class MySemafaro
    {
        public int CountProcess { get; set; }
        public Semaphore Semaphore { get; set; }

        public MySemafaro(int countProcess)
        {
            CountProcess = countProcess <= 0
                ? throw new Exception("Quantidade de processos maximos deve ser maior que zero")
                : countProcess;
            Semaphore = new Semaphore(0, countProcess);
        }
        public Task Up(Action action)
        {
            return Task.Run(() =>
            {
                Semaphore.WaitOne();
                action();

            });
        }

        public void Down()
        {
            Semaphore.Release(1);
        }
    }
}
