/*
*                               The MIT License (MIT)
* Permission is hereby granted, free of charge, to any person obtaining a copy of
* this software and associated documentation files (the "Software"), to deal in
* the Software without restriction, including without limitation the rights to
* use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
* the Software, and to permit persons to whom the Software is furnished to do so.
*/

using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;

namespace UVtools.AvaloniaControls;

[TemplatePart("PART_ContentScrollViewer", typeof(ScrollViewer))]
[TemplatePart("PART_ScrollContentPresenter", typeof(ContentPresenter))]
public class ScrollGroupBox : GroupBox
{
}