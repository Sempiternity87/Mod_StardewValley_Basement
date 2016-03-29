/*******************************************************************************
* 
*******************************************************************************/
using System;
using System.IO;
using System.Collections.Generic;

using StardewValley;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using xTile.Tiles;
using xTile.Layers;
using xTile;
using StardewValley.Locations;
using System.Reflection;

namespace Basement
  {
  public class BasementModMainClass : Mod
    {
    /***************************************************************************
    * Properties
    ***************************************************************************/
    protected static string modPath      = "";
    protected static int    upgradeLevel = -1;
    
    /***************************************************************************
    * Override: Entry */
    /**
    * 
    ***************************************************************************/
    public override void Entry(params object[] objects) 
      {
      GameEvents    .UpdateTick             += Event_UpdateTick;
      LocationEvents.CurrentLocationChanged += Event_CurrentLocationChanged;

      modPath = this.PathOnDisk;
      }

    /***************************************************************************
    * getLocation */
    /**
    * 
    ***************************************************************************/
    protected static GameLocation getLocation(string locationName)
      {
      foreach (GameLocation location in Game1.locations)
        if (location.name == locationName)
          return location;
      
      return null;
      }
    
    /***************************************************************************
    * addBasement */
    /**
    * 
    ***************************************************************************/
    protected static void addBasement()
      {
      if (getLocation("Basement") == null)
        {
        /*--------------------------------------------------------*/
        /* Load custom map, add location properties as necessary, */
        /* add the location to the game, and add warp points.     */
        /*--------------------------------------------------------*/
        Log.Info("Loading Basement...");
        string relativeAssetPath = Path.Combine(modPath, "Basement.xnb");

        GameLocation basement  = new GameLocation(LoadMap(relativeAssetPath), "Basement");
        FarmHouse    farmHouse = Game1.getLocationFromName("FarmHouse") as FarmHouse;

        int farmHouseX = farmHouse.getEntryLocation().X+4;
        int farmHouseY = farmHouse.getEntryLocation().Y-1;

        Log.Info("Entry [{0}, {1}]", farmHouse.getEntryLocation().X,
                                     farmHouse.getEntryLocation().Y);
        Log.Info("Warp [{0}, {1}]", farmHouseX, farmHouseY);

        basement.isFarm     = false;
        basement.isOutdoors = false;

        Log.Info("Adding warps to Basement...");
        basement.warps.Add(new Warp(11, 2, farmHouse.name, 13, 11, false));
        basement.warps.Add(new Warp(12, 2, farmHouse.name, 14, 11, false));

        Log.Info("Adding Basment to locations for house upgrade {0}...", farmHouse.upgradeLevel);
        Game1.locations.Add(basement);
        }
      }
    
    /***************************************************************************
    * Event_CurrentLocationChanged */
    /**
    * 
    ***************************************************************************/
    protected static void Event_CurrentLocationChanged(object sender, EventArgs e)
      {
      Log.Info("{0} [{1}, {2}]", Game1.currentLocation.name,
                                 Game1.player.getStandingX()/64,
                                 Game1.player.getStandingY()/64);
      
      foreach (Warp warp in Game1.currentLocation.warps)
        Log.Info("{0} [{1}, {2}]", warp.TargetName, warp.TargetX, warp.TargetY);
      }

    /***************************************************************************
    * Event_UpdateTick */
    /**
    * 
    ***************************************************************************/
    protected static void Event_UpdateTick(object sender, EventArgs e)
      {
      if (!(Game1.currentLocation is FarmHouse))
        return;
      
      FarmHouse farmHouse = Game1.currentLocation as FarmHouse;

      if (farmHouse.upgradeLevel == upgradeLevel)
        return;
      
      upgradeLevel = farmHouse.upgradeLevel;
      patchMap(farmHouse);
      }
    
    /***************************************************************************
    * LoadMap */
    /**
    * 
    ***************************************************************************/
    protected static Map LoadMap(string filePath)
      {
      var ext = Path.GetExtension(filePath);
      Map map = null;

      var path = Path.GetDirectoryName(filePath);
      var fileName = Path.GetFileNameWithoutExtension(filePath);
      var cm = new ContentManager(new GameServiceContainer(), path);
      map = cm.Load<Map>(fileName);

      if (map == null) throw new FileLoadException();

      return map;
      }
    
    /***************************************************************************
    * patchMap */
    /**
    * Meticulously planned tile edits for the Farm House xnb files. We skip the
    * FarmHouse.xnb and FarmHouse3.xnb because the former should not have the
    * basement, and the latter doesn't seem to be in use.
    ***************************************************************************/
    protected static void patchMap(FarmHouse farmHouse)
      {
      Log.Info("  Upgrade Level: {0}", farmHouse.upgradeLevel);
      if (farmHouse.upgradeLevel < 1)
        return;
        
      Map map = farmHouse.map;
      Log.Info("Patching map {0}...", farmHouse.name);

      Layer back      = map.GetLayer("Back");
      Layer buildings = map.GetLayer("Buildings");
      Layer front     = map.GetLayer("Front");
      Point entry     = farmHouse.getEntryLocation();

      TileSheet indoorSheet    = map.TileSheets[0];
      TileSheet untitledSheet  = map.TileSheets[1];
      TileSheet wallsAndFloots = map.TileSheets[2];

      int stairTile       = 181;
      int leftWall        = 64;
      int rightWall       = 68;
      int topRightWall    = 163;
      int topLeftWall     = 162;
      int bottomLeftWall  = 96;
      int bottomRightWall = 130;
      int bottomWall      = 165;

      int x = entry.X+3;
      int y = entry.Y;
      
      Tile backTile  = back     .Tiles[x, y];
      Tile buildTile = buildings.Tiles[x, y];
      Tile frontTile = front    .Tiles[x, y];
      
      /*--------------------*/
      /* Change Back tiles. */
      /*--------------------*/
      Log.Info("Adding the back tiles...");
      back.Tiles[x+1, y] = (Tile)new StaticTile(back, untitledSheet, BlendMode.Alpha, stairTile);
      back.Tiles[x+2, y] = (Tile)new StaticTile(back, untitledSheet, BlendMode.Alpha, stairTile);
      
      /*------------------------*/
      /* Change Building tiles. */
      /*------------------------*/
      Log.Info("Adding the building tiles...");
      buildings.Tiles[x,   y-1] = null;
      buildings.Tiles[x,   y]   = (Tile)new StaticTile(buildings, indoorSheet, BlendMode.Alpha, leftWall);
      buildings.Tiles[x+1, y-1] = null;
      buildings.Tiles[x+1, y]   = null;
      buildings.Tiles[x+2, y-1] = null;
      buildings.Tiles[x+2, y]   = null;
      buildings.Tiles[x+3, y-1] = null;
      buildings.Tiles[x+3, y]   = (Tile)new StaticTile(buildings, indoorSheet, BlendMode.Alpha, rightWall);
      
      /*---------------------*/
      /* Change Front tiles. */
      /*---------------------*/
      Log.Info("Adding the front tiles...");
      front.Tiles[x,   y-1] = (Tile)new StaticTile(front, indoorSheet, BlendMode.Alpha, topLeftWall);
      front.Tiles[x,   y]   = (Tile)new StaticTile(front, indoorSheet, BlendMode.Alpha, bottomLeftWall);
      front.Tiles[x+1, y-1] = null;
      front.Tiles[x+1, y]   = (Tile)new StaticTile(front, indoorSheet, BlendMode.Alpha, bottomWall);
      front.Tiles[x+2, y-1] = null;
      front.Tiles[x+2, y]   = (Tile)new StaticTile(front, indoorSheet, BlendMode.Alpha, bottomWall);
      front.Tiles[x+3, y-1] = (Tile)new StaticTile(front, indoorSheet, BlendMode.Alpha, topRightWall);
      front.Tiles[x+3, y]   = (Tile)new StaticTile(front, indoorSheet, BlendMode.Alpha, bottomRightWall);
      
      /*-------------------------------*/
      /* Adding warps to the basement. */
      /*-------------------------------*/
      Log.Info("Adding the warp points...");
      farmHouse.warps.Add(new Warp(x+1, y+1, "Basement", 11, 3, false));
      farmHouse.warps.Add(new Warp(x+2, y+1, "Basement", 12, 3, false));

      Log.Info("Map Patched");
      addBasement();
      }
    }
  }
