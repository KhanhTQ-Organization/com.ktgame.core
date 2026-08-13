using System;

namespace com.ktgame.core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ServiceAttribute : Attribute
    {
        public Type ServiceType { get; private set; }
        public string PrefabPath { get; private set; }

        public ServiceAttribute(Type serviceType, string prefabPath = "")
        {
            ServiceType = serviceType;
            PrefabPath = prefabPath;
        }
    }
}