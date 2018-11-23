using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32.SafeHandles;

namespace GCV_Tester.IO
{
    public class FilePath
    {
        #region Static Attributes
        private static Dictionary<string, string> custom_paths = new Dictionary<string, string>();
        #endregion

        #region Static Properties
        public static string[] RegisteredCustomPaths
        {
            get
            {
                lock (custom_paths)
                {
                    string[] keys = new string[custom_paths.Keys.Count];
                    custom_paths.Keys.CopyTo(keys, 0);
                    return keys;
                }
            }
        }
        #endregion

        #region Static Methods
        public static void RegisterCustomPath(string name, string file_path)
        {
            lock (custom_paths)
            {
                if (custom_paths.ContainsKey(name))
                {
                    custom_paths[name] = file_path;
                }
                else
                {
                    custom_paths.Add(name, file_path);
                }
            }
        }
        public static void UnregisterCustomPath(string name)
        {
            lock (custom_paths)
            {
                custom_paths.Remove(name);
            }
        }
        public static string GetCustomPath(string name)
        {
            lock (custom_paths) { return custom_paths[name]; }
        }
        #endregion

        #region Class Attributes
        private FileInfo fileinfo = null;
        #endregion

        #region Constructors

        public FilePath(FileInfo fileinfo)
        {
            this.fileinfo = fileinfo;
        }

        public FilePath(string filename)
        {
            filename = filename.Replace("{SYSDATE}", FilePaths.FileNameTimeStamp);

            #region Handle {SYSDATE|date_format} notation
            Regex regex = new Regex(@"\{SYSDATE\|[^\}]+\}");
            filename = regex.Replace(filename, new MatchEvaluator(delegate (Match match)
            {
                if (match.Value.Length > 10)
                {
                    string date_format = match.Value.Substring(9, match.Value.Length - 10);
                    return DateTime.Now.ToString(date_format);
                }
                else
                {
                    return string.Empty;
                }
            }));
            #endregion

            filename = filename.Replace("{APPPATH}", FilePaths.AppRoot.FullName);

            lock (custom_paths)
            {
                foreach (string custom_path_name in custom_paths.Keys)
                {
                    filename = filename.Replace("{" + custom_path_name + "}", custom_paths[custom_path_name]);
                }
            }

            this.fileinfo = new FileInfo(filename);
        }

        public FilePath(DirectoryPath directory, string filename)
        {
            this.fileinfo = new FileInfo(Path.Combine(directory.FullName, filename));
        }

        public FilePath(DirectoryInfo directory, string filename)
        {
            this.fileinfo = new FileInfo(Path.Combine(directory.FullName, filename));
        }

        public FilePath(string directory, string filename)
        {
            this.fileinfo = new FileInfo(Path.Combine(directory, filename));
        }

        #endregion

        #region Properties

        [Obsolete("Use FullName", true)]
        public string FullPath { get { return FullName; } }
        /// <summary>
        /// The file's full name and path
        /// </summary>
        public string FullName { get { return fileinfo.FullName; } }

        /// <summary>
        /// The file's name (with the extension, but without the path)
        /// </summary>
        public string Name { get { return fileinfo.Name; } }

        public DirectoryPath Directory
        {
            get
            {
                return new DirectoryPath(fileinfo.Directory);
            }
        }

        /// <summary>
        /// The file name without the extension
        /// </summary>
        /// <example>For "test.txt" it would be "test"</example>
        public string BaseName
        {
            get
            {
                return Name.Substring(0, Name.Length - Extension.Length);
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                fileinfo = new FileInfo(new FilePath(Directory, value + Extension));
            }
        }

        /// <summary>
        /// The file's extension
        /// </summary>
        public string Extension
        {
            get
            {
                return fileinfo.Extension;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                else if (value[0] != '.')
                {
                    value = "." + value;
                }
                fileinfo = new FileInfo(new FilePath(Directory, BaseName + value));
            }
        }

        public bool QuickExists
        {
            get { return File.Exists(this.FullName); }
        }

        public bool Exists
        {
            get
            {
                return fileinfo.Exists;
            }
        }

        public long Length
        {
            get
            {
                return fileinfo.Length;
            }
        }

        public DateTime DateCreated
        {
            get { return fileinfo.CreationTime; }
        }

        public DateTime DateModified
        {
            get { return fileinfo.LastWriteTime; }
        }

        #endregion

        #region Public Methods

        public void Refresh()
        {
            this.fileinfo.Refresh();
        }

        public void CopyTo(DirectoryInfo directory)
        {
            fileinfo.CopyTo(Path.Combine(directory.FullName, fileinfo.Name));
        }
        public void CopyTo(FileInfo destination)
        {
            fileinfo.CopyTo(destination.FullName);
        }
        public void CopyTo(DirectoryInfo directory, bool overwrite)
        {
            fileinfo.CopyTo(Path.Combine(directory.FullName, fileinfo.Name), overwrite);
        }
        public void CopyTo(FileInfo destination, bool overwrite)
        {
            fileinfo.CopyTo(destination.FullName, overwrite);
        }

        public void MoveTo(DirectoryPath destination)
        {
            fileinfo.MoveTo(new FilePath(destination, this.Name));
        }
        public void MoveTo(DirectoryPath destination, bool overwrite)
        {
            // combine the directory with the file base name to get the final full path
            FilePath destination_file = new FilePath(destination, this.Name);

            if (overwrite && destination_file.Exists)
            {
                // if we want to overwrite and the file exists, then delete it
                destination_file.Delete();
            }

            // finally, move the file.  If overwrite wasnt specified and the file exists 
            // then an error will generate here.
            fileinfo.MoveTo(destination_file);

        }
        public void MoveTo(FileInfo destination)
        {
            fileinfo.MoveTo(destination.FullName);
        }
        public void MoveTo(FileInfo destination, bool overwrite)
        {
            if (overwrite && destination.Exists)
            {
                destination.Delete();
            }
            fileinfo.MoveTo(destination.FullName);
        }

        public void Delete()
        {
            fileinfo.Delete();
        }
        public void Delete(bool delete_read_only)
        {
            if (delete_read_only)
            {
                EnsureNotReadOnly();
            }
            fileinfo.Delete();
        }

        public override string ToString()
        {
            return FullName;
        }

        public override bool Equals(object obj)
        {
            if (obj is FilePath)
            {
                return this.FullName.Equals(((FilePath)obj).FullName);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }

        /// <summary>
        /// Copies this file to the given destination, and converts it to unicode format
        /// </summary>
        /// <remarks>
        /// We need to be sure to do the conversion on copying the temp file -> the original,
        /// otherwise, if we were to convert the original -> temp file, and then move the temp
        /// file back to the original's location, the original file would acquire the temp file's
        /// permissions (which may, and likely are, wrong)
        /// </remarks>
        public void Convert(Encoding encoding)
        {
            FilePath tempfile = FilePaths.TempFile;

            this.CopyTo(tempfile);
            using (StreamWriter sw = new StreamWriter(this, false, encoding))
            {
                using (StreamReader sr = new StreamReader(tempfile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        sw.WriteLine(line);
                    }
                }
            }

            tempfile.Delete();
            this.Refresh();
        }

        public void EnsureNotReadOnly()
        {
            if ((fileinfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                fileinfo.Attributes = fileinfo.Attributes ^ FileAttributes.ReadOnly;
            }
        }

        public byte[] ReadBytes()
        {
            return File.ReadAllBytes(this.FullName);
        }
        public string[] ReadAllLines()
        {

            return File.ReadAllLines(this.FullName);
        }
        public string ReadAllText()
        {
            return File.ReadAllText(this.FullName);
        }

        public void WriteAllBytes(byte[] data)
        {
            File.WriteAllBytes(this.FullName, data);
        }
        public void WriteAllLines(string[] contents)
        {
            File.WriteAllLines(this.FullName, contents);
        }
        public void WriteAllLines(string[] contents, Encoding encoding)
        {
            File.WriteAllLines(this.FullName, contents, encoding);
        }
        public void WriteAllText(string contents)
        {
            File.WriteAllText(this.FullName, contents);
        }
        public void WriteAllText(string contents, Encoding encoding)
        {
            File.WriteAllText(this.FullName, contents, encoding);
        }

        internal void Append(string text)
        {
            using (StreamWriter sw = File.AppendText(this.FullName))
            {
                sw.Write(text);
            }
        }

        public void AppendLine(string line)
        {
            using (StreamWriter sw = File.AppendText(this.FullName))
            {
                sw.WriteLine(line);
            }
        }

        #endregion

        #region Conversion Operators

        public static implicit operator FileInfo(FilePath filepath)
        {
            return filepath.fileinfo;
        }

        public static implicit operator string(FilePath filepath)
        {
            return filepath.ToString();
        }

        public static explicit operator FilePath(FileInfo fileinfo)
        {
            return new FilePath(fileinfo);
        }

        #endregion
    }
}
