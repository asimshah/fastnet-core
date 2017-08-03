using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core.Indus
{
    public enum Lifetime
    {
        Transient,
        Singleton
    }
    internal class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public Func<object> FactoryMethod { get; set; }
        public Lifetime Lifetime { get; set; }
        public object Instance { get; set; }
    }
    public class ServiceContainer
    {
        private static ServiceContainer container;
        private readonly object _sync = new object();
        private ConcurrentDictionary<Type, ServiceDescriptor> register;
        private ConcurrentDictionary<Type, object> genericInstances;
        //private ILogger logger;
        internal ServiceContainer()
        {
            //logger = new Logger<ServiceContainer>();
            register = new ConcurrentDictionary<Type, ServiceDescriptor>();
            genericInstances = new ConcurrentDictionary<Type, object>();
        }
        public static ServiceContainer GetContainer()
        {
            if (container == null)
            {
                container = new ServiceContainer();
            }
            return container;
        }
        public ServiceContainer Register<T>()
        {
            return Register(typeof(T));
        }
        public ServiceContainer Register<T>(Func<T> factory)
        {
            return Register<T>(factory, Lifetime.Transient);
        }
        public ServiceContainer Register<T>(Lifetime lifetime)
        {
            return Register(typeof(T), lifetime);
        }
        public ServiceContainer Register<T>(Func<T> factory, Lifetime lifetime)
        {
            Type serviceType = typeof(T);
            lock (_sync)
            {
                if (!IsRegistered(serviceType))
                {
                    var serviceDescriptor = new ServiceDescriptor
                    {
                        ServiceType = serviceType,
                        //ImplementationType = implementationType,
                        FactoryMethod = (Func<object>) (object)factory,
                        Lifetime = lifetime,
                        Instance = null
                    };
                    register.TryAdd(serviceType, serviceDescriptor);
                }
                else
                {
                    //logger.Write(LogLevel.Error, $"Service type {serviceType.Name} already registered");
                    throw new Exception($"Service type {serviceType.Name} already registered");
                }
            }
            return this;
        }
        public ServiceContainer Register(Type type)
        {
            return Register(type, type);
        }
        public ServiceContainer Register(Type type, Lifetime lifetime)
        {
            return Register(type, type, lifetime);
        }
        /// <summary>
        /// default lifetime is Transient
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <returns></returns>
        public ServiceContainer Register<TService, TImplementation>()
        {
            return Register(typeof(TService), typeof(TImplementation));
        }
        /// <summary>
        /// default lifetime is Transient
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public ServiceContainer Register(Type serviceType, Type implementationType)
        {
            return Register(serviceType, implementationType, Lifetime.Transient);
        }
        public ServiceContainer Register<TService, TImplementation>(Lifetime lifetime)
        {
            return Register(typeof(TService), typeof(TImplementation), lifetime);
        }
        public ServiceContainer Register(Type serviceType, Type implementationType, Lifetime lifetime)
        {
            lock (_sync)
            {
                if (!IsRegistered(serviceType))
                {
                    var serviceDescriptor = new ServiceDescriptor { ServiceType = serviceType, ImplementationType = implementationType, Lifetime = lifetime, Instance = null };
                    register.TryAdd(serviceType, serviceDescriptor);
                }
                else
                {
                    //logger.Write(LogLevel.Error, $"Service type {serviceType.Name} already registered");
                    throw new Exception($"Service type {serviceType.Name} already registered");
                }
            }
            return this;
        }
        public TService GetInstance<TService>() where TService : class
        {
            return GetInstance(typeof(TService)) as TService;
        }
        public object GetInstance(Type serviceType)
        {
            if (serviceType.GetTypeInfo().IsGenericType)
            {
                return GetGenericServiceInstance(serviceType) ?? CreateGenericInstance(serviceType);
                //return CreateInstance(serviceType);
            }
            object s = GetServiceInstance(serviceType) ?? CreateInstance(serviceType);
            //logger.Write($"Instance type {s?.GetType().Name} found");
            return s;// GetServiceInstance(serviceType) ?? CreateInstance(serviceType);
        }
        /// <summary>
        /// Injects property values if the property type is registered and is null
        /// </summary>
        /// <param name="instance"></param>
        public void InjectProperties(object instance)
        {
            Type type = instance.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.GetValue(instance) == null)
                {
                    Type propertyType = prop.PropertyType;
                    //if (register.ContainsKey(propertyType))
                    if (IsRegistered(propertyType))
                    {
                        var val = GetInstance(propertyType);
                        prop.SetValue(instance, val);
                    }
                }
            }
        }
        private object GetServiceInstance(Type serviceType)
        {
            object instance = null;
            lock (_sync)
            {
                if (IsRegistered(serviceType))
                {
                    var serviceDescriptor = register[serviceType];
                    if (serviceDescriptor.Lifetime == Lifetime.Singleton)
                    {
                        instance = serviceDescriptor.Instance;
                    }
                }
                else
                {
                    //logger.Write(LogLevel.Error, $"Type {serviceType.Name} is not registered");
                    throw new Exception($"Type {serviceType.Name} is not registered");
                }
            }
            return instance;
        }
        private object GetGenericServiceInstance(Type serviceType)
        {
            if (IsRegistered(serviceType))
            {
                var registeredType = serviceType.GetGenericTypeDefinition();
                var serviceDescriptor = register[registeredType];
                if (serviceDescriptor.Lifetime == Lifetime.Singleton)
                {
                    if (genericInstances.ContainsKey(serviceType))
                    {
                        return genericInstances[serviceType];
                    }
                }
                return null;
            }
            else
            {
                //logger.Write(LogLevel.Error, $"Type {serviceType.Name} is not registered");
                throw new Exception($"Type {serviceType.Name} is not registered");
            }
        }
        private bool TryGetInstance(Type serviceType, out object instance)
        {
            instance = null;
            //if (register.ContainsKey(serviceType))
            if (IsRegistered(serviceType))
            {
                instance = GetInstance(serviceType);
                return true;
            }
            return false;
        }
        private object CreateInstance(Type serviceType)
        {
            lock (_sync)
            {
                //if (register.ContainsKey(serviceType))
                if (IsRegistered(serviceType))
                {
                    object instance = null;
                    var serviceDescriptor = register[serviceType];
                    if (serviceDescriptor.FactoryMethod == null)
                    {
                        ConstructorInfo ci = GetConstructor(serviceDescriptor.ImplementationType);
                        instance = CreateTypeInstance(serviceDescriptor.ImplementationType, ci);
                    }
                    else
                    {
                        instance = serviceDescriptor.FactoryMethod();
                    }
                    if (serviceDescriptor.Lifetime == Lifetime.Singleton)
                    {
                        serviceDescriptor.Instance = instance;
                    }
                    return instance;
                }
                else
                {
                    //logger.Write(LogLevel.Error, $"Type {serviceType.Name} is not registered");
                    throw new Exception($"Type {serviceType.Name} is not registered");
                }
            }
        }
        private object CreateGenericInstance(Type serviceType)
        {
            lock (_sync)
            {
                //if (register.ContainsKey(registeredType))
                if (IsRegistered(serviceType))
                {
                    var registeredType = serviceType.GetGenericTypeDefinition();
                    var serviceDescriptor = register[registeredType];
                    var args = serviceType.GetTypeInfo().GenericTypeArguments;
                    Type resultType = serviceDescriptor.ImplementationType.MakeGenericType(args);
                    ConstructorInfo ci = GetConstructor(resultType);
                    object instance = CreateTypeInstance(resultType, ci);
                    if (serviceDescriptor.Lifetime == Lifetime.Singleton)
                    {
                        genericInstances[serviceType] = instance;
                    }
                    return instance;
                }
                else
                {
                    //logger.Write(LogLevel.Error, $"Type {serviceType.GetGenericTypeDefinition().Name} is not registered");
                    throw new Exception($"Type {serviceType.GetGenericTypeDefinition().Name} is not registered");
                }
            }
        }
        private object CreateTypeInstance(Type type, ConstructorInfo ci)
        {
            var parameters = GetParameters(ci);
            object instance = Activator.CreateInstance(type, parameters);
            return instance;
        }
        private object[] GetParameters(ConstructorInfo ci)
        {
            List<object> parameters = new List<object>();
            foreach (var parameterInfo in ci.GetParameters())
            {
                Type parameterType = parameterInfo.ParameterType;
                object parameterInstance = null;
                if (!TryGetInstance(parameterType, out parameterInstance))
                {
                    //Debug.WriteLine($"Parameter type {parameterType.Name} not registered, null injected");
                }
                if (parameterInstance != null)
                {
                    //logger.Write(LogLevel.Debug, $"Parameter type {parameterInstance.GetType().Name} found");
                }
                parameters.Add(parameterInstance);

            }
            return parameters.ToArray();
        }
        private ConstructorInfo GetConstructor(Type type)
        {
            var constructors = type.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic).ToArray();
            if (constructors.Length == 0)
            {
                //logger.Write(LogLevel.Error, $"Missing public constructor for Type: {type.FullName}");
                throw new InvalidOperationException($"Missing public constructor for Type: {type.FullName}");
            }
            if (constructors.Length == 1)
            {
                return constructors[0];
            }
            // get the constructor with the most arguments
            return constructors.OrderByDescending(c => c.GetParameters().Count()).First();
        }
        private bool IsRegistered(Type type)
        {
            var registeredType = type.GetTypeInfo().IsGenericType ? type.GetGenericTypeDefinition() : type;
            return register.ContainsKey(registeredType);
        }
        private ServiceDescriptor GetServiceDescriptor(Type type)
        {
            var registeredType = type.GetTypeInfo().IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (register.ContainsKey(registeredType))
            {
                return register[registeredType];
            }
            else
            {
                return null;
            }
        }
    }
}
