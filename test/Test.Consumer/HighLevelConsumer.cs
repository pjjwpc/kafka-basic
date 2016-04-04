﻿using System;
using System.Threading;
using Kafka.Basic;
using Metrics;

namespace Consumer
{
    class HighLevelConsumer
    {
        private Thread _consoleThread;

        public int Start(HighLevelConsumerOptions opts)
        {
            var timer = Metric.Timer("Received", Unit.Events);
            Metric.Config.WithReporting(r => r.WithConsoleReport(TimeSpan.FromSeconds(5)));

            using (var client = new KafkaClient(opts.ZkConnect))
            {
                var consumerGroup = client.Consumer(opts.Group);
                using (var instance = consumerGroup.Join())
                {
                    var stream = instance.Subscribe(opts.Topic);

                    ListenToConsole(instance, stream);

                    stream
                        .Data(message =>
                        {
                            var time = DateTime.UtcNow.Ticks;
                            var value = long.Parse(message.Value);
                            var diff = (time - value) / 10000;
                            timer.Record(diff, TimeUnit.Milliseconds);
                        })
                        .Start()
                        .Block();
                }
            }

            return 0;
        }

        private void ListenToConsole(IKafkaConsumerInstance instance, IKafkaConsumerStream stream)
        {
            _consoleThread = new Thread(() =>
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    Console.WriteLine("Kill!");
                    _consoleThread.Abort();
                };
                while (true)
                {
                    var input = Console.ReadKey(true);
                    if (input.KeyChar == 'p')
                    {
                        stream.Pause();
                        Console.WriteLine("Paused.");
                    }
                    if (input.KeyChar == 'r')
                    {
                        stream.Resume();
                        Console.WriteLine("Resumed.");
                    }
                    if (input.KeyChar == 'q')
                    {
                        Console.WriteLine("Shutting down...");
                        instance.Shutdown();
                        break;
                    }
                }
            });
            _consoleThread.Start();
        }
    }
}
