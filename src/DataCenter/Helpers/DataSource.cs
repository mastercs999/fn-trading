using DataCenter;
using DataCenter.Data;
using DataCenter.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter.Helpers
{
    internal abstract class DataSource
    {
        protected string Folder;
        protected bool Reload;
        protected string SerializedFile;

        public DataSource()
        {
            Init(false);
        }
        public DataSource(bool reload)
        {
            Init(reload);
        }
        private void Init(bool reload)
        {
            string ns = this.GetType().Namespace;
            Folder = ns.Substring(ns.IndexOf('.') + 1);

            Reload = reload;
            SerializedFile = Path.Combine(Folder, "_Serialized.bin");
        }

        public abstract Task Prepare(DataContainer dataContainer);

        protected void DrawHeader()
        {
            // All data sources
            Type[] allDs = this.GetType().Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(DataSource))).ToArray();

            // Number of current source
            string ns = this.GetType().Namespace;
            int order = int.Parse(ns.Substring(ns.IndexOf('_') + 1, ns.LastIndexOf('_') - ns.IndexOf('_') - 1));

            // Write headers
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write((order + "/" + allDs.Length).PadLeft(allDs.Length.ToString().Length * 2 + 1) + " ");
            Console.WriteLine(this.GetType().Name);
            Console.Write(new String('-', 80));
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
