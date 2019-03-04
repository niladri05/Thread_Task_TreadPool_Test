using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace ThreadTest
{
    class Program
    {
        public static int MAX_THREAD = 100;
        public static void Main(string[] args)
        {
            try
            {

                var threadType = ProcessParameter(args);

                switch (threadType)
                {
                    case 1:
                        new TestThread().Start();
                        break;
                    case 2:
                        new TestTask().Start();
                        break;
                    case 3:
                        new TestBackgroundWorker().Start();
                        break;
                    case 4:
                        new TestThreadPool().Start();
                        break;
                    case 5:
                        new TestParallel().Start();
                        break;


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static int ProcessParameter(string[] args)
        {
            var arguments = new List<string>(args);
            var threadType = 1;
            if (arguments.Count == 1)
            {
                var argumentThreadType = Convert.ToInt32(arguments[0]);
                if (argumentThreadType < 1 && argumentThreadType > 5)
                {
                    Console.WriteLine("Wrong Parameters. First Parameter must be between 1-5");
                    return -1;
                }
                threadType = argumentThreadType;
            }
            else if (arguments.Count == 2)
            {
                var maxThread = Convert.ToInt32(arguments[1]);
                if (maxThread < 2 && maxThread > 100)
                {
                    Console.WriteLine("Wrong Parameters. Second Parameter must be between 2-100");
                    return -1;
                }
                MAX_THREAD = maxThread;
            }
            else if (arguments.Count == 0)
            {
                threadType = 1;
                MAX_THREAD = 100;
            }
            else
            {
                Console.WriteLine("Wrong Parameters");
                return -1;
            }
            return threadType;
        }
    }

    public class TestThreadPool
    {
        public void Start()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();


            using (var countdownEvent = new CountdownEvent(Program.MAX_THREAD))
            {
                for (var i = 0; i < Program.MAX_THREAD; i++)
                {
                    var ex = new Executor() { Name = "Thread" + i.ToString() };

                    ThreadPool.QueueUserWorkItem(
                            x =>
                            {
                                ex.Execute();
                                countdownEvent.Signal();
                            });

                }
                countdownEvent.Wait();

            }

            sw.Stop();
            Console.WriteLine("Total Time ThreadPool: " + sw.ElapsedMilliseconds);

        }
    }

    public class TestThread
    {
        public void Start()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < Program.MAX_THREAD; i++)
            {
                var ex = new Executor() { Name = "Thread" + i.ToString() };

                Thread t = new Thread(new ThreadStart(ex.Execute));
                threads.Add(t);
                t.Start();
            }

            threads.ForEach(x => x.Join());

            sw.Stop();
            Console.WriteLine("Total Time : " + sw.ElapsedMilliseconds);

        }
    }

    public class TestTask
    {
        private static object lockObj = new object();
        public void Start()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<Task> threads = new List<Task>();

            for (int t = 0; t < Program.MAX_THREAD; t++)
            {
                var ex = new Executor() { Name = "Thread" + t.ToString() };

                var task = Task.Run(() => ex.Execute());
                threads.Add(task);
            }

            Task.WaitAll(threads.ToArray());

            //threads.ForEach(x => x.Join());

            sw.Stop();
            Console.WriteLine("Total Time Task: " + sw.ElapsedMilliseconds);

        }
    }

    public class TestParallel
    {
        private static object lockObj = new object();
        public void Start()
        {
            List<int> list = new List<int>();

            for (int t = 0; t < Program.MAX_THREAD; t++)
            {
                list.Add(t);
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Parallel.ForEach(list, x =>
            {
                var ex = new Executor() { Name = "Thread" + x.ToString() };
                ex.Execute();
            });

            //List<Task> threads = new List<Task>();

            //for (int t = 0; t < 100; t++)
            //{
            //    var ex = new Executor() { Name = "Thread" + t.ToString() };

            //    var task = Task.Run(() => ex.Execute());
            //    threads.Add(task);
            //}

            //Task.WaitAll(threads.ToArray());

            //threads.ForEach(x => x.Join());

            sw.Stop();
            Console.WriteLine("Total Time Parallel: " + sw.ElapsedMilliseconds);

        }


    }

    public class TestBackgroundWorker
    {
        static List<BackGroundWorkerProxy> threads = new List<BackGroundWorkerProxy>();
        Stopwatch sw = new Stopwatch();

        public void Start()
        {

            sw.Start();

            for (int t = 0; t < Program.MAX_THREAD; t++)
            {
                var ex = new BackGroundWorkerProxy("Thread" + t.ToString());
                threads.Add(ex);
                ex.Start();
            }

            while (true)
            {
                if (threads.All(x => x.IsCompleted))
                {
                    sw.Stop();
                    Console.WriteLine("Total Time BG Worker: " + sw.ElapsedMilliseconds);
                    break;
                }
            }

        }
    }

    public class BackGroundWorkerProxy
    {
        private BackgroundWorker bgWorker;

        public string Name { get; }

        public bool IsCompleted { get; private set; }

        public BackGroundWorkerProxy(string name)
        {
            bgWorker = new BackgroundWorker();
            Name = name;
        }
        public void Start()
        {
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
            bgWorker.RunWorkerAsync();

        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsCompleted = true;
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var executor = new Executor() { Name = Name };
            executor.Execute();
        }
    }

    public class Executor
    {
        public string Name { get; set; }
        public void Execute()
        {
            for (int i = 0; i < 50; i++)
            {
                Console.WriteLine($"Processing {Name} and Number {i} ");
                Thread.Sleep(10);
            }
        }
    }
}
