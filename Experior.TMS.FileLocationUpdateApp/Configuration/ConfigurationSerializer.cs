using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Experior.TMS.FileLocationUpdateApp.Configuration
{
    public class ConfigurationSerializer
    {
        public T Deserialize<T>(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return default(T);
            }
            
            XmlSerializer deserializer = new XmlSerializer(typeof(T));

            TextReader reader = new StreamReader(fileName);

            object obj = deserializer.Deserialize(reader);

            reader.Close();

            return (T)obj;
        }

        public void Serialize<T>(T data, string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (var stream = File.OpenWrite(fileName))
            {
                serializer.Serialize(stream, data);
            }
        }
    }
}