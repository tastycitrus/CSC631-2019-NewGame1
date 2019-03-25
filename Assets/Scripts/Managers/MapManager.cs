﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MapUtils;
using static MapUtils.MapConstants;

public class MapManager : MonoBehaviour
{
	private class MapCell {
        public bool traversable;
        public bool occupied;
        public GameAgent resident;
        public MapCell(bool traversable) {
            this.traversable = traversable;
            occupied = false;
            resident = null;
        }
    }

	public GameObject mapPrefab;

	// config variables
	private int width;
	private int height;
	private float cell_size;
	private Vector3 offset;

	// map data
	private int[,] map_raw;
    private MapCell[,] map;
	private NavigationHandler nav_map;

	private GameManager parentManager = null;
	private TileSelector tileSelector = null;

	private void set_config_variables()
	{
		MapConfiguration config = GameObject.FindGameObjectWithTag("Map").GetComponent<MapConfiguration>();
		this.width = config.width;
		this.height = config.height;
		this.cell_size = config.cell_size;
		this.offset = config.GetOffset();
	}

    // called by gamemanager
    public void Init(GameManager parent)
	{
		parentManager = parent;

		// begin component init
		GameObject mapObject = GameObject.FindGameObjectWithTag("Map");
		if (mapObject == null)
			mapObject = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);

		map_raw = mapObject.GetComponent<MapGenerator>().generate_map();

		nav_map = new NavigationHandler(map_raw);

		set_config_variables();
		map = new MapCell[width, height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				map[x, y] = new MapCell(traversable(map_raw[x, y]));
			}
		}

		tileSelector = mapObject.transform.Find("TileSelector").GetComponent<TileSelector>();
		tileSelector.init_tile_selector(map_raw);
	}

	public GameObject instantiate_randomly(GameObject type)
	{
		System.Random rng = new System.Random(1);

		int x = rng.Next(0, width - 1);
		int y = rng.Next(0, height - 1);

		while (!IsWalkable(new Pos(x, y))) {
			x = rng.Next(0, width - 1);
			y = rng.Next(0, height - 1);
		}

		return instantiate(type, new Pos(x, y));
	}

    public GameObject instantiate(GameObject prefab, Pos pos, GameAgentStats stats = null)
	{
		if (!IsWalkable(pos)) return null;
		
		GameObject clone = Instantiate(prefab, grid_to_world(pos), Quaternion.identity);
		GameAgent agent = clone.GetComponent<GameAgent>();

        if (stats == null) {
            agent.init_agent(pos, new GameAgentStats(CharacterRaceOptions.Human, CharacterClassOptions.Knight, 1, CharacterClassOptions.Sword));
        } else {
            agent.init_agent(pos, stats);
        }

		nav_map.removeTraversableTile(pos);
		map[pos.x, pos.y].resident = agent;
		map[pos.x, pos.y].occupied = true;
		return clone;
	}

	public void de_instantiate(Pos pos)
	{
		Debug.Log("Killing character...");
		Destroy(map[pos.x, pos.y].resident.gameObject, 5.0f);

		nav_map.insertTraversableTile(pos);
		map[pos.x, pos.y].resident = null;
		map[pos.x, pos.y].occupied = false;
	}

	// destroys all game objects currently on the map
	public void clear_map()
	{
		for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
				if (map[x, y].resident != null)
					Destroy(map[x, y].resident.gameObject);
	}
	
	public int getDistance(Pos source, Pos dest)
	{
		List<Pos> path = get_path(source, dest);
		if (path == null) return -1;
		
		int distance = 0;
		for (int i = 1; i < path.Count; i++)
			distance += Pos.abs_dist(path[i], path[i-1]);
		return distance;
	}
	
	// Gets a list of map distances from a source point to a number of destination points
	/* <param name="source"> 
	 * 		the origin point that paths are searched relative to </param>
	 * <param name="destinations">
	 * 		the list of destination points we want to find distances to </param>
	 * <param name="preserve_null"> 
	 * 		when a path is not found, by default it is not added as an entry to the results list
	 *		when preserve_null is set to true, paths that are not found are instead added as null entries </param>
	 * <param name="maxDistance">
	 *		the maximum allowed distance for resulting distances. By default this value is zero, which means there is no limit to distance </param>
	 *		enabling this can significantly improve pathfinding performance
	 * <returns>
	 * A list of distances from the source to each o the destination points </returns> */
	public List<int> getDistances(Pos source, List<Pos> destinations, bool preserve_null = false, int maxDistance=0)
	{
		// getDistances will ignore whether or not the destination tiles are traversable, just gets distances to them
		foreach (Pos dest in destinations) { if (!IsWalkable(dest)) nav_map.insertTraversableTile(dest); }
		List<List<Pos>> paths = get_paths(source, destinations, preserve_null, maxDistance);
		foreach (Pos dest in destinations) { if (!IsWalkable(dest)) nav_map.removeTraversableTile(dest); }
		
		if (paths.Count == 0) return null;
		
		List<int> distances = new List<int>();
		foreach (List<Pos> path in paths) {
			
			if (path == null) {
				distances.Add(-1);
				continue;
			}
			int distance = 0;
			for (int i = 1; i < path.Count; i++)
				distance += Pos.abs_dist(path[i], path[i-1]);
			distances.Add(distance);
		}
		return distances;
	}
	
	public List<int> getExistingDistances(List<List<Pos>> paths)
	{
		List<int> distances = new List<int>();
		foreach (List<Pos> path in paths) {
			
			if (path == null) {
				distances.Add(-1);
				continue;
			}
			int distance = 0;
			for (int i = 1; i < path.Count; i++)
				distance += Pos.abs_dist(path[i], path[i-1]);
			distances.Add(distance);
		}
		return distances;
	}

	public List<Pos> get_path(Pos source, Pos dest, int maxDistance = 0)
	{
		if (!IsWalkable(source)) nav_map.insertTraversableTile(source);
		List<Pos> result = nav_map.shortestPath(source, dest);
		if (!IsWalkable(source)) nav_map.removeTraversableTile(source);
		return result;
	}

	// Gets a list of paths from a source point to a number of destination points
	/* <param name="source"> 
	 * 		the origin point that paths are searched relative to </param>
	 * <param name="destinations">
	 * 		the list of destination points we want to find paths to </param>
	 * <param name="preserve_null"> 
	 * 		when a path is not found, by default it is not added as an entry to the results list
	 *		when preserve_null is set to true, distances that are not found are instead added as -1 </param>
	 * <param name="maxDistance">
	 *		the maximum allowed distance for resulting paths. By default this value is zero, which means there is no limit to distance
	 *		enabling this can significantly improve pathfinding performance </param>
	 * <returns>
	 * A list of paths from the source to each of the destination points </returns> */
	public List<List<Pos>> get_paths(Pos source, List<Pos> destinations, bool preserve_null = false, int maxDistance = 0)
	{
		List<List<Pos>> results = null;
		
		if (!IsWalkable(source)) nav_map.insertTraversableTile(source);
			if (maxDistance == 0)
				results = nav_map.shortestPathBatched(source, destinations);
			else
				results = nav_map.shortestPathBatchedInRange(source, destinations, maxDistance);
		if (!IsWalkable(source)) nav_map.removeTraversableTile(source);
		
		if (!preserve_null)
			return results;
		else {
			List<List<Pos>> new_results = new List<List<Pos>>();
			int i = 0, j = 0;
			while (i < destinations.Count) {
				if (j < results.Count && destinations[i] == results[j].Last()) {
					new_results.Add(results[j]);
					j++;
				}
				else {
					new_results.Add(null);
				}
				i++;
			}
			return new_results;
		}
	}
	
	public bool move(Pos source, Pos dest)
	{
		if (!map[source.x, source.y].occupied
		 || !IsWalkable(dest)) {
			Debug.Log("Move failed!");
			if (!IsWalkable(dest)) Debug.Log("Because dest wasn't walkable");
			else Debug.Log("Because source wasn't occupied");
			return false;
		}
		
		List<Pos> path = get_path(source, dest);
		GameAgent agent = map[source.x, source.y].resident;
		
		if (path != null) {
			nav_map.removeTraversableTile(dest);
			map[dest.x, dest.y].occupied = true;
			map[dest.x, dest.y].resident = agent;
			
			nav_map.insertTraversableTile(source);
			map[source.x, source.y].occupied = false;
			map[source.x, source.y].resident = null;
			
			StartCoroutine(agent.smooth_movement(path));
			return true;
		}
		else return false;
	}

    public bool IsTraversable(Pos pos)
	{
		//Debug.Log("Ok, testing, map value is " + map[pos.x, pos.y].traversable);
		if (pos.x >= width || pos.x < 0 || pos.y >= height || pos.y < 0)
			return false;
		return map[pos.x, pos.y].traversable;
    }

    public bool IsOccupied(Pos pos) {
        if (pos.x >= width || pos.x < 0 || pos.y >= height || pos.y < 0)
			return false;
		return map[pos.x, pos.y].occupied;
    }
	
	public bool IsWalkable(Pos pos)
	{
		return IsTraversable(pos) && !IsOccupied(pos);
	}

	public bool attack(Pos dest, int damage_amount)
	{
		if (!IsOccupied(dest))
			return false;

		map[dest.x, dest.y].resident.take_damage(damage_amount);
		return true;
	}

    public Transform GetUnitTransform(Pos pos) {
        if (!map[pos.x, pos.y].occupied)
            return null;
        return map[pos.x, pos.y].resident.transform;
    }

	public Vector3 grid_to_world(Pos pos)
	{
		return new Vector3(pos.x * cell_size + cell_size / 2f, 0f, pos.y * cell_size + cell_size / 2f) - offset;
	}

	public Pos world_to_grid(Vector3 pos)
	{
		pos = pos + offset;
		return new Pos((int) pos.x, (int) pos.z);
	}
	
	public void DrawLine(Pos a, Pos b, Color color, float time=5.0f)
	{
		Vector3 origin = grid_to_world(a);
		Vector3 destination = grid_to_world(b);
		
		Debug.DrawLine(origin, destination, color, time);
	}
}
