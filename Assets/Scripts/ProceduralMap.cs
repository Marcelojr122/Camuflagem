using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralMap : MonoBehaviour
{
    //Header é usado para organizar as variáveis no Inspector do Unity
    [Header("Tamanho do mapa")]
    // Define o tamanho do mapa em tiles
    public int mapWidth = 80;
    public int mapHeight = 60;

    // Define a quantidade de salas que serão geradas no mapa
    [Header("Salas")]
    public int roomCount = 10;

    //Define o tamanho mínimo e máximo das salas
    public int minRoomWidth = 8;
    public int maxRoomWidth = 16;

    public int minRoomHeight = 6;
    public int maxRoomHeight = 12;

    // Define o espaço mínimo entre as salas
    [Header("Espaço entre salas")]
    public int roomPadding = 2;

    // Define a quantidade mínima e máxima de tapetes que podem ser gerados em cada sala
    [Header("Tapetes")]
    public int minCarpetsPerRoom = 1;
    public int maxCarpetsPerRoom = 2;

    public int minCarpetWidth = 3;
    public int maxCarpetWidth = 6;

    public int minCarpetHeight = 2;
    public int maxCarpetHeight = 4;

    // Define a quantidade mínima e máxima de tapetes que podem ser gerados nos corredores
    [Header("Tapetes nos corredores")]
    public int minCarpetsPerCorridor = 1;
    public int maxCarpetsPerCorridor = 2;

    [Header("Seed")]
    public int seed = 12345;
    public bool randomSeed = true;

    // Referência para o Tilemap onde o mapa será desenhado
    [Header("Tilemap")]
    public Tilemap tilemap;

    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase carpetTile;

    // 0 = vazio
    // 1 = chão
    // 2 = parede
    // 3 = tapete
    private int[,] map;

    // Lista de salas geradas
    private List<RectInt> rooms = new List<RectInt>();


    void Start()
    {
        GenerateMap();
    }


    // =========================================================
    // GERAR MAPA
    // =========================================================

    public void GenerateMap()
    {
        if (randomSeed)
        {
            seed = Random.Range(0, 999999);
        }

        Random.InitState(seed);

        map = new int[mapWidth, mapHeight];

        rooms.Clear();

        // 1. Criar salas
        GenerateRooms();

        // 2. Conectar salas
        ConnectRooms();

        // 3. Criar paredes
        GenerateWalls();

        DrawTiles();

        Debug.Log("=================================");
        Debug.Log("MAPA GERADO");
        Debug.Log("Seed: " + seed);
        Debug.Log("Salas criadas: " + rooms.Count);
        Debug.Log("=================================");
    }


    // =========================================================
    // GERAR SALAS
    // =========================================================

    void GenerateRooms()
    {
        int attempts = 0;
        int maxAttempts = roomCount * 20;


        // =====================================================
        // PRIMEIRA SALA GERA NO CANTO SUPERIOR ESQUERDO PARA GARANTIR QUE O JOGADOR TENHA UM LUGAR PARA COMEÇAR
        // =====================================================

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


        // =====================================================
        // OUTRAS SALAS
        // =====================================================

        while (rooms.Count < roomCount && attempts < maxAttempts)
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

            // Verifica se a nova sala se sobrepõe a alguma sala existente, considerando o padding
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
            {
                continue;
            }

            // Se não houver sobreposição, adiciona a nova sala à lista e cria o chão e os tapetes
            rooms.Add(newRoom);

            CreateRoom(newRoom);
        }
    }


    // =========================================================
    // CRIAR SALA
    // =========================================================

    void CreateRoom(RectInt room)
    {
        // Cria o chão da sala

        // O chão é representado pelo valor 1 no array map
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                map[x, y] = 1;
            }
        }


        // Cria os tapetes

        CreateRoomCarpets(room);
    }


    // =========================================================
    // CRIAR TAPETES DA SALA
    // =========================================================

    void CreateRoomCarpets(RectInt room)
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


    // =========================================================
    // CRIAR UM TAPETE
    // =========================================================

    void CreateCarpet(RectInt room)
    {
        int carpetWidth = Random.Range(
            minCarpetWidth,
            maxCarpetWidth + 1
        );

        int carpetHeight = Random.Range(
            minCarpetHeight,
            maxCarpetHeight + 1
        );


        // Garante que o tapete tenha uma margem
        // em relação às paredes da sala

        if (
            carpetWidth >= room.width - 2 ||
            carpetHeight >= room.height - 2
        )
        {
            return;
        }


        int startX = Random.Range(
            room.xMin + 1,
            room.xMax - carpetWidth
        );

        int startY = Random.Range(
            room.yMin + 1,
            room.yMax - carpetHeight
        );


        for (
            int x = startX;
            x < startX + carpetWidth;
            x++
        )
        {
            for (
                int y = startY;
                y < startY + carpetHeight;
                y++
            )
            {
                map[x, y] = 3;
            }
        }
    }


    // =========================================================
    // CONECTAR SALAS
    // =========================================================

    void ConnectRooms()
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int centerA =
                GetRoomCenter(rooms[i - 1]);

            Vector2Int centerB =
                GetRoomCenter(rooms[i]);

            CreateCorridor(
                centerA,
                centerB
            );
        }
    }


    // =========================================================
    // CENTRO DA SALA
    // =========================================================

    Vector2Int GetRoomCenter(RectInt room)
    {
        return new Vector2Int(
            room.x + room.width / 2,
            room.y + room.height / 2
        );
    }


    // =========================================================
    // CRIAR CORREDOR
    // =========================================================

    void CreateCorridor(
        Vector2Int start,
        Vector2Int end
    )
    {
        Vector2Int current = start;

        bool horizontalFirst =
            Random.value > 0.5f;


        // Guarda o caminho do corredor

        List<Vector2Int> corridorPositions =
            new List<Vector2Int>();


        // =====================================================
        // HORIZONTAL PRIMEIRO
        // =====================================================

        if (horizontalFirst)
        {
            while (current.x != end.x)
            {
                current.x +=
                    current.x < end.x
                    ? 1
                    : -1;

                CreateFloor(current);

                corridorPositions.Add(current);
            }


            while (current.y != end.y)
            {
                current.y +=
                    current.y < end.y
                    ? 1
                    : -1;

                CreateFloor(current);

                corridorPositions.Add(current);
            }
        }


        // =====================================================
        // VERTICAL PRIMEIRO
        // =====================================================

        else
        {
            while (current.y != end.y)
            {
                current.y +=
                    current.y < end.y
                    ? 1
                    : -1;

                CreateFloor(current);

                corridorPositions.Add(current);
            }


            while (current.x != end.x)
            {
                current.x +=
                    current.x < end.x
                    ? 1
                    : -1;

                CreateFloor(current);

                corridorPositions.Add(current);
            }
        }


        // Depois que o corredor foi criado,
        // colocamos tapetes nele.

        CreateCorridorCarpets(corridorPositions);
    }


    // =========================================================
    // CRIAR TAPETES NOS CORREDORES
    // =========================================================

    void CreateCorridorCarpets(
        List<Vector2Int> corridorPositions
    )
    {
        if (corridorPositions.Count == 0)
        {
            return;
        }


        int carpetCount = Random.Range(
            minCarpetsPerCorridor,
            maxCarpetsPerCorridor + 1
        );


        for (int i = 0; i < carpetCount; i++)
        {
            // Escolhe uma posição aleatória
            // no corredor

            Vector2Int position =
                corridorPositions[
                    Random.Range(
                        0,
                        corridorPositions.Count
                    )
                ];


            // Cria um pequeno tapete
            // ao redor dessa posição

            CreateCorridorCarpet(position);
        }
    }


    // =========================================================
    // CRIAR TAPETE PEQUENO NO CORREDOR
    // =========================================================

    void CreateCorridorCarpet(
        Vector2Int center
    )
    {
        // Tapete de corredor menor
        // para não bloquear todo o caminho

        int width = Random.Range(2, 4);
        int height = Random.Range(2, 4);


        for (int x = -width / 2; x <= width / 2; x++)
        {
            for (int y = -height / 2; y <= height / 2; y++)
            {
                int px = center.x + x;
                int py = center.y + y;


                if (!IsInsideMap(px, py))
                {
                    continue;
                }


                // Só coloca tapete onde já existe chão

                if (map[px, py] == 1)
                {
                    map[px, py] = 3;
                }
            }
        }
    }


    // =========================================================
    // CRIAR CHÃO
    // =========================================================

    void CreateFloor(Vector2Int position)
    {
        if (!IsInsideMap(
            position.x,
            position.y
        ))
        {
            return;
        }


        map[
            position.x,
            position.y
        ] = 1;


        // Corredor com 3 tiles de largura

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int px = position.x + x;
                int py = position.y + y;


                if (IsInsideMap(px, py))
                {
                    map[px, py] = 1;
                }
            }
        }
    }


    // =========================================================
    // GERAR PAREDES
    // =========================================================

    void GenerateWalls()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Considera chão e tapete

                if (
                    map[x, y] != 1 &&
                    map[x, y] != 3
                )
                {
                    continue;
                }

                /* Verifica os 8 tiles ao redor 
                 p/ não colocar paredes no meio do caminho*/
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        // Posição do tile ao redor
                        int nx = x + dx;
                        int ny = y + dy;

                        // Se estiver fora do mapa, ignora
                        if (!IsInsideMap(nx, ny))
                        {
                            continue;
                        }

                        // Se o tile ao redor for vazio, coloca uma parede
                        if (map[nx, ny] == 0)
                        {
                            map[nx, ny] = 2;
                        }
                    }
                }
            }
        }
    }


    // =========================================================
    // VERIFICAR POSIÇÃO
    // =========================================================

    bool IsInsideMap(int x, int y)
    {
        return
            x >= 0 &&
            x < mapWidth &&
            y >= 0 &&
            y < mapHeight;
    }


    // =========================================================
    // VERIFICAR CHÃO
    // =========================================================

    public bool IsFloor(Vector2Int position)
    {
        if (!IsInsideMap(
            position.x,
            position.y
        ))
        {
            return false;
        }


        // Tapete também pode ser considerado chão

        return
            map[position.x, position.y] == 1 ||
            map[position.x, position.y] == 3;
    }


    // =========================================================
    // VERIFICAR PAREDE
    // =========================================================

    public bool IsWall(Vector2Int position)
    {
        if (!IsInsideMap(
            position.x,
            position.y
        ))
        {
            return false;
        }


        return map[
            position.x,
            position.y
        ] == 2;
    }


    // =========================================================
    // VERIFICAR TAPETE
    // =========================================================

    public bool IsCarpet(Vector2Int position)
    {
        if (!IsInsideMap(
            position.x,
            position.y
        ))
        {
            return false;
        }


        return map[
            position.x,
            position.y
        ] == 3;
    }


    // =========================================================
    // PEGAR POSIÇÃO ALEATÓRIA NO CHÃO
    // =========================================================

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


            if (
                map[x, y] == 1 ||
                map[x, y] == 3
            )
            {
                return new Vector2Int(
                    x,
                    y
                );
            }
        }


        return Vector2Int.zero;
    }


    // =========================================================
    // DESENHAR MAPA NA SCENE
    // =========================================================

    void OnDrawGizmos()
    {
        if (map == null)
        {
            return;
        }


        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3 position =
                    new Vector3(
                        x,
                        y,
                        0
                    );


                // -----------------------------------------
                // CHÃO
                // -----------------------------------------

                if (map[x, y] == 1)
                {
                    Gizmos.color =
                        new Color(
                            0.55f,
                            0.45f,
                            0.30f
                        );

                    Gizmos.DrawCube(
                        position,
                        Vector3.one * 0.9f
                    );
                }


                // -----------------------------------------
                // PAREDE
                // -----------------------------------------

                else if (map[x, y] == 2)
                {
                    Gizmos.color =
                        Color.gray;

                    Gizmos.DrawCube(
                        position,
                        Vector3.one * 0.9f
                    );
                }


                // -----------------------------------------
                // TAPETE
                // -----------------------------------------

                else if (map[x, y] == 3)
                {
                    Gizmos.color =
                        new Color(
                            0.8f,
                            0.15f,
                            0.1f
                        );

                    Gizmos.DrawCube(
                        position,
                        Vector3.one * 0.9f
                    );
                }
            }
        }
    }

    void DrawTiles()
    {
        tilemap.ClearAllTiles();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);

                switch (map[x, y])
                {
                    case 1:
                        tilemap.SetTile(position, floorTile);
                        break;

                    case 2:
                        tilemap.SetTile(position, wallTile);
                        break;

                    case 3:
                        tilemap.SetTile(position, carpetTile);
                        break;
                }
            }
        }
    }

}
