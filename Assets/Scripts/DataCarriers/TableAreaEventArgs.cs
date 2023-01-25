using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Event Args for setting up the play area
/// </summary>
public class TableAreaEventArgs : EventArgs {
    public int NumberOfColumns { get; private set; }
    public int NumberOfRows { get; private set; }
    public float SpaceBetweenTables { get; private set; }

    public TableAreaEventArgs(int columns, int rows, float spaceBetween) {
        NumberOfColumns = columns;
        NumberOfRows = rows;
        SpaceBetweenTables = spaceBetween;
    }
}