using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Threading;

namespace CES.AlphaScan.Base
{
    /// <summary>
    /// Contains methods for splitting a <see cref="System.Threading.Channels.Channel"/> object into multiple channels./>
    /// </summary>
    public static class ChannelSplitter
    {
        /// <summary>
        /// Reads data from an input channel and writes each read item
        /// to each of a number of new channels.
        /// </summary>
        /// <typeparam name="T">Channel data type</typeparam>
        /// <param name="inputChannel">Input channel to copy.</param>
        /// <param name="n">Number of channels to copy to.</param>
        /// <param name="token">Cancellation token for </param>
        /// <returns></returns>
        public static IList<ChannelReader<T>> CopyChannel<T>(this ChannelReader<T> inputChannel, int n, CancellationToken token)
        {
            if (n < 1) throw new ArgumentException("Number of output channels is invalid. n= " + n);

            var newChannels = new Channel<T>[n];

            for (int i = 0; i < n; i++)
                newChannels[i] = Channel.CreateUnbounded<T>();

            // Stop channel spliiter when cancelled or channel completed.
            CancellationTokenSource channelCompleted = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, channelCompleted.Token);
            CancellationToken stopOrCompleteToken = linkedCts.Token;

            Task.Run(async () =>
            {
                while (!stopOrCompleteToken.IsCancellationRequested)
                {
                    T val = default;

                    try
                    {
                        val = await inputChannel.ReadAsync(stopOrCompleteToken);

                        foreach (Channel<T> ch in newChannels)
                        {
                            ch.Writer.TryWrite(val);
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            });

            Task.Run(async () =>
            {
                await inputChannel.Completion;
                channelCompleted.Cancel();
                foreach (Channel<T> ch in newChannels)
                {
                    ch.Writer.Complete();
                }
            });

            return newChannels.Select(ch => ch.Reader).ToArray();
        }
    }
}
