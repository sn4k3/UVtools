/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UVtools.WPF.Structures
{
    public class RecentFiles : IList<string>
    {
        #region Properties
        /// <summary>
        /// Default filepath for store
        /// </summary>
        private static string FilePath => Path.Combine(UserSettings.SettingsFolder, "recentfiles.dat");

        private readonly List<string> _files = new();

        public byte MaxEntries { get; set; } = 40;

        #endregion

        #region Singleton

        private static Lazy<RecentFiles> _instanceHolder =
            new(() => new RecentFiles());

        /// <summary>
        /// Instance of <see cref="UserSettings"/> (singleton)
        /// </summary>
        public static RecentFiles Instance => _instanceHolder.Value;

        //public static List<Operation> Operations => _instance.Operations;
        #endregion

        #region Constructor

        private RecentFiles()
        { }

        #endregion

        #region Static Methods
        /// <summary>
        /// Clear all files
        /// </summary>
        /// <param name="save">True to save settings on file, otherwise false</param>
        public static void ClearFiles(bool save = true)
        {
            Instance.Clear();
            if (save) Save();
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            Instance.Clear();

            try
            {
                using var tr = new StreamReader(FilePath);

                string path;
                while ((path = tr.ReadLine()) is not null)
                {
                    if(string.IsNullOrWhiteSpace(path)) continue;

                    try
                    {
                        Path.GetFullPath(path);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }

                    Instance.Add(path);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public static void Save()
        {
            try
            {
                using var tw = new StreamWriter(FilePath);

                foreach (var file in Instance)
                {
                    tw.WriteLine(file);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public static int PurgenNonExistingFiles(bool save = true)
        {
            Load();
            var count = Instance.RemoveAll(s => !File.Exists(s));
            if(save && count > 0) Save();
            return count;
        }

        #endregion

        #region List Implementation
        public IEnumerator<string> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_files).GetEnumerator();
        }

        public void Add(string item)
        {
            _files.RemoveAll(s => s == item);
            if (Count >= MaxEntries) return;
            _files.Add(item);
        }

        public void Clear()
        {
            _files.Clear();
        }

        public bool Contains(string item)
        {
            return _files.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _files.CopyTo(array, arrayIndex);
        }

        public bool Remove(string item)
        {
            return _files.Remove(item);
        }

        public int Count => _files.Count;

        public bool IsReadOnly => ((ICollection<string>)_files).IsReadOnly;

        public int IndexOf(string item)
        {
            return _files.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _files.RemoveAll(s => s == item);
            _files.Insert(index, item);
            while (Count > MaxEntries)
            {
                RemoveAt(Count-1);
            }
        }

        public void RemoveAt(int index)
        {
            _files.RemoveAt(index);
        }

        public int RemoveAll(Predicate<string> match) => _files.RemoveAll(match);

        public string this[int index]
        {
            get => _files[index];
            set => _files[index] = value;
        }
        #endregion
    }
}
