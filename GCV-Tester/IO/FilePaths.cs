using System;
using System.IO;
using System.Text;

namespace GCV_Tester.IO
{
    public class FilePaths
    {
        #region Generic Paths

        /// <summary>
        /// The current application's working directory: c:\Program Files\TELMAGEN
        /// </summary>
        /// <example>c:\Program Files\TELMAGEN</example>
        public static DirectoryPath AppRoot
        {
            get
            {
                return new DirectoryPath(AppDomain.CurrentDomain.BaseDirectory);
            }
        }
        #endregion

        #region TELMAGEN Paths
        /// <summary>
        /// c:\Program Files\TELMAGEN\Projects
        /// </summary>
        public static DirectoryPath Projects
        {
            get
            {
                DirectoryPath path = new DirectoryPath(AppRoot, "Projects");
                path.Create();
                return path;
            }
        }

        #endregion

        #region Windows Special Paths

        /// <summary>
        /// C:\Documents and Settings\{USERNAME}\Desktop
        /// </summary>
        public static DirectoryPath Desktop
        {
            get
            {
                return new DirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            }
        }

        /// <summary>
        /// C:\Windows\Startup
        /// </summary>
        public static DirectoryPath Startup
        {
            get
            {
                return new DirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
            }
        }

        #endregion

        #region Helper Methods

        public static string FileNameTimeStamp
        {
            get
            {
                return DateTime.Now.ToString("yyyy-MM-dd.HH`mm`ss");
            }
        }

        public static FilePath TempFile
        {
            get
            {
                FilePath retval = new FilePath(System.IO.Path.GetTempFileName());
                if (retval.Exists)
                {
                    // the .NET helper function will create the file first,
                    // but this method assumes it doesn't exist, so it is immediately
                    // deleted
                    retval.Delete();
                }
                return retval;
            }
        }

        #endregion
    }
}
