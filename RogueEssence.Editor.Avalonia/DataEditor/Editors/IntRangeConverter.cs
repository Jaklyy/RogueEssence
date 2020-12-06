﻿using System;
using System.Collections.Generic;
using System.Text;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using System.Drawing;
using RogueElements;
using Avalonia.Controls;
using RogueEssence.Dev.Views;
using System.Collections;
using Avalonia;
using System.Reactive.Subjects;

namespace RogueEssence.Dev
{
    public class IntRangeConverter : EditorConverter<IntRange>
    {
        public override void LoadClassControls(StackPanel control, string name, Type type, object[] attributes, IntRange member, bool isWindow)
        {
            LoadLabelControl(control, name);

            Avalonia.Controls.Grid innerPanel = DataEditor.getSharedRowPanel(4);

            TextBlock lblX = new TextBlock();
            lblX.Text = "Min:";
            innerPanel.Children.Add(lblX);
            lblX.SetValue(Avalonia.Controls.Grid.ColumnProperty, 0);

            NumericUpDown nudValueX = new NumericUpDown();
            nudValueX.Margin = new Thickness(4, 0, 0, 0);
            nudValueX.Minimum = Int32.MinValue;
            nudValueX.Maximum = Int32.MaxValue;
            nudValueX.Value = (member == null) ? 0 : ((IntRange)member).Min;
            innerPanel.Children.Add(nudValueX);
            nudValueX.SetValue(Avalonia.Controls.Grid.ColumnProperty, 1);

            TextBlock lblY = new TextBlock();
            lblY.Margin = new Thickness(8, 0, 0, 0);
            lblY.Text = "Map:";
            innerPanel.Children.Add(lblY);
            lblY.SetValue(Avalonia.Controls.Grid.ColumnProperty, 2);

            NumericUpDown nudValueY = new NumericUpDown();
            nudValueY.Margin = new Thickness(4, 0, 0, 0);
            nudValueY.Minimum = Int32.MinValue;
            nudValueY.Maximum = Int32.MaxValue;
            nudValueY.Value = (member == null) ? 0 : ((IntRange)member).Max;
            innerPanel.Children.Add(nudValueY);
            nudValueY.SetValue(Avalonia.Controls.Grid.ColumnProperty, 3);

            control.Children.Add(innerPanel);
        }


        public override void SaveClassControls(StackPanel control, string name, Type type, object[] attributes, ref IntRange member, bool isWindow)
        {
            int controlIndex = 0;
            controlIndex++;
            Avalonia.Controls.Grid innerControl = (Avalonia.Controls.Grid)control.Children[controlIndex];
            int innerControlIndex = 0;

            innerControlIndex++;
            NumericUpDown nudValueX = (NumericUpDown)innerControl.Children[innerControlIndex];
            innerControlIndex++;
            innerControlIndex++;
            NumericUpDown nudValueY = (NumericUpDown)innerControl.Children[innerControlIndex];
            member = new IntRange((int)nudValueX.Value, (int)nudValueY.Value);
            innerControlIndex++;
        }
    }
}
