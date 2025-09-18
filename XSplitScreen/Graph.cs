using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dodad.XSplitscreen
{
	/// <summary>
	/// Represents a 2D square subdivided into a perfect grid of faces (rectangles) covering the interior.
	/// Supports edge insertion, joining of columns/rows into single faces, and subdivision of faces.
	/// </summary>
	public class Graph
	{
		public class Face
		{
			public int Index { get; private set; }
			public Rect Rect;
			public int GridRow;
			public int GridCol;
			public float WidthWeight = 1f;
			public float HeightWeight = 1f;

			public Face(int index, int gridRow = 0, int gridCol = 0)
			{
				Index = index;
				GridRow = gridRow;
				GridCol = gridCol;
			}
		}

		private readonly float size;
		private Dictionary<int, Face> faces = new Dictionary<int, Face>();
		private int nextFaceIndex = 0;
		private List<List<int>> grid = new List<List<int>>(); // grid[row][col] = face index
		private List<float> colWeights = new List<float>(); // Relative width of each column
		private List<float> rowWeights = new List<float>(); // Relative height of each row

		public IReadOnlyDictionary<int, Face> Faces => faces;

		public Graph(float size = 1f)
		{
			this.size = size;
		}

		public int AddInitialFace()
		{
			var face = new Face(nextFaceIndex++);
			faces.Add(face.Index, face);
			grid.Clear();
			grid.Add(new List<int> { face.Index });
			colWeights.Clear();
			colWeights.Add(1f);
			rowWeights.Clear();
			rowWeights.Add(1f);
			RecalculateRects();
			return face.Index;
		}

		/// <summary>
		/// Insert a new face from an edge, always forming a perfect square grid.
		/// </summary>
		public int InsertFromEdge(string edge)
		{
			string dir = edge.ToLower();
			int nRows = grid.Count;
			int nCols = grid.Count > 0 ? grid[0].Count : 0;

			if (faces.Count == 0)
				throw new InvalidOperationException("No faces in graph.");

			int newFaceIdx = nextFaceIndex++;

			if (dir == "bottom")
			{
				// Create a new row at the bottom with the new face spanning the entire row
				var newRow = new List<int>();
				for (int i = 0; i < nCols; i++)
				{
					newRow.Add(newFaceIdx);
				}

				// Create new face for the bottom row
				var newFace = new Face(newFaceIdx, 0, 0);
				faces.Add(newFaceIdx, newFace);

				// Insert new row at the bottom
				grid.Insert(0, newRow);
				// Add row weight (equal to average of existing rows)
				float avgRowWeight = rowWeights.Count > 0 ? rowWeights.Average() : 1f;
				rowWeights.Insert(0, avgRowWeight);
			}
			else if (dir == "top")
			{
				// Create a new row at the top with the new face spanning the entire row
				var newRow = new List<int>();
				for (int i = 0; i < nCols; i++)
				{
					newRow.Add(newFaceIdx);
				}

				// Create new face for the top row
				var newFace = new Face(newFaceIdx, grid.Count, 0);
				faces.Add(newFaceIdx, newFace);

				// Add new row at the top
				grid.Add(newRow);
				// Add row weight (equal to average of existing rows)
				float avgRowWeight = rowWeights.Count > 0 ? rowWeights.Average() : 1f;
				rowWeights.Add(avgRowWeight);
			}
			else if (dir == "left")
			{
				// Insert a new column at the left with the new face spanning the entire column
				for (int i = 0; i < nRows; i++)
				{
					grid[i].Insert(0, newFaceIdx);
				}

				// Create new face for the left column
				var newFace = new Face(newFaceIdx, 0, 0);
				faces.Add(newFaceIdx, newFace);

				// Add column weight (equal to average of existing columns)
				float avgColWeight = colWeights.Count > 0 ? colWeights.Average() : 1f;
				colWeights.Insert(0, avgColWeight);
			}
			else if (dir == "right")
			{
				// Insert a new column at the right with the new face spanning the entire column
				for (int i = 0; i < nRows; i++)
				{
					grid[i].Add(newFaceIdx);
				}

				// Create new face for the right column
				var newFace = new Face(newFaceIdx, 0, nCols);
				faces.Add(newFaceIdx, newFace);

				// Add column weight (equal to average of existing columns)
				float avgColWeight = colWeights.Count > 0 ? colWeights.Average() : 1f;
				colWeights.Add(avgColWeight);
			}
			else
			{
				throw new ArgumentException("Edge must be left, right, top, or bottom.");
			}

			RecalculateRects();
			return newFaceIdx;
		}

		/// <summary>
		/// Subdivides the face with the given index in half, either vertically or horizontally.
		/// The original face is kept for one half, and a new face is created for the other half.
		/// Returns the new indices as (originalFaceIdx, newFaceIdx).
		/// </summary>
		public (int, int) Subdivide(int faceIdx, bool vertical)
		{
			if (!faces.TryGetValue(faceIdx, out Face face))
				throw new ArgumentException($"Face index {faceIdx} not found.");

			int newFaceIdx = nextFaceIndex++;

			// Find all grid cells containing this face
			List<(int row, int col)> faceCells = GetFaceCells(faceIdx);

			if (faceCells.Count == 0)
				throw new InvalidOperationException($"Face {faceIdx} not found in grid");

			// Create the new face
			var newFace = new Face(newFaceIdx);
			faces.Add(newFaceIdx, newFace);

			if (vertical)
			{
				// Group cells by column to find width
				var columns = faceCells.Select(cell => cell.col).Distinct().OrderBy(c => c).ToList();

				if (columns.Count < 2)
				{
					// Need to expand the grid to accommodate the split
					int col = columns[0];
					float originalWeight = colWeights[col];

					// For each row that contains this face
					var affectedRows = faceCells.Select(cell => cell.row).Distinct().OrderBy(r => r).ToList();

					// Insert a new column after the current one with half the weight
					colWeights[col] = originalWeight / 2;
					colWeights.Insert(col + 1, originalWeight / 2);

					// Insert a new column after the current one
					for (int r = 0; r < grid.Count; r++)
					{
						// If this row contains our face, put the new face in the new column
						if (affectedRows.Contains(r))
						{
							grid[r].Insert(col + 1, newFaceIdx);
						}
						else
						{
							// Otherwise just duplicate whatever is in this column
							int existingIdx = grid[r][col];
							grid[r].Insert(col + 1, existingIdx);
						}
					}
				}
				else
				{
					// The face already spans multiple columns, split in the middle
					int midPoint = columns.Count / 2;
					int splitCol = columns[midPoint];

					// Assign right half to new face
					foreach (var cell in faceCells)
					{
						if (cell.col >= splitCol)
						{
							grid[cell.row][cell.col] = newFaceIdx;
						}
					}
				}
			}
			else // horizontal split
			{
				// Group cells by row to find height
				var rows = faceCells.Select(cell => cell.row).Distinct().OrderBy(r => r).ToList();

				if (rows.Count < 2)
				{
					// Need to expand the grid to accommodate the split
					int row = rows[0];
					float originalWeight = rowWeights[row];

					// For each column that contains this face
					var affectedCols = faceCells.Select(cell => cell.col).Distinct().OrderBy(c => c).ToList();

					// Split the row weight in half
					rowWeights[row] = originalWeight / 2;

					// Create a new row to insert
					List<int> newRow = new List<int>();
					for (int c = 0; c < grid[row].Count; c++)
					{
						// If this column contains our face, put the new face in the new row
						if (affectedCols.Contains(c))
						{
							newRow.Add(newFaceIdx);
						}
						else
						{
							// Otherwise just duplicate whatever is in this column
							int existingIdx = grid[row][c];
							newRow.Add(existingIdx);
						}
					}

					// Insert the new row after the current one with half the weight
					grid.Insert(row + 1, newRow);
					rowWeights.Insert(row + 1, originalWeight / 2);
				}
				else
				{
					// The face already spans multiple rows, split in the middle
					int midPoint = rows.Count / 2;
					int splitRow = rows[midPoint];

					// Assign bottom half to new face
					foreach (var cell in faceCells)
					{
						if (cell.row >= splitRow)
						{
							grid[cell.row][cell.col] = newFaceIdx;
						}
					}
				}
			}

			RecalculateRects();
			return (faceIdx, newFaceIdx);
		}

		/// <summary>
		/// Removes a face from the graph and replaces it with appropriate neighboring faces
		/// to maintain rectangular shapes.
		/// </summary>
		public void RemoveFace(int faceIdx)
		{
			if (!faces.ContainsKey(faceIdx))
				return;

			// Get all cells of the face to be removed
			var faceCells = GetFaceCells(faceIdx);
			if (faceCells.Count == 0)
				return;

			// Find neighboring faces
			Dictionary<int, HashSet<(int row, int col)>> neighborCells = FindNeighborCells(faceIdx, faceCells);

			// If no neighbors found, remove this face and adjust grid
			if (neighborCells.Count == 0)
			{
				// No neighbors, just remove the face and clean up the grid
				faces.Remove(faceIdx);
				CleanupGridAfterFaceRemoval(faceCells);
				RecalculateRects();
				return;
			}

			// Calculate rectangular expansion areas for each neighbor
			Dictionary<int, List<(int minRow, int maxRow, int minCol, int maxCol)>> neighborRectangles =
				CalculateExpandableRectangles(neighborCells, faceCells);

			// Find the best replacement strategy to maintain rectangular faces
			ApplyRectangularReplacements(faceIdx, faceCells, neighborRectangles);

			// Remove the face from our collection
			faces.Remove(faceIdx);

			// Optimize the grid
			OptimizeGrid();

			// Ensure the grid only references valid faces
			ValidateGrid();

			RecalculateRects();
		}

		/// <summary>
		/// Finds all neighboring cells for a given face
		/// </summary>
		private Dictionary<int, HashSet<(int row, int col)>> FindNeighborCells(int faceIdx, List<(int row, int col)> faceCells)
		{
			Dictionary<int, HashSet<(int row, int col)>> neighborCells = new Dictionary<int, HashSet<(int row, int col)>>();

			// Directions to check: up, right, down, left
			int[][] directions = new int[][] { new int[] { -1, 0 }, new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 } };

			foreach (var cell in faceCells)
			{
				foreach (var dir in directions)
				{
					int newRow = cell.row + dir[0];
					int newCol = cell.col + dir[1];

					// Check boundaries
					if (newRow < 0 || newRow >= grid.Count || newCol < 0 || newCol >= grid[newRow].Count)
						continue;

					int neighborIdx = grid[newRow][newCol];
					if (neighborIdx != faceIdx && faces.ContainsKey(neighborIdx))
					{
						if (!neighborCells.ContainsKey(neighborIdx))
							neighborCells[neighborIdx] = new HashSet<(int row, int col)>();

						neighborCells[neighborIdx].Add((newRow, newCol));
					}
				}
			}

			return neighborCells;
		}

		/// <summary>
		/// For each neighbor, calculate maximum rectangular areas that could be expanded into the removed face
		/// </summary>
		private Dictionary<int, List<(int minRow, int maxRow, int minCol, int maxCol)>> CalculateExpandableRectangles(
			Dictionary<int, HashSet<(int row, int col)>> neighborCells,
			List<(int row, int col)> removedCells)
		{
			Dictionary<int, List<(int minRow, int maxRow, int minCol, int maxCol)>> result =
				new Dictionary<int, List<(int minRow, int maxRow, int minCol, int maxCol)>>();

			// Create a set of removed cells for quick lookup
			HashSet<(int row, int col)> removedCellSet = new HashSet<(int row, int col)>(removedCells);

			// For each neighbor face
			foreach (var kvp in neighborCells)
			{
				int neighborIdx = kvp.Key;
				HashSet<(int row, int col)> cells = kvp.Value;

				// Get all cells of this neighbor face
				List<(int row, int col)> allNeighborCells = GetFaceCells(neighborIdx);

				// Calculate the current bounds of this face
				int minRow = allNeighborCells.Min(c => c.row);
				int maxRow = allNeighborCells.Max(c => c.row);
				int minCol = allNeighborCells.Min(c => c.col);
				int maxCol = allNeighborCells.Max(c => c.col);

				// Store potential rectangular expansions
				result[neighborIdx] = CalculatePotentialRectangles(
					minRow, maxRow, minCol, maxCol,
					allNeighborCells, removedCellSet);
			}

			return result;
		}

		/// <summary>
		/// Calculate potential rectangular expansions for a face
		/// </summary>
		private List<(int minRow, int maxRow, int minCol, int maxCol)> CalculatePotentialRectangles(
			int minRow, int maxRow, int minCol, int maxCol,
			List<(int row, int col)> faceCells, HashSet<(int row, int col)> removedCells)
		{
			List<(int minRow, int maxRow, int minCol, int maxCol)> rectangles = new List<(int minRow, int maxRow, int minCol, int maxCol)>();

			// Try expanding right
			int rightExpand = maxCol + 1;
			while (CanExpandToCol(minRow, maxRow, rightExpand, faceCells, removedCells))
			{
				rectangles.Add((minRow, maxRow, minCol, rightExpand));
				rightExpand++;
			}

			// Try expanding left
			int leftExpand = minCol - 1;
			while (leftExpand >= 0 && CanExpandToCol(minRow, maxRow, leftExpand, faceCells, removedCells))
			{
				rectangles.Add((minRow, maxRow, leftExpand, maxCol));
				leftExpand--;
			}

			// Try expanding down
			int downExpand = maxRow + 1;
			while (CanExpandToRow(minCol, maxCol, downExpand, faceCells, removedCells))
			{
				rectangles.Add((minRow, downExpand, minCol, maxCol));
				downExpand++;
			}

			// Try expanding up
			int upExpand = minRow - 1;
			while (upExpand >= 0 && CanExpandToRow(minCol, maxCol, upExpand, faceCells, removedCells))
			{
				rectangles.Add((upExpand, maxRow, minCol, maxCol));
				upExpand--;
			}

			return rectangles;
		}

		/// <summary>
		/// Check if we can expand a face to include a specific column
		/// </summary>
		private bool CanExpandToCol(int minRow, int maxRow, int col, List<(int row, int col)> faceCells, HashSet<(int row, int col)> removedCells)
		{
			if (col < 0 || col >= grid[0].Count) return false;

			for (int r = minRow; r <= maxRow; r++)
			{
				if (!removedCells.Contains((r, col)) && !faceCells.Contains((r, col)))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if we can expand a face to include a specific row
		/// </summary>
		private bool CanExpandToRow(int minCol, int maxCol, int row, List<(int row, int col)> faceCells, HashSet<(int row, int col)> removedCells)
		{
			if (row < 0 || row >= grid.Count) return false;

			for (int c = minCol; c <= maxCol; c++)
			{
				if (!removedCells.Contains((row, c)) && !faceCells.Contains((row, c)))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Apply the best replacement strategy to maintain rectangular shapes
		/// </summary>
		private void ApplyRectangularReplacements(int faceIdx, List<(int row, int col)> removedCells,
			Dictionary<int, List<(int minRow, int maxRow, int minCol, int maxCol)>> neighborRectangles)
		{
			// Create a temporary grid to test replacements without affecting the original grid
			List<List<int>> replacementGrid = new List<List<int>>(grid.Count);
			for (int r = 0; r < grid.Count; r++)
			{
				replacementGrid.Add(new List<int>(grid[r]));
			}

			// Create a set of removed cells for quick lookup
			HashSet<(int row, int col)> remainingCells = new HashSet<(int row, int col)>(removedCells);

			// Try to find the best rectangle expansions to cover the removed face
			while (remainingCells.Count > 0)
			{
				int bestNeighbor = -1;
				(int minRow, int maxRow, int minCol, int maxCol) bestRect = (0, 0, 0, 0);
				int bestCoverage = 0;

				// For each neighbor, find the rectangle that covers the most remaining cells
				foreach (var kvp in neighborRectangles)
				{
					int neighborIdx = kvp.Key;
					foreach (var rect in kvp.Value)
					{
						int coverage = CountCoveredCells(rect.minRow, rect.maxRow, rect.minCol, rect.maxCol, remainingCells);
						if (coverage > bestCoverage)
						{
							bestCoverage = coverage;
							bestNeighbor = neighborIdx;
							bestRect = rect;
						}
					}
				}

				// If we found a good expansion, apply it
				if (bestCoverage > 0)
				{
					// Apply this replacement to the grid
					for (int r = bestRect.minRow; r <= bestRect.maxRow; r++)
					{
						for (int c = bestRect.minCol; c <= bestRect.maxCol; c++)
						{
							if (r < grid.Count && c < grid[r].Count && grid[r][c] == faceIdx)
							{
								grid[r][c] = bestNeighbor;
								remainingCells.Remove((r, c));
							}
						}
					}

					// Update the potential rectangles after this change
					if (neighborRectangles.ContainsKey(bestNeighbor))
					{
						List<(int row, int col)> updatedCells = GetFaceCells(bestNeighbor);
						int minRow = updatedCells.Min(c => c.row);
						int maxRow = updatedCells.Max(c => c.row);
						int minCol = updatedCells.Min(c => c.col);
						int maxCol = updatedCells.Max(c => c.col);

						neighborRectangles[bestNeighbor] = CalculatePotentialRectangles(
							minRow, maxRow, minCol, maxCol,
							updatedCells, remainingCells);
					}
				}
				else
				{
					// If we can't find a good expansion, create a new face for the remaining cells
					int newFaceIdx = nextFaceIndex++;
					var newFace = new Face(newFaceIdx);
					faces.Add(newFaceIdx, newFace);

					foreach (var cell in remainingCells)
					{
						grid[cell.row][cell.col] = newFaceIdx;
					}

					// We've handled all remaining cells
					remainingCells.Clear();
				}
			}
		}

		/// <summary>
		/// Count how many cells from the provided set are covered by the specified rectangle
		/// </summary>
		private int CountCoveredCells(int minRow, int maxRow, int minCol, int maxCol, HashSet<(int row, int col)> cells)
		{
			int count = 0;
			for (int r = minRow; r <= maxRow; r++)
			{
				for (int c = minCol; c <= maxCol; c++)
				{
					if (cells.Contains((r, c)))
						count++;
				}
			}
			return count;
		}

		/// <summary>
		/// Ensures the grid only references faces that exist in the faces dictionary
		/// </summary>
		private void ValidateGrid()
		{
			if (grid.Count == 0 || faces.Count == 0)
				return;

			int defaultFaceIdx = faces.Keys.First();

			for (int r = 0; r < grid.Count; r++)
			{
				for (int c = 0; c < grid[r].Count; c++)
				{
					int faceIdx = grid[r][c];
					if (!faces.ContainsKey(faceIdx))
					{
						grid[r][c] = defaultFaceIdx;
					}
				}
			}
		}

		/// <summary>
		/// Gets all grid cells occupied by a specific face.
		/// </summary>
		private List<(int row, int col)> GetFaceCells(int faceIdx)
		{
			List<(int row, int col)> cells = new List<(int row, int col)>();
			for (int row = 0; row < grid.Count; row++)
			{
				for (int col = 0; col < grid[row].Count; col++)
				{
					if (grid[row][col] == faceIdx)
						cells.Add((row, col));
				}
			}
			return cells;
		}

		/// <summary>
		/// Cleans up the grid by removing rows/columns that don't contain any valid faces
		/// </summary>
		private void CleanupGridAfterFaceRemoval(List<(int row, int col)> removedCells)
		{
			// First identify which rows and columns might need to be removed
			var affectedRows = removedCells.Select(cell => cell.row).Distinct().OrderByDescending(r => r).ToList();
			var affectedCols = removedCells.Select(cell => cell.col).Distinct().OrderByDescending(c => c).ToList();

			// Check if any rows need to be removed (if an entire row only contained the removed face)
			foreach (int row in affectedRows)
			{
				if (row < grid.Count)
				{
					bool rowEmpty = true;
					for (int col = 0; col < grid[row].Count; col++)
					{
						int idx = grid[row][col];
						if (faces.ContainsKey(idx))
						{
							rowEmpty = false;
							break;
						}
					}

					if (rowEmpty && grid.Count > 1)
					{
						grid.RemoveAt(row);
						if (row < rowWeights.Count)
							rowWeights.RemoveAt(row);
					}
				}
			}

			// Check if any columns need to be removed
			if (grid.Count > 0)
			{
				int currentColCount = grid[0].Count;
				foreach (int col in affectedCols)
				{
					if (col < currentColCount)
					{
						bool colEmpty = true;
						for (int row = 0; row < grid.Count; row++)
						{
							if (col < grid[row].Count)
							{
								int idx = grid[row][col];
								if (faces.ContainsKey(idx))
								{
									colEmpty = false;
									break;
								}
							}
						}

						if (colEmpty && currentColCount > 1)
						{
							for (int row = 0; row < grid.Count; row++)
							{
								if (col < grid[row].Count)
									grid[row].RemoveAt(col);
							}
							if (col < colWeights.Count)
								colWeights.RemoveAt(col);
							currentColCount--;
						}
					}
				}
			}

			// If we removed everything, add back a default face
			if (grid.Count == 0 || grid[0].Count == 0)
			{
				if (faces.Count > 0)
				{
					// Pick the first remaining face
					int remainingFaceIdx = faces.Keys.First();
					grid.Clear();
					grid.Add(new List<int> { remainingFaceIdx });
					colWeights.Clear();
					colWeights.Add(1f);
					rowWeights.Clear();
					rowWeights.Add(1f);
				}
			}
		}

		/// <summary>
		/// Optimizes the grid by merging rows/columns that contain the same face pattern,
		/// and ensuring all faces are rectangular.
		/// </summary>
		private void OptimizeGrid()
		{
			if (grid.Count == 0)
				return;

			// 1. Check if we can merge rows
			for (int r = grid.Count - 2; r >= 0; r--)
			{
				if (grid[r].Count == grid[r + 1].Count)
				{
					bool canMerge = true;
					for (int c = 0; c < grid[r].Count; c++)
					{
						if (grid[r][c] != grid[r + 1][c])
						{
							canMerge = false;
							break;
						}
					}

					if (canMerge)
					{
						// Merge weights
						rowWeights[r] += rowWeights[r + 1];
						rowWeights.RemoveAt(r + 1);

						// Remove the duplicate row
						grid.RemoveAt(r + 1);
					}
				}
			}

			// 2. Check if we can merge columns
			for (int c = grid[0].Count - 2; c >= 0; c--)
			{
				bool canMerge = true;
				for (int r = 0; r < grid.Count; r++)
				{
					if (c + 1 >= grid[r].Count || grid[r][c] != grid[r][c + 1])
					{
						canMerge = false;
						break;
					}
				}

				if (canMerge)
				{
					// Merge weights
					colWeights[c] += colWeights[c + 1];
					colWeights.RemoveAt(c + 1);

					// Remove the duplicate column
					for (int r = 0; r < grid.Count; r++)
					{
						grid[r].RemoveAt(c + 1);
					}
				}
			}

			// 3. Ensure all faces form rectangles
			EnsureFacesAreRectangular();

			// 4. Final normalization to ensure the grid is valid
			NormalizeGrid();
		}

		/// <summary>
		/// Ensures all faces in the grid form rectangles.
		/// This is a complex operation that might need to modify the grid to achieve this.
		/// </summary>
		private void EnsureFacesAreRectangular()
		{
			// First map each face to the cells it occupies
			Dictionary<int, List<(int row, int col)>> faceCellMap = new Dictionary<int, List<(int row, int col)>>();

			for (int r = 0; r < grid.Count; r++)
			{
				for (int c = 0; c < grid[r].Count; c++)
				{
					int faceIdx = grid[r][c];
					if (!faceCellMap.ContainsKey(faceIdx))
						faceCellMap[faceIdx] = new List<(int row, int col)>();
					faceCellMap[faceIdx].Add((r, c));
				}
			}

			// Keep track of problematic faces to fix
			HashSet<int> problematicFaces = new HashSet<int>();

			// For each face, check if it forms a rectangle
			foreach (var kvp in faceCellMap)
			{
				int faceIdx = kvp.Key;
				var cells = kvp.Value;

				// Skip if face doesn't exist or has only one cell
				if (!faces.ContainsKey(faceIdx) || cells.Count <= 1)
					continue;

				// Find the bounding rectangle of this face
				int minRow = int.MaxValue, maxRow = int.MinValue;
				int minCol = int.MaxValue, maxCol = int.MinValue;

				foreach (var cell in cells)
				{
					minRow = Math.Min(minRow, cell.row);
					maxRow = Math.Max(maxRow, cell.row);
					minCol = Math.Min(minCol, cell.col);
					maxCol = Math.Max(maxCol, cell.col);
				}

				// Check if all cells within the bounding rectangle belong to this face
				bool isRectangular = true;
				for (int r = minRow; r <= maxRow; r++)
				{
					for (int c = minCol; c <= maxCol; c++)
					{
						if (r < grid.Count && c < grid[r].Count && grid[r][c] != faceIdx)
						{
							isRectangular = false;
							problematicFaces.Add(faceIdx);
							break;
						}
					}
					if (!isRectangular) break;
				}
			}

			// Fix problematic faces if any
			if (problematicFaces.Count > 0)
			{
				// For each problematic face, split it into multiple rectangular faces
				foreach (int faceIdx in problematicFaces)
				{
					// Skip if the face no longer exists (it could have been fixed by other operations)
					if (!faces.ContainsKey(faceIdx))
						continue;

					// Get the cells of this face
					var cells = faceCellMap[faceIdx];

					// Identify connected rectangular regions
					List<List<(int row, int col)>> rectangularRegions = FindRectangularRegions(cells);

					// Create new faces for each additional rectangular region
					for (int i = 1; i < rectangularRegions.Count; i++)
					{
						// Create a new face for this region
						int newFaceIdx = nextFaceIndex++;
						var newFace = new Face(newFaceIdx);
						faces.Add(newFaceIdx, newFace);

						// Assign the region to the new face
						foreach (var cell in rectangularRegions[i])
						{
							grid[cell.row][cell.col] = newFaceIdx;
						}
					}
				}
			}
		}

		/// <summary>
		/// Find connected rectangular regions from a set of cells
		/// </summary>
		private List<List<(int row, int col)>> FindRectangularRegions(List<(int row, int col)> cells)
		{
			List<List<(int row, int col)>> regions = new List<List<(int row, int col)>>();
			HashSet<(int row, int col)> remainingCells = new HashSet<(int row, int col)>(cells);

			while (remainingCells.Count > 0)
			{
				// Start with any cell
				var startCell = remainingCells.First();

				// Find the largest rectangle that can be formed starting at this cell
				int minRow = startCell.row;
				int maxRow = startCell.row;
				int minCol = startCell.col;
				int maxCol = startCell.col;

				// Try to expand the rectangle in each direction
				bool expandable;
				do
				{
					expandable = false;

					// Try expanding right
					if (CanExpandRectangle(minRow, maxRow, minCol, maxCol + 1, remainingCells))
					{
						maxCol++;
						expandable = true;
					}

					// Try expanding down
					if (CanExpandRectangle(minRow, maxRow + 1, minCol, maxCol, remainingCells))
					{
						maxRow++;
						expandable = true;
					}

					// Try expanding left
					if (CanExpandRectangle(minRow, maxRow, minCol - 1, maxCol, remainingCells))
					{
						minCol--;
						expandable = true;
					}

					// Try expanding up
					if (CanExpandRectangle(minRow - 1, maxRow, minCol, maxCol, remainingCells))
					{
						minRow--;
						expandable = true;
					}

				} while (expandable);

				// Extract the cells in this rectangle
				List<(int row, int col)> rectangle = new List<(int row, int col)>();
				for (int r = minRow; r <= maxRow; r++)
				{
					for (int c = minCol; c <= maxCol; c++)
					{
						var cell = (r, c);
						if (remainingCells.Contains(cell))
						{
							rectangle.Add(cell);
							remainingCells.Remove(cell);
						}
					}
				}

				regions.Add(rectangle);
			}

			return regions;
		}

		/// <summary>
		/// Check if a rectangle can be expanded to include a new coordinate
		/// </summary>
		private bool CanExpandRectangle(int minRow, int maxRow, int minCol, int maxCol, HashSet<(int row, int col)> cells)
		{
			for (int r = minRow; r <= maxRow; r++)
			{
				for (int c = minCol; c <= maxCol; c++)
				{
					if (!cells.Contains((r, c)))
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Ensures the grid remains rectangular (all rows have the same number of columns)
		/// </summary>
		private void NormalizeGrid()
		{
			if (grid.Count == 0) return;

			// Find the maximum column count
			int maxCols = 0;
			foreach (var row in grid)
				maxCols = Mathf.Max(maxCols, row.Count);

			if (maxCols == 0) return;

			// Ensure all rows have the same number of columns
			for (int r = 0; r < grid.Count; r++)
			{
				while (grid[r].Count < maxCols)
				{
					// If a row has missing columns, extend the last face in the row
					int lastFaceIdx = grid[r].Count > 0 ? grid[r][grid[r].Count - 1] : -1;
					if (lastFaceIdx >= 0 && faces.ContainsKey(lastFaceIdx))
						grid[r].Add(lastFaceIdx);
					else if (grid[r].Count > 0)
						grid[r].Add(grid[r][grid[r].Count - 1]); // Copy the last column if face not found
					else
						break; // Can't extend if no faces in row
				}
			}

			// Ensure we have the right number of weights
			while (colWeights.Count < maxCols)
			{
				colWeights.Add(1.0f);
			}
			while (colWeights.Count > maxCols && maxCols > 0)
			{
				colWeights.RemoveAt(colWeights.Count - 1);
			}

			while (rowWeights.Count < grid.Count)
			{
				rowWeights.Add(1.0f);
			}
			while (rowWeights.Count > grid.Count && grid.Count > 0)
			{
				rowWeights.RemoveAt(rowWeights.Count - 1);
			}
		}

		private void RecalculateRects()
		{
			int nRows = grid.Count;
			int nCols = grid.Count > 0 ? grid[0].Count : 0;
			if (nRows == 0 || nCols == 0) return;

			// Normalize weights
			float totalColWeight = colWeights.Sum();
			float totalRowWeight = rowWeights.Sum();

			// Calculate accumulated positions
			List<float> colPositions = new List<float>();
			List<float> colSizes = new List<float>();
			float accX = 0f;
			for (int c = 0; c < nCols; c++)
			{
				float normalizedWeight = colWeights[c] / totalColWeight;
				colPositions.Add(accX);
				float colSize = normalizedWeight * size;
				colSizes.Add(colSize);
				accX += colSize;
			}

			List<float> rowPositions = new List<float>();
			List<float> rowSizes = new List<float>();
			float accY = 0f;
			for (int r = 0; r < nRows; r++)
			{
				float normalizedWeight = rowWeights[r] / totalRowWeight;
				rowPositions.Add(accY);
				float rowSize = normalizedWeight * size;
				rowSizes.Add(rowSize);
				accY += rowSize;
			}

			// Reset all face rectangles
			foreach (var f in faces.Values)
			{
				f.Rect = new Rect(0, 0, 0, 0);
			}

			// Track which faces we've started calculating
			HashSet<int> processed = new HashSet<int>();

			// For each cell in the grid
			for (int row = 0; row < nRows; row++)
			{
				for (int col = 0; col < nCols; col++)
				{
					int idx = grid[row][col];
					if (idx < 0 || !faces.ContainsKey(idx)) continue;

					Face face = faces[idx];

					// Calculate cell rect based on weights
					float x = colPositions[col];
					float y = rowPositions[row];
					float width = colSizes[col];
					float height = rowSizes[row];
					Rect cellRect = new Rect(x, y, width, height);

					if (!processed.Contains(idx))
					{
						// First cell for this face
						face.GridRow = row;
						face.GridCol = col;
						face.Rect = cellRect;
						processed.Add(idx);
					}
					else
					{
						// Extend existing rectangle
						float minX = Mathf.Min(face.Rect.x, cellRect.x);
						float minY = Mathf.Min(face.Rect.y, cellRect.y);
						float maxX = Mathf.Max(face.Rect.x + face.Rect.width, cellRect.x + cellRect.width);
						float maxY = Mathf.Max(face.Rect.y + face.Rect.height, cellRect.y + cellRect.height);

						face.Rect = new Rect(minX, minY, maxX - minX, maxY - minY);

						// Update grid position to be top-left corner
						if (row < face.GridRow)
						{
							face.GridRow = row;
						}
						if (col < face.GridCol)
						{
							face.GridCol = col;
						}
					}
				}
			}
		}

		public Face GetFace(int index)
		{
			faces.TryGetValue(index, out var face);
			return face;
		}

		public void Clear()
		{
			faces.Clear();
			grid.Clear();
			colWeights.Clear();
			rowWeights.Clear();
		}

		public void DebugPrintFaces()
		{
			Log.Print("Faces in graph:");
			foreach (var face in faces.Values)
			{
				Log.Print(
					$"Face {face.Index}: Rect=({face.Rect.x}, {face.Rect.y}, {face.Rect.width}, {face.Rect.height}) " +
					$"GridRow={face.GridRow} GridCol={face.GridCol}"
				);
			}

			Log.Print("Grid layout:");
			for (int row = 0; row < grid.Count; row++)
			{
				string rowStr = $"Row {row}: ";
				for (int col = 0; col < grid[row].Count; col++)
				{
					rowStr += $"[{grid[row][col]}] ";
				}
				Log.Print(rowStr);
			}

			Log.Print("Column weights: " + string.Join(", ", colWeights));
			Log.Print("Row weights: " + string.Join(", ", rowWeights));
			Log.Print($" -- End --");
		}
	}
}