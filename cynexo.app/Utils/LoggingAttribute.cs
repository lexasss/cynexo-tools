using System;
using System.Diagnostics;

namespace Cynexo.App.Utils
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class LoggingAttribute : Attribute
    {
        public LoggingAttribute()
        {
            Debug.WriteLine($"RESULT: {this.TypeId}");
        }
    }
}
