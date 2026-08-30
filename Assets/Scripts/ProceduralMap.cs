using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralMap : MonoBehaviour
{
    private const int TamanhoSpriteFallback = 16;

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
    public bool imprimirMapaNoConsole = false;

    [Header("Tilemap")]
    public Tilemap tilemap;

    public TileBase floorTile;

    public TileBase wallTopTile;
    public TileBase wallLeftTile;
    public TileBase wallRightTile;
    public TileBase wallBottomTile;
    public TileBase wallTile;

    [Header("Quinas externas das paredes")]
    public TileBase wallCornerTopLeftTile;
    public TileBase wallCornerTopRightTile;
    public TileBase wallCornerBottomLeftTile;
    public TileBase wallCornerBottomRightTile;

    [Header("Quinas internas das paredes")]
    public TileBase wallInnerCornerTopLeftTile;
    public TileBase wallInnerCornerTopRightTile;
    public TileBase wallInnerCornerBottomLeftTile;
    public TileBase wallInnerCornerBottomRightTile;

    [Header("Colisao das paredes")]
    [Range(0.02f, 0.5f)]
    public float espessuraColisaoParede = 0.15f;

    public TileBase carpetTile;
    public TileBase carpetTopTile;
    public TileBase carpetBottomTile;
    public TileBase carpetLeftTile;
    public TileBase carpetRightTile;
    public TileBase carpetTopLeftTile;
    public TileBase carpetTopRightTile;
    public TileBase carpetBottomLeftTile;
    public TileBase carpetBottomRightTile;

    [Header("Cores fallback")]
    public Color floorFallbackColor = new Color(0.42f, 0.45f, 0.39f, 1f);
    public Color wallFallbackColor = new Color(0.18f, 0.19f, 0.18f, 1f);
    public Color carpetFallbackColor = new Color(0.2f, 0.55f, 0.45f, 1f);
    public bool usarFallbackVisualParaChaoEParede = false;

    [Header("Gameplay")]
    public bool gerarAoIniciar = true;
    public bool popularMapa = true;
    public bool criarTriggersTapete = true;
    public bool bloquearParedes = true;
    public GameObject jogadorPrefab;
    public GameObject cobraPrefab;
    public GameObject cientistaPrefab;
    public Camera cameraPrincipal;

    [Header("Material do chao e paredes")]
    [Tooltip("Opcional. Material normal usado SOMENTE pelo Tilemap principal (chao e paredes). Se ficar vazio e o Tilemap estiver usando SpriteHue, o codigo cria um material Sprite normal automaticamente.")]
    public Material materialChaoParede;

    [Header("Hue dos tapetes")]
    [Tooltip("Opcional. Se existir um Material SpriteHue nos Assets, arraste aqui. No Editor o codigo tambem tenta encontra-lo automaticamente.")]
    public Material spriteHueMaterial;
    [Tooltip("Opcional. Shader SpriteHue. Se vazio, o codigo tenta Shader.Find(\"Custom/SpriteHue\").")]
    public Shader spriteHueShader;
    public Material[] carpetMaterials;

    [Header("Puzzle de saida")]
    public bool gerarPuzzle = true;
    public Sprite caixaSprite;
    public Sprite botaoSprite;
    public Sprite botaoPressionadoSprite;
    public float tamanhoVisualPuzzle = 0.9f;

    // 0 = vazio | 1 = chão | 2 = parede | 3 = tapete
    private int[,] map;

    private List<RectInt> rooms = new List<RectInt>();
    private readonly List<Vector2Int> posicoesChao = new List<Vector2Int>();
    private readonly List<Vector2Int> posicoesTapete = new List<Vector2Int>();
    private readonly Dictionary<Vector2Int, float> huesTapetes = new Dictionary<Vector2Int, float>();
    private readonly Dictionary<float, Material> materiaisHueTapetes = new Dictionary<float, Material>();
    private Transform objetosGerados;
    private TileBase floorFallbackTile;
    private TileBase wallFallbackRuntimeTile;
    private TileBase carpetFallbackRuntimeTile;
    private Transform tapetesVisuaisGerados;
    private Material spriteHueMaterialEncontrado;
    private Material materialNormalTilemapRuntime;

    private enum TipoParede
    {
        Generica,
        Top,
        Bottom,
        Left,
        Right,
        CornerTopLeft,
        CornerTopRight,
        CornerBottomLeft,
        CornerBottomRight,
        InnerCornerTopLeft,
        InnerCornerTopRight,
        InnerCornerBottomLeft,
        InnerCornerBottomRight
    }

    private static readonly Vector2Int[] DirecoesCardinais =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public bool TemMapaGerado => map != null && posicoesChao.Count > 0;


    private void Start()
    {
        if (gerarAoIniciar)
        {
            GenerateMap();
        }
    }


    public void GenerateMap()
    {
        EncontrarTilemapSePreciso();

        if (tilemap == null)
        {
            Debug.LogError("ProceduralMap: Tilemap não foi configurado e nenhum Tilemap foi encontrado na cena.");
            return;
        }

        // GARANTIA IMPORTANTE:
        // SpriteHue nunca pode ficar no Tilemap principal.
        // O Tilemap principal desenha somente chao e paredes.
        PrepararMaterialDoTilemapPrincipal();

        if (randomSeed)
            seed = Random.Range(0, 999999);

        Random.InitState(seed);

        map = new int[mapWidth, mapHeight];
        rooms.Clear();
        LimparTapetesVisuaisGerados();
        LimparMateriaisHueTapetes();
        huesTapetes.Clear();

        GenerateRooms();
        ConnectRooms();
        DefinirHuesDosTapetes();
        GenerateWalls();
        DrawTiles();
        PrepararFisicaDoTilemap();
        PrepararObjetosDeGameplay();

        Debug.Log(
            $"Mapa gerado | Seed: {seed} | Salas: {rooms.Count}"
        );

        if (imprimirMapaNoConsole)
        {
            PrintMap(map);
        }
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

                // Paredes retas ao redor do chão.
                TryCreateWall(x + 1, y);
                TryCreateWall(x - 1, y);
                TryCreateWall(x, y + 1);
                TryCreateWall(x, y - 1);

                // Células diagonais são necessárias para desenhar
                // corretamente as quatro quinas externas do tileset.
                // Isso NÃO altera chão nem tapete: TryCreateWall só
                // transforma células vazias (0) em parede (2).
                TryCreateWall(x + 1, y + 1);
                TryCreateWall(x - 1, y + 1);
                TryCreateWall(x + 1, y - 1);
                TryCreateWall(x - 1, y - 1);
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

    private TipoParede ObterTipoParede(int x, int y)
    {
        bool floorUp = IsFloorTile(x, y + 1);
        bool floorDown = IsFloorTile(x, y - 1);
        bool floorLeft = IsFloorTile(x - 1, y);
        bool floorRight = IsFloorTile(x + 1, y);

        bool floorUpLeft = IsFloorTile(x - 1, y + 1);
        bool floorUpRight = IsFloorTile(x + 1, y + 1);
        bool floorDownLeft = IsFloorTile(x - 1, y - 1);
        bool floorDownRight = IsFloorTile(x + 1, y - 1);

        // Quinas internas: dois lados cardinais de chao encostam na mesma celula.
        if (floorDown && floorRight && !floorUp && !floorLeft)
            return TipoParede.InnerCornerTopLeft;

        if (floorDown && floorLeft && !floorUp && !floorRight)
            return TipoParede.InnerCornerTopRight;

        if (floorUp && floorRight && !floorDown && !floorLeft)
            return TipoParede.InnerCornerBottomLeft;

        if (floorUp && floorLeft && !floorDown && !floorRight)
            return TipoParede.InnerCornerBottomRight;

        // Paredes retas. A orientacao e definida pelo lado onde esta o chao.
        if (floorDown && !floorUp)
            return TipoParede.Top;

        if (floorUp && !floorDown)
            return TipoParede.Bottom;

        if (floorRight && !floorLeft)
            return TipoParede.Left;

        if (floorLeft && !floorRight)
            return TipoParede.Right;

        // Quinas externas: a parede existe apenas por causa do chao diagonal.
        if (!floorUp && !floorDown && !floorLeft && !floorRight)
        {
            if (floorDownRight)
                return TipoParede.CornerTopLeft;

            if (floorDownLeft)
                return TipoParede.CornerTopRight;

            if (floorUpRight)
                return TipoParede.CornerBottomLeft;

            if (floorUpLeft)
                return TipoParede.CornerBottomRight;
        }

        return TipoParede.Generica;
    }


    private TileBase GetWallTile(int x, int y)
    {
        switch (ObterTipoParede(x, y))
        {
            case TipoParede.Top:
                return ObterTileParede(wallTopTile != null ? wallTopTile : wallTile);

            case TipoParede.Bottom:
                return ObterTileParede(wallBottomTile != null ? wallBottomTile : wallTile);

            case TipoParede.Left:
                return ObterTileParede(wallLeftTile != null ? wallLeftTile : wallTile);

            case TipoParede.Right:
                return ObterTileParede(wallRightTile != null ? wallRightTile : wallTile);

            case TipoParede.CornerTopLeft:
                return ObterTileParede(wallCornerTopLeftTile != null ? wallCornerTopLeftTile : wallTile);

            case TipoParede.CornerTopRight:
                return ObterTileParede(wallCornerTopRightTile != null ? wallCornerTopRightTile : wallTile);

            case TipoParede.CornerBottomLeft:
                return ObterTileParede(wallCornerBottomLeftTile != null ? wallCornerBottomLeftTile : wallTile);

            case TipoParede.CornerBottomRight:
                return ObterTileParede(wallCornerBottomRightTile != null ? wallCornerBottomRightTile : wallTile);

            // Se as sprites internas ainda nao tiverem sido configuradas no Inspector,
            // o fallback e a quina correspondente, e NAO a parede generica.
            case TipoParede.InnerCornerTopLeft:
                return ObterTileParede(
                    wallInnerCornerTopLeftTile != null
                        ? wallInnerCornerTopLeftTile
                        : (wallCornerTopLeftTile != null ? wallCornerTopLeftTile : wallTile)
                );

            case TipoParede.InnerCornerTopRight:
                return ObterTileParede(
                    wallInnerCornerTopRightTile != null
                        ? wallInnerCornerTopRightTile
                        : (wallCornerTopRightTile != null ? wallCornerTopRightTile : wallTile)
                );

            case TipoParede.InnerCornerBottomLeft:
                return ObterTileParede(
                    wallInnerCornerBottomLeftTile != null
                        ? wallInnerCornerBottomLeftTile
                        : (wallCornerBottomLeftTile != null ? wallCornerBottomLeftTile : wallTile)
                );

            case TipoParede.InnerCornerBottomRight:
                return ObterTileParede(
                    wallInnerCornerBottomRightTile != null
                        ? wallInnerCornerBottomRightTile
                        : (wallCornerBottomRightTile != null ? wallCornerBottomRightTile : wallTile)
                );

            default:
                return ObterTileParede(wallTile);
        }
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
        if (posicoesChao.Count > 0)
        {
            return posicoesChao[
                Random.Range(0, posicoesChao.Count)
            ];
        }

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


    public Vector3 ObterCentroMundo(Vector2Int position)
    {
        if (tilemap != null)
        {
            return tilemap.GetCellCenterWorld(
                new Vector3Int(position.x, position.y, 0)
            );
        }

        return new Vector3(
            position.x + 0.5f,
            position.y + 0.5f,
            0f
        ) + transform.position;
    }


    public Vector2Int ObterCelula(Vector2 worldPosition)
    {
        if (tilemap != null)
        {
            Vector3Int cell = tilemap.WorldToCell(worldPosition);
            return new Vector2Int(cell.x, cell.y);
        }

        Vector2 local = worldPosition - (Vector2)transform.position;
        return new Vector2Int(
            Mathf.FloorToInt(local.x),
            Mathf.FloorToInt(local.y)
        );
    }


    public bool EhChaoEmMundo(Vector2 worldPosition)
    {
        return IsFloor(ObterCelula(worldPosition));
    }


    public bool TemParedeEntreMundo(Vector2 origem, Vector2 destino)
    {
        if (!TemMapaGerado)
        {
            return false;
        }

        float distancia = Vector2.Distance(origem, destino);
        int passos = Mathf.Max(1, Mathf.CeilToInt(distancia * 4f));

        for (int i = 1; i < passos; i++)
        {
            Vector2 ponto = Vector2.Lerp(origem, destino, i / (float)passos);

            if (IsWall(ObterCelula(ponto)))
            {
                return true;
            }
        }

        return false;
    }


    public Vector2 ObterPontoAntesDaParedeMundo(Vector2 origem, Vector2 destino, out bool bateuNaParede)
    {
        bateuNaParede = false;

        if (!TemMapaGerado)
        {
            return destino;
        }

        float distancia = Vector2.Distance(origem, destino);
        int passos = Mathf.Max(1, Mathf.CeilToInt(distancia * 8f));
        Vector2 pontoAnterior = origem;

        for (int i = 1; i <= passos; i++)
        {
            Vector2 ponto = Vector2.Lerp(origem, destino, i / (float)passos);

            if (IsWall(ObterCelula(ponto)))
            {
                bateuNaParede = true;
                return pontoAnterior;
            }

            pontoAnterior = ponto;
        }

        return destino;
    }


    public Vector2 ObterPosicaoMundoAleatoriaDeChao(Vector2 origem, float raio)
    {
        if (!TemMapaGerado)
        {
            return origem;
        }

        float raioSeguro = Mathf.Max(1f, raio);

        for (int i = 0; i < 80; i++)
        {
            Vector2Int celula = GetRandomFloorPosition();
            Vector2 posicao = ObterCentroMundo(celula);
            float distancia = Vector2.Distance(origem, posicao);

            if (distancia <= raioSeguro && distancia >= 1f)
            {
                return posicao;
            }
        }

        return ObterCentroMundo(GetRandomFloorPosition());
    }


    public Vector2 ObterPosicaoMundoAleatoriaLongeDe(Vector2 origem, Vector2 evitar, float raio)
    {
        if (!TemMapaGerado)
        {
            return origem;
        }

        Vector2 melhorPosicao = origem;
        float melhorPontuacao = float.MinValue;
        float raioSeguro = Mathf.Max(1f, raio);

        for (int i = 0; i < 120; i++)
        {
            Vector2Int celula = GetRandomFloorPosition();
            Vector2 posicao = ObterCentroMundo(celula);
            float distanciaOrigem = Vector2.Distance(origem, posicao);

            if (distanciaOrigem > raioSeguro || distanciaOrigem < 1f)
            {
                continue;
            }

            float pontuacao = Vector2.Distance(evitar, posicao) - distanciaOrigem * 0.15f;

            if (pontuacao > melhorPontuacao)
            {
                melhorPontuacao = pontuacao;
                melhorPosicao = posicao;
            }
        }

        return melhorPontuacao > float.MinValue
            ? melhorPosicao
            : ObterCentroMundo(GetRandomFloorPosition());
    }


    public bool TentarObterCaminhoMundo(Vector2 origemMundo, Vector2 destinoMundo, List<Vector2> caminho)
    {
        caminho.Clear();

        if (!TemMapaGerado)
        {
            return false;
        }

        Vector2Int inicio = ObterCelula(origemMundo);
        Vector2Int destino = ObterCelula(destinoMundo);

        if (!IsFloor(inicio) && !TentarEncontrarChaoMaisProximo(inicio, out inicio))
        {
            return false;
        }

        if (!IsFloor(destino) && !TentarEncontrarChaoMaisProximo(destino, out destino))
        {
            return false;
        }

        if (inicio == destino)
        {
            caminho.Add(ObterCentroMundo(destino));
            return true;
        }

        Queue<Vector2Int> fila = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> anterior = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visitados = new HashSet<Vector2Int>();

        fila.Enqueue(inicio);
        visitados.Add(inicio);

        while (fila.Count > 0)
        {
            Vector2Int atual = fila.Dequeue();

            if (atual == destino)
            {
                break;
            }

            foreach (Vector2Int direcao in DirecoesCardinais)
            {
                Vector2Int proximo = atual + direcao;

                if (visitados.Contains(proximo) || !IsFloor(proximo))
                {
                    continue;
                }

                visitados.Add(proximo);
                anterior[proximo] = atual;
                fila.Enqueue(proximo);
            }
        }

        if (!visitados.Contains(destino))
        {
            return false;
        }

        List<Vector2Int> celulas = new List<Vector2Int>();
        Vector2Int passo = destino;

        while (passo != inicio)
        {
            celulas.Add(passo);
            passo = anterior[passo];
        }

        celulas.Reverse();

        foreach (Vector2Int celula in celulas)
        {
            caminho.Add(ObterCentroMundo(celula));
        }

        return caminho.Count > 0;
    }


    private bool TentarEncontrarChaoMaisProximo(Vector2Int origem, out Vector2Int chao)
    {
        chao = origem;
        int raioMaximo = Mathf.Max(mapWidth, mapHeight);

        for (int raio = 1; raio <= raioMaximo; raio++)
        {
            for (int x = origem.x - raio; x <= origem.x + raio; x++)
            {
                for (int y = origem.y - raio; y <= origem.y + raio; y++)
                {
                    bool estaNaBorda = x == origem.x - raio || x == origem.x + raio || y == origem.y - raio || y == origem.y + raio;

                    if (!estaNaBorda)
                    {
                        continue;
                    }

                    Vector2Int candidato = new Vector2Int(x, y);

                    if (IsFloor(candidato))
                    {
                        chao = candidato;
                        return true;
                    }
                }
            }
        }

        return false;
    }


    // =========================================================
    // DESENHAR TILEMAP
    // =========================================================

    private void DrawTiles()
    {
        tilemap.ClearAllTiles();
        posicoesChao.Clear();
        posicoesTapete.Clear();

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
                            ObterTileChao()
                        );
                        tilemap.SetColliderType(position, Tile.ColliderType.None);
                        tilemap.SetTileFlags(position, TileFlags.None);
                        tilemap.SetColor(position, Color.white);
                        posicoesChao.Add(new Vector2Int(x, y));
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
                            // O visual continua no Tilemap, mas a colisao e criada separadamente
                            // para podermos colocar Bottom/Left/Right na borda correta.
                            tilemap.SetColliderType(position, Tile.ColliderType.None);
                            tilemap.SetTileFlags(position, TileFlags.None);
                            tilemap.SetColor(position, Color.white);
                        }

                        break;

                    case 3:
                        // O Tilemap principal nunca recebe SpriteHue e nunca desenha
                        // a arte do tapete. Nesta camada existe apenas o chao que fica
                        // por baixo dele. O tapete visivel e desenhado em Tilemaps
                        // separados por CriarTapetesVisuaisComSpriteHue().
                        tilemap.SetTile(
                            position,
                            ObterTileChao()
                        );
                        tilemap.SetColliderType(position, Tile.ColliderType.None);
                        tilemap.SetTileFlags(position, TileFlags.None);
                        tilemap.SetColor(position, Color.white);
                        posicoesChao.Add(new Vector2Int(x, y));
                        posicoesTapete.Add(new Vector2Int(x, y));
                        break;
                }
            }
        }
    }


    private TileBase ObterTileChao()
    {
        if (usarFallbackVisualParaChaoEParede)
        {
            return ObterTileValido(null, ref floorFallbackTile, floorFallbackColor, "ChaoFallback", Tile.ColliderType.None);
        }

        return ObterTileValido(floorTile, ref floorFallbackTile, floorFallbackColor, "ChaoFallback", Tile.ColliderType.None);
    }


    private TileBase ObterTileParede(TileBase tileConfigurado)
    {
        if (usarFallbackVisualParaChaoEParede)
        {
            return ObterTileValido(null, ref wallFallbackRuntimeTile, wallFallbackColor, "ParedeFallback", Tile.ColliderType.Sprite);
        }

        return ObterTileValido(tileConfigurado, ref wallFallbackRuntimeTile, wallFallbackColor, "ParedeFallback", Tile.ColliderType.Sprite);
    }


    private TileBase ObterTileTapete(int x, int y)
    {
        TileBase tileEscolhido = EscolherTileTapete(x, y);
        return ObterTileValido(tileEscolhido, ref carpetFallbackRuntimeTile, carpetFallbackColor, "TapeteFallback", Tile.ColliderType.None);
    }


    private TileBase EscolherTileTapete(int x, int y)
    {
        bool up = IsCarpet(new Vector2Int(x, y + 1));
        bool down = IsCarpet(new Vector2Int(x, y - 1));
        bool left = IsCarpet(new Vector2Int(x - 1, y));
        bool right = IsCarpet(new Vector2Int(x + 1, y));

        if (!up && !left)
        {
            return carpetTopLeftTile != null ? carpetTopLeftTile : carpetTile;
        }

        if (!up && !right)
        {
            return carpetTopRightTile != null ? carpetTopRightTile : carpetTile;
        }

        if (!down && !left)
        {
            return carpetBottomLeftTile != null ? carpetBottomLeftTile : carpetTile;
        }

        if (!down && !right)
        {
            return carpetBottomRightTile != null ? carpetBottomRightTile : carpetTile;
        }

        if (!up)
        {
            return carpetTopTile != null ? carpetTopTile : carpetTile;
        }

        if (!down)
        {
            return carpetBottomTile != null ? carpetBottomTile : carpetTile;
        }

        if (!left)
        {
            return carpetLeftTile != null ? carpetLeftTile : carpetTile;
        }

        if (!right)
        {
            return carpetRightTile != null ? carpetRightTile : carpetTile;
        }

        return carpetTile;
    }


    private void DefinirHuesDosTapetes()
    {
        huesTapetes.Clear();
        HashSet<Vector2Int> visitados = new HashSet<Vector2Int>();

        // Percorre o mapa em ordem fixa. Como Random.InitState(seed) já foi chamado,
        // os hues também ficam totalmente determinados pela seed.
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector2Int inicio = new Vector2Int(x, y);

                if (!IsCarpet(inicio) || visitados.Contains(inicio))
                    continue;

                // Um único hue para todo o tapete conectado.
                float hueDoTapete = Random.Range(0f, 360f);
                Queue<Vector2Int> fila = new Queue<Vector2Int>();
                fila.Enqueue(inicio);
                visitados.Add(inicio);

                while (fila.Count > 0)
                {
                    Vector2Int atual = fila.Dequeue();
                    huesTapetes[atual] = hueDoTapete;

                    foreach (Vector2Int direcao in DirecoesCardinais)
                    {
                        Vector2Int vizinho = atual + direcao;

                        if (visitados.Contains(vizinho) || !IsCarpet(vizinho))
                            continue;

                        visitados.Add(vizinho);
                        fila.Enqueue(vizinho);
                    }
                }
            }
        }
    }


    private float ObterHueDoTapete(Vector2Int posicao)
    {
        return huesTapetes.TryGetValue(posicao, out float hue)
            ? hue
            : 0f;
    }


    private static TileBase ObterTileValido(TileBase tileConfigurado, ref TileBase fallback, Color cor, string nome, Tile.ColliderType colliderType)
    {
        if (TilePareceRenderizavel(tileConfigurado))
        {
            if (tileConfigurado is Tile tilePadrao)
            {
                tilePadrao.colliderType = colliderType;
            }

            return tileConfigurado;
        }

        if (fallback == null)
        {
            fallback = CriarTileFallback(cor, nome, colliderType);
        }

        return fallback;
    }


    private static bool TilePareceRenderizavel(TileBase tile)
    {
        if (tile == null)
        {
            return false;
        }

        if (tile is Tile tilePadrao)
        {
            return tilePadrao.sprite != null;
        }

        return true;
    }


    private static TileBase CriarTileFallback(Color cor, string nome, Tile.ColliderType colliderType)
    {
        Texture2D textura = new Texture2D(TamanhoSpriteFallback, TamanhoSpriteFallback, TextureFormat.RGBA32, false)
        {
            name = $"{nome}Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[TamanhoSpriteFallback * TamanhoSpriteFallback];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = cor;
        }

        textura.SetPixels(pixels);
        textura.Apply();

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = nome;
        tile.colliderType = colliderType;
        tile.sprite = Sprite.Create(
            textura,
            new Rect(0f, 0f, TamanhoSpriteFallback, TamanhoSpriteFallback),
            new Vector2(0.5f, 0.5f),
            TamanhoSpriteFallback
        );

        return tile;
    }


    private void EncontrarTilemapSePreciso()
    {
        if (tilemap != null)
        {
            return;
        }

        tilemap = GetComponentInChildren<Tilemap>();

        if (tilemap == null)
        {
            tilemap = FindFirstObjectByType<Tilemap>();
        }
    }


    private void PrepararFisicaDoTilemap()
    {
        if (!bloquearParedes || tilemap == null)
        {
            return;
        }

        Rigidbody2D corpo = tilemap.GetComponent<Rigidbody2D>();

        if (corpo == null)
        {
            corpo = tilemap.gameObject.AddComponent<Rigidbody2D>();
        }

        corpo.bodyType = RigidbodyType2D.Static;
        corpo.simulated = true;

        CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();

        if (composite == null)
        {
            composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
        }

        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        TilemapCollider2D colisor = tilemap.GetComponent<TilemapCollider2D>();

        if (colisor == null)
        {
            colisor = tilemap.gameObject.AddComponent<TilemapCollider2D>();
        }

        colisor.compositeOperation = Collider2D.CompositeOperation.Merge;
    }


    private void PrepararObjetosDeGameplay()
    {
        LimparObjetosGerados();

        // Nao substitui o Tilemap principal. E apenas uma camada visual por cima
        // usando o SpriteHue. Se o shader/material nao existir, o tapete original
        // continua aparecendo normalmente no Tilemap.
        CriarTapetesVisuaisComSpriteHue();

        if (bloquearParedes)
        {
            CriarColisoresDasParedes();
        }

        if (criarTriggersTapete)
        {
            CriarTriggersDosTapetes();
        }

        if (popularMapa)
        {
            PopularMapaComPersonagens();
        }
    }


    private void LimparObjetosGerados()
    {
        Transform existente = transform.Find("Objetos Gerados");

        if (existente != null)
        {
            DestruirSeguro(existente.gameObject);
        }

        GameObject raiz = new GameObject("Objetos Gerados");
        raiz.transform.SetParent(transform, false);
        objetosGerados = raiz.transform;
    }


    private void PrepararMaterialDoTilemapPrincipal()
    {
        if (tilemap == null)
            return;

        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();

        if (renderer == null)
            return;

        // Se o usuario forneceu explicitamente um material normal para chao/parede,
        // ele sempre tem prioridade.
        if (materialChaoParede != null && !EhMaterialSpriteHue(materialChaoParede))
        {
            renderer.sharedMaterial = materialChaoParede;
            return;
        }

        Material atual = renderer.sharedMaterial;

        // Se o material atual nao for SpriteHue, nao mexemos nele.
        if (atual != null && !EhMaterialSpriteHue(atual))
            return;

        // Neste ponto o Tilemap principal esta sem material ou ficou com SpriteHue
        // atribuido no Inspector/por uma versao anterior do script. Criamos um
        // material de sprite NORMAL para chao e paredes.
        if (materialNormalTilemapRuntime == null)
        {
            Shader shaderNormal = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            if (shaderNormal == null)
                shaderNormal = Shader.Find("Sprites/Default");

            if (shaderNormal != null)
            {
                materialNormalTilemapRuntime = new Material(shaderNormal);
                materialNormalTilemapRuntime.name = "Material Normal - Chao e Paredes (Runtime)";
            }
        }

        // Se conseguimos um shader normal, usa ele. Se nao, deixa null para o
        // TilemapRenderer recorrer ao material padrao da pipeline em vez de SpriteHue.
        renderer.sharedMaterial = materialNormalTilemapRuntime;
    }


    private bool EhMaterialSpriteHue(Material material)
    {
        if (material == null)
            return false;

        if (spriteHueMaterial != null && material == spriteHueMaterial)
            return true;

        Shader shader = material.shader;

        if (shader == null)
            return false;

        if (spriteHueShader != null && shader == spriteHueShader)
            return true;

        return shader.name.IndexOf("SpriteHue", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }


    private void LimparTapetesVisuaisGerados()
    {
        if (tapetesVisuaisGerados != null)
        {
            DestruirSeguro(tapetesVisuaisGerados.gameObject);
            tapetesVisuaisGerados = null;
            return;
        }

        if (tilemap == null || tilemap.transform.parent == null)
            return;

        Transform existente = tilemap.transform.parent.Find("Tapetes SpriteHue Procedurais");

        if (existente != null)
            DestruirSeguro(existente.gameObject);
    }


    private void CriarTapetesVisuaisComSpriteHue()
    {
        if (tilemap == null || posicoesTapete.Count == 0)
            return;

        LimparTapetesVisuaisGerados();

        Transform pai = tilemap.transform.parent;
        GameObject raiz = new GameObject("Tapetes SpriteHue Procedurais");
        raiz.transform.SetParent(pai, false);
        raiz.transform.localPosition = tilemap.transform.localPosition;
        raiz.transform.localRotation = tilemap.transform.localRotation;
        raiz.transform.localScale = tilemap.transform.localScale;
        tapetesVisuaisGerados = raiz.transform;

        TilemapRenderer rendererOriginal = tilemap.GetComponent<TilemapRenderer>();
        Material materialOriginal = rendererOriginal != null ? rendererOriginal.sharedMaterial : null;

        // IMPORTANTE:
        // Cada Tilemap abaixo contem UMA UNICA celula de tapete.
        // Isso impede que um shader SpriteHue que nao se comporte bem com bounds/chunks
        // grandes do TilemapRenderer pinte areas vazias, chao ou paredes.
        // O Material continua sendo compartilhado entre todas as celulas do mesmo
        // tapete, portanto o tapete inteiro permanece com exatamente o mesmo Hue.
        foreach (Vector2Int posicao in posicoesTapete)
        {
            float hue = ObterHueDoTapete(posicao);
            Material materialHue = ObterMaterialDeTapete(posicao);

            GameObject objeto = new GameObject(
                $"Tapete Hue {hue:0.##} [{posicao.x},{posicao.y}]"
            );
            objeto.transform.SetParent(raiz.transform, false);

            Tilemap tilemapTapete = objeto.AddComponent<Tilemap>();
            tilemapTapete.tileAnchor = tilemap.tileAnchor;
            tilemapTapete.orientation = tilemap.orientation;

            TilemapRenderer renderer = objeto.AddComponent<TilemapRenderer>();

            // SpriteHue existe SOMENTE neste renderer de uma unica celula.
            renderer.sharedMaterial = materialHue != null
                ? materialHue
                : materialOriginal;

            if (rendererOriginal != null)
            {
                renderer.sortingLayerID = rendererOriginal.sortingLayerID;
                renderer.sortingOrder = rendererOriginal.sortingOrder + 1;
            }

            Vector3Int celula = new Vector3Int(posicao.x, posicao.y, 0);
            tilemapTapete.SetTile(
                celula,
                ObterTileTapete(posicao.x, posicao.y)
            );
            tilemapTapete.SetColliderType(
                celula,
                Tile.ColliderType.None
            );
            tilemapTapete.SetTileFlags(
                celula,
                TileFlags.None
            );
            tilemapTapete.SetColor(
                celula,
                Color.white
            );
        }
    }

    private void CriarColisoresDasParedes()
    {
        float espessura = Mathf.Clamp(espessuraColisaoParede, 0.02f, 0.5f);

        // Calcula o tamanho real de uma celula no mundo. Assim a colisao continua
        // alinhada mesmo se o Grid/Tilemap nao estiver usando exatamente 1 unidade.
        Vector3 origem = tilemap.CellToWorld(Vector3Int.zero);
        float larguraCelula = Vector3.Distance(
            origem,
            tilemap.CellToWorld(Vector3Int.right)
        );
        float alturaCelula = Vector3.Distance(
            origem,
            tilemap.CellToWorld(Vector3Int.up)
        );

        if (larguraCelula <= 0.0001f)
            larguraCelula = 1f;

        if (alturaCelula <= 0.0001f)
            alturaCelula = 1f;

        float espessuraX = larguraCelula * espessura;
        float espessuraY = alturaCelula * espessura;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (map[x, y] != 2)
                    continue;

                TipoParede tipo = ObterTipoParede(x, y);

                // Somente as QUINAS INTERNAS nao possuem hitbox.
                // Elas representam um detalhe visual pequeno e a colisao nelas
                // mais atrapalha o movimento do que ajuda.
                if (
                    tipo == TipoParede.InnerCornerTopLeft ||
                    tipo == TipoParede.InnerCornerTopRight ||
                    tipo == TipoParede.InnerCornerBottomLeft ||
                    tipo == TipoParede.InnerCornerBottomRight
                )
                {
                    continue;
                }

                GameObject objeto = new GameObject($"Colisor Parede {x},{y}");
                objeto.transform.SetParent(objetosGerados, true);
                objeto.transform.position = ObterCentroMundo(new Vector2Int(x, y));

                switch (tipo)
                {
                    case TipoParede.Top:
                        {
                            // A parede TOP deve bloquear a celula inteira.
                            BoxCollider2D completo = objeto.AddComponent<BoxCollider2D>();
                            completo.size = new Vector2(larguraCelula, alturaCelula);
                            completo.offset = Vector2.zero;
                            break;
                        }

                    case TipoParede.Bottom:
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.down,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        break;

                    case TipoParede.Left:
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.left,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        break;

                    case TipoParede.Right:
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.right,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        break;

                    // QUINAS EXTERNAS voltam ao comportamento anterior:
                    // duas pequenas bordas de colisao acompanhando a sprite.
                    case TipoParede.CornerTopLeft:
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.up,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.left,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        break;

                    case TipoParede.CornerTopRight:
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.up,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.right,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        break;

                    case TipoParede.CornerBottomLeft:
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.down,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.left,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        break;

                    case TipoParede.CornerBottomRight:
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.down,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        AdicionarColisorBorda(
                            objeto,
                            Vector2Int.right,
                            larguraCelula,
                            alturaCelula,
                            espessuraX,
                            espessuraY
                        );
                        break;

                    default:
                        {
                            BoxCollider2D completo = objeto.AddComponent<BoxCollider2D>();
                            completo.size = new Vector2(larguraCelula, alturaCelula);
                            completo.offset = Vector2.zero;
                            break;
                        }
                }
            }
        }
    }

    private static void AdicionarColisorBorda(
        GameObject objeto,
        Vector2Int direcao,
        float larguraCelula,
        float alturaCelula,
        float espessuraX,
        float espessuraY
    )
    {
        BoxCollider2D colisor = objeto.AddComponent<BoxCollider2D>();

        if (direcao == Vector2Int.up)
        {
            colisor.size = new Vector2(larguraCelula, espessuraY);
            colisor.offset = new Vector2(0f, alturaCelula * 0.5f - espessuraY * 0.5f);
        }
        else if (direcao == Vector2Int.down)
        {
            colisor.size = new Vector2(larguraCelula, espessuraY);
            colisor.offset = new Vector2(0f, -alturaCelula * 0.5f + espessuraY * 0.5f);
        }
        else if (direcao == Vector2Int.left)
        {
            colisor.size = new Vector2(espessuraX, alturaCelula);
            colisor.offset = new Vector2(-larguraCelula * 0.5f + espessuraX * 0.5f, 0f);
        }
        else if (direcao == Vector2Int.right)
        {
            colisor.size = new Vector2(espessuraX, alturaCelula);
            colisor.offset = new Vector2(larguraCelula * 0.5f - espessuraX * 0.5f, 0f);
        }
    }


    private void CriarTriggersDosTapetes()
    {
        foreach (Vector2Int posicao in posicoesTapete)
        {
            GameObject tapete = new GameObject("Tapete Procedural");
            tapete.tag = "Tapete";
            tapete.transform.SetParent(objetosGerados, false);
            tapete.transform.position = ObterCentroMundo(posicao);

            BoxCollider2D colisor = tapete.AddComponent<BoxCollider2D>();
            // É somente trigger para a interação do player.
            // Não deve produzir colisão física.
            colisor.isTrigger = true;
            colisor.size = Vector2.one;

            SpriteRenderer renderer = tapete.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            renderer.sharedMaterial = ObterMaterialDeTapete(posicao);
        }
    }


    private Material ObterMaterialDeTapete(Vector2Int posicao)
    {
        float hue = ObterHueDoTapete(posicao);

        if (materiaisHueTapetes.TryGetValue(hue, out Material materialExistente))
            return materialExistente;

        Material materialBase = ObterMaterialBaseSpriteHue();
        Material material = null;

        if (materialBase != null)
        {
            material = new Material(materialBase);
        }
        else
        {
            Shader shader = spriteHueShader != null
                ? spriteHueShader
                : Shader.Find("Custom/SpriteHue");

            if (shader != null)
                material = new Material(shader);
        }

        if (material == null)
        {
            Debug.LogWarning(
                "ProceduralMap: SpriteHue nao foi encontrado. " +
                "O tapete continua sendo desenhado normalmente pelo Tilemap, mas sem o shader SpriteHue."
            );
            return null;
        }

        material.name = $"SpriteHue Tapete {hue:0.##}";

        if (material.HasProperty("_Hue"))
        {
            material.SetFloat("_Hue", hue);
        }
        else
        {
            Debug.LogWarning(
                $"ProceduralMap: o SpriteHue '{material.shader.name}' nao possui a propriedade _Hue."
            );
        }

        materiaisHueTapetes[hue] = material;
        return material;
    }


    private Material ObterMaterialBaseSpriteHue()
    {
        if (spriteHueMaterial != null)
            return spriteHueMaterial;

        if (spriteHueMaterialEncontrado != null)
            return spriteHueMaterialEncontrado;

        // Funciona automaticamente se o Material estiver em uma pasta Resources.
        spriteHueMaterialEncontrado = Resources.Load<Material>("SpriteHue");

#if UNITY_EDITOR
        // Durante o desenvolvimento, procura um Material chamado SpriteHue em qualquer
        // pasta de Assets sem exigir que ele esteja dentro de Resources.
        if (spriteHueMaterialEncontrado == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("SpriteHue t:Material");

            if (guids != null && guids.Length > 0)
            {
                string caminho = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                spriteHueMaterialEncontrado = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(caminho);
            }
        }
#endif

        // Mantem compatibilidade com o array que ja existia no projeto.
        if (spriteHueMaterialEncontrado == null && carpetMaterials != null && carpetMaterials.Length > 0)
            spriteHueMaterialEncontrado = carpetMaterials[0];

        return spriteHueMaterialEncontrado;
    }


    private void LimparMateriaisHueTapetes()
    {
        foreach (Material material in materiaisHueTapetes.Values)
        {
            if (material == null)
                continue;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }

        materiaisHueTapetes.Clear();
    }


    private void OnDestroy()
    {
        if (materialNormalTilemapRuntime != null)
        {
            if (Application.isPlaying)
                Destroy(materialNormalTilemapRuntime);
            else
                DestroyImmediate(materialNormalTilemapRuntime);
        }
    }


    private void PopularMapaComPersonagens()
    {
        if (!TemMapaGerado)
        {
            return;
        }

        Vector2Int celulaJogador = ObterCelulaInicialDoJogador();
        Vector3 posicaoJogador = ObterCentroMundo(celulaJogador);
        GameObject jogador = ObterOuCriarJogador(posicaoJogador);

        if (jogador == null)
        {
            return;
        }

        ConfigurarCamera(jogador.transform);
        GarantirGameOver();
        CriarInimigo(cobraPrefab, "Cobra", posicaoJogador, Vector2.right);
        CriarInimigo(cientistaPrefab, "Cientista", posicaoJogador, Vector2.left);
        CriarPuzzleDeSaida(celulaJogador);
    }


    private Vector2Int ObterCelulaInicialDoJogador()
    {
        if (rooms.Count > 0)
        {
            Vector2Int centro = GetRoomCenter(rooms[0]);

            if (IsFloor(centro))
            {
                return centro;
            }
        }

        return GetRandomFloorPosition();
    }


    private GameObject ObterOuCriarJogador(Vector3 posicao)
    {
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");

        if (jogador == null && jogadorPrefab != null)
        {
            jogador = Instantiate(jogadorPrefab, posicao, Quaternion.identity, objetosGerados);
            jogador.name = "Jogador";
        }

        if (jogador == null)
        {
            Debug.LogWarning("ProceduralMap: jogadorPrefab não foi configurado.");
            return null;
        }

        jogador.transform.position = posicao;
        jogador.transform.rotation = Quaternion.identity;
        jogador.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
        ConfigurarFisicaDoJogador(jogador);
        jogador.transform.position = CalcularPosicaoComColliderNoCentro(jogador, posicao);
        return jogador;
    }


    private static void ConfigurarFisicaDoJogador(GameObject jogador)
    {
        Rigidbody2D corpo = jogador.GetComponent<Rigidbody2D>();

        if (corpo == null)
        {
            return;
        }

        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.rotation = 0f;
        corpo.angularVelocity = 0f;
        corpo.linearVelocity = Vector2.zero;
    }


    private static Vector3 CalcularPosicaoComColliderNoCentro(GameObject objeto, Vector3 centroDaCelula)
    {
        Collider2D colisor = objeto.GetComponent<Collider2D>();

        if (colisor == null)
        {
            return centroDaCelula;
        }

        Physics2D.SyncTransforms();
        Vector3 diferenca = centroDaCelula - colisor.bounds.center;
        diferenca.z = 0f;
        return objeto.transform.position + diferenca;
    }


    private void CriarInimigo(GameObject prefab, string nome, Vector2 posicaoJogador, Vector2 direcaoInicial)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"ProceduralMap: prefab de {nome} não foi configurado.");
            return;
        }

        Vector2 posicao = ObterPosicaoMundoAleatoriaLongeDe(
            posicaoJogador,
            posicaoJogador,
            Mathf.Max(mapWidth, mapHeight)
        );

        GameObject inimigoObjeto = Instantiate(prefab, posicao, Quaternion.identity, objetosGerados);
        inimigoObjeto.name = nome;
        inimigoObjeto.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

        Inimigo inimigo = inimigoObjeto.GetComponent<Inimigo>();

        if (inimigo != null)
        {
            Vector2 direcaoParaLongeDoJogador = (posicao - posicaoJogador).normalized;

            if (direcaoParaLongeDoJogador.sqrMagnitude <= 0.01f)
            {
                direcaoParaLongeDoJogador = direcaoInicial;
            }

            inimigo.ConfigurarPatrulhaProcedural(this, direcaoParaLongeDoJogador);
        }
    }


    private void CriarPuzzleDeSaida(Vector2Int celulaJogador)
    {
        if (!gerarPuzzle || !TemMapaGerado)
        {
            return;
        }

        HashSet<Vector2Int> ocupadas = new HashSet<Vector2Int> { celulaJogador };
        Vector2Int celulaSaida = ObterCelulaMaisDistanteDe(celulaJogador, ocupadas);
        ocupadas.Add(celulaSaida);

        Vector2Int celulaBotao = ObterCelulaLivreAleatoria(ocupadas, celulaJogador, 5f);
        ocupadas.Add(celulaBotao);

        Vector2Int celulaCaixa = ObterCelulaLivreProxima(celulaBotao, ocupadas, 4);
        ocupadas.Add(celulaCaixa);

        SaidaPuzzle saida = CriarSaidaPuzzle(celulaSaida);
        CriarBotaoPuzzle(celulaBotao, saida);
        CriarCaixaPuzzle(celulaCaixa);
    }


    private Vector2Int ObterCelulaMaisDistanteDe(Vector2Int origem, HashSet<Vector2Int> ocupadas)
    {
        Vector2Int melhor = GetRandomFloorPosition();
        float melhorDistancia = float.MinValue;

        foreach (Vector2Int posicao in posicoesChao)
        {
            if (ocupadas.Contains(posicao))
            {
                continue;
            }

            float distancia = Vector2Int.Distance(origem, posicao);

            if (distancia > melhorDistancia)
            {
                melhorDistancia = distancia;
                melhor = posicao;
            }
        }

        return melhor;
    }


    private Vector2Int ObterCelulaLivreAleatoria(HashSet<Vector2Int> ocupadas, Vector2Int evitar, float distanciaMinima)
    {
        for (int i = 0; i < 120; i++)
        {
            Vector2Int candidato = GetRandomFloorPosition();

            if (!ocupadas.Contains(candidato) && Vector2Int.Distance(candidato, evitar) >= distanciaMinima)
            {
                return candidato;
            }
        }

        foreach (Vector2Int posicao in posicoesChao)
        {
            if (!ocupadas.Contains(posicao))
            {
                return posicao;
            }
        }

        return GetRandomFloorPosition();
    }


    private Vector2Int ObterCelulaLivreProxima(Vector2Int origem, HashSet<Vector2Int> ocupadas, int raioMaximo)
    {
        for (int raio = 1; raio <= raioMaximo; raio++)
        {
            for (int x = origem.x - raio; x <= origem.x + raio; x++)
            {
                for (int y = origem.y - raio; y <= origem.y + raio; y++)
                {
                    Vector2Int candidato = new Vector2Int(x, y);

                    if (!IsFloor(candidato) || ocupadas.Contains(candidato))
                    {
                        continue;
                    }

                    return candidato;
                }
            }
        }

        return ObterCelulaLivreAleatoria(ocupadas, origem, 1f);
    }


    private SaidaPuzzle CriarSaidaPuzzle(Vector2Int celula)
    {
        GameObject saidaObjeto = new GameObject("Saida Puzzle");
        saidaObjeto.transform.SetParent(objetosGerados, false);
        saidaObjeto.transform.position = ObterCentroMundo(celula);

        SpriteRenderer renderer = saidaObjeto.AddComponent<SpriteRenderer>();
        renderer.sprite = CriarSpriteQuadrado(Color.white, "SaidaPuzzleSprite");
        renderer.sortingOrder = 3;

        AjustarSpriteParaCelula(saidaObjeto.transform, renderer.sprite, tamanhoVisualPuzzle);
        CentralizarRenderer(saidaObjeto, ObterCentroMundo(celula));

        BoxCollider2D colisor = saidaObjeto.AddComponent<BoxCollider2D>();
        colisor.size = Vector2.one * 0.9f;

        return saidaObjeto.AddComponent<SaidaPuzzle>();
    }


    private BotaoPuzzle CriarBotaoPuzzle(Vector2Int celula, SaidaPuzzle saida)
    {
        GameObject botaoObjeto = CriarObjetoComSprite("Botao Puzzle", botaoSprite, ObterCentroMundo(celula), 2);
        BoxCollider2D colisor = botaoObjeto.AddComponent<BoxCollider2D>();
        colisor.size = Vector2.one * 0.85f;
        colisor.isTrigger = true;

        BotaoPuzzle botao = botaoObjeto.AddComponent<BotaoPuzzle>();
        botao.spriteSolto = botaoSprite;
        botao.spritePressionado = botaoPressionadoSprite;
        botao.saida = saida;
        return botao;
    }


    private void CriarCaixaPuzzle(Vector2Int celula)
    {
        GameObject caixaObjeto = CriarObjetoComSprite("Caixa Arrastavel", caixaSprite, ObterCentroMundo(celula), 4);
        BoxCollider2D colisor = caixaObjeto.AddComponent<BoxCollider2D>();
        colisor.size = Vector2.one * 0.85f;

        Rigidbody2D corpo = caixaObjeto.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Dynamic;
        corpo.gravityScale = 0f;
        corpo.freezeRotation = true;
        corpo.linearDamping = 8f;

        caixaObjeto.AddComponent<CaixaArrastavel>();
    }


    private GameObject CriarObjetoComSprite(string nome, Sprite sprite, Vector3 posicao, int sortingOrder)
    {
        GameObject objeto = new GameObject(nome);
        objeto.transform.SetParent(objetosGerados, false);
        objeto.transform.position = posicao;

        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : CriarSpriteQuadrado(Color.white, $"{nome}Sprite");
        renderer.sortingOrder = sortingOrder;

        AjustarSpriteParaCelula(objeto.transform, renderer.sprite, tamanhoVisualPuzzle);
        CentralizarRenderer(objeto, posicao);
        return objeto;
    }


    private void AjustarSpriteParaCelula(Transform alvo, Sprite sprite, float tamanho)
    {
        if (sprite == null)
        {
            return;
        }

        Vector2 tamanhoSprite = sprite.bounds.size;
        float maiorEixo = Mathf.Max(tamanhoSprite.x, tamanhoSprite.y);

        if (maiorEixo <= 0.001f)
        {
            return;
        }

        float escala = tamanho / maiorEixo;
        alvo.localScale = new Vector3(escala, escala, 1f);
    }


    private static void CentralizarRenderer(GameObject objeto, Vector3 centro)
    {
        SpriteRenderer renderer = objeto.GetComponent<SpriteRenderer>();

        if (renderer == null)
        {
            return;
        }

        Vector3 diferenca = centro - renderer.bounds.center;
        diferenca.z = 0f;
        objeto.transform.position += diferenca;
    }


    private static Sprite CriarSpriteQuadrado(Color cor, string nome)
    {
        Texture2D textura = new Texture2D(TamanhoSpriteFallback, TamanhoSpriteFallback, TextureFormat.RGBA32, false)
        {
            name = $"{nome}Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[TamanhoSpriteFallback * TamanhoSpriteFallback];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = cor;
        }

        textura.SetPixels(pixels);
        textura.Apply();

        return Sprite.Create(
            textura,
            new Rect(0f, 0f, TamanhoSpriteFallback, TamanhoSpriteFallback),
            new Vector2(0.5f, 0.5f),
            TamanhoSpriteFallback
        );
    }


    private void ConfigurarCamera(Transform alvo)
    {
        Camera cameraDaCena = cameraPrincipal != null ? cameraPrincipal : Camera.main;

        if (cameraDaCena == null)
        {
            return;
        }

        CameraSeguirJogador seguir = cameraDaCena.GetComponent<CameraSeguirJogador>();

        if (seguir == null)
        {
            seguir = cameraDaCena.gameObject.AddComponent<CameraSeguirJogador>();
        }

        seguir.alvo = alvo;
        cameraDaCena.transform.position = alvo.position + seguir.deslocamento;
    }


    private static void GarantirGameOver()
    {
        if (FindFirstObjectByType<GerenciadorGameOver>() == null)
        {
            new GameObject("GerenciadorGameOver").AddComponent<GerenciadorGameOver>();
        }
    }


    private static void DestruirSeguro(GameObject objeto)
    {
        if (Application.isPlaying)
        {
            Destroy(objeto);
        }
        else
        {
            DestroyImmediate(objeto);
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
