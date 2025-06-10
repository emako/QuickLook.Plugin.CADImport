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

using CADImport.FaceModule;
using System;
using UserControl = System.Windows.Controls.UserControl;

namespace QuickLook.Plugin.CADImport;

public partial class RenderPanel : UserControl, IDisposable
{
    private CADImaging cadImaging;
    private CADPictureBox cadPictBox;

    public RenderPanel()
    {
        InitializeComponent();

        cadImaging = new(cadPictBox = new CADPictureBox());
        presenter.Child = cadPictBox;
    }

    public void Dispose()
    {
        cadImaging?.Dispose();
        cadImaging = null;

        cadPictBox?.Dispose();
        cadPictBox = null;
    }

    public void LoadCadFile(string path)
    {
        cadImaging.LoadFile(path);
        cadPictBox.Invalidate();
    }

    public void SetTheme(bool isDark)
    {
        cadImaging.IsDark = isDark;
    }
}
