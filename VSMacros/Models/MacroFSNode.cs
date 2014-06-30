﻿// MacroFsNode

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using VSMacros.Engines;
using GelUtilities = Microsoft.Internal.VisualStudio.PlatformUI.Utilities;

namespace VSMacros.Models
{
    public sealed class MacroFSNode : INotifyPropertyChanged
    {
        private static HashSet<string> enabledDirectories = new HashSet<string>();

        private string fullPath;
        private bool isEditable;
        private bool isExpanded;
        private bool isSelected;

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
                        // TODO export VSMacros.Engines.Manager.Instance.ShowMessageBox to a helper class?
                        VSMacros.Engines.Manager.Instance.ShowMessageBox(e.Message);
                    }
                }
            }
        }

        public string Shortcut
        {
            get
            {
                // TODO First draft (crappy!)
                for (int i = 1; i < 10; i++)
                {
                    if (String.Compare(Manager.Instance.Shortcuts[i], this.FullPath, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return "(CTRL+M, " + i + ")";
                    }
                }

                return string.Empty;
            }
            set
            {
                // Just notify the binding
                this.NotifyPropertyChanged("Shortcut");
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

        public bool IsSelected
        {
            get
            {
                return this.isSelected;
            }

            set
            {
                this.isSelected = value;
                this.NotifyPropertyChanged("IsSelected");
            }
        }

        public BitmapSource Icon
        {
            get
            {
                if (this.IsDirectory)
                {
                    Bitmap bmp;


                    if (this.isExpanded)
                    {
                        // TODO FromFile uses the working directory -> figure out how to make that work
                        Image icon = Image.FromFile(@"C:\Users\t-jusdom\Source\Repos\Macro Extension\VSMacros\Resources\folderopened.png");
                        bmp = new Bitmap(icon);
                    }
                    else
                    {
                        Image icon = Image.FromFile(@"C:\Users\t-jusdom\Source\Repos\Macro Extension\VSMacros\Resources\folderclosed.png");
                        bmp = new Bitmap(icon);
                    }

                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            bmp.GetHbitmap(),
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                }
                else
                {
                    IVsImageService imageService = (IVsImageService)((IServiceProvider)VSMacrosPackage.Current).GetService(typeof(SVsImageService));
                    if (imageService != null)
                    {
                        IVsUIObject uiObject = imageService.GetIconForFile(Path.GetFileName(this.FullPath), __VSUIDATAFORMAT.VSDF_WPF);
                        if (uiObject != null)
                        {
                            BitmapSource bitmapSource = GelUtilities.GetObjectData(uiObject) as BitmapSource;
                            return bitmapSource;
                        }
                    }
                }

                // Would it be better to have a sane default image?
                return null;
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

                if (this.IsExpanded)
                {
                    enabledDirectories.Add(this.FullPath);
                }
                else
                {
                    enabledDirectories.Remove(this.FullPath);
                }

                this.NotifyPropertyChanged("IsExpanded");
                this.NotifyPropertyChanged("Icon");
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
            this.IsEditable = false;
        }

        public void RefreshTree()
        {
            // Make a copy of the hashset
            HashSet<string> dirs = new HashSet<string>(enabledDirectories);

            // Clear enableDirectories
            enabledDirectories.Clear();

            // Refetch the children of the root node
            RootNode.children = this.GetChildNodes();

            // Recursively set IsEnabled for each folders
            this.SetIsExpanded(RootNode, dirs);

            // Notify change
            this.NotifyPropertyChanged("Children");
        }
        #endregion
        
        // OPTIMIZATION IDEA instead of iterating over the children, iterate over the enableDirs
        private void SetIsExpanded(MacroFSNode node, HashSet<string> enabledDirs)
        {
            if (node.Children.Count > 0 && enabledDirs.Count > 0)
            {
                foreach (var item in node.children)
                {
                    if (item.IsDirectory && enabledDirs.Contains(item.FullPath))
                    {
                        // Set IsExpanded
                        item.IsExpanded = true;

                        // Remove path from dirs
                        enabledDirs.Remove(item.FullPath);

                        // Recursion on children
                        this.SetIsExpanded(item, enabledDirs);
                    }
                }
            }
        }

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
