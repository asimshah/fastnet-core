using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core.Web
{
    public static class EF6Extensions
    {
        public static void AddEf6ConnectionSupport(this IServiceCollection services)
        {
            services.AddSingleton<EF6ConnectionStringManager>(EF6ConnectionStringManager.Instance);
        }
    }
    public sealed class EF6ConnectionStringManager
    {
        private Dictionary<string, string> connectionStrings = new Dictionary<string, string>();
        private static readonly EF6ConnectionStringManager instance = new EF6ConnectionStringManager();
        static EF6ConnectionStringManager()
        {
        }
        public static EF6ConnectionStringManager Instance
        {
            get { return instance; }
        }
        public void AddConnectionString(string name, string cs)
        {
            if(connectionStrings.ContainsKey(name))
            {
                connectionStrings[name] = cs;
            }
            else
            {
                connectionStrings.Add(name, cs);
            }
        }
        public string GetConnectionString(string name)
        {
            if(connectionStrings.ContainsKey(name))
            {
                return connectionStrings[name];
            }
            return null;
        }
    }
}
