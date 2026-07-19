using System;
using System.IO;

namespace Mz.Logging.SpaceEngineers
{
    /// <summary>
    /// Opens writers in Space Engineers storage locations.
    /// </summary>
    public interface IStorageWriterFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="callingType"></param>
        /// <returns></returns>
        TextWriter OpenLocal(
            string fileName,
            Type callingType
        );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="callingType"></param>
        /// <returns></returns>
        TextWriter OpenWorld(
            string fileName,
            Type callingType
        );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        TextWriter OpenGlobal(
            string fileName
        );
    }
}