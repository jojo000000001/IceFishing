using System;
using System.Collections.Generic;

namespace IceFishing.Core
{
    /// <summary>
    /// 轻量定位器，只在启动时注册 EventBus / Profile。
    /// 不要用它代替构造函数注入；测试时可 Clear 后重新注册假实现。
    /// </summary>
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T service)
        {
            Services[typeof(T)] = service;
        }

        public static T Get<T>()
        {
            return (T)Services[typeof(T)];
        }

        public static bool TryGet<T>(out T service)
        {
            if (Services.TryGetValue(typeof(T), out var boxed))
            {
                service = (T)boxed;
                return true;
            }

            service = default;
            return false;
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
