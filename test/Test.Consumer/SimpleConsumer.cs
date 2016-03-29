﻿using System;
using System.Threading;
using Kafka.Basic;
using Metrics;

namespace Consumer
{
    class SimpleConsumer
    {
        public int Start(SimpleConsumerOptions opts)
        {
            var zookeeperString = opts.ZkConnect;
            var testTopic = opts.Topic;
            var partition = opts.Partition;
            var offset = opts.Offset;
            
            var timer = Metric.Timer("Received", Unit.Events);
            Metric.Config.WithReporting(r => r.WithConsoleReport(TimeSpan.FromSeconds(5)));

            using (var client = new KafkaClient(zookeeperString))
            using (var consumer = client.SimpleConsumer())
            using (var stream = consumer.Subscribe(testTopic, partition, offset))
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    stream.Shutdown();
                };

                stream
                    .Data(message =>
                    {
                        var time = DateTime.UtcNow.Ticks;
                        var value = long.Parse(message.Value);
                        var diff = (time - value) / 10000;
                        timer.Record(diff, TimeUnit.Milliseconds);
                    })
                    .Error(e => Console.Error.WriteLine(e.Message))
                    .Start()
                    .Block();
            }

            return 0;
        }
    }
}
