using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core.Indus
{
    public class NotifyingObject : INotifyPropertyChanged
    {
        private Dictionary<string, object> propertyValues = new Dictionary<string, object>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool Set<T>(T value, [CallerMemberName] string propertyName = "")
        {
            bool result = false;
            T oldValue = default(T);
            if (propertyValues.ContainsKey(propertyName))
            {
                oldValue = (T)propertyValues[propertyName];
            }
            else
            {
                propertyValues.Add(propertyName, oldValue);
            }
            if (!EqualityComparer<T>.Default.Equals(oldValue, value))
            //if (!Equals(oldValue, value))
            {
                propertyValues[propertyName] = value;
                OnPropertyChanged(propertyName);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                result = true;
            }
            return result;
        }
        protected T Get<T>([CallerMemberName] string name = "")
        {
            if (propertyValues.ContainsKey(name))
            {
                return (T)propertyValues[name];
            }
            else
            {
                return default(T);
            }
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {

        }
    }
}
