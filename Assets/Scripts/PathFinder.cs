using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinder
{
    Board _board;

    List<Vector2Int> _path = new List<Vector2Int>();
    List<PathNode> _nodePath = new List<PathNode>();
    List<PathNode> _openList = new List<PathNode>();
    List<PathNode> _closedList = new List<PathNode>();
    PathNode _tmpNode = new PathNode();
    PathNode _currentNode = new PathNode();

    public PathFinder(Board board)
    {
        _board = board;
    }

    
    public List<Vector2Int> PathTo(Vector2Int start, Vector2Int destination)
    {
        _path.Clear();
        _nodePath.Clear();
        _openList.Clear();
        _closedList.Clear();

        // First node will not have a parent
        _openList.Insert(0, new PathNode(start, 0, null));

        // Keep looking for a path until the list becomes empty
        while (_openList.Count != 0)
        {
            // Get the node from the start of the list
            _currentNode = _openList[0];
            _openList.RemoveAt(0);

            // If at the destination, finish the path finding
            if (_currentNode.Position == destination)
            {
                _nodePath.Insert(0, _currentNode);
                _path.Insert(0, _nodePath[0].Position);

                while (_nodePath[0].Parent != null)
                {
                    _nodePath.Insert(0, _nodePath[0].Parent);
                    _path.Insert(0, _nodePath[0].Position);
                }

                return _path;
            }

            // Check all possible directions
            // North
            _tmpNode.Position = _currentNode.Position;
            _tmpNode.Parent = _currentNode;
            _tmpNode.Position.y = _currentNode.Position.y + 1;
            if (_board.IsWithinBounds(_tmpNode.Position) && !_board.GetBox(_tmpNode.Position).IsWall())
            {
                // If not in the open list or the closed list
                if (!IsInEitherList(_tmpNode, _closedList, _openList))
                {
                    // Calculate cost
                    if (_tmpNode.Parent != null) { _tmpNode.Cost = _tmpNode.Parent.Cost; }

                    // Add to the list and sort the list
                    AddToListAndSort(_openList, _closedList, _tmpNode, destination);
                }

            }

            // South
            _tmpNode.Position = _currentNode.Position;
            _tmpNode.Parent = _currentNode;
            _tmpNode.Position.y = _currentNode.Position.y - 1;
            if (_board.IsWithinBounds(_tmpNode.Position) && !_board.GetBox(_tmpNode.Position).IsWall())
            {
                // If not in the open list or the closed list
                if (!IsInEitherList(_tmpNode, _closedList, _openList))
                {
                    // Calculate cost
                    if (_tmpNode.Parent != null) { _tmpNode.Cost = _tmpNode.Parent.Cost; }

                    // Add to the list and sort the list
                    AddToListAndSort(_openList, _closedList, _tmpNode, destination);
                }
            }

            // East
            _tmpNode.Position = _currentNode.Position;
            _tmpNode.Parent = _currentNode;
            _tmpNode.Position.x = _currentNode.Position.x + 1;
            if (_board.IsWithinBounds(_tmpNode.Position) && !_board.GetBox(_tmpNode.Position).IsWall())
            {
                // If not in the open list or the closed list
                if (!IsInEitherList(_tmpNode, _closedList, _openList))
                {
                    // Calculate cost
                    if (_tmpNode.Parent != null) { _tmpNode.Cost = _tmpNode.Parent.Cost; }

                    // Add to the list and sort the list
                    AddToListAndSort(_openList, _closedList, _tmpNode, destination);
                }
            }

            // West
            _tmpNode.Position = _currentNode.Position;
            _tmpNode.Parent = _currentNode;
            _tmpNode.Position.x = _currentNode.Position.x - 1;
            if (_board.IsWithinBounds(_tmpNode.Position) && !_board.GetBox(_tmpNode.Position).IsWall())
            {
                // If not in the open list or the closed list
                if (!IsInEitherList(_tmpNode, _closedList, _openList))
                {
                    // Calculate cost
                    if (_tmpNode.Parent != null) { _tmpNode.Cost = _tmpNode.Parent.Cost; }

                    // Add to the list and sort the list
                    AddToListAndSort(_openList, _closedList, _tmpNode, destination);
                }
            }

            _closedList.Add(_currentNode);
        }

        // Return an empty list if failed to path find
        return _path;
    }

    // Checks if the specified position is present in the specified lists
    bool IsInEitherList(PathNode position, List<PathNode> list1, List<PathNode> list2)
    {
        if (list1.Count > list2.Count) return IsInEitherListLoop(position, list2, list1);
        else return IsInEitherListLoop(position, list1, list2);
    }

    // An optimised loop iterating through 2 lists of different size
    bool IsInEitherListLoop(PathNode position, List<PathNode> smallerList, List<PathNode> biggerList)
    {
        for (int i = 0; i < smallerList.Count; ++i)
        {
            if (position.Position == smallerList[i].Position || position.Position == biggerList[i].Position) return true;
        }

        for (int i = smallerList.Count; i < biggerList.Count; ++i)
        {
            if (position.Position == biggerList[i].Position) return true;
        }

        return false;
    }

    public void AddToListAndSort(List<PathNode> openList, List<PathNode> closedList, PathNode toAdd, Vector2Int goal)
    {
        //Check if the node already exists and replace if the value is smaller
        for (int i = 0; i < closedList.Count; i++)
        {
            if (closedList[i].Position.x == toAdd.Position.x && closedList[i].Position.y == toAdd.Position.y &&
               (closedList[i].Cost + Vector2Int.Distance(closedList[i].Position, goal)) > (toAdd.Cost + Vector2Int.Distance(toAdd.Position, goal)))
            {
                //Delete the old value from the closed list
                closedList.RemoveAt(i);
            }
        }

        //Add the value to the open list and sort the list
        for (int i = 0; i < openList.Count; i++)
        {
            //If the new node's cost is smaller than the next node's, shift everything by one and insert the new node in the empty space
            if ((openList[i].Cost + Vector2Int.Distance(openList[i].Position, goal)) > (toAdd.Cost + Vector2Int.Distance(toAdd.Position, goal)))
            {
                openList.Insert(i, new PathNode(toAdd));
                return;
            }
        }
        //Push back the value if nothing was added in the for loop
        openList.Add(new PathNode(toAdd));

    }
}
