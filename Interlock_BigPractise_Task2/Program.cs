using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Interlock_BigPractise_Task2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            object locker = new object();
            Queue<Bun> showcase = new Queue<Bun>();

            int totalBaked = 0;
            int totalSold = 0;
            bool isFinished = false;

            string[] types = { "з маком", "з корицею", "з вишнею" };
            Random rand = new Random();

            Thread[] bakers = new Thread[3];
            Thread[] buyers = new Thread[5];

            for (int i = 0; i < bakers.Length; i++)
            {
                int bakerNum = i + 1;
                bakers[i] = new Thread(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(rand.Next(200, 601));

                        lock (locker)
                        {
                            if (totalBaked >= 60)
                            {
                                isFinished = true;
                                Monitor.PulseAll(locker);
                                break;
                            }

                            while (showcase.Count >= 5 && totalBaked < 60)
                            {
                                Console.WriteLine("Пекар " + bakerNum + " чекає");
                                Monitor.Wait(locker);
                            }

                            if (totalBaked >= 60)
                            {
                                isFinished = true;
                                Monitor.PulseAll(locker);
                                break;
                            }

                            int current = Interlocked.Increment(ref totalBaked);
                            if (current > 60)
                            {
                                Interlocked.Decrement(ref totalBaked);
                                isFinished = true;
                                Monitor.PulseAll(locker);
                                break;
                            }

                            Bun bun = new Bun();
                            bun.number = current;
                            bun.type = types[rand.Next(types.Length)];
                            bun.time = DateTime.Now;

                            showcase.Enqueue(bun);
                            Console.WriteLine("Пекар " + bakerNum + " спек булку #" + bun.number + " (" + bun.type + "). На вітрині: " + showcase.Count);

                            Monitor.PulseAll(locker);
                        }
                    }
                });
            }

            for (int i = 0; i < buyers.Length; i++)
            {
                int buyerNum = i + 1;
                buyers[i] = new Thread(() =>
                {
                    while (true)
                    {
                        lock (locker)
                        {
                            while (showcase.Count == 0 && !isFinished)
                            {
                                Console.WriteLine("   Покупець " + buyerNum + " чекає");
                                Monitor.Wait(locker);
                            }

                            if (showcase.Count == 0 && isFinished) break;

                            Bun bun = showcase.Dequeue();
                            int timeInSeconds = (int)(DateTime.Now - bun.time).TotalMilliseconds;

                            int soldCount = Interlocked.Increment(ref totalSold);
                            Console.WriteLine("Покупець " + buyerNum + " купив булку #" + bun.number + " (" + bun.type + "). Лежала: " + timeInSeconds + " мс. Продано: " + soldCount);

                            Monitor.PulseAll(locker);

                            if (soldCount >= 60)
                            {
                                isFinished = true;
                                Monitor.PulseAll(locker);
                                break;
                            }
                        }

                        Thread.Sleep(rand.Next(300, 901));
                    }
                });
            }

            foreach (var thread in bakers) thread.Start();
            foreach (var thread in buyers) thread.Start();

            foreach (var thread in bakers) thread.Join();
            foreach (var thread in buyers) thread.Join();

            Console.WriteLine("\nВсього спечено: " + totalBaked + " | Всього продано: " + totalSold);
        }
    }
}
