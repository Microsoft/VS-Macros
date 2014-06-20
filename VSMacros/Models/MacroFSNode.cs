﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Windows;

namespace VSMacros.Model
{
    public sealed class MacroFSNode : INotifyPropertyChanged
    {
        public bool IsDirectory;
        
        private string full_path;
        private bool isEditing;

        private MacroFSNode parent;
        private ObservableCollection<MacroFSNode> children;

        public event PropertyChangedEventHandler PropertyChanged;

        public MacroFSNode(string path, MacroFSNode parent = null)
        {
            FullPath = path;
            this.IsDirectory = ((File.GetAttributes(this.FullPath) & FileAttributes.Directory) == FileAttributes.Directory);
            this.isEditing = false;
            this.parent = parent;
        }

        public string FullPath
        {
            get { return this.full_path; }
            set 
            { 
                this.full_path = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Name
        {
            get
            {
                string path = Path.GetFileNameWithoutExtension(this.FullPath);

                if (string.IsNullOrWhiteSpace(path))
                    return this.FullPath;

                return path;
            }
            set
            {
                string oldFullPath = FullPath;
                string newFullPath = Path.Combine(Path.GetDirectoryName(FullPath), value + Path.GetExtension(FullPath));

                try
                {
                     // Update file system
                    if (this.IsDirectory)
                        Directory.Move(oldFullPath, newFullPath);
                    else
                        File.Move(oldFullPath, newFullPath);

                    // Update object
                    FullPath = newFullPath;

                    // Notify the property change
                    NotifyPropertyChanged("Name");
                }
                catch(Exception e)
                {
                    if (e.Message != null)
                        MessageBox.Show(e.Message);
                }
            }
        }

        public bool IsEditing
        {
            get { return !this.isEditing; }
            set
            {
                this.isEditing = value;
                NotifyPropertyChanged("IsEditing");
            }
        }

        public string Icon
        {
            get
            {
                if (this.IsDirectory)
                    return @"C:\Users\t-jusdom\Downloads\folder.png";
                else
                    return @"C:\Users\t-jusdom\Downloads\js.png";
            }
        }

        public IEnumerable<MacroFSNode> Children
        {
            get
            {
                if (!this.IsDirectory)
                {
                    return Enumerable.Empty<MacroFSNode>();
                }

                if (this.children == null)
                {
                    this.children = GetChildNodes();
                }

                return this.children;
            }
        }

        // Returns a list of children of the current node
        private ObservableCollection<MacroFSNode> GetChildNodes()
        {
            var file = from childFile in Directory.GetFiles(this.FullPath)
                       where Path.GetExtension(childFile) == ".js"
                       orderby childFile
                       select childFile;

            var directories = from childDirectory in Directory.GetDirectories(this.FullPath)
                              orderby childDirectory
                              select childDirectory;

            return new ObservableCollection<MacroFSNode>(directories.Union(file)
                    .Select((item) => new MacroFSNode(item, this)));
        }

        #region Context Menu
        public void Delete()
        {
            this.parent.children.Remove(this);
        }

        public void EnableEdit()
        {
            IsEditing = true;
        }

        public void Refresh()
        {
            
        }

        #endregion

        // Create the OnPropertyChanged method to raise the event 
        private void NotifyPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}