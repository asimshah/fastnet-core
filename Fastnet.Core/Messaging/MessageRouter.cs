using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core
{
    public class MessageRouter
    {
        private object sentinel = new object();
        private Dictionary<Type, List<Action<MessageBase>>> subscriptions;
        public MessageRouter()
        {
            subscriptions = new Dictionary<Type, List<Action<MessageBase>>>();
        }
        public void AddSubscription<T>(Action<MessageBase> handler) where T : MessageBase
        {
            lock (sentinel)
            {
                if (!subscriptions.ContainsKey((typeof(T))))
                {
                    subscriptions.Add(typeof(T), new List<Action<MessageBase>>());
                }
                subscriptions[typeof(T)].Add(handler);
            }
        }
        public void Route(MessageBase message)
        {
            Type t = message.GetType();
            IEnumerable<Action<MessageBase>> handlerList = null;
            lock (sentinel)
            {
                if (subscriptions.ContainsKey(t))
                {
                    handlerList = subscriptions[t];
                }
            }
            if (handlerList != null)
            {
                foreach (var h in handlerList)
                {
                    h(message);
                }
            }
        }
    }
}
