#region Copyright Notice

/*
 * gitter - VCS repository management tool
 * Copyright (C) 2013  Popovskiy Maxim Vladimirovitch <amgine.gitter@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

#endregion

namespace gitter.Git.Gui;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using AccessLayer;

using Dialogs;

using Framework;
using Framework.Controls;

using Views;

using Resources = Properties.Resources;

internal sealed class MainGitMenus : IDisposable
{
	private readonly List<ViewMenuItem> _viewMenuItems = new();
	private ToolStripMenuItem _gitMenu;
	private ToolStripMenuItem[] _menus;

	private Repository _repository;

	public MainGitMenus(GuiProvider guiProvider)
	{
		Verify.Argument.IsNotNull(guiProvider);

		Gui = guiProvider;

		var repository = guiProvider.Repository;

		_menus = new[]
		{
			_gitMenu = new ToolStripMenuItem(
				Resources.StrGit)
		};

		var dpiBindings = guiProvider.MainFormDpiBindings;

		AddToolStripMenuItem(Resources.StrCreateBranch.AddEllipsis(), OnCreateBranchClick, Keys.Control | Keys.B,
			Icons.BranchAdd);
		AddToolStripMenuItem(Resources.StrCreateTag.AddEllipsis(), OnCreateTagClick, Keys.Control | Keys.T,
			Icons.TagAdd);
		AddToolStripMenuItem(Resources.StrCommit.AddEllipsis(), OnCommitClick, Keys.Control | Keys.C, Icons.Commit);
		AddToolStripMenuItem(Resources.StrFetch.AddEllipsis(), OnFetchCLick, Keys.Control | Keys.Shift | Keys.F,
			Icons.Fetch);
		AddToolStripMenuItem(Resources.StrPull.AddEllipsis(), OnPullClick, Keys.Control | Keys.P, Icons.Pull);
		AddToolStripMenuItem(Resources.StrPush.AddEllipsis(), OnPushClick, Keys.Control | Keys.Alt | Keys.P,
			Icons.Push);
		AddToolStripMenuItem(Resources.StrStageAll.AddEllipsis(), OnStageAllClick, Keys.Control | Keys.S,
			Icons.StageAll);
		AddToolStripMenuItem((Resources.StrStageAll + " and " + Resources.StrCommit).AddEllipsis(),
			OnStageAllAndCommitClick, Keys.Control | Keys.Alt | Keys.S, Icons.Pull);

		_gitMenu.DropDownItems.Add(new ToolStripSeparator());

		AddToolStripMenuItem(Resources.StrlGui, OnGitGuiClick, Keys.F5, Icons.Git);
		AddToolStripMenuItem(Resources.StrlGitk, OnGitGitkClick, Keys.F6, Icons.Git, StandardTools.CanStartGitk);
		AddToolStripMenuItem(Resources.StrlBash, OnGitBashClick, Keys.F7, Icons.Terminal, StandardTools.CanStartBash);
		AddToolStripMenuItem(Resources.StrlCmd, OnCmdClick, Keys.F8, Icons.Terminal);

		foreach(var factory in Gui.ViewFactories)
			if(factory.IsSingleton)
			{
				var item = new ViewMenuItem(factory);
				_viewMenuItems.Add(item);
			}

		if(repository is not null) AttachToRepository(repository);
		return;

		void AddToolStripMenuItem(string text, EventHandler onClick, Keys keys, IImageProvider provider,
			bool enabled = true)
		{
			var item = new ToolStripMenuItem(text, null, onClick)
			{
				Enabled = enabled,
				ShortcutKeys = keys
			};
			dpiBindings.BindImage(item, provider);
			_gitMenu.DropDownItems.Add(item);
		}
	}

	public IReadOnlyList<ToolStripMenuItem> Menus => _menus;

	public IReadOnlyList<ViewMenuItem> ViewMenuItems => _viewMenuItems;

	public GuiProvider Gui { get; }

	public Repository Repository
	{
		get => _repository;
		set
		{
			if(_repository != value)
			{
				if(_repository is not null) DetachFromRepository(_repository);

				if(value is not null) AttachToRepository(value);
			}
		}
	}

	#region IDisposable Members

	public void Dispose()
	{
		if(_gitMenu is not null)
		{
			_gitMenu.Dispose();
			_gitMenu = null;
		}

		_menus = null;
		_repository = null;
	}

	#endregion

	private void OnStageAllAndCommitClick(object sender, EventArgs e)
	{
		OnStageAllClick(sender, e);
		var item = (ToolStripItem)sender;
		var parent = Utility.GetParentControl(item);

		using var dlg = new CommitDialog(_repository);
		dlg.Run(parent);
	}

	private void OnStageAllClick(object sender, EventArgs e)
	{
		Repository.Status.StageAll();
	}

	private void OnPushClick(object sender, EventArgs e)
	{
		Gui.StartPushDialog();
	}

	private void OnPullClick(object sender, EventArgs e)
	{
		GuiCommands.Pull(Gui.Environment.MainForm, Repository);
	}

	private void OnFetchCLick(object sender, EventArgs e)
	{
		GuiCommands.Fetch(Gui.Environment.MainForm, Repository);
	}

	private void OnCreateBranchClick(object sender, EventArgs e)
	{
		Gui.StartCreateBranchDialog();
	}

	private void OnCreateTagClick(object sender, EventArgs e)
	{
		Gui.StartCreateTagDialog();
	}

	private void OnCommitClick(object sender, EventArgs e)
	{
		Gui.Environment.ViewDockService.ShowView(Guids.CommitViewGuid);
	}

	private void OnGitGuiClick(object sender, EventArgs e)
	{
		StandardTools.StartGitGui(_repository.WorkingDirectory);
	}

	private void OnGitGitkClick(object sender, EventArgs e)
	{
		StandardTools.StartGitk(_repository.WorkingDirectory);
	}

	private void OnGitBashClick(object sender, EventArgs e)
	{
		StandardTools.StartBash(_repository.WorkingDirectory);
	}

	private void OnCmdClick(object sender, EventArgs e)
	{
		var psi = new ProcessStartInfo(@"cmd")
		{
			WorkingDirectory = Repository.WorkingDirectory
		};
		Process.Start(psi)?.Dispose();
	}

	private void AttachToRepository(Repository repository)
	{
		_gitMenu.Enabled = true;
		_repository = repository;
	}

	private void DetachFromRepository(Repository repository)
	{
		_gitMenu.Enabled = false;
		_repository = null;
	}
}
