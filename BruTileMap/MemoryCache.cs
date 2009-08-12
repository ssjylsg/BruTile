﻿// Copyright 2008 - Paul den Dulk (Geodan)
// 
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using Tiling;

namespace BruTileMap
{
  public class MemoryCache<T> : ITileCache<T>
  {
    #region Fields
      
    private Dictionary<TileKey, T> bitmaps 
      = new Dictionary<TileKey, T>();

    private Dictionary<TileKey, DateTime> touched
      = new Dictionary<TileKey, DateTime>();

    private object syncRoot = new object();
    private const int upperLimit = 20;
    private const int lowerLimit = 10;
    private const int counter = 0;
    private delegate void SetTileCountDelegate(int count);
    
    #endregion
    
    #region Public Methods

    public void Add(TileKey key, T item)
    {
      lock (syncRoot)
      {
        if (bitmaps.ContainsKey(key))
        {
          bitmaps[key] = item;
          touched[key] = DateTime.Now;
        }
        else
        {
          touched.Add(key, DateTime.Now);
          bitmaps.Add(key, item);
          if (bitmaps.Count > upperLimit) CleanUp();
         }
      }
    }
 
    public void Remove(TileKey key)
    {
      lock (syncRoot)
      {
        if (!bitmaps.ContainsKey(key)) return; //ignore if not exists
        touched.Remove(key);
        bitmaps.Remove(key);
      }
    }

    public T Find(TileKey key)
    {
      lock (syncRoot)
      {
        if (!bitmaps.ContainsKey(key))
        {
          return default(T);
        }
        else
        {
          touched[key] = DateTime.Now;
          return bitmaps[key];
        }
      }
    }

    #endregion

    #region Private Methods

    private void CleanUp()
    {
      lock (syncRoot)
      {
        //Purpose: Remove the older tiles so that the newest x tiles are left.
        TouchPermaCache(touched);
        DateTime cutoff = GetCutOff(touched, lowerLimit);
        List<TileKey> oldItems = GetOldItems(touched, ref cutoff);
        foreach (TileKey key in oldItems)
        {
          Remove(key);
        }
      }
    }

    private void TouchPermaCache(Dictionary<TileKey, DateTime> touched)
    {
      List<TileKey> keys = new List<TileKey>();
      //This is a temporary soluvtion to preserve level zero tiles in memory.
      foreach (TileKey key in touched.Keys) if (key.Level == 0) keys.Add(key); 
      foreach (TileKey key in keys) touched[key] = DateTime.Now;
    }
    
    private static DateTime GetCutOff(Dictionary<TileKey, DateTime> touched,
      int lowerLimit)
    {
      List<DateTime> times = new List<DateTime>();
      foreach (DateTime time in touched.Values)
      {
        times.Add(time);
      }
      times.Sort();
      return times[times.Count - lowerLimit];
    }

    private static List<TileKey> GetOldItems(Dictionary<TileKey, DateTime> touched, 
      ref DateTime cutoff)
    {
      List<TileKey> oldItems = new List<TileKey>();
      foreach (TileKey key in touched.Keys)
      {
        if (touched[key] < cutoff)
        {
          oldItems.Add(key);
        }
      }
      return oldItems;
    }

    #endregion
  }
}
