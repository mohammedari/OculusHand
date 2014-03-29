using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ConfigUtil;

namespace OculusHand.Models
{
    public class Util
    {
        const string _configFileName = "app.config";
        ConfigManager<MyConfigParameter> _config = null;

        Util() {}

        static Util _instance = null;
        static Util getInstance()
        {
            if (_instance == null)
                _instance = new Util();

            return _instance;
        }

        //////////////////////////////////////////////////////////

        /// <summary>
        /// Get configuration manager class which manages application configuration parameters.
        /// </summary>
        /// <returns>configuration manager </returns>
        public static ConfigManager<MyConfigParameter> GetConfigManager()
        {
            var instance = getInstance();
            if (instance._config == null)
                instance._config = new ConfigManager<MyConfigParameter>(_configFileName);

            return instance._config;
        }
    }
}
