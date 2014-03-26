using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConfigUtil
{
    /// <summary>
    /// Class which wraps configuration parameters.
    /// </summary>
    [Serializable]
    public abstract class ConfigParameter : ICloneable
    {
        /// <summary>
        /// Initialize parameters with default values.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Returns deep copy of this instance.
        /// </summary>
        /// <returns>deep copy</returns>
        public abstract object Clone();
    }
}
