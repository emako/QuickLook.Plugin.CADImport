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

using CADImport;
using System.Drawing;
using System.Windows.Forms;

namespace QuickLook.Plugin.CADImport;

internal partial class CADImaging
{
    public bool SetDrawingColors(bool val)
    {
        if (val)
            SetNormalColor();
        else
            SetBlackColor();

        return IsNormalDrawMode;
    }

    public bool SetBackColor(bool val)
    {
        if (val)
            SetBlackRender();
        else
            SetWhiteRender();

        return IsBlackBackColor;
    }

    public void LoadFile(string fileName)
    {
        if (fileName != null)
        {
            if (cadImage != null)
            {
                cadImage.Dispose();
                cadImage = null;
            }

            CursorUpdated?.Invoke(this, CADImagingEventArgs.NewCursorUpdatedEventArgs(Cursors.WaitCursor));

            imageScale = 1f;
            imageScalePrev = 1f;
            position = new PointF();
            cadImage = CADImage.CreateImageByExtension(fileName);
        }

        if (cadImage != null)
        {
            if (CADConst.IsWebPath(fileName))
                cadImage.LoadFromWeb(fileName);
            else
                cadImage.LoadFromFile(fileName);
        }
        LastLoadedFilePath = fileName;

        SetCADImageOptions();

        AfterLoaded?.Invoke(this, fileName);
    }

    public void ResetScaling()
    {
        imageScale = 1f;
        imageScalePrev = 1f;
        LeftImagePosition = (cadPictBox.ClientRectangle.Width - visibleArea.Width) / 2f;
        TopImagePosition = (cadPictBox.ClientRectangle.Height - visibleArea.Height) / 2f;
        cadPictBox.Invalidate();
    }

    public void ZoomIn()
    {
        Zoom(1.3f);
        Shift();
        SetPictureBoxPosition(position);
    }

    public void ZoomOut()
    {
        Zoom(0.7f);
        Shift();
        SetPictureBoxPosition(position);
    }

    public void ZoomFit(float scaleRatio = 1f)
    {
        Zoom(scaleRatio);
        Shift();
        SetPictureBoxPosition(position);
    }

    public void SetWhiteRender()
    {
        cadPictBox.BackColor = Color.White;

        if (cadImage != null)
        {
            cadImage.Painter.Settings.DefaultColor = Color.Black.ToArgb();
            cadImage.Painter.Settings.BackgroundColor = Color.White.ToArgb();
        }

        if (clipRectangle != null)
        {
            clipRectangle.Color = Color.Black;
        }
    }

    public void SetBlackRender()
    {
        cadPictBox.BackColor = Color.Black;

        if (cadImage != null)
        {
            cadImage.Painter.Settings.DefaultColor = Color.White.ToArgb();
            cadImage.Painter.Settings.BackgroundColor = Color.Black.ToArgb();
        }

        if (clipRectangle != null)
        {
            clipRectangle.Color = Color.White;
        }
    }

    public void SetNormalColor()
    {
        if (cadImage == null)
        {
            return;
        }

        cadImage.DrawMode = CADDrawMode.Normal;
        cadPictBox.Invalidate();
    }

    public void SetBlackColor()
    {
        if (cadImage == null)
        {
            return;
        }

        cadImage.DrawMode = CADDrawMode.Black;
        cadPictBox.Invalidate();
    }

    public bool SetTextVisble(bool isTextVisible)
    {
        textVisible = isTextVisible;
        cadImage.TextVisible = isTextVisible;
        cadPictBox.Invalidate();
        return textVisible;
    }

    public bool InvertTextVisble()
    {
        textVisible = !textVisible;
        cadImage.TextVisible = textVisible;
        cadPictBox.Invalidate();
        return textVisible;
    }
}
