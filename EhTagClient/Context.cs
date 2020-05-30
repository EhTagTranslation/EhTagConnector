using System;
using System.Collections.Generic;
using System.Text;

namespace EhTagClient
{
    static class Context
    {
        public static Database Database { get; set; }
        public static Namespace Namespace { get; set; }
        public static string Raw { get; set; }
        public static Record Record { get; set; }
    }
}
