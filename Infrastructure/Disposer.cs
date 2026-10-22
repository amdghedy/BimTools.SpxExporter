using System;

namespace BimTools.SpxExporter.Infrastructure
{
    internal static class Disposer
    {
        public static void Release(object instance)
        {
            IDisposable disposable = instance as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
