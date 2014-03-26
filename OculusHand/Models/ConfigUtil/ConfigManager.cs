using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml.Serialization;

namespace ConfigUtil
{
    /// <summary>
    /// Class which manages configuration parameters.
    /// This class provide save/load method of configuration parameter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConfigManager<T>
        where T : ConfigParameter, new()
    {
        //////////////////////////////////////////////////
        #region Properties
        /// <summary>
        /// Name of configuration file
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Configuration parameters which this class manages
        /// </summary>
        public T Parameters { get; private set; }
        #endregion

        //////////////////////////////////////////////////
        #region Public methods
        /// <summary>
        /// Create instance with file name of configuration file.
        /// If a configuration file exists, read the file load parameters.
        /// Otherwise, parameters are initialized with default values.
        /// </summary>
        /// <param name="fileName"></param>
        public ConfigManager(string fileName)
        {
            FileName = fileName;

            if (File.Exists(fileName))
            {
                try
                {
                    Load();
                }
                catch (ConfigException)
                {
                    create();
                }
            }
            else
                create();
        }

        /// <summary>
        /// Save parameters to configuration file.
        /// </summary>
        public void Save()
        {
            var ser = new XmlSerializer(typeof(T));
            using (FileStream fs = new FileStream(FileName, FileMode.Create))
            {
                ser.Serialize(fs, Parameters);
            }
        }

        /// <summary>
        /// Load parameters from configuration file.
        /// </summary>
        public void Load()
        {
            var ser = new XmlSerializer(typeof(T));
            using (FileStream fs = new FileStream(FileName, FileMode.Open))
            {
                try
                {
                    Parameters = ser.Deserialize(fs) as T;
                }
                catch (Exception e)
                {
                    throw new ConfigException("Failed to load configuration file.", e);
                }
            }
        }
        #endregion

        //////////////////////////////////////////////////
        #region Private methods
        void create()
        {
            Parameters = new T();
            Parameters.Initialize();

            Save();
        }
        #endregion
    }
}
