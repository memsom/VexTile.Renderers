namespace VexTile.Renderer.Mvt.AliFlux.GlobalMercator;
/*
    GlobalMercator.cs
    Copyright (c) 2014 Bill Dollins. All rights reserved.
    http://blog.geomusings.com
*************************************************************
    Based on GlobalMapTiles.js - part of Aggregate Map Tools
    Version 1.0
    Copyright (c) 2009 The Bivings Group
    All rights reserved.
    Author: John Bafford

    http://www.bivings.com/
    http://bafford.com/softare/aggregate-map-tools/
*************************************************************
    Based on GDAL2Tiles / globalmaptiles.py
    Original python version Copyright (c) 2008 Klokan Petr Pridal. All rights reserved.
    http://www.klokan.cz/projects/gdal2tiles/

    Permission is hereby granted, free of charge, to any person obtaining a
    copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included
    in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
    OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
    THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
    DEALINGS IN THE SOFTWARE.
*/

public class CoordinatePair
{
    public double X { get; set; }
    public double Y { get; set; }
}