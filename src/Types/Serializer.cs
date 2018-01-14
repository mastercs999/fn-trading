using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Types
{
    public class Serializer
    {
        public static void Serialize(string file, object graph)
        {
            using (Stream stream = File.Open(file, FileMode.Create))
            {

                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, graph);
            }
        }
        public static object Deserialize(string file)
        {
            using (Stream stream = File.Open(file, FileMode.Open))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return bformatter.Deserialize(stream);
            }
        }
    }
}
