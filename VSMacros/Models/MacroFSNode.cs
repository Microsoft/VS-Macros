﻿//-----------------------------------------------------------------------
// <copyright file="MacroFSNode.cs" company="Microsoft Corporation">
//     Copyright Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSMacros.Engines;
using GelUtilities = Microsoft.Internal.VisualStudio.PlatformUI.Utilities;

namespace VSMacros.Models
{
    public sealed class MacroFSNode : INotifyPropertyChanged
    {
        private static HashSet<string> enabledDirectories = new HashSet<string>();

        private string fullPath;
        private string shortcut;
        private bool isEditable;
        private bool isExpanded;
        private bool isSelected;
        private bool isMatch;

        private MacroFSNode parent;
        private ObservableCollection<MacroFSNode> children;

        public event PropertyChangedEventHandler PropertyChanged;

        public static MacroFSNode RootNode { get; set; }
        private static bool searching = false;

        public bool IsDirectory { get; private set; }

        public MacroFSNode(string path, MacroFSNode parent = null)
        {
            this.IsDirectory = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
            this.FullPath = path;
            this.isEditable = false;
            this.isSelected = false;
            this.isMatch = false;
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
                try
                {
                    // Path.GetFullPath will throw an exception if the path is invalid
                    Path.GetFileName(value);

                    if (value != this.Name)
                    {
                        string oldFullPath = this.FullPath;
                        string newFullPath = Path.Combine(Path.GetDirectoryName(this.FullPath), value + Path.GetExtension(this.FullPath));

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

                        // Update shortcut
                        if (!string.IsNullOrEmpty(this.Shortcut))
                        {
                            Manager.Instance.Shortcuts[this.Shortcut[this.Shortcut.Length - 1]] = newFullPath;
                        }
                    }
                }
                catch (Exception e)
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
                if (this.shortcut == null)
                {
                    this.shortcut = string.Empty;

                    // TODO can probably be optimized
                    for (int i = 1; i < 10; i++)
                    {
                        if (string.Compare(Manager.Instance.Shortcuts[i], this.FullPath, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            this.shortcut = "(CTRL+M, " + i + ")";
                        }
                    }                  
                }

                return this.shortcut;
            }
            set
            {
                // Set shortcut to null so it will be re-fetched
                this.shortcut = null;

                // Just notify the binding
                this.NotifyPropertyChanged("Shortcut");
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
                        bmp = Resources.FolderOpened;
                    }
                    else
                    {
                        bmp = Resources.FolderClosed;
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
                    if (this.IsDirectory)
                    {
                        enabledDirectories.Add(this.FullPath);
                    }

                    // Expand parent as well
                    if (this.parent != null)
                    {
                        this.parent.IsExpanded = true;
                    }
                }
                else
                {
                    if (this.IsDirectory)
                    {
                        enabledDirectories.Remove(this.FullPath);
                    }
                }

                this.NotifyPropertyChanged("IsExpanded");
                this.NotifyPropertyChanged("Icon");
            }
        }

        public bool IsMatch
        {
            get
            {
                // If searching is not enabled, always return true
                if (!MacroFSNode.searching)
                {
                    return true;
                }
                else
                {
                    return this.isMatch;
                }
            }
            set
            {
                this.isMatch = value;

                if (this.IsMatch == true && this.parent != null)
                {
                    this.IsExpanded = true;
                    this.parent.IsMatch = true;
                }

                this.NotifyPropertyChanged("IsMatch");
            }
        }

        public bool IsNotRoot
        {
            get
            {
                return this != MacroFSNode.RootNode;
            }
        }

        public int Depth
        {
            get
            {
                if (this.parent == null)
                {
                    return 0;
                }
                else
                {
                    return this.parent.Depth + 1;
                }
            }
        }

        public MacroFSNode Parent
        {
            get
            {
                return this.parent;
            }
        }

        public bool Equals(MacroFSNode node)
        {
            return this.FullPath == node.FullPath;
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
            if (!string.IsNullOrEmpty(this.shortcut))
            {
                // Remove shortcut from shortcut list
                Manager.Instance.Shortcuts[this.shortcut[this.shortcut.Length - 2] - '0'] = string.Empty;
            }

            // Remove macro from collection
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

        public static void RefreshTree()
        {
            MacroFSNode root = MacroFSNode.RootNode;

            // Make a copy of the hashset
            HashSet<string> dirs = new HashSet<string>(enabledDirectories);

            // Clear enableDirectories
            enabledDirectories.Clear();

            // Refetch the children of the root node
            root.children = root.GetChildNodes();

            // Recursively set IsEnabled for each folders
            root.SetIsExpanded(root, dirs);

            // Select Current macro
            MacroFSNode.FindNodeFromFullPath(Path.Combine(VSMacrosPackage.Current.MacroDirectory, "Current.js")).IsSelected = true;

            // Notify change
            root.NotifyPropertyChanged("Children");
        }
        #endregion

        public static void EnableSearch()
        {
            // Set Searching to true
            MacroFSNode.searching = true;

            // And then notify all node that their IsMatch property might have changed
            MacroFSNode.NotifyAllNode(MacroFSNode.RootNode, "IsMatch");
        }

        public static void DisableSearch()
        {
            // Set Searching to true
            MacroFSNode.searching = false;

            // And then notify all node that their IsMatch property might have changed
            MacroFSNode.NotifyAllNode(MacroFSNode.RootNode, "IsMatch");
        }

        /// <summary>
        /// Expands all the node marked as expanded in enabledDirs
        /// </summary>
        /// <param name="node">Tree rooted at node</param>
        /// <param name="enabledDirs">Hash set containing the enabled dirs</param>
        private void SetIsExpanded(MacroFSNode node, HashSet<string> enabledDirs)
        {
            // OPTIMIZATION IDEA instead of iterating over the children, iterate over the enableDirs
            if (node.Children.Count > 0 && enabledDirs.Count > 0)
            {
                foreach (var item in node.children)
                {
                    if (item.IsDirectory && enabledDirs.Contains(item.FullPath))
                    {
                        // Set IsExpanded
                        item.IsExpanded = true;

                        // Remove path from dirs to improve performance of next call to HashSet.Contains
                        enabledDirs.Remove(item.FullPath);

                        // Recursion on children
                        this.SetIsExpanded(item, enabledDirs);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the node with FullPath path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>MacroFSNode  whose FullPath is path</returns>
        public static MacroFSNode FindNodeFromFullPath(string path)
        {
            // shortenPath is the path relative to the Macros folder
            string shortenPath = path.Substring(path.IndexOf(@"\Macros\"));

            string[] substrings = shortenPath.Split(new char[] { '\\' });

            // Starting from the root,
            MacroFSNode node = MacroFSNode.RootNode;

            try
            {
                // Go down the tree to find the right node
                // 2 because substrings[0] == "" and substrings[1] is root
                for (int i = 2; i < substrings.Length; i++)
                {
                    node = node.Children.Single(x => x.Name == Path.GetFileNameWithoutExtension(substrings[i]));
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                node = null;
            }

            return node;
        }

        private void NotifyPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        // Notifies all the nodes of the tree rooted at 'node'
        public static void NotifyAllNode(MacroFSNode node, string property)
        {
            node.NotifyPropertyChanged(property);

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    MacroFSNode.NotifyAllNode(child, "IsMatch");
                }
            }
        }
    }
}