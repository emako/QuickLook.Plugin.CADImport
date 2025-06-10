// Copyright © 2017-2025 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace QuickLook.Plugin.CADImport;

public class Plugin : IViewer
{
    private static readonly string[] _extensions =
    [
        ".dwg", // AutoCAD Binary Drawing Format
        ".dxf", // AutoCAD Drawing Exchange Format
        ".plt", // Hewlett-Packard Graphics Language
        ".cgm", // Computer Graphics Metafile
    ];

    private static double _width = 900;
    private static double _height = 600;
    private RenderPanel _rp;
    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && _extensions.Any(path.ToLower().EndsWith);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = _width, Height = _height };
    }

    public void View(string path, ContextObject context)
    {
        context.Title = $"{Path.GetFileName(path)}";

        _rp = new RenderPanel();
        _rp.SetTheme(OSThemeHelper.AppsUseDarkTheme());
        _rp.LoadCadFile(path);

        context.ViewerContent = _rp;
        context.IsBusy = false;
    }

    public void Cleanup()
    {
        _width = _rp.ActualWidth;
        _height = _rp.ActualHeight;

        _rp?.Dispose();
        _rp = null;

        GC.SuppressFinalize(this);
    }
}
