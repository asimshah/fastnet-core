using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core.Web
{
    public class TimerMonitor
    {
        private int callbackCount;
        public bool Enable { get; set; }
        public int CallbackCount
        {
            get { return callbackCount; }
            private set
            {
                if (callbackCount >= 99999)
                {
                    callbackCount = 0;
                }
                else
                {
                    callbackCount = value;
                }
            }
        }
        public void BumpCallbackCount(object state)
        {
            this.State = state;
            CallbackCount++;
            if (ActionCondition != null)
            {
                if (ActionCondition.Invoke(CallbackCount))
                {
                    Action?.Invoke(State);
                }
            }
        }
        public Action<object> Action { get; set; }
        public Func<int, bool> ActionCondition { get; set; }
        public object State { get; private set; }
    }
}
