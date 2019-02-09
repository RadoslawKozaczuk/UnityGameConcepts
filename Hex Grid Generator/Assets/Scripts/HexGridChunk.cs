using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    // Water and water edge have different meshes and shaders
    public HexMesh Terrain, Rivers, Roads, Water, WaterShore, Estuaries;
    public FeatureManager Features;

	// we have three predefined splat map configurations
	// all three colors are blended on the connection triangles when all surrounding hexes have different color
	static Color _color1 = new Color(1f, 0f, 0f);
	static Color _color2 = new Color(0f, 1f, 0f);
	static Color _color3 = new Color(0f, 0f, 1f);

	HexCell[] _cells;
    Canvas _gridCanvas;

    void Awake()
    {
        _gridCanvas = GetComponentInChildren<Canvas>();
        _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
    }

    void LateUpdate()
    {
        TriangulateAllCells();
        enabled = false;
    }

    public void Refresh() => enabled = true;

    public void AddCell(int index, HexCell cell)
    {
        _cells[index] = cell;
        cell.Chunk = this;
        cell.transform.SetParent(transform, false);
        cell.UiRect.SetParent(_gridCanvas.transform, false);
    }

    public void ShowUI(bool visible) => _gridCanvas.gameObject.SetActive(visible);

    public void TriangulateAllCells()
    {
        // This method could be invoked at any time, even when cells have already been triangulated earlier.
        // So we should begin by clearing the old data.
        Terrain.Clear();
        Rivers.Clear();
        Roads.Clear();
        Water.Clear();
        WaterShore.Clear();
        Estuaries.Clear();
        Features.Clear();

        for (int i = 0; i < _cells.Length; i++)
            Precalculation(_cells[i]);

        for (int i = 0; i < _cells.Length; i++)
        {
            TriangulateCell(_cells[i]);
            AddFeatures(_cells[i]);
        }

        Terrain.Apply();
        Rivers.Apply();
        Roads.Apply();
        Water.Apply();
        WaterShore.Apply();
        Estuaries.Apply();
        Features.Apply();
    }

    void Precalculation(HexCell cell)
    {
        cell.Center = cell.WaterCenter = cell.transform.localPosition;
        cell.WaterCenter.y = HexMetrics.WaterSurfaceY;

        for (int i = 0; i <= 5; i++)
        {
            var direction = (HexDirection)i;

            cell.Edges[i] = new EdgeVertices(
                cell.Center + HexMetrics.GetLeftSolidCorner(direction),
                cell.Center + HexMetrics.GetRightSolidCorner(direction));

            if (cell.HasRiver && cell.HasRiverThroughEdge(direction))
                cell.Edges[i].V3.y = cell.StreamBedY;

            cell.WaterEdges[i] = new EdgeVertices(
                cell.WaterCenter + HexMetrics.GetLeftWaterCorner(direction),
                cell.WaterCenter + HexMetrics.GetRightWaterCorner(direction));
        }
    }

    void TriangulateCell(HexCell cell)
    {
        var center = cell.Center;

        // hexagon itself is made of 12 triangles to add some variety
        for (int i = 0; i <= 5; i++)
        {
            var direction = (HexDirection)i;
            var edge = cell.Edges[(int)direction];

            if (cell.HasRiver)
            {
                if (cell.HasRiverThroughEdge(direction))
                {
                    if (cell.HasRiverBeginOrEnd)
                        TriangulateWithRiverBeginOrEnd(direction, cell);
                    else
                        TriangulateWithRiver(direction, cell);
                }
                else
                {
                    // draw the inner circles as two sets of triangles
                    TriangulateAdjacentToRiver(direction, cell, cell.Edges[i]);
                }
            }
            else
            {
                TriangulateEdgeFan(center, edge, cell.TerrainTypeIndex);

                if (cell.HasRoads)
                {
                    var interpolators = GetRoadInterpolators(direction, cell);
                    TriangulateRoad(center,
                        Vector3.Lerp(center, edge.V1, interpolators.x),
                        Vector3.Lerp(center, edge.V5, interpolators.y),
                        edge, cell.HasRoadThroughEdge(direction));
                }
            }

            if (cell.IsUnderwater)
                TriangulateWater(direction, cell);
        }

        for (int i = 0; i <= 2; i++)
            TriangulateConnection((HexDirection)i, cell, cell.Edges[i]);
    }

    void AddFeatures(HexCell cell)
    {
        for (int i = 0; i <= 5; i++)
        {
            var direction = (HexDirection)i;
            if (!cell.IsUnderwater && !cell.HasRiverThroughEdge(direction) && !cell.HasRoadThroughEdge(direction))
            {
                var edge = cell.Edges[i];
                Features.AddFeature((cell.Center + edge.V1 + edge.V5) * (1f / 3f));
            }
        }
    }

    // Each corner is connected to three edges, which could be flats, slopes, or cliffs. So there are many possible configurations.
    void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        // If both edges are slopes, then we have terraces on both the left and the right side.
        // Also, because the bottom cell is the lowest, we know that those slopes go up.
        // Furthermore, this means that the left and right cell have the same elevation, so the top edge connection is flat.
        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            else if (rightEdgeType == HexEdgeType.Flat)
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            else
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);

            return;
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            else
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);

            return;
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            else
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);

            return;
        }

        Terrain.AddTriangle(bottom, left, right);
        Terrain.AddTriangleColor(_color1, _color2, _color3);
		Vector3 types;
		types.x = bottomCell.TerrainTypeIndex;
		types.y = leftCell.TerrainTypeIndex;
		types.z = rightCell.TerrainTypeIndex;
		Terrain.AddTriangleTerrainTypes(types);
	}

    void TriangulateCornerTerraces(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(_color1, _color2, 1);
        Color c4 = HexMetrics.TerraceLerp(_color1, _color3, 1);
		Vector3 types;
		types.x = beginCell.TerrainTypeIndex;
		types.y = leftCell.TerrainTypeIndex;
		types.z = rightCell.TerrainTypeIndex;

		Terrain.AddTriangle(begin, v3, v4);
        Terrain.AddTriangleColor(_color1, c3, c4);
		Terrain.AddTriangleTerrainTypes(types);

		for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(_color1, _color2, i);
            c4 = HexMetrics.TerraceLerp(_color1, _color3, i);
            Terrain.AddQuad(v1, v2, v3, v4);
            Terrain.AddQuadColor(c1, c2, c3, c4);
			Terrain.AddQuadTerrainTypes(types);
		}

        Terrain.AddQuad(v3, v4, left, right);
        Terrain.AddQuadColor(c3, c4, _color2, _color3);
		Terrain.AddQuadTerrainTypes(types);
	}

    void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float boundaryInterpolator = 1f / (rightCell.Elevation - beginCell.Elevation);

        // boundary interpolators should not be negative
        if (boundaryInterpolator < 0)
            boundaryInterpolator = -boundaryInterpolator;

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), boundaryInterpolator);
        Color boundaryColor = Color.Lerp(_color1, _color2, boundaryInterpolator);
		Vector3 types;
		types.x = beginCell.TerrainTypeIndex;
		types.y = leftCell.TerrainTypeIndex;
		types.z = rightCell.TerrainTypeIndex;

		TriangulateBoundaryTriangle(begin, _color3, left, _color1, boundary, boundaryColor, types);

        // complete the top part
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, _color2, right, _color3, boundary, boundaryColor, types);
        }
        else
        {
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(_color2, _color3, boundaryColor);
			Terrain.AddTriangleTerrainTypes(types);
		}
    }

    // mirrored version of the above
    // this one covers the case when
    // 1 - 2
    //  \ /
    //   0
    void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float boundaryInterpolator = 1f / (leftCell.Elevation - beginCell.Elevation);

        // boundary interpolators should not be negative
        if (boundaryInterpolator < 0)
            boundaryInterpolator = -boundaryInterpolator;

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), boundaryInterpolator);
        Color boundaryColor = Color.Lerp(_color1, _color2, boundaryInterpolator);
		Vector3 types;
		types.x = beginCell.TerrainTypeIndex;
		types.y = leftCell.TerrainTypeIndex;
		types.z = rightCell.TerrainTypeIndex;

		TriangulateBoundaryTriangle(right, _color1, begin, _color2, boundary, boundaryColor, types);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, _color2, right, _color3, boundary, boundaryColor, types);
        }
        else
        {
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(_color2, _color3, boundaryColor);
			Terrain.AddTriangleTerrainTypes(types);
		}
    }

    void TriangulateBoundaryTriangle(
		Vector3 begin, Color beginColor,
		Vector3 left, Color leftColor,
		Vector3 boundary, Color boundaryColor,
		Vector3 types)
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);

        Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        Terrain.AddTriangleColor(beginColor, c2, boundaryColor);
		Terrain.AddTriangleTerrainTypes(types);

		for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginColor, leftColor, i);
            Terrain.AddTriangleUnperturbed(v1, v2, boundary);
            Terrain.AddTriangleColor(c1, c2, boundaryColor);
			Terrain.AddTriangleTerrainTypes(types);
		}

        Terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        Terrain.AddTriangleColor(c2, leftColor, boundaryColor);
		Terrain.AddTriangleTerrainTypes(types);
	}

    // Placing the left and right vertices halfway between the center and corners is ﬁne, when there's a road adjacent to them.
    // But when there isn't, it results in a bulge. To counter this, we could place the vertices closer to the center in those cases.
    // Speciﬁcally, by interpolating with ¼ instead of with ½.
    Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        // X component is the interpolator for the left point, sY component is the interpolator for the right point
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction))
        {
            // if there's a road going in the current direction, we can put the points halfway
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            // Otherwise, it depends. For the left point, we can use ½ when there's a road going through the previous direction.
            // If not, we should use ¼. The same goes for the right point, but with the next direction.
            interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }

        return interpolators;
    }

    /// <summary>
    /// Fill the cell triangle with a strip and a fan. We cannot suffice with a single fan,
    /// because we have to make sure that we match the middle edge of the parts that do contain a river.
    /// </summary>
    void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, EdgeVertices e)
    {
        if (cell.HasRoads)
            TriangulateRoadAdjacentToRiver(direction, cell, e);

        var center = cell.Center;

        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            // we are inside a curve
            if (cell.HasRiverThroughEdge(direction.Previous()))
                center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.InnerToOuter * 0.5f);
            // we are inside a straight line
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
                center += HexMetrics.GetLeftSolidCorner(direction) * 0.25f;
        }
        // The final case is when we have a river in the previous direction, and it is a straight one.
        // That requires moving the center towards the next solid corner.
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
            center += HexMetrics.GetRightSolidCorner(direction) * 0.25f;

        var m = new EdgeVertices(Vector3.Lerp(center, e.V1, 0.5f), Vector3.Lerp(center, e.V5, 0.5f));
        m.V3.y = e.V3.y;

        TriangulateEdgeStrip(m, _color1, cell.TerrainTypeIndex, e, _color1, cell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);
    }

    void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell)
    {
        var center = cell.Center;
        var closerEdge = cell.Edges[(int)direction];

        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
                center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.InnerToOuter * 0.5f);
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
                center += HexMetrics.GetLeftSolidCorner(direction) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
        {
            center += HexMetrics.GetRightSolidCorner(direction) * 0.25f;
        }

        var m = new EdgeVertices(Vector3.Lerp(center, closerEdge.V1, 0.5f), Vector3.Lerp(center, closerEdge.V5, 0.5f));
        m.V3.y = closerEdge.V3.y; // reassign middle verticle height as it is ommited in the calculation above

        TriangulateEdgeStrip(m, _color1, cell.TerrainTypeIndex, closerEdge, _color1, cell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);

        // river segments are added only if the current segment is not under water
        if (!cell.IsUnderwater)
        {
            bool reversed = cell.HasIncomingRiver;

            // outer circle of the hex
            TriangulateRiverQuadUnperturbed(m.V2, m.V4, closerEdge.V2, closerEdge.V4, cell.RiverSurfaceY, 0.6f, reversed);

            // end (or start) triangle
            center.y = m.V2.y = m.V4.y = cell.RiverSurfaceY;
            Rivers.AddTriangleUnperturbed(center, m.V2, m.V4);

            if (reversed)
                Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            else
                Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
        }
    }

    void TriangulateWithRiver(HexDirection direction, HexCell cell)
    {
        var center = cell.Center;
        var closerEdge = cell.Edges[(int)direction];

        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
                center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.InnerToOuter * 0.5f);
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
                center += HexMetrics.GetLeftSolidCorner(direction) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
        {
            center += HexMetrics.GetRightSolidCorner(direction) * 0.25f;
        }

        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            centerL = center + HexMetrics.GetLeftSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetRightSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, closerEdge.V5, 0.65f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, closerEdge.V1, 0.65f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.InnerToOuter);
        }
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.InnerToOuter);
            centerR = center;
        }

        // after deciding where the left and right points are, we can determine the final center by averaging them
        center = Vector3.Lerp(centerL, centerR, 0.5f);

        var m = new EdgeVertices(Vector3.Lerp(centerL, closerEdge.V1, 0.5f), Vector3.Lerp(centerR, closerEdge.V5, 0.5f), 1f / 12f);
        m.V3.y = closerEdge.V3.y;

        // external hex circle
        TriangulateEdgeStrip(m, _color1, cell.TerrainTypeIndex, closerEdge, _color1, cell.TerrainTypeIndex);

        // connection between hexes
        Terrain.AddTriangle(centerL, m.V1, m.V2);
        Terrain.AddTriangleColor(_color1);

        Terrain.AddQuad(centerL, new Vector3(center.x, cell.StreamBedY, center.z), m.V2, m.V3);
        Terrain.AddQuadColor(_color1);
        Terrain.AddQuad(new Vector3(center.x, cell.StreamBedY, center.z), centerR, m.V3, m.V4);
        Terrain.AddQuadColor(_color1);

        Terrain.AddTriangle(centerR, m.V4, m.V5);
        Terrain.AddTriangleColor(_color1);

		Vector3 types;
		types.x = types.y = types.z = cell.TerrainTypeIndex;
		Terrain.AddTriangleTerrainTypes(types);
		Terrain.AddQuadTerrainTypes(types);
		Terrain.AddQuadTerrainTypes(types);
		Terrain.AddTriangleTerrainTypes(types);

		// create river quads
		if (!cell.IsUnderwater)
        {
            bool reversed = cell.IncomingRiver == direction;

            // inner fan of the hex
            TriangulateRiverQuadUnperturbed(centerL, centerR, m.V2, m.V4, cell.RiverSurfaceY, 0.4f, reversed);

            // external circle of the hex
            var neighbor = cell.GetNeighbor(direction);
            if (neighbor.IsUnderwater)
            {
                TriangulateRiverQuadUnperturbed(m.V2, m.V4, closerEdge.V2, closerEdge.V4,
                    cell.RiverSurfaceY, HexMetrics.WaterSurfaceY, 0.6f, reversed);
            }
            // normal connection between two rivers
            else
            {
                TriangulateRiverQuadUnperturbed(m.V2, m.V4, closerEdge.V2, closerEdge.V4, cell.RiverSurfaceY, 0.6f, reversed);
            }
        }
    }

    /// <summary>
    /// Creates four triangles from the center to the edge strip
    /// </summary>
    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float type)
    {
        Terrain.AddTriangle(center, edge.V1, edge.V2);
        Terrain.AddTriangle(center, edge.V2, edge.V3);
        Terrain.AddTriangle(center, edge.V3, edge.V4);
        Terrain.AddTriangle(center, edge.V4, edge.V5);

		Terrain.AddTriangleColor(_color1);
		Terrain.AddTriangleColor(_color1);
		Terrain.AddTriangleColor(_color1);
		Terrain.AddTriangleColor(_color1);

		Vector3 types;
		types.x = types.y = types.z = type;
		Terrain.AddTriangleTerrainTypes(types);
		Terrain.AddTriangleTerrainTypes(types);
		Terrain.AddTriangleTerrainTypes(types);
		Terrain.AddTriangleTerrainTypes(types);
	}

    /// <summary>
    /// Creates four quads betweeen edge one and edge.
    /// Color of these quads is a gradient between color one and color two.
    /// If hasRoad is true the function creates a road segment.
    /// </summary>
    void TriangulateEdgeStrip(
		EdgeVertices e1, Color c1, float type1,
		EdgeVertices e2, Color c2, float type2,
		bool hasRoad = false)
    {
        Terrain.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2);
        Terrain.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3);
        Terrain.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4);
        Terrain.AddQuad(e1.V4, e1.V5, e2.V4, e2.V5);

		Terrain.AddQuadColor(c1, c2);
		Terrain.AddQuadColor(c1, c2);
		Terrain.AddQuadColor(c1, c2);
		Terrain.AddQuadColor(c1, c2);

		Vector3 types;
		types.x = types.z = type1;
		types.y = type2;
		Terrain.AddQuadTerrainTypes(types);
		Terrain.AddQuadTerrainTypes(types);
		Terrain.AddQuadTerrainTypes(types);
		Terrain.AddQuadTerrainTypes(types);

		if (hasRoad)
            TriangulateRoadSegment(e1.V2, e1.V3, e1.V4, e2.V2, e2.V3, e2.V4);
    }

    // Every two hexagons are connected by a single rectangular bridge.
    // And every three hexagons are connected by a single triangle.
    void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices edgeBegin)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null) return;

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        var edgeEnd = new EdgeVertices(edgeBegin.V1 + bridge, edgeBegin.V5 + bridge);

        if (cell.HasRiverThroughEdge(direction))
        {
            // only if not underwater
            if (!cell.IsUnderwater)
            {
                // neighbor is underwater
                if (neighbor.IsUnderwater)
                {
                    // waterfall
                    if (cell.Elevation > neighbor.WaterLevel)
                    {
                        TriangulateWaterfallInWater(
                            edgeBegin.V2, edgeBegin.V4, edgeEnd.V2, edgeEnd.V4,
                            cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                            HexMetrics.WaterSurfaceY);
                    }
                }
                // connection between river quads
                else
                {
                    edgeEnd.V3.y = neighbor.StreamBedY;
                    TriangulateRiverQuadUnperturbed(edgeBegin.V2, edgeBegin.V4, edgeEnd.V2, edgeEnd.V4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        0.8f, cell.HasIncomingRiver && cell.IncomingRiver == direction);
                }
            }
            else if (!neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel)
            {
                TriangulateWaterfallInWater(
                    edgeEnd.V4, edgeEnd.V2, edgeBegin.V4, edgeBegin.V2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    HexMetrics.WaterSurfaceY);
            }
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            TriangulateEdgeStairs(edgeBegin, cell, edgeEnd, neighbor, cell.HasRoadThroughEdge(direction));
        else
            TriangulateEdgeStrip(
				edgeBegin, _color1, cell.TerrainTypeIndex,
				edgeEnd, _color2, neighbor.TerrainTypeIndex,
				cell.HasRoadThroughEdge(direction));

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.East && nextNeighbor != null)
        {
            Vector3 v5 = edgeBegin.V5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                    TriangulateCorner(edgeBegin.V5, cell, edgeEnd.V5, neighbor, v5, nextNeighbor);

                // If the innermost check fails, it means that the next neighbor is the lowest cell.
                // We have to rotate the triangle counterclockwise to keep it correctly oriented.
                else
                    TriangulateCorner(v5, nextNeighbor, edgeBegin.V5, cell, edgeEnd.V5, neighbor);
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
                TriangulateCorner(edgeEnd.V5, neighbor, v5, nextNeighbor, edgeBegin.V5, cell);
            else
                TriangulateCorner(v5, nextNeighbor, edgeBegin.V5, cell, edgeEnd.V5, neighbor);
        }
    }

    /// <summary>
    /// Traingulates (creates) stairs between two hexes
    /// </summary>
    void TriangulateEdgeStairs(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad)
    {
        var e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(_color1, _color2, 1);

        TriangulateEdgeStrip(
			begin, _color1, beginCell.TerrainTypeIndex,
			e2, c2, endCell.TerrainTypeIndex,
			hasRoad);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(_color1, _color2, i);
            TriangulateEdgeStrip(
				e1, c1, beginCell.TerrainTypeIndex,
				e2, c2, endCell.TerrainTypeIndex,
				hasRoad);
        }

        TriangulateEdgeStrip(
			e2, c2, beginCell.TerrainTypeIndex,
			end, _color2, endCell.TerrainTypeIndex,
			hasRoad);
    }

    /// <summary>
    /// Creates a river quad and adds uvs to it.
    /// Height1 is applied to v1 and v2 vectors and height2 is applied to v3 and v4 vectors.
    /// Therefore vector original y values are ignored.
    /// </summary>
    void TriangulateRiverQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float height1, float height2, float uv, bool reversed)
    {
        v1.y = v2.y = height1;
        v3.y = v4.y = height2;
        Rivers.AddQuadUnperturbed(v1, v2, v3, v4);

        if (reversed)
            Rivers.AddQuadUV(1f, 0f, 0.8f - uv, 0.6f - uv);
        else
            Rivers.AddQuadUV(0f, 1f, uv, uv + 0.2f);
    }

    /// <summary>
    /// Creates a river quad and adds uvs to it.
    /// Height parameter is applied to all vector parameters.
    /// Therefore vector original y values are ignored.
    /// </summary>
    void TriangulateRiverQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float height, float uv, bool reversed)
        => TriangulateRiverQuadUnperturbed(v1, v2, v3, v4, height, height, uv, reversed);

    void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6)
    {
        Roads.AddQuad(v1, v2, v4, v5);
        Roads.AddQuad(v2, v3, v5, v6);
        Roads.AddQuadUV(0f, 1f, 0f, 0f);
        Roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices edge, bool hasRoadThroughCellEdge)
    {
        if (hasRoadThroughCellEdge)
        {
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, edge.V2, edge.V3, edge.V4);
            Roads.AddTriangle(center, mL, mC);
            Roads.AddTriangle(center, mC, mR);
            Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        Roads.AddTriangle(center, mL, mR);
        Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
    }

    void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, EdgeVertices e)
    {
        var center = cell.Center;

        // produce partial roads in cells with rivers. The directions with rivers through them will cut gaps in the roads.
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
        Vector3 roadCenter = center;

        if (cell.HasRiverBeginOrEnd)
            roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            Vector3 corner;
            if (previousHasRiver)
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
                    return;
                corner = HexMetrics.GetRightSolidCorner(direction);
            }
            else
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
                    return;
                corner = HexMetrics.GetLeftSolidCorner(direction);
            }

            roadCenter += corner * 0.5f;

            // add bridges
            if (cell.IncomingRiver == direction.Next()
                && (cell.HasRoadThroughEdge(direction.Next2()) || cell.HasRoadThroughEdge(direction.Opposite())))
            {
                Features.AddBridge(roadCenter, center - corner * 0.5f);
            }

            center += corner * 0.25f;
        }
        // in case of zigzags
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
        {
            roadCenter -= HexMetrics.GetRightCorner(cell.IncomingRiver) * 0.2f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            roadCenter -= HexMetrics.GetLeftCorner(cell.IncomingRiver) * 0.2f;
        }
        // in case of curved rivers
        else if (previousHasRiver && nextHasRiver)
        {
            if (!hasRoadThroughEdge)
                return;

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.InnerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        // outside of the curved river
        else
        {
            HexDirection middle;
            if (previousHasRiver)
                middle = direction.Next();
            else if (nextHasRiver)
                middle = direction.Previous();
            else
                middle = direction;

            // get rid off roads on the other side of the river
            if (!cell.HasRoadThroughEdge(middle)
                && !cell.HasRoadThroughEdge(middle.Previous())
                && !cell.HasRoadThroughEdge(middle.Next()))
                return;

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offset * 0.25f;

            // prevent duplications
            if (direction == middle && cell.HasRoadThroughEdge(direction.Opposite()))
                Features.AddBridge(roadCenter, center - offset * (HexMetrics.InnerToOuter * 0.7f));
        }

        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 mL = Vector3.Lerp(roadCenter, e.V1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.V5, interpolators.y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        // close the gaps
        if (previousHasRiver)
            TriangulateRoadEdge(roadCenter, center, mL);
        if (nextHasRiver)
            TriangulateRoadEdge(roadCenter, mR, center);
    }

    void TriangulateWater(HexDirection direction, HexCell cell)
    {
        EdgeVertices closerWaterEdge = cell.WaterEdges[(int)direction];

        // water fan
        Water.AddTriangleUnperturbed(cell.WaterCenter, closerWaterEdge.V1, closerWaterEdge.V5);

        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null) return;

        // estuary
        if (!neighbor.IsUnderwater && cell.HasRiverThroughEdge(direction))
            TriangulateEstuary(cell, direction);

        EdgeVertices furtherEdge = neighbor.Edges[(int)direction.Opposite()];
        EdgeVertices furtherWaterEdge = neighbor.WaterEdges[(int)direction.Opposite()];
        HexDirection nextDirection = direction.Next();
        HexCell nextNeighbor = cell.GetNeighbor(nextDirection);
        HexCell previousNeighbor = cell.GetNeighbor(direction.Previous());

        // neighbor is under water as well
        if (neighbor.IsUnderwater)
        {
            if (direction <= HexDirection.SouthEast)
                Water.AddQuadUnperturbed(closerWaterEdge.V1, closerWaterEdge.V5, furtherWaterEdge.V5, furtherWaterEdge.V1);
        }
        // estuary is on the right side
        else if (nextNeighbor != null && nextNeighbor.HasEstuaryThroughEdge(direction.Previous()))
        {
            Vector3 v4 = furtherEdge.V1;
            v4.y = HexMetrics.WaterSurfaceY;
            WaterShore.AddQuadUnperturbed(closerWaterEdge.V1, closerWaterEdge.V5, furtherWaterEdge.V5, v4);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }
        // estuary is on the left side
        else if (previousNeighbor != null && previousNeighbor.HasEstuaryThroughEdge(direction.Next()))
        {
            Vector3 v3 = furtherEdge.V5;
            v3.y = HexMetrics.WaterSurfaceY;
            WaterShore.AddQuadUnperturbed(closerWaterEdge.V1, closerWaterEdge.V5, v3, furtherWaterEdge.V1);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }
        // standard connection quad
        else if (!cell.HasRiverThroughEdge(direction))
        {
            WaterShore.AddQuadUnperturbed(closerWaterEdge.V1, closerWaterEdge.V5, furtherWaterEdge.V5, furtherWaterEdge.V1);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }

        // calculate connection triangles
        if (neighbor.IsUnderwater)
        {
            if (direction <= HexDirection.East && nextNeighbor != null && nextNeighbor.IsUnderwater)
            {
                Vector3 v3 = nextNeighbor.WaterEdges[(int)nextDirection.Opposite()].V5;
                Water.AddTriangleUnperturbed(closerWaterEdge.V5, furtherWaterEdge.V1, v3);
            }
        }
        // calculate water shore
        else if (cell.HasRiverThroughEdge(direction) && nextNeighbor != null)
        {
            Vector3 v2 = neighbor.Edges[(int)direction.Opposite()].V1;
            v2.y = HexMetrics.WaterSurfaceY;
            Vector3 v3 = nextNeighbor.WaterEdges[(int)nextDirection.Opposite()].V5;

            WaterShore.AddTriangleUnperturbed(closerWaterEdge.V5, v2, v3);
            WaterShore.AddTriangleUV(new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));
        }
        else if (nextNeighbor != null)
        {
            Vector3 v2, v3;
            // estuary is on the right side
            if (cell.HasRiverThroughEdge(nextDirection))
            {
                v2 = furtherWaterEdge.V1;
                v3 = nextNeighbor.Edges[(int)nextDirection.Opposite()].V5;
                v3.y = HexMetrics.WaterSurfaceY;
            }
            // estuary is on the left side
            else if(nextNeighbor.HasEstuaryThroughEdge(direction.Previous()))
            {
                v2 = furtherEdge.V1;
                v2.y = HexMetrics.WaterSurfaceY;
                v3 = nextNeighbor.WaterEdges[(int)nextDirection.Opposite()].V5;
            }
            else
            {
                v2 = furtherWaterEdge.V1;
                v3 = nextNeighbor.WaterEdges[(int)nextDirection.Opposite()].V5;
            }

            WaterShore.AddTriangleUnperturbed(closerWaterEdge.V5, v2, v3);
            WaterShore.AddTriangleUV(new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));
        }
    }

    void TriangulateEstuary(HexCell cell, HexDirection direction)
    {
        var neighbor = cell.GetNeighbor(direction);

        EdgeVertices closerWaterEdge = cell.WaterEdges[(int)direction];
        EdgeVertices furtherWaterEdge = neighbor.WaterEdges[(int)direction.Opposite()];
        EdgeVertices furtherEdge = neighbor.Edges[(int)direction.Opposite()];
        furtherEdge.V1.y = HexMetrics.WaterSurfaceY;
        furtherEdge.V2.y = HexMetrics.WaterSurfaceY;
        furtherEdge.V3.y = HexMetrics.WaterSurfaceY;
        furtherEdge.V4.y = HexMetrics.WaterSurfaceY;
        furtherEdge.V5.y = HexMetrics.WaterSurfaceY;

        WaterShore.AddTriangleUnperturbed(furtherEdge.V5, closerWaterEdge.V2, closerWaterEdge.V1);
        WaterShore.AddTriangleUnperturbed(furtherEdge.V1, closerWaterEdge.V5, closerWaterEdge.V4);
        WaterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        WaterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        // left quad
        // quad is rotated 90 degrees to the right so the diagonal connection is shorter
        // it is done to maintain the symetry with the other quad
        Estuaries.AddQuadUnperturbed(furtherEdge.V5, closerWaterEdge.V2, furtherEdge.V4, closerWaterEdge.V3);
        Estuaries.AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));

        // big triangle in the middle
        Estuaries.AddTriangleUnperturbed(closerWaterEdge.V3, furtherEdge.V4, furtherEdge.V2);
        Estuaries.AddTriangleUV(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

        // right quad
        Estuaries.AddQuadUnperturbed(closerWaterEdge.V3, closerWaterEdge.V4, furtherEdge.V2, furtherEdge.V1);
        Estuaries.AddQuadUV(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

        // is incoming river
        if (cell.IncomingRiver == direction)
        {
            Estuaries.AddQuadUV2(
                new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            Estuaries.AddTriangleUV2(
                new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
            Estuaries.AddQuadUV2(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
        }
        else
        {
            // the U coordinates have to be mirrored for outgoing rivers
            // the V coordinates are a little bit less straightforward
            Estuaries.AddQuadUV2(
                new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
            Estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            Estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
        }
    }

    void TriangulateWaterfallInWater(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float waterY)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);

        Rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        Rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }
}