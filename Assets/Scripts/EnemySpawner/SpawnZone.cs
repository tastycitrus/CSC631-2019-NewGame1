﻿using System.Collections.Generic;
using UnityEngine;

// A number of tiles which enemies are able to spawn in
public class SpawnZone {
    // Center of the Spawn Zone
    private Vector3 position;
    // Reach of the Spawn Zone
    private float radius;
    // Traversable unpopulated tiles within the Spawn Zone
    private List<Vector3> unpopulatedZoneTiles;
    // Populated tiles within the Spawn Zone
    private List<Vector3> populatedZoneTiles;

    private bool isPopulated = false;

    public SpawnZone(Vector3 position, float radius) {
        this.position = position;
        this.radius = radius;
        unpopulatedZoneTiles = new List<Vector3>();
        populatedZoneTiles = new List<Vector3>();
    }

    // Returns the center of the Spawn Zone
    public Vector3 GetPosition() {
        return position;
    }

    // Returns the radius of the Spawn Zone
    public float GetRadius() {
        return radius;
    }

    // Sets the traversable tiles within the radius of the Spawn Zone
    public void SetZoneTiles(List<Vector3> unpopulatedZoneTiles) {
        this.unpopulatedZoneTiles = unpopulatedZoneTiles;
    }

    // Gets the traversable tiles within the radius of the Spawn Zone
    public List<Vector3> GetUnpopulatedZoneTiles() {
        return unpopulatedZoneTiles;
    }

    // Returns the number of unpopulated traversable tiles within the radius
    // of the Spawn Zone
    public int GetNumberOfUnpopulatedTilesInZone() {
        return unpopulatedZoneTiles.Count;
    }

    // Returns the number of populated tiles within the radius
    // of the Spawn Zone
    public int GetNumberOfPopulatedTilesInZone() {
        return populatedZoneTiles.Count;
    }

    public bool IsPopulated() {
        return isPopulated;
    }

    public void PopulateTiles(List<Vector3> populatedZoneTiles) {
        if (populatedZoneTiles.Count > 0) {
            isPopulated = true;
            this.populatedZoneTiles = populatedZoneTiles;
        }
    }
}
