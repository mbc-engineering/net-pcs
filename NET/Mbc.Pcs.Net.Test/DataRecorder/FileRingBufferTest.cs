﻿using FluentAssertions;
using Mbc.Pcs.Net.DataRecorder;
using System;
using System.IO;
using System.Linq;
using Xbehave;

namespace Mbc.Pcs.Net.Test.DataRecorder
{
    public class FileRingBufferTest
    {
        [Scenario]
        public void SmokeTest()
        {
            FileRingBuffer buffer = null;

            "Given a new empty file ring buffer"
                .x(() =>
                {
                    var bufferPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(bufferPath);
                    buffer = new FileRingBuffer(bufferPath, 5, 2);
                });

            "When I add some data"
                .x(() =>
                {
                    for (int i = 0; i < 18; i++)
                    {
                        buffer.AppendData(i);
                    }
                });

            "The I could retriev it again"
                .x(() =>
                {
                    var bufferedData = buffer.ReadData(0).ToList();
                    bufferedData.Should().BeEquivalentTo(17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5);
                });
        }

        [Scenario]
        public void SmokeTestWithReopen()
        {
            /*
             * Files:
             * 0-4:   0
             * 5-7:   1
             * 8-12:  2
             * 13-17: 3
             */
            string bufferPath = null;
            FileRingBuffer buffer = null;

            "Given a buffer Path"
                .x(() =>
                {
                    bufferPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(bufferPath);
                });

            "And some previously written data"
                .x(() =>
                {
                    var prevBuffer = new FileRingBuffer(bufferPath, 5, 3);
                    for (int i = 0; i < 8; i++)
                    {
                        prevBuffer.AppendData(i);
                    }

                    prevBuffer.Dispose();
                });

            "And a new file ring buffer"
                .x(() =>
                {
                    buffer = new FileRingBuffer(bufferPath, 5, 3);
                });

            "When I add some data"
                .x(() =>
                {
                    for (int i = 8; i < 18; i++)
                    {
                        buffer.AppendData(i);
                    }
                });

            "The I could retriev all data again"
                .x(() =>
                {
                    var bufferedData = buffer.ReadData(0).ToList();
                    bufferedData.Should().BeEquivalentTo(17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5);
                });
        }

        [Scenario]
        public void SmokeTestWithBinary()
        {
            FileRingBuffer buffer = null;

            "Given a new empty file ring buffer"
                .x(() =>
                {
                    var bufferPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(bufferPath);
                    buffer = new FileRingBuffer(bufferPath, 5, 2, persister: new BinaryObjectPersister<ValueHolder>());
                });

            "When I add some data"
                .x(() =>
                {
                    for (int i = 0; i < 18; i++)
                    {
                        buffer.AppendData(new ValueHolder { Value = i });
                    }
                });

            "The I could retriev it again"
                .x(() =>
                {
                    var bufferedData = buffer.ReadData(0).Cast<ValueHolder>().Select(x => x.Value).ToList();
                    bufferedData.Should().BeEquivalentTo(17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5);
                });
        }

        internal class ValueHolder
        {
            public int Value { get; set; }
        }
    }
}
