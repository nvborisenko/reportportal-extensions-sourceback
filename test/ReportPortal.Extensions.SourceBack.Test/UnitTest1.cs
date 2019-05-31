using NUnit.Framework;
using ReportPortal.Extensions.SourceBack.Pdb;
using System;

namespace ReportPortal.Extensions.SourceBack.Test
{
    public class Tests
    {
        [Test]
        public void Test1()
        {
            try
            {
                throw new Exception("Test");
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
    }
}