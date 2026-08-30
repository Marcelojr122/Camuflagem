using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralMap : MonoBehaviour
{
    [Header("Tamanho do mapa")]
    public int mapWidth = 80;
    public int mapHeight = 60;

    [Header("Salas")]
    public int roomCount = 10;
    public int minRoomWidth = 8;
    public int maxRoomWidth = 16;
    public int minRoomHeight = 6;
    public int maxRoomHeight = 12;

    [Header("Espaço entre salas")]
    public int roomPadding = 2;

    [Header("Tapetes")]
    public int minCarpetsPerRoom = 1;
    public int maxCarpetsPerRoom = 2;
    public int minCarpetWidth = 3;
    public int maxCarpetWidth = 6;
    public int minCarpetHeight = 2;
    public int maxCarpetHeight = 4;

    [Header("Tapetes nos corredores")]
    public int minCarpetsPerCorridor = 1;
    public int maxCarpetsPerCorridor = 2;

    [Header("Seed")]
    public int seed = 12345;
    public bool randomSeed = true;

    [Header("Tilemap")]
    public Tilemap tilemap;

    public TileBase floorTile;

    public TileBase wallTopTile;
    public TileBase wallLeftTile;
    public TileBase wallRightTile;
    public TileBase wallBottomTile;
    public TileBase wallTile;

    public TileBase wallCornerTopLeftTile;
    public TileBase wallCornerTopRightTile;
    public TileBase wallCornerBottomLeftTile;
    public TileBase wallCornerBottomRightTile;

    public TileBase carpetTile;

    // 0 = vazio | 1 = chão | 2 = parede | 3 = tapete
    private int[,] map;

    private List<RectInt> rooms = new List<RectInt>();


    private void Start()
    {
        GenerateMap();
    }


    public void GenerateMap()
    {
        if (randomSeed)
            seed = Random.Range(0, 999999);

        Random.InitState(seed);

        map = new int[mapWidth, mapHeight];
        rooms.Clear();

        GenerateRooms();
        ConnectRooms();
        GenerateWalls();
        DrawTiles();

        Debug.Log(
            $"Mapa gerado | Seed: {seed} | Salas: {rooms.Count}"
        );

        PrintMap(map);
    }


    // =========================================================
    // SALAS
    // =========================================================

    private void GenerateRooms()
    {
        int attempts = 0;
        int maxAttempts = roomCount * 20;

        int firstWidth = Random.Range(
            minRoomWidth,
            maxRoomWidth + 1
        );

        int firstHeight = Random.Range(
            minRoomHeight,
            maxRoomHeight + 1
        );

        RectInt firstRoom = new RectInt(
            2,
            2,
            firstWidth,
            firstHeight
        );

        rooms.Add(firstRoom);
        CreateRoom(firstRoom);

        while (
            rooms.Count < roomCount &&
            attempts < maxAttempts
        )
        {
            attempts++;

            int width = Random.Range(
                minRoomWidth,
                maxRoomWidth + 1
            );

            int height = Random.Range(
                minRoomHeight,
                maxRoomHeight + 1
            );

            int x = Random.Range(
                roomPadding,
                mapWidth - width - roomPadding
            );

            int y = Random.Range(
                roomPadding,
                mapHeight - height - roomPadding
            );

            RectInt newRoom = new RectInt(
                x,
                y,
                width,
                height
            );

            bool overlaps = false;

            foreach (RectInt room in rooms)
            {
                RectInt expandedRoom = new RectInt(
                    room.xMin - roomPadding,
                    room.yMin - roomPadding,
                    room.width + roomPadding * 2,
                    room.height + roomPadding * 2
                );

                if (expandedRoom.Overlaps(newRoom))
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
                continue;

            rooms.Add(newRoom);
            CreateRoom(newRoom);
        }
    }


    private void CreateRoom(RectInt room)
    {
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                SetFloor(x, y);
            }
        }

        CreateRoomCarpets(room);
    }


    private void CreateRoomCarpets(RectInt room)
    {
        int carpetCount = Random.Range(
            minCarpetsPerRoom,
            maxCarpetsPerRoom + 1
        );

        for (int i = 0; i < carpetCount; i++)
        {
            CreateCarpet(room);
        }
    }


    private void CreateCarpet(RectInt room)
    {
        int width = Random.Range(
            minCarpetWidth,
            maxCarpetWidth + 1
        );

        int height = Random.Range(
            minCarpetHeight,
            maxCarpetHeight + 1
        );

        if (
            width >= room.width - 2 ||
            height >= room.height - 2
        )
        {
            return;
        }

        int startX = Random.Range(
            room.xMin + 1,
            room.xMax - width
        );

        int startY = Random.Range(
            room.yMin + 1,
            room.yMax - height
        );

        for (
            int x = startX;
            x < startX + width;
            x++
        )
        {
            for (
                int y = startY;
                y < startY + height;
                y++
            )
            {
                if (map[x, y] == 1)
                    map[x, y] = 3;
            }
        }
    }


    // =========================================================
    // CORREDORES
    // =========================================================

    private void ConnectRooms()
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int start =
                GetRoomCenter(rooms[i - 1]);

            Vector2Int end =
                GetRoomCenter(rooms[i]);

            CreateCorridor(start, end);
        }
    }


    private Vector2Int GetRoomCenter(RectInt room)
    {
        return new Vector2Int(
            room.x + room.width / 2,
            room.y + room.height / 2
        );
    }


    private void CreateCorridor(
        Vector2Int start,
        Vector2Int end
    )
    {
        Vector2Int current = start;

        bool horizontalFirst =
            Random.value > 0.5f;

        List<Vector2Int> positions =
            new List<Vector2Int>();

        if (horizontalFirst)
        {
            while (current.x != end.x)
            {
                current.x +=
                    current.x < end.x ? 1 : -1;

                CreateFloor(current);
                positions.Add(current);
            }

            while (current.y != end.y)
            {
                current.y +=
                    current.y < end.y ? 1 : -1;

                CreateFloor(current);
                positions.Add(current);
            }
        }
        else
        {
            while (current.y != end.y)
            {
                current.y +=
                    current.y < end.y ? 1 : -1;

                CreateFloor(current);
                positions.Add(current);
            }

            while (current.x != end.x)
            {
                current.x +=
                    current.x < end.x ? 1 : -1;

                CreateFloor(current);
                positions.Add(current);
            }
        }

        CreateCorridorCarpets(positions);
    }


    private void CreateFloor(Vector2Int position)
    {
        if (!IsInsideMap(position.x, position.y))
            return;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                SetFloor(
                    position.x + x,
                    position.y + y
                );
            }
        }
    }


    private void SetFloor(int x, int y)
    {
        if (!IsInsideMap(x, y))
            return;

        if (map[x, y] == 0)
            map[x, y] = 1;
    }


    // =========================================================
    // TAPETES DOS CORREDORES
    // =========================================================

    private void CreateCorridorCarpets(
        List<Vector2Int> positions
    )
    {
        if (positions.Count == 0)
            return;

        int count = Random.Range(
            minCarpetsPerCorridor,
            maxCarpetsPerCorridor + 1
        );

        for (int i = 0; i < count; i++)
        {
            Vector2Int position =
                positions[
                    Random.Range(0, positions.Count)
                ];

            CreateCorridorCarpet(position);
        }
    }


    private void CreateCorridorCarpet(
        Vector2Int center
    )
    {
        int width = Random.Range(2, 4);
        int height = Random.Range(2, 4);

        for (
            int x = -width / 2;
            x <= width / 2;
            x++
        )
        {
            for (
                int y = -height / 2;
                y <= height / 2;
                y++
            )
            {
                int px = center.x + x;
                int py = center.y + y;

                if (!IsInsideMap(px, py))
                    continue;

                if (map[px, py] == 1)
                    map[px, py] = 3;
            }
        }
    }


    // =========================================================
    // PAREDES
    // =========================================================

    private void GenerateWalls()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (!IsFloorTile(x, y))
                    continue;

                TryCreateWall(x + 1, y);
                TryCreateWall(x - 1, y);
                TryCreateWall(x, y + 1);
                TryCreateWall(x, y - 1);
            }
        }
    }


    private void TryCreateWall(int x, int y)
    {
        if (!IsInsideMap(x, y))
            return;

        if (IsFloorTile(x, y))
            return;

        if (map[x, y] == 0)
            map[x, y] = 2;
    }


    // =========================================================
    // TILE DA PAREDE
    // =========================================================

    private TileBase GetWallTile(int x, int y)
    {
        // Verificações de chão vizinho (Cardinais)
        bool floorUp = IsFloorTile(x, y + 1);
        bool floorDown = IsFloorTile(x, y - 1);
        bool floorLeft = IsFloorTile(x - 1, y);
        bool floorRight = IsFloorTile(x + 1, y);

        // Verificações de chão vizinho (Diagonais)
        bool floorUpLeft = IsFloorTile(x - 1, y + 1);
        bool floorUpRight = IsFloorTile(x + 1, y + 1);
        bool floorDownLeft = IsFloorTile(x - 1, y - 1);
        bool floorDownRight = IsFloorTile(x + 1, y - 1);

        // =====================================================
        // 1. QUINAS CONCAVAS / EXTERNAS (Chão na diagonal)
        // =====================================================

        // Superior Esquerda: Chão está exclusivamente no canto inferior direito
        if (floorDownRight && !floorUp && !floorLeft && !floorDown && !floorRight)
        {
            return wallCornerTopLeftTile;
        }

        // Superior Direita: Chão está exclusivamente no canto inferior esquerdo
        if (floorDownLeft && !floorUp && !floorRight && !floorDown && !floorLeft)
        {
            return wallCornerTopRightTile;
        }

        // Inferior Esquerda: Chão está exclusivamente no canto superior direito
        if (floorUpRight && !floorDown && !floorLeft && !floorUp && !floorRight)
        {
            return wallCornerBottomLeftTile;
        }

        // Inferior Direita: Chão está exclusivamente no canto superior esquerdo
        if (floorUpLeft && !floorDown && !floorRight && !floorUp && !floorLeft)
        {
            return wallCornerBottomRightTile;
        }

        // =====================================================
        // 2. PAREDES RETAS
        // =====================================================

        // Parede superior (Chão fica abaixo dela)
        if (floorDown)
        {
            return wallTopTile;
        }

        // Parede inferior (Chão fica acima dela)
        if (floorUp)
        {
            return wallBottomTile;
        }

        // Parede esquerda (Chão fica à direita dela)
        if (floorRight)
        {
            return wallLeftTile;
        }

        // Parede direita (Chão fica à esquerda dela)
        if (floorLeft)
        {
            return wallRightTile;
        }

        // =====================================================
        // 3. PAREDE GENÉRICA (Preenchimento / Quinas Internas)
        // =====================================================

        return wallTile;
    }


    // =========================================================
    // VERIFICAÇÕES
    // =========================================================

    private bool IsInsideMap(int x, int y)
    {
        return
            x >= 0 &&
            x < mapWidth &&
            y >= 0 &&
            y < mapHeight;
    }


    private bool IsFloorTile(int x, int y)
    {
        if (!IsInsideMap(x, y))
            return false;

        return
            map[x, y] == 1 ||
            map[x, y] == 3;
    }


    public bool IsFloor(Vector2Int position)
    {
        return
            IsInsideMap(
                position.x,
                position.y
            ) &&
            IsFloorTile(
                position.x,
                position.y
            );
    }


    public bool IsWall(Vector2Int position)
    {
        return
            IsInsideMap(
                position.x,
                position.y
            ) &&
            map[
                position.x,
                position.y
            ] == 2;
    }


    public bool IsCarpet(Vector2Int position)
    {
        return
            IsInsideMap(
                position.x,
                position.y
            ) &&
            map[
                position.x,
                position.y
            ] == 3;
    }


    public Vector2Int GetRandomFloorPosition()
    {
        for (int i = 0; i < 1000; i++)
        {
            int x = Random.Range(
                0,
                mapWidth
            );

            int y = Random.Range(
                0,
                mapHeight
            );

            if (IsFloorTile(x, y))
            {
                return new Vector2Int(x, y);
            }
        }

        return Vector2Int.zero;
    }


    // =========================================================
    // DESENHAR TILEMAP
    // =========================================================

    private void DrawTiles()
    {
        if (tilemap == null)
        {
            Debug.LogError(
                "ProceduralMap: Tilemap não foi configurado!"
            );

            return;
        }

        tilemap.ClearAllTiles();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int position =
                    new Vector3Int(x, y, 0);

                switch (map[x, y])
                {
                    case 1:
                        tilemap.SetTile(
                            position,
                            floorTile
                        );
                        break;

                    case 2:
                        TileBase wall =
                            GetWallTile(x, y);

                        if (wall != null)
                        {
                            tilemap.SetTile(
                                position,
                                wall
                            );
                        }

                        break;

                    case 3:
                        tilemap.SetTile(
                            position,
                            carpetTile
                        );
                        break;
                }
            }
        }
    }


    // =========================================================
    // IMPRIMIR MAPA
    // =========================================================

    private void PrintMap(int[,] map)
    {
        if (map == null)
        {
            Debug.LogWarning(
                "Não foi possível imprimir o mapa."
            );

            return;
        }

        string result = "\n";

        result +=
            "================ MAPA ================\n";

        result +=
            $"Seed: {seed}\n";

        result +=
            $"Tamanho: {mapWidth} x {mapHeight}\n\n";

        for (
            int y = mapHeight - 1;
            y >= 0;
            y--
        )
        {
            result += $"{y,3} | ";

            for (
                int x = 0;
                x < mapWidth;
                x++
            )
            {
                result +=
                    map[x, y] + " ";
            }

            result += "\n";
        }

        result += "\n";
        result += "0 = vazio\n";
        result += "1 = chão\n";
        result += "2 = parede\n";
        result += "3 = tapete\n";

        result +=
            "======================================";

        Debug.Log(result);
    }
}