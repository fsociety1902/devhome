﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LibGit2Sharp;

namespace FileExplorerGitIntegration.Models;

internal sealed class GitRepositoryStatus
{
    private readonly Dictionary<string, GitStatusEntry> _fileEntries = new();
    private readonly List<GitStatusEntry> _added = new();
    private readonly List<GitStatusEntry> _staged = new();
    private readonly List<GitStatusEntry> _removed = new();
    private readonly List<GitStatusEntry> _untracked = new();
    private readonly List<GitStatusEntry> _modified = new();
    private readonly List<GitStatusEntry> _missing = new();
    private readonly List<GitStatusEntry> _ignored = new();
    private readonly List<GitStatusEntry> _renamedInIndex = new();
    private readonly List<GitStatusEntry> _renamedInWorkDir = new();
    private readonly List<GitStatusEntry> _conflicted = new();
    private readonly Dictionary<FileStatus, List<GitStatusEntry>> _statusEntries = new();
    private string _branchName = string.Empty;
    private bool _isBranchDetached;
    private string _upstreamBranch = string.Empty;
    private int _aheadBy;
    private int _behindBy;
    private string _sha = string.Empty;

    public GitRepositoryStatus()
    {
        _statusEntries.Add(FileStatus.NewInIndex, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.ModifiedInIndex, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.DeletedFromIndex, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.NewInWorkdir, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.ModifiedInWorkdir, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.DeletedFromWorkdir, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.RenamedInIndex, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.RenamedInWorkdir, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.Conflicted, new List<GitStatusEntry>());
    }

    public void Add(string path, GitStatusEntry status)
    {
        _fileEntries.Add(path, status);
        foreach (var entry in _statusEntries)
        {
            if (status.Status.HasFlag(entry.Key))
            {
                entry.Value.Add(status);
            }
        }
    }

    public void SetSha(string sha)
    {
        _sha = sha;
    }

    public void SetBranchName(string branchName)
    {
        _branchName = branchName;
    }

    public void SetIsBranchDetached(bool isBranchDetached)
    {
        _isBranchDetached = isBranchDetached;
    }

    public void SetAheadBy(int aheadBy)
    {
        _aheadBy = aheadBy;
    }

    public void SetBehindBy(int behindBy)
    {
        _behindBy = behindBy;
    }

    public void SetUpstreamBranch(string upstreamBranch)
    {
        _upstreamBranch = upstreamBranch;
    }

    public Dictionary<string, GitStatusEntry> FileEntries => _fileEntries;

    public List<GitStatusEntry> Added => _statusEntries[FileStatus.NewInIndex];

    public List<GitStatusEntry> Staged => _statusEntries[FileStatus.ModifiedInIndex];

    public List<GitStatusEntry> Removed => _statusEntries[FileStatus.DeletedFromIndex];

    public List<GitStatusEntry> Untracked => _statusEntries[FileStatus.NewInWorkdir];

    public List<GitStatusEntry> Modified => _statusEntries[FileStatus.ModifiedInWorkdir];

    public List<GitStatusEntry> Missing => _statusEntries[FileStatus.DeletedFromWorkdir];

    public List<GitStatusEntry> RenamedInIndex => _statusEntries[FileStatus.RenamedInIndex];

    public List<GitStatusEntry> RenamedInWorkDir => _statusEntries[FileStatus.RenamedInWorkdir];

    public List<GitStatusEntry> Conflicted => _statusEntries[FileStatus.Conflicted];

    public string BranchName() => _branchName;

    public bool IsHeadDetached() => _isBranchDetached;

    public int AheadBy() => _aheadBy;

    public int BehindBy() => _behindBy;

    public string Sha() => _sha;

    public string UpstreamBranch() => _upstreamBranch;
}
