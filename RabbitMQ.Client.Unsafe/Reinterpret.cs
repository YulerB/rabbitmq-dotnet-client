using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Client.Unsafe
{
    public static class Reinterpret
    {
        public static unsafe TDest Cast<TSource, TDest>(TSource source)
        {
            var sourceRef = __makeref(source);
            var dest = default(TDest);
            var destRef = __makeref(dest);
            *(IntPtr*)&destRef = *(IntPtr*)&sourceRef;
            return __refvalue(destRef, TDest);
        }
    }
}
