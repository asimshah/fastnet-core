using Fastnet.Core.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Core
{
    public enum Periods
    {
        EveryMinute = 1,
        [Description("On the quarter hour at 00, 15, 30 and 45 mins")]
        QuarterHourly = 15,
        [Description("On the half hour at 00, and 30 mins")]
        HalfHourly = 30,
        [Description("Hourly, on the hour")]
        Hourly = 60,
        [Description("At 00:00, 3:00, 6:00, 9:00, 12:00, 15:00, 18:00, 21:00")]
        ThreeHourly = 3 * 60,
        [Description("At 00:00, 6:00, 12:00, 18:00")]
        SixHourly = 6 * 60,
        [Description("At 00:00, 12:00")]
        TwelveHourly = 12 * 60,
        [Description("At 00:00")]
        Daily = 24 * 60
    }
    public class Clock
    {
        //private ILogger log;
        private Action onTick;
        private int beat;// in minutes
        private string descriptor;
        public Clock(/* ILogger log, */ Action onTick, string descriptor = null)
        {
            //this.log = log;
            this.onTick = onTick;
            this.descriptor = descriptor;
        }
        public async Task StartAsync(CancellationToken token)
        {
            await StartAsync(Periods.EveryMinute, token);
        }
        public async Task StartAsync(Periods period, CancellationToken token)
        {
            try
            {
                SetBeat(period);
                var initially = SyncToRealTimeInterval();
                //log.Write($"{(string.IsNullOrWhiteSpace(descriptor)? "" : descriptor + " ")}Initial wait for {initially.TotalSeconds} seconds");
                await Task.Delay(initially, token);
                token.ThrowIfCancellationRequested();
                var next = DateTime.Now;
                while (!token.IsCancellationRequested)
                {
                    //log.Write("Clock poll");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Task.Run(onTick);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    next = next.Add(TimeSpan.FromMinutes(beat));
                    var waitFor = next - DateTime.Now;
                    await Task.Delay(waitFor, token);
                    token.ThrowIfCancellationRequested();
                }
            }
            catch { }
        }
        private void SetBeat(Periods period)
        {
            beat = (int) period;
            //switch (period)
            //{
            //    case Periods.EveryMinute:
            //        beat = TimeSpan.FromMinutes(1);
            //        break;
            //    case Periods.QuarterHourly:
            //        beat = TimeSpan.FromMinutes(15);
            //        break;
            //    case Periods.HalfHourly:
            //        beat = TimeSpan.FromMinutes(30);
            //        break;
            //    case Periods.Hourly:
            //        beat = TimeSpan.FromMinutes(60);
            //        break;
            //    case Periods.ThreeHourly:
            //        beat = TimeSpan.FromMinutes(3 * 60);
            //        break;
            //    case Periods.SixHourly:
            //        beat = TimeSpan.FromMinutes(6 * 60);
            //        break;
            //    case Periods.TwelveHourly:
            //        beat = TimeSpan.FromMinutes(12 * 60);
            //        break;
            //    case Periods.Daily:
            //        beat = TimeSpan.FromMinutes(24 * 60);
            //        break;
            //}
        }
        private TimeSpan SyncToRealTimeInterval()
        {
            var start = DateTime.Now;
            var syncTime = start.RoundUp(beat);
            return syncTime - start;
            //var now = start;// DateTime.Now;
            //var seconds = now.Second;
            //if (seconds > 0)
            //{
            //    now = now.AddMinutes(1);
            //}
            //var minutes = now.Minute;
            //var remainder = minutes % beat.TotalMinutes;
            //now = now.AddMinutes(remainder).AddSeconds(-seconds);
            ////log.Write($"start: {start.ToString("HH:mm:ss")}, minutes: {minutes.ToString()}, now: {now.ToString("HH:mm:ss")}");
            //return now - DateTime.Now;
        }
    }
}
