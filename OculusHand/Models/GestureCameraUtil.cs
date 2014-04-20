using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusHand.Models
{
    public class GestureCameraUtil
    {
        PXCMSession _session = null;
        PXCMAccelerator _accelerator = null;

        GestureCameraUtil() {}
        ~GestureCameraUtil()
        {
            if (_accelerator != null)
                _accelerator.Dispose();

            if (_session != null)
                _session.Dispose();
        }

        static GestureCameraUtil _instance = null;
        static GestureCameraUtil getInstance()
        {
            if (_instance == null)
                _instance = new GestureCameraUtil();

            return _instance;
        }

        /// <summary>
        /// Get PXC session which is required to communicate with PerC SDK.
        /// If session exists already, this method does not create new instance but return it.
        /// </summary>
        /// <returns>PXC session</returns>
        public static PXCMSession GetSession()
        {
            var instance = getInstance();
            if (instance._session == null)
                Assert(PXCMSession.CreateInstance(out instance._session), "Failed to create PXC session.");

            return instance._session;
        }

        /// <summary>
        /// Check the PXC status, and if it has errors, throw an exception.
        /// </summary>
        /// <param name="status">PXC status</param>
        /// <param name="message">Error message which displayed when error occured</param>
        public static void Assert(pxcmStatus status, string message)
        {
            if (HasError(status))
                throw new GestureCameraException(message);
        }

        /// <summary>
        /// Check if the PXC status has an error or not.
        /// </summary>
        /// <param name="status">PXC status</param>
        /// <returns>Result which is true if the status has an error</returns>
        public static bool HasError(pxcmStatus status)
        {
            return status < pxcmStatus.PXCM_STATUS_NO_ERROR;
        }
    }
}
