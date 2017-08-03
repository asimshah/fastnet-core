using System;
using System.Threading;

namespace Fastnet.Core.Logging
{
    public class RollingFileLogScope
    {
        private readonly string _name;
        private readonly object _state;

        internal RollingFileLogScope(string name, object state)
        {
            _name = name;
            _state = state;
        }

        public RollingFileLogScope Parent { get; private set; }

        private static AsyncLocal<RollingFileLogScope> _value = new AsyncLocal<RollingFileLogScope>();
        public static RollingFileLogScope Current
        {
            set
            {
                _value.Value = value;
            }
            get
            {
                return _value.Value;
            }
        }

        public static IDisposable Push(string name, object state)
        {
            var temp = Current;
            Current = new RollingFileLogScope(name, state);
            Current.Parent = temp;

            return new DisposableScope();
        }

        public override string ToString()
        {
            return _state?.ToString();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
                Current = Current.Parent;
            }
        }
    }
}
