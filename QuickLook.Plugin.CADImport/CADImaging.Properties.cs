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
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuickLook.Plugin.CADImport;

internal partial class CADImaging
{
    public event EventHandler<CADImagingEventArgs> StatusUpdated = null;

    public event EventHandler<CADImagingEventArgs> CursorUpdated = null;

    public event EventHandler<CADImagingEventArgs> RealPointUpdated = null;

    public event EventHandler<CADImagingEventArgs> OffsetPointUpdated = null;

    public event EventHandler<string> AfterLoaded = null;

    private CADPictureBox cadPictBox = null;

    private CADImage cadImage = null;

    private DPoint originalPoint = new(default, default, default);

    private readonly ClipRect clipRectangle = null;

    private PointF positionPrev = default;

    private PointF position = default;

    private float imageScalePrev;

    private float imageScale;

    private SizeF visibleArea;

    private int currentXClickPosition;

    private int currentYClickPosition;

    private bool isMouseDown;

    private bool textVisible;

    private bool drawingColor;

    private float LeftImagePosition
    {
        get => position.X;
        set => position.X = value;
    }

    private float TopImagePosition
    {
        get => position.Y;
        set => position.Y = value;
    }

    private RectangleF ImageRectangleF => new(LeftImagePosition, TopImagePosition, visibleArea.Width * imageScale, visibleArea.Height * imageScale);

    private string RealScale
    {
        get
        {
            if (cadImage != null)
                return string.Format("{0,2:F}", visibleArea.Width * imageScale / cadImage.AbsWidth * cadImage.MMToPixelX * 100);
            else
                return string.Format("{0}", imageScale);
        }
    }

    public CADImage CADImage => cadImage;

    public DPoint OriginalPoint
    {
        get => originalPoint;
        set => originalPoint = value;
    }

    public bool IsLoaded => cadImage != null;

    public bool IsDark { get; set; } = false;

    public bool IsNormalDrawMode => cadImage.DrawMode == CADDrawMode.Normal;

    public bool IsBlackBackColor => cadPictBox.BackColor == Color.Black;

    public string LastLoadedFilePath
    {
        get => CADImage.LastLoadedFilePath;
        set => CADImage.LastLoadedFilePath = value;
    }
}

public class CADImagingEventArgs : EventArgs
{
    public enum EventType
    {
        None,
        Status,
        Cursor,
        RealPoint,
        OffsetPoint,
    }

    public EventType Type = default;

    public object[] Arguments = null;

    public string ScaleRatio
    {
        get
        {
            if (Type == EventType.Status && Arguments.Length > 0)
            {
                if (Arguments[0] is string)
                {
                    return $"{Arguments[0]}%";
                }
            }
            return string.Empty;
        }
    }

    public Cursor Cursor
    {
        get
        {
            if (Type == EventType.Cursor && Arguments.Length > 0 && Arguments[0] is Cursor)
            {
                return Arguments[0] as Cursor;
            }
            return Cursors.Default;
        }
    }

    public string RealPointString
    {
        get
        {
            if (Type == EventType.RealPoint && Arguments.Length >= 2)
            {
                if (Arguments[0] is string && Arguments[1] is string)
                {
                    return $"{Arguments[0]}, {Arguments[1]}";
                }
            }
            return string.Empty;
        }
    }

    public string OffsetPointString
    {
        get
        {
            if (Type == EventType.OffsetPoint && Arguments.Length >= 2)
            {
                if (Arguments[0] is string && Arguments[1] is string)
                {
                    return $"{Arguments[0]}, {Arguments[1]}";
                }
            }
            return string.Empty;
        }
    }

    public CADImagingEventArgs()
    {
        Type = default;
        Arguments = null;
    }

    public CADImagingEventArgs(object[] args)
    {
        Type = default;
        Arguments = args;
    }

    public CADImagingEventArgs(EventType type, object[] args)
    {
        Type = type;
        Arguments = args;
    }

    public static CADImagingEventArgs NewCursorUpdatedEventArgs(Cursor cursor)
    {
        return new CADImagingEventArgs(EventType.Cursor, [cursor]);
    }

    public static CADImagingEventArgs NewStatusUpdatedEventArgs(params object[] texts)
    {
        return new CADImagingEventArgs(EventType.Status, texts);
    }

    public static CADImagingEventArgs NewRealPointUpdatedEventArgs(DPoint point)
    {
        return new CADImagingEventArgs(EventType.RealPoint,
        [
            point.X.ToString("f2"),
            point.Y.ToString("f2")
        ]);
    }

    public static CADImagingEventArgs NewOffsetPointUpdatedEventArgs(DPoint realPoint, DPoint orgPoint)
    {
        return new CADImagingEventArgs(EventType.OffsetPoint,
        [
            (realPoint.X - orgPoint.X).ToString("f2"),
            (realPoint.Y - orgPoint.Y).ToString("f2")
        ]);
    }
}
