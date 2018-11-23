using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GCV_Tester.IO
{
    public class DirectoryPath
    {
        private DirectoryInfo directoryinfo = null;

        #region Constructors

        public DirectoryPath(DirectoryInfo directoryinfo)
        {
            this.directoryinfo = directoryinfo;
        }

        public DirectoryPath(string fullname) : this(fullname, false) { }

        public DirectoryPath(string fullname, bool create)
        {
            if (fullname.Contains("{SYSDATE") || fullname.Contains("{APPPATH}"))
            {
                fullname = fullname.Replace("{SYSDATE}", FilePaths.FileNameTimeStamp);

                #region Handle {SYSDATE|date_format} notation
                Regex regex = new Regex(@"\{SYSDATE\|[^\}]+\}");
                fullname = regex.Replace(fullname, new MatchEvaluator(delegate (Match match)
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
                fullname = fullname.Replace("{APPPATH}", FilePaths.AppRoot.FullName);
            }

            this.directoryinfo = new DirectoryInfo(fullname);
            if (create && (!directoryinfo.Exists))
            {
                this.directoryinfo.Create();
                this.directoryinfo.Refresh();
            }
        }

        public DirectoryPath(DirectoryPath directory, string subfolder)
        {
            this.directoryinfo = new DirectoryInfo(Path.Combine(directory.FullName, subfolder));
        }

        public DirectoryPath(DirectoryPath directory, string subfolder, bool create)
        {
            this.directoryinfo = new DirectoryInfo(Path.Combine(directory.FullName, subfolder));
            if (create && (!directoryinfo.Exists))
            {
                this.directoryinfo.Create();
                this.directoryinfo.Refresh();
            }
        }

        #endregion

        #region Properties

        public DirectoryInfo DirectoryInfo
        {
            get
            {
                return this.directoryinfo;
            }
        }

        public string FullName
        {
            get
            {
                return directoryinfo.FullName;
            }
        }

        public string Name
        {
            get
            {
                return directoryinfo.Name;
            }
        }

        public bool Exists
        {
            get
            {
                return directoryinfo.Exists;
            }
        }

        public long Size
        {
            get
            {
                long file_size = 0;
                foreach (DirectoryPath sub_directory in this.SubDirectories())
                {
                    file_size += sub_directory.Size;
                }
                foreach (FilePath file in this.GetFiles())
                {
                    file_size += file.Length;
                }
                return file_size;
            }
        }

        public override string ToString()
        {
            return FullName;
        }

        #endregion

        #region Public Methods

        public void Create()
        {
            directoryinfo.Create();
        }

        public void Delete()
        {
            directoryinfo.Delete();
        }
        public void Delete(bool recursive)
        {
            directoryinfo.Delete(recursive);
        }
        public void Delete(bool recursive, bool delete_read_only)
        {
            if (delete_read_only)
            {
                EnsureAllFilesNotReadOnly(recursive);
            }
            Delete(recursive);
        }

        public void EnsureAllFilesNotReadOnly(bool recursive)
        {
            foreach (FilePath file in GetFiles())
            {
                file.EnsureNotReadOnly();
            }
            if (recursive)
            {
                foreach (DirectoryPath directory in SubDirectories())
                {
                    directory.EnsureAllFilesNotReadOnly(recursive);
                }
            }
        }

        public FilePath[] GetFiles()
        {
            List<FilePath> filepaths = new List<FilePath>();

            FileInfo[] files = directoryinfo.GetFiles();

            foreach (FileInfo file in files)
            {
                filepaths.Add(new FilePath(file));
            }

            return filepaths.ToArray();
        }

        public FilePath[] GetFiles(string searchpattern)
        {
            List<FilePath> filepaths = new List<FilePath>();

            FileInfo[] files = directoryinfo.GetFiles(searchpattern);

            foreach (FileInfo file in files)
            {
                filepaths.Add(new FilePath(file));
            }
            return filepaths.ToArray();
        }

        public DirectoryPath[] SubDirectories()
        {
            return SubDirectories(false);
        }

        public DirectoryPath[] SubDirectories(bool recursive)
        {
            List<DirectoryPath> directories = new List<DirectoryPath>();

            foreach (DirectoryInfo info in this.DirectoryInfo.GetDirectories())
            {
                DirectoryPath directory = new DirectoryPath(info);

                directories.Add(directory);

                if (recursive)
                {
                    directories.AddRange(directory.SubDirectories(recursive));
                }
            }

            return directories.ToArray();
        }
        public void Refresh()
        {
            this.directoryinfo.Refresh();
        }
        public void CopyTo(DirectoryPath directory)
        {
            DirectoryPath dest_subdir = new DirectoryPath(directory, this.Name, true);
            foreach (FilePath file in this.GetFiles())
            {
                file.CopyTo(dest_subdir);
            }
            foreach (DirectoryPath sub_dir in this.SubDirectories())
            {
                sub_dir.CopyTo(dest_subdir);
            }

        }

        public override bool Equals(object obj)
        {
            if (obj is DirectoryPath)
            {
                return string.Equals(this.FullName, ((DirectoryPath)obj).FullName, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (obj is string)
            {
                try
                {
                    return this.Equals(new DirectoryPath((string)obj));
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region Conversion Operators

        public static implicit operator string(DirectoryPath directory)
        {
            return directory.ToString();
        }

        public static implicit operator DirectoryInfo(DirectoryPath directory)
        {
            return directory.DirectoryInfo;
        }

        public static explicit operator DirectoryPath(DirectoryInfo directory)
        {
            return new DirectoryPath(directory);
        }

        #endregion
    }
}