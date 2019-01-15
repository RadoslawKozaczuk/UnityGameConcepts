﻿using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    // Water and water edge have different meshes and shaders
    public HexMesh Terrain, Rivers, Roads, Water, WaterShore, Estuaries;
    HexCell[] _cells;
    Canvas _gridCanvas;

    void Awake()
    {
        _gridCanvas = GetComponentInChildren<Canvas>();
        _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];

        ShowUI(false);
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

        for (int i = 0; i < _cells.Length; i++)
            Precalculation(_cells[i]);

        for (int i = 0; i < _cells.Length; i++)
            TriangulateCell(_cells[i]);

        Terrain.Apply();
        Rivers.Apply();
        Roads.Apply();
        Water.Apply();
        WaterShore.Apply();
        Estuaries.Apply();
    }

    void Precalculation(HexCell cell)
    {
        cell.Center = cell.WaterCenter = cell.transform.localPosition;
        cell.WaterCenter.y = HexCell.WaterSurfaceY;

        for (int i = 0; i <= 5; i++)
        {
            var direction = (HexDirection)i;

            cell.Edges[i] = new EdgeVertices(
                cell.Center + HexMetrics.GetLeftSolidCorner(direction),
                cell.Center + HexMetrics.GetRightSolidCorner(direction));

            if (cell.HasRiver && cell.HasRiverThroughEdge(direction))
                cell.Edges[i].V3.y = cell.StreamBedY;
            
            var leftCorner = HexMetrics.GetLeftWaterCorner(direction);
            leftCorner += cell.WaterCenter;
            leftCorner.y = HexCell.WaterSurfaceY;
        
            var rightCorner = HexMetrics.GetRightWaterCorner(direction);
            rightCorner += cell.WaterCenter;
            rightCorner.y = HexCell.WaterSurfaceY;
            
            cell.WaterEdges[i] = new EdgeVertices(leftCorner, rightCorner);
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
        Terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
    }

    void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        Terrain.AddTriangle(begin, v3, v4);
        Terrain.AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            Terrain.AddQuad(v1, v2, v3, v4);
            Terrain.AddQuadColor(c1, c2, c3, c4);
        }

        Terrain.AddQuad(v3, v4, left, right);
        Terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float boundaryInterpolator = 1f / (rightCell.Elevation - beginCell.Elevation);

        // boundary interpolators should not be negative
        if (boundaryInterpolator < 0)
            boundaryInterpolator = -boundaryInterpolator;

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), boundaryInterpolator);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, boundaryInterpolator);

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        // complete the top part
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
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
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, boundaryInterpolator);

        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left,
        HexCell leftCell, Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        Terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            Terrain.AddTriangleUnperturbed(v1, v2, boundary);
            Terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        Terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        Terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
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
                        TriangulateWithRiverBeginOrEnd(direction, cell, center, cell.Edges[i]);
                    else
                        TriangulateWithRiver(direction, cell, center, cell.Edges[i]);
                }
                else
                {
                    // draw the inner circles as two sets of triangles
                    TriangulateAdjacentToRiver(direction, cell, cell.Edges[i]);
                }
            }
            else
            {
                TriangulateEdgeFan(center, edge, cell.Color);

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

        for(int i = 0; i <= 2; i++)
            TriangulateConnection((HexDirection)i, cell, cell.Edges[i]);
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

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);
    }

    void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
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

        var m = new EdgeVertices(Vector3.Lerp(center, e.V1, 0.5f), Vector3.Lerp(center, e.V5, 0.5f));
        m.V3.y = e.V3.y; // reassign middle verticle height as it is ommited in the calculation above

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

        // river segments are added only if the current segment is not under water
        if (!cell.IsUnderwater)
        {
            bool reversed = cell.HasIncomingRiver;

            TriangulateRiverQuad(m.V2, m.V4, e.V2, e.V4, cell.RiverSurfaceY, 0.6f, reversed);

            center.y = m.V2.y = m.V4.y = cell.RiverSurfaceY;
            Rivers.AddTriangle(center, m.V2, m.V4);

            if (reversed)
                Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            else
                Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
        }
    }

    void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
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
            centerR = Vector3.Lerp(center, e.V5, 0.65f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.V1, 0.65f);
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

        var m = new EdgeVertices(Vector3.Lerp(centerL, e.V1, 0.5f), Vector3.Lerp(centerR, e.V5, 0.5f), 1f / 6f);
        m.V3.y = e.V3.y;

        // external hex circle
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        // connection between hexes
        Terrain.AddTriangle(centerL, m.V1, m.V2);
        Terrain.AddTriangleColor(cell.Color);
        
        Terrain.AddQuad(centerL, new Vector3(center.x, cell.StreamBedY, center.z), m.V2, m.V3);
        Terrain.AddQuadColor(cell.Color);
        Terrain.AddQuad(new Vector3(center.x, cell.StreamBedY, center.z), centerR, m.V3, m.V4);
        Terrain.AddQuadColor(cell.Color);

        Terrain.AddTriangle(centerR, m.V4, m.V5);
        Terrain.AddTriangleColor(cell.Color);

        // create river quads
        if (!cell.IsUnderwater)
        {
            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.V2, m.V4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.V2, m.V4, e.V2, e.V4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    /// <summary>
    /// Creates four triangles from the center to the edge strip 
    /// </summary>
    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        Terrain.AddTriangle(center, edge.V1, edge.V2);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.V2, edge.V3);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.V3, edge.V4);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.V4, edge.V5);
        Terrain.AddTriangleColor(color);
    }

    /// <summary>
    /// Creates four quads betweeen edge one and edge.
    /// Color of these quads is a gradient between color one and color two.
    /// If hasRoad is true the function creates a road segment.
    /// </summary>
    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2, bool hasRoad = false)
    {
        Terrain.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.V4, e1.V5, e2.V4, e2.V5);
        Terrain.AddQuadColor(c1, c2);

        if (hasRoad)
            TriangulateRoadSegment(e1.V2, e1.V3, e1.V4, e2.V2, e2.V3, e2.V4);
    }

    // Every two hexagons are connected by a single rectangular bridge. And every three hexagons are connected by a single triangle.
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
                if (!neighbor.IsUnderwater)
                {
                    edgeEnd.V3.y = neighbor.StreamBedY;
                    TriangulateRiverQuad(edgeBegin.V2, edgeBegin.V4, edgeEnd.V2, edgeEnd.V4, cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        0.8f, cell.HasIncomingRiver && cell.IncomingRiver == direction);
                }
                else if (cell.Elevation > neighbor.WaterLevel)
                {
                    TriangulateWaterfallInWater(
                        edgeBegin.V2, edgeBegin.V4, edgeEnd.V2, edgeEnd.V4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        HexCell.WaterSurfaceY);
                }
            }
            else if (!neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel)
            {
                TriangulateWaterfallInWater(
                    edgeEnd.V4, edgeEnd.V2, edgeBegin.V4, edgeBegin.V2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    HexCell.WaterSurfaceY);
            }
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            TriangulateEdgeStairs(edgeBegin, cell, edgeEnd, neighbor, cell.HasRoadThroughEdge(direction));
        else
            TriangulateEdgeStrip(edgeBegin, cell.Color, edgeEnd, neighbor.Color, cell.HasRoadThroughEdge(direction));

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
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2, hasRoad);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.Color, hasRoad);
    }
    
    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float height1, float height2, float uv, bool reversed)
    {
        v1.y = v2.y = height1;
        v3.y = v4.y = height2;
        Rivers.AddQuad(v1, v2, v3, v4);

        if (reversed)
            Rivers.AddQuadUV(1f, 0f, 0.8f - uv, 0.6f - uv);
        else
            Rivers.AddQuadUV(0f, 1f, uv, uv + 0.2f);
    }

    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float height, float uv, bool reversed)
        => TriangulateRiverQuad(v1, v2, v3, v4, height, height, uv, reversed);

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
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
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
            if (!cell.HasRoadThroughEdge(middle) && !cell.HasRoadThroughEdge(middle.Previous()) && !cell.HasRoadThroughEdge(middle.Next()))
                return;

            roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
        }

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
        var closerEdge = cell.WaterEdges[(int)direction];
        
        // water fan
        Water.AddTriangleUnperturbed(cell.WaterCenter, closerEdge.V1, closerEdge.V5);

        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null) return;

        var nextDirection = direction.Next();
        HexCell nextNeighbor = cell.GetNeighbor(nextDirection);

        EdgeVertices furtherEdge = neighbor.WaterEdges[(int)direction.Opposite()];
        EdgeVertices furtherEdgeNext;

        // calculate connection quads and triangles
        if (neighbor.IsUnderwater)
        {
            // we always generate connection only on the eastern side to avoid duplications
            if (direction <= HexDirection.SouthEast)
            {
                // connection betweem two cells
                Water.AddQuadUnperturbed(closerEdge.V1, closerEdge.V5, furtherEdge.V5, furtherEdge.V1);

                // connection triangle - only for east and north east
                if (direction <= HexDirection.East && nextNeighbor != null && nextNeighbor.IsUnderwater)
                {
                    furtherEdgeNext = nextNeighbor.WaterEdges[(int)nextDirection.Opposite()];
                    Water.AddTriangleUnperturbed(closerEdge.V5, furtherEdge.V1, furtherEdgeNext.V5);
                }
            }
        }
        // calculate water shore
        else
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                TriangulateEstuary(closerEdge, furtherEdge, cell.IncomingRiver == direction);
            }
            else
            {
                // water shore quads
                WaterShore.AddQuadUnperturbed(closerEdge.V1, closerEdge.V5, furtherEdge.V5, furtherEdge.V1);
                WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
            }

            // connection triangle
            if (!neighbor.IsUnderwater && nextNeighbor != null)
            {
                furtherEdgeNext = nextNeighbor.WaterEdges[(int)nextDirection.Opposite()];
                WaterShore.AddTriangleUnperturbed(closerEdge.V5, furtherEdge.V1, furtherEdgeNext.V5);
                WaterShore.AddTriangleUV(new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));
            }
        }
    }

    void TriangulateEstuary(EdgeVertices closerEdge, EdgeVertices furtherEdge, bool incomingRiver)
    {
        WaterShore.AddTriangle(furtherEdge.V1, closerEdge.V2, closerEdge.V1);
        WaterShore.AddTriangle(furtherEdge.V5, closerEdge.V5, closerEdge.V4);
        WaterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        WaterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        // left quad
        // quad is rotated 90 degrees to the right so the diagonal connection is shorter 
        // it is done to maintain the symetry with the other quad
        Estuaries.AddQuad(furtherEdge.V1, closerEdge.V2, furtherEdge.V2, closerEdge.V3);
        Estuaries.AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));

        // big triangle in the middle
        Estuaries.AddTriangle(closerEdge.V3, furtherEdge.V2, furtherEdge.V4);
        Estuaries.AddTriangleUV(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        
        // right quad
        Estuaries.AddQuad(closerEdge.V3, closerEdge.V4, furtherEdge.V4, furtherEdge.V5);
        Estuaries.AddQuadUV(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        
        // second UV set
        if (incomingRiver)
        {
            Estuaries.AddQuadUV2(new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f), new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            Estuaries.AddTriangleUV2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
            Estuaries.AddQuadUV2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
        }
        else
        {
            // the U coordinates have to be mirrored for outgoing rivers
            // the V coordinates are a little bit less straightforward
            Estuaries.AddQuadUV2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f), new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
            Estuaries.AddTriangleUV2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            Estuaries.AddQuadUV2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f), new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
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