﻿using MongoQueue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TaskBroker;
using TaskBroker.Configuration;
using TaskScheduler;


namespace TApp
{
    class Program
    {
        static ManualResetEvent sync;
        //netsh http add urlacl url=http://+:82/ user=User
        static void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo("TApp.exe") { Verb = "runas" };
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        class zModel : TaskQueue.Providers.TaskMessage
        {
            public const string Name = "z";
            public zModel() : base(Name) { }
            public string SomeProperty { get; set; }
        }
        class zConsumer : IModConsumer
        {
            public bool Push(TaskQueue.Providers.TItemModel parameters, ref TaskQueue.Providers.TaskMessage q_parameter)
            {
                Console.WriteLine("zMessage: {0} - {1} {2}", q_parameter.GetHolder()["MType"], q_parameter.AddedTime, q_parameter.GetHolder()["_id"]);
                return true;
            }

            public void Exit()
            {

            }

            public void Initialise(ModMod thisModule)
            {

            }

            public string Name
            {
                get { return "ZConsume"; }
            }

            public string Description
            {
                get { throw new NotImplementedException(); }
            }


            public ModuleSelfTask[] RegisterTasks(ModMod thisModule)
            {
                return null;
            }
        }

        static void Main(string[] args)
        {
            //if (System.Diagnostics.Debugger.IsAttached)
            //{
            //    ManualResetEvent mre = new ManualResetEvent(false);
            //    BrokerApplication ba = new BrokerApplication();
            //    ba.Run(mre);
            //    mre.WaitOne();
            //}
            //else
            //{
                ApplicationKeeper.AppdomainLoop();
            //}
            Console.WriteLine("ok, done");
            Console.ReadLine();
            return;
            //var prefix = QueueService.ModProducer.ListeningOn;
            //var username = Environment.GetEnvironmentVariable("USERNAME");
            //var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
            //Console.WriteLine("  netsh http add urlacl url={0} user={1}\\{2} listen=yes",
            //        prefix, userdomain, username);


            //TaskQueue.QueueItemModel tm = new TaskQueue.QueueItemModel(typeof(zModel));
            //MSSQLQueue.SqlTable t = new MSSQLQueue.SqlTable(tm, "Z");
            //string rst = MSSQLQueue.SqlScript.ForTableGen(t);
            //
            TaskBroker.Broker b = new TaskBroker.Broker();
            //
            b.AddAssemblyByPath("QueueService.dll");
            //b.AddAssemblyByPath("Dllwithotrefs.dll");

            b.RegisterConnection<MongoDbQueue>("MongoLocalhost",
                "mongodb://user:1234@localhost:27017/?safe=true", "Messages", "TaskMQ");
            b.RegisterConnection<MongoDbQueue>("MongoLocalhostEmail",
                "mongodb://user:1234@localhost:27017/?safe=true", "Messages", "email");
            //
            //b.RegisterMessageModel<zModel>();
            //b.RegisterConsumerModule<zConsumer, zModel>();
            b.RegisterChannel<zModel>("MongoLocalhost", "z");

            //b.RegisterSelfValuedModule<QueueService.ModProducer>();
            //b.RegisterSelfValuedModule<EmailConsumer.ModConsumer>();

            b.RegisterChannel<EmailConsumer.MailModel>("MongoLocalhostEmail", "EmailC");
            EmailConsumer.SmtpModel smtp = new EmailConsumer.SmtpModel()
            {
                Login = "user",
                UseSSL = true,
                Port = 587,
                Password = "",
                Server = "smtp.yandex.ru"
            };
            //
            //b.ReloadModules();
            b.ReloadAssemblys();
            b.RegisterTask(
                "EmailC", "ThroughputTest",//"EmailSender",
                IntervalType.intervalMilliseconds, 1000, null /*smtp*/,
                "Email Common channel consumer on mongo db queue channel");


            //
            File.WriteAllBytes("cc.json", BrokerConfiguration.ExtractFromBroker(b).Serialise());
            File.WriteAllBytes("mm.json", BrokerConfiguration.ExtractModulesFromBroker(b).Serialise());
            File.WriteAllBytes("mma.json", BrokerConfiguration.ExtractAssemblysFromBroker(b).Serialise());
            //Console.ReadLine();
            //b.PushMessage(new EmailConsumer.MailModel());

            Console.ReadLine();
            b.PushMessage(new EmailConsumer.MailModel());
            Console.ReadLine();
            //b.ReloadModules();
        }
    }
}
