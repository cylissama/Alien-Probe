using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using CES.AlphaScan.Base;
using CES.AlphaScan.mmWave;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;

namespace TestTiming
{
    class Program
    {
        static void Main(string[] args)
        {
            //Write();
            //TimemmWaveCluster2();
            //TestNewIsMagic();
            //TimemmWaveParse();

            TestChannels();
            Console.ReadKey();
        }

        // Reads a mmWave packet binary file and returns list of PacketData objects.
        public static List<PacketData> ReadBinFile(string rawBinFile, int maxNum = -1)
        {
            DateTime time;
            byte[] data;
            List<PacketData> packetList = new List<PacketData>();

            using (FileStream fileStream = new FileStream(rawBinFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                while (true)
                {
                    try
                    {
                        time = new DateTime(BitConverter.ToInt64(reader.ReadBytes(8), 0));
                        data = reader.ReadBytes(8256); // Hopefully this is the right length for now.
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    packetList.Add(new PacketData(data, time));
                    if (maxNum >= 0 && packetList.Count >= maxNum) break;
                }
            }

            return packetList;
        }

        // Times the FindmmWaveClusters function on a list of mmWave packet data.
        public static void TimemmWaveCluster()
        {
            //string rawBinFile = "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs_Oct5\\Run35_1-5m_All\\mmWaveRawData.bin";
            //string rawBinFile =  "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs 2020-11-10\\Run 86 (partial)\\mmWaveRawData.bin";
            string rawBinFile = "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs 2020-11-10\\Run 86 (partial)\\mmWaveRawData.bin";

            List<PacketData> packetList = ReadBinFile(rawBinFile);

            mmWaveDataProcessor dataProcessor = new mmWaveDataProcessor();

            List<ClusteredObject> clusteredList = null;

            //int num = Math.Min(packetList.Count, 100);
            int num = packetList.Count;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < num; i++)
            {
                clusteredList = dataProcessor.FindmmWaveClusters(packetList[i], VehicleSide.Right);
                //Console.WriteLine("Found " + clusteredList.Count + " mmWave clusters.");
            }
            sw.Stop();
            Console.WriteLine("Finished clustering of " + num + " packets in " + sw.ElapsedMilliseconds + " milliseconds." + "Average rate of " + (sw.ElapsedMilliseconds / (1000.0 * num)) + " seconds per packet.");


        }

        // Times the FindmmWaveClusters function on a list of mmWave packet data.
        public static void TimemmWaveCluster2()
        {
            //string rawBinFile = "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs_Oct5\\Run35_1-5m_All\\mmWaveRawData.bin";
            //string rawBinFile =  "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs 2020-11-10\\Run 86 (partial)\\mmWaveRawData.bin";
            string rawBinFile = "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs 2020-11-10\\Run 86 (partial)\\mmWaveRawData.bin";

            List<PacketData> packetList = ReadBinFile(rawBinFile, -1);

            ConcurrentQueue<PacketData> pq = new ConcurrentQueue<PacketData>(packetList);

            mmWaveDataProcessor dataProcessor = new mmWaveDataProcessor();

            List<Thread> threadList = new List<Thread>();
            int numThreads = 9;

            for (int i = 0; i < numThreads; i++)
            {
                threadList.Add(new Thread((object nnn) =>
                {
                    List<ClusteredObject> clusteredList = null;
                    int t = (int)nnn;
                    Stopwatch sw = Stopwatch.StartNew();
                    while (pq.Count > 0)
                    {
                        if (pq.TryDequeue(out PacketData p))
                        {
                            try
                            {
                                clusteredList = dataProcessor.FindmmWaveClusters(p, VehicleSide.Right);
                            }
                            catch { };
                            
                        }
                    }
                    sw.Stop();
                    Console.WriteLine("Finished thread " + nnn + " after " + sw.ElapsedMilliseconds + " ms.");
                }));
            }

            for (int t = 0; t < threadList.Count; t++)
            {
                threadList[t].Start(t);
            }

        }

        // Times the mmWave packet parsing on a list of binary data.
        public static void TimemmWaveParse()
        {
            string rawBinFile = "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs 2020-11-10\\Run 86 (partial)\\mmWaveRawData.bin";

            List<PacketData> packetList = ReadBinFile(rawBinFile, -1);

            //PacketData to raw bytes;

            List<byte> raw = new List<byte>();
            for (int i = 0; i < 500; i++)
            {
                raw.Add((byte)(i % 256));
            }
            foreach (PacketData pd in packetList)
            {
                raw.AddRange(pd.FullPacket);
            }

            double time;
            List<double> times = new List<double>();

            // Run test n times
            for (int i = 0; i < 100; i++)
            {
                time = Parse2(raw);
                //time = Parse(raw.ToList());
                if (time >= 0) times.Add(time);
            }

            double avgTime = times.Average();

            Console.WriteLine("Ran test " + times.Count + " times.");
            Console.WriteLine("Average time: " + avgTime);
            
        }

        // Parses packets and times it.
        public static double Parse(List<byte> byteVec)
        {
            Stopwatch sw = new Stopwatch();

            Queue<PacketData> packetOutChannel = new Queue<PacketData>();
            
            int magicIdx = 0;
            DateTime dataReceiveTime;
            int packetLength = 0;
            byte[] packet;

            //Way 1
            int remCount = 0;

            sw.Start();

            if (byteVec.Count < 8256)
            {
                Console.WriteLine("F1: byteVec too short.");
                return -1;
            }

            // Look for magic numbers until no more packets
            while (true)
            {
                // Look for magic number
                if (!mmWavePacketParsing.IsMagic(byteVec, magicIdx, out magicIdx) || byteVec.Count < magicIdx + 8256 )
                    break;

                //Get time stamp as early as possible //$? Maybe should be earlier?
                dataReceiveTime = DateTime.UtcNow;

                // Find packetLength and create byte array
                packetLength = mmWavePacketParsing.EvalFourUBytes(byteVec, magicIdx + 12);

                // Check if full packet in byteVec
                if (byteVec.Count < magicIdx + packetLength)
                    break;

                //Way 1: remove count
                remCount = magicIdx + packetLength;

                packet = new byte[packetLength];

                byteVec.CopyTo(magicIdx, packet, 0, packetLength);
                //Way 2: remove here
                //byteVec.RemoveRange(0, magicIdx + packetLength);

                // Write packet to channel as PacketData object
                try
                {
                    packetOutChannel.Enqueue(new PacketData(packet, dataReceiveTime));
                    //if (!packetOutChannel.Writer.TryWrite(new PacketData(packet, dataReceiveTime)))
                    //    Console.WriteLine("Failed to write mmWave packet to channel."); //$? What do if fails?
                }
                catch (Exception e)
                {
                    Console.WriteLine("F2: Failed to write mmWave packet to channel." + " Exception: " + e.Message);
                }

                //Way 2:
                //magicIdx = 0;

                //Way 1:
                magicIdx += packetLength;
                if (byteVec.Count <= magicIdx)
                    break;
                
            }

            //$$$ saves packet length in year, month, day - hour, minute, second, ms format for title, just raw data, use matlab to post process

            //byteVec.RemoveRange(0, magicIdx + packetLength); //Packet length should always be 8256 for this use case.

            //Way 1: remove here
            byteVec.RemoveRange(0, remCount); //Packet length should always be 8256 for this use case.

            sw.Stop();

            double timems = sw.Elapsed.Ticks / (10000.0);

            //Console.WriteLine("Completed parsing after " + timems + " ms.");
            //Console.WriteLine(" -> " + packetOutChannel.Count + " packets completed with rate of " + (timems / packetOutChannel.Count) +" ms per packet.");

            //return packetOutChannel.ToList();
            return timems;
        }

        // Parses packets and times it. Also adds bytes to byteVec.
        public static double Parse2(List<byte> rawBytes)
        {
            List<byte> byteVec = new List<byte>(8256);

            // Cut up rawBytes into length n pieces
            Queue<byte[]> aa = new Queue<byte[]>();
            {
                int n = 5000;
                byte[] a;
                int num = rawBytes.Count;

                for (int i = 0; i < num; i += n)
                {
                    a = new byte[n];

                    if (i + n > num)
                    {
                        rawBytes.CopyTo(i, a, 0, num - i);

                        //D2: For messy data. Probs not needed.
                        //for (int j = num - i; j < n; j++)
                        //{
                        //    a[j] = (byte)(j % 256);
                        //}
                    }
                    else
                    {
                        rawBytes.CopyTo(i, a, 0, n);
                    }

                    aa.Enqueue(a);
                }
            }
            

            Stopwatch sw = new Stopwatch();

            Queue<PacketData> packetOutChannel = new Queue<PacketData>();

            int magicIdx = 0;
            DateTime dataReceiveTime;
            int packetLength = 0;
            byte[] packet;

            //Way 1
            int remCount = 0;

            sw.Start();

            while (aa.Count > 0)
            {
                byteVec.AddRange(aa.Dequeue());

                if (byteVec.Count < 8256)
                    continue;

                magicIdx = 0;
                remCount = 0;

                // Look for magic numbers until no more packets
                while (true)
                {
                    // Look for magic number
                    if (!mmWavePacketParsing.IsMagic(byteVec, magicIdx, out magicIdx) || byteVec.Count < magicIdx + 8256)
                        break;

                    //Get time stamp as early as possible //$? Maybe should be earlier?
                    dataReceiveTime = DateTime.UtcNow;

                    // Find packetLength and create byte array
                    packetLength = mmWavePacketParsing.EvalFourUBytes(byteVec, magicIdx + 12);

                    // Check if full packet in byteVec
                    if (byteVec.Count < magicIdx + packetLength)
                        break;

                    //Way 1: remove count
                    remCount = magicIdx + packetLength;

                    packet = new byte[packetLength];

                    byteVec.CopyTo(magicIdx, packet, 0, packetLength);
                    //Way 2: remove here
                    //byteVec.RemoveRange(0, magicIdx + packetLength);

                    // Write packet to channel as PacketData object
                    try
                    {
                        packetOutChannel.Enqueue(new PacketData(packet, dataReceiveTime));
                        //if (!packetOutChannel.Writer.TryWrite(new PacketData(packet, dataReceiveTime)))
                        //    Console.WriteLine("Failed to write mmWave packet to channel."); //$? What do if fails?
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("F2: Failed to write mmWave packet to channel." + " Exception: " + e.Message);
                    }

                    //Way 2:
                    //magicIdx = 0;

                    //Way 1:
                    magicIdx += packetLength;
                    if (byteVec.Count <= magicIdx)
                        break;

                }

                //$$$ saves packet length in year, month, day - hour, minute, second, ms format for title, just raw data, use matlab to post process

                //byteVec.RemoveRange(0, magicIdx + packetLength); //Packet length should always be 8256 for this use case.

                //Way 1: remove here
                byteVec.RemoveRange(0, remCount); //Packet length should always be 8256 for this use case.

            }

            sw.Stop();

            double timems = sw.Elapsed.Ticks / (10000.0);

            //Console.WriteLine("Completed parsing after " + timems + " ms.");
            //Console.WriteLine(" -> " + packetOutChannel.Count + " packets completed with rate of " + (timems / packetOutChannel.Count) +" ms per packet.");

            //return packetOutChannel.ToList();
            return timems;
        }

        public static void Write()
        {
            string rawBinFile = "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs 2020-11-10\\Run 86 (partial)\\mmWaveRawData2.bin";

            byte[] buffer = new byte[8256];

            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 129; j++)
                {
                    buffer[i + j * 64] = (byte)(i + j);
                }
            }

            DateTime time = new DateTime(1999, 9, 9, 9, 9, 9, 9);

            var p = new PacketData(buffer, time);

            for (int i = 0; i < 8256; i++)
            {
                Console.Write(buffer[i].ToString() + ' ');
            }
            

            //Saving
            byte[] datetimeTicks = BitConverter.GetBytes(time.Ticks);
            byte[] bytes = buffer.ToArray();

            using (FileStream fileStream = new FileStream(rawBinFile, FileMode.Append, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                writer.Write(datetimeTicks);
                writer.Write(bytes);
            }
        }

        public static void TestNewIsMagic()
        {
            mmWavePacketParsing pp = new mmWavePacketParsing();

            byte[] magic = new byte[] { 2, 1, 4, 3, 6, 5, 8, 7 };

            List<byte> t1 = new List<byte>();
            List<byte> tmp = new List<byte>();
            for (int i = 0; i < 25; i++)
            {
                tmp.Add((byte)i);
            }

            t1.AddRange(tmp);
            t1.AddRange(magic);
            t1.AddRange(tmp);

            int startIdx = 0;
            int endIdx = -1;
            bool isFound = false;

            isFound = mmWavePacketParsing.IsMagic(t1, startIdx, out endIdx);

            Console.WriteLine("Found: " + isFound);
            Console.WriteLine("Start: " + startIdx);
            Console.WriteLine("End: " + endIdx);
        }

        public static void TestChannels()
        {
            string rawBinFile = "C:\\Users\\samev\\Documents\\Lab Stuff\\Dynamic RFID Testing\\Runs 2020-11-10\\Run 86 (partial)\\mmWaveRawData.bin";
            var d = ReadBinFile(rawBinFile, 1);

            Channel<PacketData> channel = Channel.CreateUnbounded<PacketData>();

            var r = channel.Reader;
            var w = channel.Writer;

            var t1 = new Thread(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        w.TryWrite(d[0]);
                    }
                    Console.WriteLine("( ) Count: " + r.Count);
                }
                Console.WriteLine("( ) Count: " + r.Count);

            });

            t1 = new Thread(async () =>
            {
                for (int j = 0; j < 100; j++)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        await w.WriteAsync(d[0]);
                    }
                    Console.WriteLine("( ) Count: " + r.Count);
                }
                Console.WriteLine("( ) Count: " + r.Count);

            });

            var t2 = new Thread(async() =>
            {
                while (!await r.WaitToReadAsync()) ;
                PacketData p = null;
                
                while(await r.WaitToReadAsync())
                {
                    for (int i = 0; i < 99; i++)
                    {
                        if (!r.TryRead(out p)) break;
                        p.Time.ToString();
                    }
                    Console.WriteLine("(+) Count: " + r.Count);
                }
                Console.WriteLine("(+) Count: " + r.Count);

            });

            t2 = new Thread(() =>
            {
                while (!r.WaitToReadAsync().AsTask().Result) ;
                PacketData p = null;

                while (r.WaitToReadAsync().AsTask().Result)
                {
                    for (int i = 0; i < 99; i++)
                    {
                        p = r.ReadAsync().AsTask().Result;
                        //if (!r.TryRead(out p)) break;
                        p.Time.ToString();
                    }
                    Console.WriteLine("(+) Count: " + r.Count);
                }
                Console.WriteLine("(+) Count: " + r.Count);

            });


            t2.Start();
            t1.Start();

        }

    }
}
