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

                for (int i = 0; i < 10; i++)
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
                StringAssert.Contains("throw new Exception", log.Text);
            }
        }

        [Test]
        public void Test2()
        {
            var inputException = @"
System.AggregateException: One or more errors occurred. ---> System.Exception: Test
   at SomeMethod() in C:\some\file.cs:line 20
   at SomeMethod() in C:\some\file.cs:line 21
   at System.Threading.Tasks.Task`1.InnerInvoke()
   at System.Threading.Tasks.Task.Execute()
";
            var ext = new SourceBackFormatter();
            var log = new Client.Requests.AddLogItemRequest { Level = Client.Models.LogLevel.Error, Text = inputException };
            ext.FormatLog(ref log);
            StringAssert.Contains("!!!", log.Text);
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