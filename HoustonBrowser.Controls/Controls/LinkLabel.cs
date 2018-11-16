using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace HoustonBrowser.Controls
{
    public class LinkLabel: Label
    {
        public override IBrush ForegroundBrush {get;set;} = new SolidColorBrush(new Color(255,0,0,102));
        public override void Render(DrawingContext context)
        {
            if(this.Form==null)
            {
                this.Form=new RectangleGeometry(new Rect(this.Left,this.Top,this.Width,this.Height));
            }
            Pen underlinePen=new Pen(this.ForegroundBrush);

            var lines = FormattedText.GetLines().ToList();
            int start = 0;
            for(int i=0; i<lines.Count; i++)
            {
                var bounds = this.FormattedText.HitTestTextRange(start, lines[i].Length).ToList();
                context.DrawLine(underlinePen, bounds[0].BottomLeft+new Point(Left,Top), bounds[0].BottomRight+new Point(Left, Top));
                start=lines[i].Length;
            }

            base.Render(context);
        }       

    }
}