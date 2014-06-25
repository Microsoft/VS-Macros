﻿using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Windows;
using GelUtilities = Microsoft.Internal.VisualStudio.PlatformUI.Utilities;
using System.Windows.Media.Imaging;

namespace VSMacros.Models
{
    public sealed class MacroFSNode : INotifyPropertyChanged
    {
        private string fullPath;
        private bool isEditable;
        private bool isExpanded;

        private MacroFSNode parent;
        private ObservableCollection<MacroFSNode> children;

        public event PropertyChangedEventHandler PropertyChanged;

        public static MacroFSNode RootNode;

        public bool IsDirectory { get; private set; }

        public MacroFSNode(string path, MacroFSNode parent = null)
        {
            this.IsDirectory = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
            this.FullPath = path;
            this.isEditable = false;
            this.parent = parent;

            if (this.parent != null)
            {
                this.isExpanded = false;
            }
            else
            {
                this.isExpanded = true;
            }
        }

        public string FullPath
        {
            get
            { 
                return this.fullPath;
            }

            set 
            { 
                this.fullPath = value;
                this.NotifyPropertyChanged("FullPath");
                this.NotifyPropertyChanged("Name");
            }
        }

        public string Name
        {
            get
            {
                string path = Path.GetFileNameWithoutExtension(this.FullPath);

                if (string.IsNullOrWhiteSpace(path))
                {
                    return this.FullPath;
                }

                return path;
            }

            set
            {
                string oldFullPath = this.FullPath;
                string newFullPath = Path.Combine(Path.GetDirectoryName(this.FullPath), value + Path.GetExtension(this.FullPath));

                try
                {
                     // Update file system
                    if (this.IsDirectory)
                    {
                        Directory.Move(oldFullPath, newFullPath);
                    }
                    else
                    {
                        File.Move(oldFullPath, newFullPath);
                    }

                    // Update object
                    this.FullPath = newFullPath;
                }
                catch(Exception e)
                {
                    if (e.Message != null)
                    {
                        MessageBox.Show(e.Message);
                    }
                }
            }
        }

        public bool IsEditable
        {
            get 
            { 
                return this.isEditable; 
            }

            set
            {
                this.isEditable = value;
                this.NotifyPropertyChanged("IsEditable");
            }
        }

        public string Icon
        {
            get
            {
                //IVsImageService imageService = (IVsImageService)((IServiceProvider)VSMacrosPackage.Current).GetService(typeof(SVsImageService));

                //if (imageService != null)
                //{
                //    IVsUIObject uiObject = imageService.GetIconForFile(Path.GetFileName(this.FullPath), __VSUIDATAFORMAT.VSDF_WPF);

                //    if (uiObject != null)
                //    {
                //        BitmapSource bitmapSource = GelUtilities.GetObjectData(uiObject) as BitmapSource;

                //        return bitmapSource.ToString();
                //    }
                //}

                //return string.Empty;

                if (this.IsDirectory)
                {
                    return @"..\Resources\folder.png";
                }
                else
                {
                    return @"..\Resources\js.png";
                }
            }
        }

        public bool IsExpanded
        {
            get
            {
                return this.isExpanded;
            }

            set
            {
                this.isExpanded = value;
            }
        }

        public ObservableCollection<MacroFSNode> Children
        {
            get
            {
                if (!this.IsDirectory)
                {
                    return null;
                }

                if (this.children == null)
                {
                    this.children = this.GetChildNodes();
                }

                return this.children;
            }
        }

        // Returns a list of children of the current node
        private ObservableCollection<MacroFSNode> GetChildNodes()
        {
            var files = from childFile in Directory.GetFiles(this.FullPath)
                       where Path.GetExtension(childFile) == ".js"
                       orderby childFile
                       select childFile;

            var directories = from childDirectory in Directory.GetDirectories(this.FullPath)
                              orderby childDirectory
                              select childDirectory;

            return new ObservableCollection<MacroFSNode>(files.Union(directories)
                    .Select((item) => new MacroFSNode(item, this)));
        }

        #region Context Menu
        public void Delete()
        {
            this.parent.children.Remove(this);
        }

        public void EnableEdit()
        {
            this.IsEditable = true;
        }

        public void DisableEdit()
        {
            this.isEditable = false;
        }

        public void RefreshTree()
        {
            // Refetch the children of the root node
            RootNode.children = this.GetChildNodes();

            // Notify change
            NotifyPropertyChanged("Children");
        }

        #endregion

        // Create the OnPropertyChanged method to raise the event 
        private void NotifyPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
