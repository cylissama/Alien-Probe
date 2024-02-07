using System;
using System.Collections.Generic;
using System.Threading;
using CES.AlphaScan.Base;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CES.AlphaScan.mmWave
{
    public class mmWavePacketParsing : ILogMessage
    {
        /*
            First parsing of mmWave packets occur here. This includes dividing read serial port data into packets of 8256 bytes (default for mmWave data) for processing.
            This process is completed by finding a magic number and the packet after with a valid size. A mmWave magic number is in the format of the sequential bytes
            2 1 4 3 6 5 8 7. After finding the packet, the data is then saved to a channel for packet parsing to get header and heatmap data which will then be analyzed
            (see AzimuthProcessing) and then clustered (see ClusterData).
        */

        #region Logging

        /// <summary>
        /// Name of the mmWave packet parser.
        /// </summary>
        public string Name { get; protected set; } = "mmWavePacketParser";

        /// <summary>
        /// Logs message string.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogMessage(string message)
        {
            MessageLogged?.Invoke(this, new LogMessageEventArgs(message, Name));
        }

        /// <summary>
        /// Re-logs a message from event arguments and sender object.
        /// </summary>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="messageArgs">Event arguments of message to log.</param>
        private void LogMessage(object sender, LogMessageEventArgs messageArgs)
        {
            MessageLogged?.Invoke(this, new LogMessageEventArgs(messageArgs, Name));
        }

        public event EventHandler<LogMessageEventArgs> MessageLogged;
        #endregion

        /// <summary>
        /// gets whether the mmWave packets were processing
        /// </summary>
        public bool IsProcessing { get; private set; } = false;

        /// <summary>
        ///  byte vector for processing data
        /// </summary>
        private readonly List<byte> byteVec = new List<byte>();

        /// <summary>
        /// cancels the Start function for processing mmWave packets
        /// </summary>
        private CancellationTokenSource cancelParserLoop;

        /// <summary>
        /// Prevents multiple instances of the loop started in <see cref="ParsePacketLoop"/> from running simultaneously.
        /// </summary>
        private readonly SemaphoreSlim loopParsing = new SemaphoreSlim(1, 1);

        public static int c = 0;

        /// <summary>
        /// Starts loop for parsing mmWave packets from bytes.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if loop already running.</exception>
        private void ParsePacketLoop()
        {
            // Only allow one loop to run at a time.
            if (!loopParsing.Wait(0))
            {
                // Only one loop can run at once.
                throw new InvalidOperationException("Attempted to start mmWave parsing loop when it was already running.");
            }

            try
            {
                if (rawByteInChannel == null || rawByteInChannel.Completion.IsCompleted)
                {
                    // No input channel.
                    throw new InvalidOperationException("Raw data input channel is null or completed.");
                }

                if (packetOutChannel == null || packetOutChannel.Reader.Completion.IsCompleted)
                {
                    // No output channel.
                    throw new InvalidOperationException("Packet data output channel is null or completed.");
                }

                // Clear old data
                byteVec.Clear();

                cancelParserLoop = new CancellationTokenSource();
                IsProcessing = true;
                // packet data that is set within the header
                int magicIdx;
                DateTime dataReceiveTime;
                int packetLength;
                byte[] packet;

                // Keep track of index to remove to in byteVec
                int remCount;

                while (!cancelParserLoop.IsCancellationRequested)
                {
                    // Wait for new raw data
                    try
                    {
                        if (!rawByteInChannel.WaitToReadAsync(cancelParserLoop.Token).AsTask().Result) continue;
                    }
                    catch
                    {
                        continue;
                    }
                    

                    // Read from channel<byte[]> of bytes read from sensor.
                    // Add byte[]'s to byteVec
                    while(rawByteInChannel.TryRead(out byte[] rawData))
                    {
                        byteVec.AddRange(rawData);
                    }

                    if (byteVec.Count < 8256)
                        continue;

                    magicIdx = 0;
                    remCount = 0;

                    // Look for magic numbers until no more packets
                    while (true)
                    {
                        // Look for magic number
                        if (!IsMagic(byteVec, magicIdx, out magicIdx) || byteVec.Count < magicIdx + 8256)
                        {
                            remCount = magicIdx;
                            break;
                        }
                            

                        //Get time stamp as early as possible //$? Maybe should be earlier?
                        dataReceiveTime = DateTime.UtcNow;

                        // Find packetLength and create byte array
                        packetLength = EvalFourUBytes(byteVec, magicIdx + 12);

                        // Check if full packet in byteVec
                        if (byteVec.Count < magicIdx + packetLength)
                            break;

                        remCount = magicIdx + packetLength;

                        packet = new byte[packetLength];

                        byteVec.CopyTo(magicIdx, packet, 0, packetLength);
                        
                        // Write packet to channel as PacketData object
                        if (!packetOutChannel.Writer.TryWrite(new PacketData(packet, dataReceiveTime)))
                        {
                            // This shouldn't ever happen. But loop should stop if it does.
                            throw new Exception("Failed to write mmWave packet to channel."); 
                        }

                        // Move to after last packet. Break if end of byteVec.
                        magicIdx += packetLength;
                        if (byteVec.Count <= magicIdx)
                            break;
                    }
                    // Remove all packets that have been read
                    byteVec.RemoveRange(0, remCount); //Packet length should always be 8256 for this use case.
                }
            }
            finally
            {
                IsProcessing = false;
                packetOutChannel?.Writer.TryComplete();

                //Release semaphore after loop completes.
                loopParsing.Release();
            }
        }

        /// <summary>
        /// create new thread for the packet processor to run on
        /// </summary>
        /// <param name="saveDirectory"> save directory of individual mmWave packets</param>
        /// <param name="outSaveDirectory">save directory of clustered mmWAve data</param>
        public void StartProcessorThread()
        {
            Thread processorThread = new Thread(() => {
                try
                {
                    ParsePacketLoop();
                }
                catch (Exception e)
                {
                    LogMessage("Failed to start mmWave packet parsing loop. " + e.GetType().FullName + ": " + e.Message);
                }
                })
            {
                Name = "PacketProcessorThread",
                IsBackground = true
            };
            processorThread.Start();
        }

        /// <summary>
        /// Requests for the processing loop to stop if not already requested. Waits for the loop to stop.
        /// </summary>
        /// <returns>Awaitable task that completes when the processing loop stops.</returns>
        public async Task StopAndWait()
        {
            cancelParserLoop.Cancel();
            await loopParsing.WaitAsync();
            loopParsing.Release();
        }

        /// <summary>
        /// Requests for the processing loop to stop, if not already requested.
        /// </summary>
        public void Stop()
        {
            cancelParserLoop.Cancel();
        }

        /// <summary>
        /// Search for magic number in mmWave data.
        /// </summary>
        /// <param name="buffer">List of mmWave data to search.</param>
        /// <returns>Index of start of magic number.</returns>
        public static bool IsMagic(List<byte> buffer, int startIdx, out int endIdx)       // looks for magic number in possible packet
        {
            int num = buffer.Count - 7;
            for (int i = startIdx; i < num; i++)
            {
                if (buffer[i] == 2 && buffer[i + 1] == 1 &&
                    buffer[i + 2] == 4 && buffer[i + 3] == 3 &&
                    buffer[i + 4] == 6 && buffer[i + 5] == 5 &&
                    buffer[i + 6] == 8 && buffer[i + 7] == 7)
                {
                    endIdx = i;
                    return true;
                }
            }
            endIdx = num;
            return false;
        }

        /// <summary>
        /// Perform little endian conversion of four bytes to int.
        /// </summary>
        /// <param name="arr">Input list of bytes.</param>
        /// <param name="startIdx">Starting index of four bytes.</param>
        /// <returns>Little endian number as int.</returns>
        public static int EvalFourUBytes(List<byte> arr, int startIdx)     // for calculating little endian values
        {
            if (arr.Count < startIdx + 8243) 
                return -1;
            int num = arr[startIdx] + arr[startIdx + 1] * 256;
            if (arr[startIdx + 2] != 0) num += arr[startIdx + 2] * 65536;
            if (arr[startIdx + 3] != 0) num += arr[startIdx + 3] * 16777216;
            return num;
        }


        private Channel<PacketData> packetOutChannel = null;

        private ChannelReader<byte[]> rawByteInChannel = null;

        /// <summary>
        /// Sets the input and output channel to a new values. Fails if processing loop is running.
        /// </summary>
        /// <param name="packetChannel">New output channel.</param>
        /// <param name="rawByteChannel">New input channel.</param>
        /// <returns>Whether new channels were successfully set.</returns>
        public bool SetChannel(Channel<PacketData> packetChannel, ChannelReader<byte[]> rawByteChannel)
        {
            if (loopParsing.Wait(0))
            {
                try
                {
                    try
                    {
                        packetOutChannel = packetChannel;
                        rawByteInChannel = rawByteChannel;
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                finally
                {
                    loopParsing.Release();
                }
            }
            else
            {
                LogMessage("Failed to set channel: loop running.");
                return false;
            }

        }
    }
}
