using NUnit.Framework;
using ReportPortal.Extensions.SourceBack.Pdb;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportPortal.Extensions.SourceBack.Test
{
    public class Tests
    {
        [Test]
        public void Test1()
        {
            try
            {
                var tasks = new List<Task>();

                for (int i = 0; i < 100; i++)
                {
                    var t = Task.Run(() => { throw new Exception("Test"); });
                    tasks.Add(t);
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception exp)
            {
                var ext = new SourceBackFormatter();
                var log = new Client.Requests.AddLogItemRequest { Level = Client.Models.LogLevel.Error, Text = exp.ToString() };
                ext.FormatLog(ref log);
                Console.WriteLine(log.Text);
                StringAssert.Contains("throw new Exception", log.Text);
            }
        }

        [Test]
        public void ThreadTest1()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                    {
                        Test1();
                    }));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}