using System;
using System.Threading;
using System.Collections;
using com.rum;

namespace csharp
{
    class Program
    {
        static void Main(string[] args)
        {
            RUMServerClient client = new RUMServerClient(41000015, "affc562c-8796-4714-b8ae-4b061ca48a6b", "52.83.220.166", 13609, true, 5000);

            client.ConnectedCallback = delegate()
            {
                Console.WriteLine("connected");

                Hashtable attrs = new Hashtable();
                attrs["test"] = 123;
                attrs["xxx"] = "yyy";

                client.SendCustomEvent("error", attrs, 5000, (exception) =>
                {
                    if (exception != null)
                        Console.WriteLine(exception.Message);
                    else
                        Console.WriteLine("send ok");
                });


                ArrayList events = new ArrayList();

                Hashtable ev1 = new Hashtable();
                ev1["ev"] = "error";
                ev1["attrs"] = attrs;

                Hashtable ev2 = new Hashtable();
                ev2["ev"] = "info";
                ev2["attrs"] = attrs;

                events.Add(ev1);
                events.Add(ev2);

                client.SendCustomEvents(events, 5000, (exception) =>
                {
                    if (exception != null)
                        Console.WriteLine(exception.Message);
                    else
                        Console.WriteLine("send ok");
                });
            };

            client.ClosedCallback = delegate()
            {
                Console.WriteLine("closed");
            };

            client.ErrorCallback = delegate(Exception e)
            {
                Console.WriteLine("error:");
                Console.WriteLine(e.Message);
            };

            client.Connect();

            while (true)
                Thread.Sleep(10000);
        }
    }
}
