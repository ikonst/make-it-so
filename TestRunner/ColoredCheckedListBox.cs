using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace TestRunner
{
    /// <summary>
    /// A checked list box that colors the rows depending on whether
    /// our tests have passed or failed.
    /// </summary>
    public class ColoredCheckedListBox : CheckedListBox
    {
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // We find the background color...
            Color backgroundColor = SystemColors.Window;
            if (e.Index < Items.Count)
            {
                SolutionInfo solutionInfo = (SolutionInfo)Items[e.Index];
                backgroundColor = solutionInfo.BackgroundColor;
            }
            DrawItemEventArgs e2 =
                new DrawItemEventArgs
                (
                    e.Graphics,
                    e.Font,
                    new Rectangle(e.Bounds.Location, e.Bounds.Size),
                    e.Index,
                    e.State,
                    e.ForeColor,
                    backgroundColor
                );

            base.OnDrawItem(e2);
        }
    }
}
