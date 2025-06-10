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
using CADImport.FaceModule;
using CADImport.RasterImage;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ScrollOrientation = CADImport.FaceModule.ScrollOrientation;

namespace QuickLook.Plugin.CADImport;

internal partial class CADImaging : IDisposable
{
    public CADImaging(CADPictureBox cadPictureBox)
    {
        cadImage = null;
        position = default;
        positionPrev = default;
        imageScale = 1f;
        imageScalePrev = 1f;
        visibleArea = default;
        currentXClickPosition = 0;
        currentYClickPosition = 0;
        isMouseDown = false;
        textVisible = true;
        drawingColor = true;

        cadPictBox = cadPictureBox;
        cadPictBox.BackColor = Color.Black;
        cadPictBox.BorderStyle = BorderStyle.None;
        cadPictBox.Cursor = Cursors.Default;
        cadPictBox.DoubleBuffering = true;
        cadPictBox.Ortho = false;
        cadPictBox.ScrollBars = ScrollBarsShow.Automatic;
        cadPictBox.Size = new Size(1000, 1000);
        cadPictBox.TabStop = false;
        cadPictBox.Dock = DockStyle.Fill;
        clipRectangle = new(cadPictureBox)
        {
            MultySelect = false,
        };

        cadPictBox.Paint += OnCADPictBoxPaint;
        cadPictBox.MouseWheel += OnCADPictBoxMouseWheel;
        cadPictBox.ScrollEvent += OnCADPictBoxScroll;
        cadPictBox.MouseDown += OnCADPictBoxMouseDown;
        cadPictBox.MouseMove += OnCADPictBoxMouseMove;
        cadPictBox.MouseUp += OnCADPictBoxMouseUp;
        cadPictBox.MouseDoubleClick += OnCADPictBoxMouseDoubleClick;
        cadPictBox.VisibleChanged += (_, _) =>
        {
            if (cadImage == null || cadPictBox == null)
                return;

            if (!cadPictBox.Visible)
                return;

            ResetScaling();
            StatusUpdated?.Invoke(this, CADImagingEventArgs.NewStatusUpdatedEventArgs(RealScale));
            RealPointUpdated?.Invoke(this, CADImagingEventArgs.NewRealPointUpdatedEventArgs(GetRealPoint((int)positionPrev.X, (int)positionPrev.Y)));
            OffsetPointUpdated?.Invoke(this, CADImagingEventArgs.NewOffsetPointUpdatedEventArgs(GetRealPoint((int)positionPrev.X, (int)positionPrev.Y), OriginalPoint));
        };
    }

    public void Dispose()
    {
        cadPictBox = null;
        cadImage?.Dispose();
        cadImage = null;
    }

    private void OnCADPictBoxMouseWheel(object sender, MouseEventArgs e)
    {
        if (e.Delta < 0)
        {
            Zoom(0.7f);
        }
        else
        {
            Zoom(1.3f);
        }
        Shift();
        SetPictureBoxPosition(position);
    }

    private void OnCADPictBoxScroll(object sender, ScrollEventArgsExt e)
    {
        if ((e.NewValue == 0) && (e.OldValue == 0))
        {
            e.NewValue = -5;
        }
        if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
        {
            TopImagePosition -= e.NewValue - e.OldValue;
        }
        if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
        {
            LeftImagePosition -= e.NewValue - e.OldValue;
        }
        cadPictBox.Invalidate();
    }

    private void OnCADPictBoxMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            CursorUpdated?.Invoke(this, new CADImagingEventArgs(CADImagingEventArgs.EventType.Cursor, [Cursors.Hand]));
            currentXClickPosition = e.X;
            currentYClickPosition = e.Y;
            isMouseDown = true;
        }
    }

    private void OnCADPictBoxMouseUp(object sender, MouseEventArgs e)
    {
        isMouseDown = false;
        cadPictBox.Cursor = Cursors.Default;
        cadPictBox.Invalidate();
    }

    private void OnCADPictBoxMouseDoubleClick(object sender, MouseEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ResetScaling();
    }

    private void OnCADPictBoxMouseMove(object sender, MouseEventArgs e)
    {
        if (cadImage == null)
            return;

        if (isMouseDown)
        {
            position.X -= currentXClickPosition - e.X;
            position.Y -= currentYClickPosition - e.Y;
            currentXClickPosition = e.X;
            currentYClickPosition = e.Y;
            cadPictBox.Invalidate();
            SetPictureBoxPosition(position);
        }
        positionPrev = new PointF(e.X, e.Y);
        RealPointUpdated?.Invoke(this, CADImagingEventArgs.NewRealPointUpdatedEventArgs(GetRealPoint(e.X, e.Y)));
        OffsetPointUpdated?.Invoke(this, CADImagingEventArgs.NewOffsetPointUpdatedEventArgs(GetRealPoint(e.X, e.Y), OriginalPoint));
    }

    private void OnCADPictBoxPaint(object sender, PaintEventArgs e)
    {
        if (cadImage == null)
            return;

        if (!cadPictBox.Visible)
            return;

        DrawCADImage(e.Graphics);
        StatusUpdated?.Invoke(this, CADImagingEventArgs.NewStatusUpdatedEventArgs(RealScale));
    }

    private DPoint GetRealPoint(int x, int y)
    {
        RectangleF tmpRect = ImageRectangleF;

        try
        {
            if (cadImage != null)
                return CADConst.GetRealPoint(x, y, cadImage, tmpRect);
            else
                return DPoint.Empty;
        }
        catch
        {
            return DPoint.Empty;
        }
    }

    private void Shift()
    {
        LeftImagePosition = positionPrev.X - (positionPrev.X - LeftImagePosition) * imageScale / imageScalePrev;
        TopImagePosition = positionPrev.Y - (positionPrev.Y - TopImagePosition) * imageScale / imageScalePrev;
        imageScalePrev = imageScale;
    }

    private void DrawCADImage(Graphics g)
    {
        try
        {
            Shift();
            RectangleF tmp = ImageRectangleF;
            SetSizePictureBox(new Size((int)tmp.Width, (int)tmp.Height));
            SetPictureBoxPosition(position);
            cadImage.Draw(g, tmp, cadPictBox);
        }
        catch
        {
        }
    }

    private void SetSizePictureBox(Size sz)
    {
        if (((position.X < 0) || (position.Y < 0) ||
             (position.X + sz.Width > cadPictBox.Width) ||
             (position.Y + sz.Height > cadPictBox.Height)) && IsSizeWithInBox())
        {
            if (position.X < 0)
            {
                sz.Width = (int)(cadPictBox.Width - position.X);
            }
            if (position.Y < 0)
            {
                sz.Height = (int)(cadPictBox.Height - position.Y);
            }
            if (position.X + sz.Width > cadPictBox.Width)
            {
                sz.Width = (int)(cadPictBox.Width + position.X);
            }
            if (position.Y + sz.Height > cadPictBox.Height)
            {
                sz.Height = (int)(cadPictBox.Height + position.Y);
            }
        }
        cadPictBox.SetVirtualSizeNoInvalidate(sz);
    }

    private bool IsSizeWithInBox()
    {
        RectangleF tmp = ImageRectangleF;
        return (tmp.Width <= cadPictBox.Size.Width) || (tmp.Height <= cadPictBox.Height);
    }

    private void Resize()
    {
        if (cadImage == null)
            return;

        if (cadPictBox.ClientRectangle.Height == 0 || cadImage.AbsHeight == 0)
            return;

        float wh = (float)(cadImage.AbsWidth / cadImage.AbsHeight);
        float new_wh = cadPictBox.ClientRectangle.Width / cadPictBox.ClientRectangle.Height;

        if (cadImage is CADRasterImage)
            visibleArea = new SizeF((float)cadImage.AbsWidth, (float)cadImage.AbsHeight);
        else
            visibleArea = cadPictBox.Size;

        if (new_wh > wh)
            visibleArea.Width = visibleArea.Height * wh;
        else
        {
            if (new_wh < wh)
                visibleArea.Height = visibleArea.Width / wh;
            else
                visibleArea = cadPictBox.Size;
        }
        LeftImagePosition = (cadPictBox.ClientRectangle.Width - visibleArea.Width) / 2f;
        TopImagePosition = (cadPictBox.ClientRectangle.Height - visibleArea.Height) / 2f;
        cadPictBox.Invalidate();
    }

    private void SetPictureBoxPosition(PointF value)
    {
        try
        {
            int w1, h1;

            if (value.X > 0)
            {
                w1 = 0;
            }
            else
            {
                w1 = (int)Math.Abs(value.X);
            }

            if (w1 > cadPictBox.VirtualSize.Width)
            {
                w1 = cadPictBox.VirtualSize.Width;
            }

            if (value.Y > 0)
            {
                h1 = 0;
            }
            else
            {
                h1 = (int)Math.Abs(value.Y);
            }

            if (h1 > cadPictBox.VirtualSize.Height)
            {
                h1 = cadPictBox.VirtualSize.Height;
            }
            cadPictBox?.SetPositionNoInvalidate(new Point(w1, h1));
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
        }
    }

    private void Zoom(float i)
    {
        if (cadImage == null)
        {
            return;
        }

        imageScale *= i;

        if (imageScale < 0.005f)
        {
            imageScale = 0.005f;
        }
        cadPictBox.Invalidate();
    }

    private void SetCADImageOptions()
    {
        cadImage.IsShowLineWeight = false;

        CursorUpdated?.Invoke(this, CADImagingEventArgs.NewCursorUpdatedEventArgs(Cursors.Default));

        ObjEntity.cadImage = cadImage;

        SetBackColor(IsDark);

        if (cadPictBox.BackColor == Color.White)
        {
            if (cadImage != null)
            {
                cadImage.DefaultColor = Color.Black;
                cadImage.BackgroundColor = Color.White;
            }
        }
        else
        {
            if (cadImage != null)
            {
                cadImage.DefaultColor = Color.White;
                cadImage.BackgroundColor = Color.Black;
            }
        }

        SetDrawingColors(drawingColor);
        SetTextVisble(textVisible);

        Resize();
        SetPictureBoxPosition(position);
    }

    public void Invalidate()
    {
        cadPictBox?.Invalidate();
    }
}
