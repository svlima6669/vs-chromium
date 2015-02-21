﻿// Copyright 2013 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using VsChromium.Core.Configuration;
using VsChromium.Core.Utility;
using VsChromium.Features.AutoUpdate;

namespace VsChromium.Features.ToolWindows.SourceExplorer {
  public class SourceExplorerViewModel : ChromiumExplorerViewModelBase {
    private List<TreeViewItemViewModel> _fileSystemTreeNodes = new List<TreeViewItemViewModel>();
    private List<TreeViewItemViewModel> _directoryNameSearchResultNodes = new List<TreeViewItemViewModel>();
    private List<TreeViewItemViewModel> _textSearchResultNodes = new List<TreeViewItemViewModel>();
    private List<TreeViewItemViewModel> _fileNameSearchResultNodes = new List<TreeViewItemViewModel>();
    private UpdateInfo _updateInfo;
    private bool _matchCase;
    private bool _matchWholeWord;
    private bool _useRegex;
    private bool _includeSymLinks;
    private bool _useRe2Regex;

    public enum DisplayKind {
      FileSystemTree,
      FileNameSearchResult,
      DirectoryNameSearchResult,
      TextSearchResult,
    }

    public SourceExplorerViewModel() {
      // Default values for options in toolbar.
      this.IncludeSymLinks = true;
      this.UseRe2Regex = true;
    }

    public DisplayKind ActiveDisplay {
      get {
        if (ReferenceEquals(ActiveRootNodes, _textSearchResultNodes))
          return DisplayKind.TextSearchResult;
        if (ReferenceEquals(ActiveRootNodes, _fileNameSearchResultNodes))
          return DisplayKind.FileNameSearchResult;
        if (ReferenceEquals(ActiveRootNodes, _directoryNameSearchResultNodes))
          return DisplayKind.DirectoryNameSearchResult;
        return DisplayKind.FileSystemTree;
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public bool MatchCase {
      get { return _matchCase; }
      set {
        if (_matchCase != value) {
          _matchCase = value;
          OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.MatchCase));
        }
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public string MatchCaseToolTip {
      get {
        return string.Format(
          "Toggle case matching for all searches. " +
          "Searches are currently case {0}.",
          MatchCase ? "sensitive" : "insensitive");
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public bool MatchWholeWord {
      get { return _matchWholeWord; }
      set {
        if (_matchWholeWord != value) {
          _matchWholeWord = value;
          OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.MatchWholeWord));
        }
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public string MatchWholeWordToolTip {
      get {
        return string.Format(
          "Toggle matching whole words only for all searches. " +
          "Match whole word is currently {0}.",
          MatchWholeWord ? "enabled" : "disabled");
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public bool UseRegex {
      get { return _useRegex; }
      set {
        if (_useRegex != value) {
          _useRegex = value;
          OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.UseRegex));
        }
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public string UseRegexToolTip {
      get {
        return string.Format(
          "Toggle usage of regular expressions for all searches. " +
          "Regular expressions are currently {0}.",
          UseRegex ? "enabled" : "disabled");
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public bool IncludeSymLinks {
      get { return _includeSymLinks; }
      set {
        if (_includeSymLinks != value) {
          _includeSymLinks = value;
          OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.IncludeSymLinks));
        }
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public string IncludeSymLinksToolTip {
      get {
        return string.Format(
          "Toggle searching inside symbolic links for all searches. " +
          "Symbolic links are currently {0} in search results.",
          IncludeSymLinks ? "included" : "excluded");
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public bool UseRe2Regex {
      get { return _useRe2Regex; }
      set {
        if (_useRe2Regex != value) {
          _useRe2Regex = value;
          OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.UseRe2Regex));
        }
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public string UseRe2RegexToolTip {
      get {
        return string.Format(
          "Toggle usage of the RE2 regular expression engine as a replacement of the standard C++ library for improved performance. " +
          "The RE2 engine is currently {0}.",
          UseRe2Regex ? "enabled" : "disabled");
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public string UpdateInfoText {
      get {
        if (_updateInfo == null)
          return "";
        return string.Format("A new version ({0}) of VsChromium is available: ", _updateInfo.Version);
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public string UpdateInfoUrl {
      get {
        if (_updateInfo == null)
          return "";
        return _updateInfo.Url.ToString();
      }
    }

    /// <summary>
    /// Databound!
    /// </summary>
    public Visibility UpdateInfoVisibility {
      get {
        if (_updateInfo == null)
          return Visibility.Collapsed;
        return Visibility.Visible;
      }
    }

    public UpdateInfo UpdateInfo {
      get {
        return _updateInfo;
      }
      set {
        _updateInfo = value;
        OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.UpdateInfoText));
        OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.UpdateInfoUrl));
        OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.UpdateInfoVisibility));
      }
    }

    /// <summary>
    /// The root nodes representing the file system tree from the server.
    /// </summary>
    public List<TreeViewItemViewModel> FileSystemTreeNodes {
      get { return _fileSystemTreeNodes; }
    }

    public bool GotoPreviousEnabled {
      get { return ActiveDisplay != DisplayKind.FileSystemTree; }
    }

    public bool GotoNextEnabled {
      get { return ActiveDisplay != DisplayKind.FileSystemTree; }
    }

    public bool CancelSearchEnabled {
      get { return ActiveDisplay != DisplayKind.FileSystemTree; }
    }

    public bool RefreshSearchResultEnabled {
      get { return ActiveDisplay != DisplayKind.FileSystemTree; }
    }

    private ImageSource GetImageFromResource(string name) {
      if (ImageSourceFactory == null) {
        return Views.ImageSourceFactory.Instance.GetImage(name);
      }
      return ImageSourceFactory.GetImage(name);
    }

    public ImageSource GotoPreviousButtonImage {
      get {
        return GetImageFromResource("ArrowLeft");
      }
    }

    public ImageSource GotoNextButtonImage {
      get {
        return GetImageFromResource("ArrowRight");
      }
    }

    public ImageSource CancelSearchButtonImage {
      get {
        return GetImageFromResource("CancelSearch");
      }
    }

    public ImageSource SearchLensButtonImage {
      get {
        return GetImageFromResource("SearchLens");
      }
    }

    public ImageSource ClearSearchButtonImage {
      get {
        return GetImageFromResource("ClearSearch");
      }
    }

    public ImageSource SyncButtonImage {
      get {
        return GetImageFromResource("SyncActiveDocument");
      }
    }

    public ImageSource RefreshFileSystemTreeButtonImage {
      get {
        return GetImageFromResource("RefreshFileSystemTree");
      }
    }

    protected override void OnRootNodesChanged() {
      base.OnRootNodesChanged();

      OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.GotoNextEnabled));
      OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.GotoPreviousEnabled));
      OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.CancelSearchEnabled));
      OnPropertyChanged(ReflectionUtils.GetPropertyName(this, x => x.RefreshSearchResultEnabled));
    }

    public void SwitchToFileSystemTree() {
      var msg1 = string.Format("Open a source file from a local Chromium enlistment or");
      var msg2 = string.Format("from a directory containing a \"{0}\" file.", ConfigurationFileNames.ProjectFileName);
      SetRootNodes(_fileSystemTreeNodes, msg1 + "\r\n" + msg2);
    }

    private void SwitchToFileNamesSearchResult() {
      SetRootNodes(_fileNameSearchResultNodes);
    }

    private void SwitchToDirectoryNamesSearchResult() {
      SetRootNodes(_directoryNameSearchResultNodes);
    }

    private void SwitchToTextSearchResult() {
      SetRootNodes(_textSearchResultNodes);
    }

    public void SetFileSystemTree(List<TreeViewItemViewModel> viewModel) {
      _fileSystemTreeNodes = viewModel;
      SwitchToFileSystemTree();
    }

    public void SetFileNamesSearchResult(List<TreeViewItemViewModel> viewModel) {
      _fileNameSearchResultNodes = viewModel;
      SwitchToFileNamesSearchResult();
    }

    public void SetDirectoryNamesSearchResult(List<TreeViewItemViewModel> viewModel) {
      _directoryNameSearchResultNodes = viewModel;
      SwitchToDirectoryNamesSearchResult();
    }

    public void SetTextSearchResult(List<TreeViewItemViewModel> viewModel) {
      _textSearchResultNodes = viewModel;
      SwitchToTextSearchResult();
    }

    public void FileSystemTreeComputing() {
      if (_fileSystemTreeNodes.Count <= 1) {
        SetRootNodes(_fileSystemTreeNodes, "(Loading files from VS Chromium projects...)");
      }
    }
  }
}
