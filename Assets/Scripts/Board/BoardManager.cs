using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 롤토체스 스타일의 게임 보드를 관리하는 클래스
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int boardRows = 5;
    public int boardColumns = 3;
    public int benchSlots = 8;
    
    [Header("Cell Settings")]
    public float cellSize = 1.0f; // 칸의 기본 크기 (2배로 확대)
    
    [Header("Visual Settings")]
    public Color cellColor = new Color(1f, 1f, 1f, 0.3f);
    public Color cellOutlineColor = new Color(0.5f, 0.8f, 1f, 0.8f);

    [Header("Ground Tile Settings")]
    public bool useTileGround = true;
    public string tilePrefix = "tile-00";
    public int tileCount = 5;
    
    [Header("UI Safe Area")]
    public float leftUiWidthPixels = 240f;
    public float leftUiMarginPixels = 20f;
    
    [Header("Field boundary walls (lane top/bottom + camera right)")]
    [Tooltip("디버그·레이아웃용. 나중에 Color 알파만 0으로 해도 콜라이더는 유지됩니다.")]
    public bool createFieldWalls = true;
    public Color fieldWallColor = new Color(1f, 0f, 0f, 0f);
    [Tooltip("월드 단위 벽 두께(스프라이트+BoxCollider2D).")]
    public float fieldWallThickness = 0.2f;
    [Tooltip("카메라(오쏘) 프레임 오른쪽 끝에서, 벽/레인 끝을 이 안으로 당깁니다. 0~0.6 정도(월드)에서 세로·상·하 벽이 잘리지 않고 덜이 보이게")]
    public float fieldWallCameraRightEdgeInset = 0.4f;
    
    private Transform boardParent;
    private Transform benchParent;
    private Transform groundParent;
    private Vector3 boardStartPosition;
    private float boardHeight;
    private Sprite[] groundTiles;
    private Sprite unitTileSprite;
    
    void Start()
    {
        useTileGround = false;
        LoadTileSprites();
        unitTileSprite = Resources.Load<Sprite>("unit_tile");
        CreateBoard();
        CreateFieldWalls();
        CreateGround();
        CreateBarrier();
        CreateBench();
        // CreateSubslotPopup();
    }
    
    /// <summary>
    /// 메인 보드 (3x5)를 생성합니다
    /// </summary>
    void CreateBoard()
    {
        boardParent = new GameObject("Board").transform;
        boardParent.SetParent(transform);
        
        // 카메라 왼쪽 끝 좌표 계산
        Camera cam = Camera.main;
        float cameraLeft = cam.transform.position.x - (cam.orthographicSize * GameCameraFit.Aspect);
        float cameraCenterY = cam.transform.position.y; // 카메라 세로 중앙
        float uiOffset = GetLeftUiWorldOffset(cam);
        
        // 보드 위치 계산 (왼쪽 중단에 붙여서 배치)
        float boardWidth = boardColumns * cellSize;
        boardHeight = boardRows * cellSize;
        float leftOffset = cellSize * 0.5f; // 왼쪽 여백
        // 보드를 세로 중앙에 배치 (보드 높이의 절반을 위로 올림)
        Vector3 boardStart = new Vector3(cameraLeft + leftOffset + uiOffset, cameraCenterY + boardHeight * 0.5f - cellSize * 0.5f, 0f);
        boardStartPosition = boardStart;
        
        for (int row = 0; row < boardRows; row++)
        {
            for (int col = 0; col < boardColumns; col++)
            {
                Vector3 position = boardStart + new Vector3(col * cellSize, -row * cellSize, 0f);
                BoardCell cell = CreateCell(position, $"BoardCell_{row}_{col}", true);
                if (cell != null) cell.isBoardCell = true;
            }
        }
    }

    void CreateGround()
    {
        if (!useTileGround)
        {
            if (groundParent != null)
            {
                for (int i = groundParent.childCount - 1; i >= 0; i--)
                {
                    Destroy(groundParent.GetChild(i).gameObject);
                }
            }
            return;
        }
        if (groundTiles == null || groundTiles.Length == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        if (groundParent == null)
        {
            groundParent = new GameObject("GroundTiles").transform;
            groundParent.SetParent(transform);
        }
        else
        {
            for (int i = groundParent.childCount - 1; i >= 0; i--)
            {
                Destroy(groundParent.GetChild(i).gameObject);
            }
        }

        float aspect = GameCameraFit.Aspect;
        float cameraLeft = cam.transform.position.x - (cam.orthographicSize * aspect);
        float cameraRight = cam.transform.position.x + (cam.orthographicSize * aspect);
        float cameraBottom = cam.transform.position.y - cam.orthographicSize;
        float cameraTop = cam.transform.position.y + cam.orthographicSize;
        float laneMinX = boardStartPosition.x - cellSize * 0.5f;
        float laneMaxX = boardStartPosition.x + (boardColumns - 1) * cellSize + cellSize * 0.5f + cellSize * 2f;
        float laneMaxY = boardStartPosition.y + cellSize * 0.5f;
        float laneMinY = boardStartPosition.y - (boardRows - 1) * cellSize - cellSize * 0.5f;

        float tileSize = cellSize;

        float laneBandMinX = cameraLeft;
        float laneBandMaxX = cameraRight;

        int cols = Mathf.CeilToInt((cameraRight - cameraLeft) / tileSize) + 1;
        int rows = Mathf.CeilToInt((cameraTop - cameraBottom) / tileSize) + 1;
        int sortingOrder = -20;

        for (int iy = 0; iy < rows; iy++)
        {
            float y = cameraBottom + iy * tileSize;
            for (int ix = 0; ix < cols; ix++)
            {
                float x = cameraLeft + ix * tileSize;
                bool isLane = x >= laneBandMinX && x <= laneBandMaxX && y >= laneMinY && y <= laneMaxY;
                Sprite tileSprite = isLane ? groundTiles[2] : GetDeterministicGroundTile(ix, iy);
                if (tileSprite == null) continue;

                CreateTile(
                    $"GroundTile_{ix}_{iy}",
                    tileSprite,
                    new Vector3(x + tileSize * 0.5f, y + tileSize * 0.5f, 0f),
                    tileSize,
                    sortingOrder
                );
            }
        }
    }

    public bool TryGetLaneBounds(out float minY, out float maxY)
    {
        if (boardRows <= 0 || cellSize <= 0f)
        {
            minY = 0f;
            maxY = 0f;
            return false;
        }

        maxY = boardStartPosition.y + cellSize * 0.5f;
        minY = boardStartPosition.y - (boardRows - 1) * cellSize - cellSize * 0.5f;
        return true;
    }

    /// <summary>
    /// 보드 왼쪽부터 전투 레인 오른쪽(스폰·카메라)까지의 전역 사거리 미리보기 영역.
    /// </summary>
    public bool TryGetCombatCorridorBounds(out float centerX, out float centerY, out float width, out float height)
    {
        centerX = 0f;
        centerY = 0f;
        width = 0f;
        height = 0f;

        if (!TryGetLaneBounds(out float minY, out float maxY))
        {
            return false;
        }

        float boardLeftX = boardStartPosition.x - cellSize * 0.5f;
        float boardRightX = boardStartPosition.x + (boardColumns - 1) * cellSize + cellSize * 0.5f;
        float rightX = boardRightX + 0.15f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            float aspect = GameCameraFit.Aspect;
            float halfW = cam.orthographicSize * aspect;
            float cameraRight = cam.transform.position.x + halfW;
            var spawner = FindFirstObjectByType<ZombieSpawner>();
            float spawnXPlus = (spawner != null ? spawner.spawnX : 10f) + 0.5f;
            float rightExtentRaw = Mathf.Max(cameraRight, spawnXPlus);
            float rightInsideCamera = cameraRight - Mathf.Max(0f, fieldWallCameraRightEdgeInset);
            rightX = Mathf.Max(boardRightX + 0.15f, Mathf.Min(rightExtentRaw, rightInsideCamera));
        }

        width = rightX - boardLeftX;
        height = maxY - minY;
        if (width < 0.1f || height < 0.1f)
        {
            return false;
        }

        centerX = (boardLeftX + rightX) * 0.5f;
        centerY = (minY + maxY) * 0.5f;
        return true;
    }

    /// <summary>배치칸 그리드 좌상단(0,0) 칸 중심 월드 좌표.</summary>
    public bool TryGetCellGridOrigin(out Vector3 origin)
    {
        if (boardRows <= 0 || cellSize <= 0f)
        {
            origin = Vector3.zero;
            return false;
        }

        origin = boardStartPosition;
        return true;
    }
    
    /// <summary>
    /// 적이 나오는 레인(입장 쪽)에 상·하·카메라 오른쪽면만의 벽. (시각은 붉은 반투명, 추후 투명 처리 가능)
    /// 전투 레인 경계 벽(FieldBoundary)이 없으면 <see cref="EnsureFieldWalls"/>로 생성을 시도합니다.
    /// </summary>
    public void EnsureFieldWalls()
    {
        if (!createFieldWalls) return;

        Transform existing = transform.Find("FieldWalls");
        if (existing != null && existing.childCount > 0) return;

        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        CreateFieldWalls();
    }

    void CreateFieldWalls()
    {
        if (!createFieldWalls) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        if (!TryGetLaneBounds(out float minY, out float maxY)) return;

        float t = Mathf.Max(0.05f, fieldWallThickness);
        float aspect = GameCameraFit.Aspect;
        float halfW = cam.orthographicSize * aspect;
        float cameraRight = cam.transform.position.x + halfW;

        float boardRightX = boardStartPosition.x + (boardColumns - 1) * cellSize + cellSize * 0.5f;
        var spawner = FindFirstObjectByType<ZombieSpawner>();
        float spawnXPlus = (spawner != null ? spawner.spawnX : 10f) + 0.5f;
        float rightExtentRaw = Mathf.Max(cameraRight, spawnXPlus);
        float rightInsideCamera = cameraRight - Mathf.Max(0f, fieldWallCameraRightEdgeInset);
        // 스폰·프레이터보다는 넓을 수 있지만, 카메라에 걸쳐 잘 보이게 오른쪽 끝을 '안쪽'으로 제한
        float rightX = Mathf.Max(boardRightX + 0.15f, Mathf.Min(rightExtentRaw, rightInsideCamera));
        if (rightX - boardRightX < 0.1f) return;

        float corridorCenterX = (boardRightX + rightX) * 0.5f;
        float corridorWidth = rightX - boardRightX;

        Transform parent = new GameObject("FieldWalls").transform;
        parent.SetParent(transform, false);

        int order = 8;
        CreateFieldWallStrip(parent, "FieldWall_LaneTop", new Vector2(corridorCenterX, maxY + t * 0.5f), new Vector2(corridorWidth, t), order);
        CreateFieldWallStrip(parent, "FieldWall_LaneBottom", new Vector2(corridorCenterX, minY - t * 0.5f), new Vector2(corridorWidth, t), order);
        float vHeight = (maxY - minY) + 2f * t;
        CreateFieldWallStrip(parent, "FieldWall_CameraRight", new Vector2(rightX - t * 0.5f, (minY + maxY) * 0.5f), new Vector2(t, vHeight), order);
    }
    
    void CreateFieldWallStrip(Transform parent, string objectName, Vector2 centerWorld, Vector2 worldSize, int sortingOrder)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerWorld.x, centerWorld.y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite(Color.white);
        sr.color = fieldWallColor;
        sr.sortingOrder = sortingOrder;
        go.transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
        int wallLayer = LayerMask.NameToLayer("FieldBoundary");
        if (wallLayer >= 0) go.layer = wallLayer;
        go.AddComponent<FieldBoundaryWall>();
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = false;
    }
    
    void CreateBarrier()
    {
        // 보드 오른쪽에 방어막 배치
        float rightEdge = boardStartPosition.x + (boardColumns - 1) * cellSize + cellSize * 0.5f;
        Vector3 barrierPos = new Vector3(rightEdge + cellSize * 0.4f, boardStartPosition.y - (boardHeight * 0.5f) + cellSize * 0.5f, 0f);
        
        GameObject barrierObj = new GameObject("Barrier");
        barrierObj.transform.SetParent(transform);
        barrierObj.transform.position = barrierPos;
        barrierObj.transform.localScale = Vector3.one;
        
        SpriteRenderer renderer = barrierObj.AddComponent<SpriteRenderer>();
        Sprite barrierSprite = Resources.Load<Sprite>("game_line");
        if (barrierSprite != null)
        {
            renderer.sprite = barrierSprite;
            renderer.color = Color.white;
        }
        else
        {
            renderer.sprite = CreateSquareSprite(new Color(0.9f, 0.8f, 0.2f));
        }
        renderer.sortingOrder = 0;
        
        // 스케일을 Barrier.Awake보다 먼저 적용해 콜라이더·스프라이트 정합
        barrierObj.transform.localScale = Vector3.one * 0.4f;
        barrierObj.AddComponent<Barrier>();
    }
    
    /// <summary>
    /// 대기칸 (8개)를 생성합니다
    /// </summary>
    void CreateBench()
    {
        benchParent = new GameObject("Bench").transform;
        benchParent.SetParent(transform);
        
        // 카메라 왼쪽 끝과 상단 좌표 계산
        Camera cam = Camera.main;
        float cameraLeft = cam.transform.position.x - (cam.orthographicSize * GameCameraFit.Aspect);
        float cameraTop = cam.transform.position.y + cam.orthographicSize;
        float uiOffset = GetLeftUiWorldOffset(cam);
        
        // 대기칸 위치 (화면 왼쪽 상단, 2줄 배치)
        float topOffset = cellSize * 1.5f; // 상단 여백 (기존 대비 반칸 아래로 이동)
        float leftOffset = cellSize * 0.5f;
        Vector3 benchStart = new Vector3(cameraLeft + leftOffset + uiOffset, cameraTop - topOffset, 0f);
        
        int colsPerRow = 4;
        for (int i = 0; i < benchSlots; i++)
        {
            int col = i % colsPerRow;
            int row = i / colsPerRow;
            Vector3 position = benchStart + new Vector3(col * cellSize, -row * cellSize, 0f);
            BoardCell cell = CreateCell(position, $"BenchSlot_{i}", false);
            if (cell != null) cell.isBoardCell = false;
        }
    }

    void CreateSubslotPopup()
    {
        Sprite popupSprite = Resources.Load<Sprite>("subslot_popup");
        if (popupSprite == null)
        {
            Debug.LogWarning("subslot_popup.png 스프라이트를 찾지 못했습니다. Assets/Resources/subslot_popup.png 확인 필요");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        float aspect = GameCameraFit.Aspect;
        float cameraLeft = cam.transform.position.x - (cam.orthographicSize * aspect);
        float cameraBottom = cam.transform.position.y - cam.orthographicSize;
        float uiOffset = GetLeftUiWorldOffset(cam);

        Vector2 popupSize = popupSprite.bounds.size;

        GameObject popupObj = new GameObject("SubslotPopup");
        popupObj.transform.SetParent(transform);
        popupObj.transform.position = new Vector3(
            cameraLeft + uiOffset + popupSize.x * 0.5f - cellSize,
            cameraBottom + popupSize.y * 0.5f,
            0f
        );

        SpriteRenderer popupRenderer = popupObj.AddComponent<SpriteRenderer>();
        popupRenderer.sprite = popupSprite;
        popupRenderer.color = Color.white;
        popupRenderer.sortingOrder = -3;
    }
    
    /// <summary>
    /// 개별 칸을 생성합니다
    /// </summary>
    BoardCell CreateCell(Vector3 position, string name, bool isBoardCellType)
    {
        GameObject cellObj = new GameObject(name);
        cellObj.transform.SetParent(isBoardCellType ? boardParent : benchParent);
        cellObj.transform.position = position;
        cellObj.transform.localScale = Vector3.one;
        
        // BoardCell 컴포넌트 추가
        BoardCell boardCell = cellObj.AddComponent<BoardCell>();
        boardCell.isBoardCell = isBoardCellType;
        
        // 칸 배경 (사각형)
        GameObject background = new GameObject("Background");
        background.transform.SetParent(cellObj.transform, false);
        background.transform.localPosition = Vector3.zero;
        background.transform.localScale = Vector3.one;
        
        SpriteRenderer bgRenderer = background.AddComponent<SpriteRenderer>();
        bgRenderer.enabled = false; // 슬롯 이미지 숨김
        
        // 칸 외곽선 제거 (요청에 따라 숨김)
        
        return boardCell;
    }
    
    /// <summary>
    /// 칸의 외곽선을 생성합니다
    /// </summary>
    void CreateCellOutline(Transform parent, bool isBoardCell)
    {
        float halfSize = cellSize * 0.5f;
        float lineWidth = cellSize * 0.02f;
        
        // 상단 선
        CreateLine(parent, new Vector2(-halfSize, halfSize), new Vector2(halfSize, halfSize), "TopLine", lineWidth);
        // 우측 선
        CreateLine(parent, new Vector2(halfSize, halfSize), new Vector2(halfSize, -halfSize), "RightLine", lineWidth);
        // 하단 선
        CreateLine(parent, new Vector2(halfSize, -halfSize), new Vector2(-halfSize, -halfSize), "BottomLine", lineWidth);
        // 좌측 선
        CreateLine(parent, new Vector2(-halfSize, -halfSize), new Vector2(-halfSize, halfSize), "LeftLine", lineWidth);
    }
    
    /// <summary>
    /// 선을 생성합니다 (외곽선용)
    /// </summary>
    void CreateLine(Transform parent, Vector2 start, Vector2 end, string name, float lineWidth)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(parent, false);
        
        // 선의 중점과 길이 계산
        Vector2 center = (start + end) * 0.5f;
        float length = Vector2.Distance(start, end);
        float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
        
        lineObj.transform.localPosition = center;
        lineObj.transform.localRotation = Quaternion.Euler(0, 0, angle);
        lineObj.transform.localScale = new Vector3(length, lineWidth, 1f);
        
        SpriteRenderer lineRenderer = lineObj.AddComponent<SpriteRenderer>();
        lineRenderer.sprite = CreateSquareSprite(cellOutlineColor);
        lineRenderer.sortingOrder = -1;
    }

    void LoadTileSprites()
    {
        if (!useTileGround) return;

        groundTiles = new Sprite[tileCount];
        for (int i = 0; i < tileCount; i++)
        {
            string name = $"{tilePrefix}{i + 1}";
            groundTiles[i] = Resources.Load<Sprite>(name);
            if (groundTiles[i] == null)
            {
                Debug.LogWarning($"타일 스프라이트를 찾지 못했습니다: {name}");
            }
        }
    }

    Sprite GetDeterministicGroundTile(int xIndex, int yIndex)
    {
        // 001~003 다수, 004~005 소량 (고정 패턴)
        int hash = xIndex * 73856093 ^ yIndex * 19349663;
        hash = Mathf.Abs(hash);
        int roll = hash % 100;
        if (roll < 40) return groundTiles[0];
        if (roll < 75) return groundTiles[1];
        if (roll < 95) return groundTiles[2];
        if (roll < 98) return groundTiles[3];
        return groundTiles[4];
    }

    float GetSpriteScale(Sprite sprite)
    {
        if (sprite == null) return 1f;
        float size = sprite.bounds.size.x;
        if (size <= 0.0001f) return 1f;
        return 1f / size;
    }

    float GetLeftUiWorldOffset(Camera cam)
    {
        if (cam == null) return 0f;
        float worldWidth = cam.orthographicSize * (Screen.width / (float)Screen.height) * 2f;
        float unitsPerPixel = worldWidth / Screen.width;
        return (leftUiWidthPixels + leftUiMarginPixels) * unitsPerPixel;
    }

    void CreateTile(string name, Sprite sprite, Vector3 position, float size, int sortingOrder)
    {
        GameObject tileObj = new GameObject(name);
        tileObj.transform.SetParent(groundParent, false);
        tileObj.transform.position = position;
        tileObj.transform.localScale = Vector3.one;

        SpriteRenderer renderer = tileObj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        float scale = GetSpriteScale(sprite);
        tileObj.transform.localScale = new Vector3(scale, scale, 1f) * size;
    }

    void CreateStretchedTile(string name, Sprite sprite, Vector3 position, float width, float height, int sortingOrder)
    {
        GameObject tileObj = new GameObject(name);
        tileObj.transform.SetParent(groundParent, false);
        tileObj.transform.position = position;

        SpriteRenderer renderer = tileObj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        Vector2 spriteSize = sprite.bounds.size;
        float scaleX = spriteSize.x > 0.0001f ? width / spriteSize.x : 1f;
        float scaleY = spriteSize.y > 0.0001f ? height / spriteSize.y : 1f;
        tileObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
    
    /// <summary>
    /// 사각형 스프라이트를 생성합니다
    /// </summary>
    Sprite CreateSquareSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
    
    /// <summary>
    /// 벤치에 캐릭터를 추가합니다 (왼쪽부터 비어있는 첫 번째 칸에 배치)
    /// </summary>
    public void AddCharacterToBench(Color characterColor)
    {
        if (benchParent == null || benchParent.childCount == 0) return;
        
        // 왼쪽부터 비어있는 첫 번째 칸 찾기
        for (int i = 0; i < benchParent.childCount; i++)
        {
            Transform benchSlot = benchParent.GetChild(i);
            BoardCell benchCell = benchSlot.GetComponent<BoardCell>();
            
            if (benchCell != null && benchCell.CanPlaceCharacter())
            {
                // 캐릭터 생성
                GameObject characterObj = new GameObject($"Character_{characterColor}");
                characterObj.transform.position = benchSlot.position;
                characterObj.transform.localScale = Vector3.one * cellSize;
                
                // 스프라이트 렌더러 추가
                SpriteRenderer spriteRenderer = characterObj.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = CreateSquareSprite(characterColor);
                spriteRenderer.sortingOrder = 1;
                
                // Character 컴포넌트 추가
                Character character = characterObj.AddComponent<Character>();
                character.characterColor = characterColor;
                
                // 칸에 배치
                benchCell.PlaceCharacter(character);
                return; // 배치 완료
            }
        }
        
        Debug.LogWarning("벤치에 빈 칸이 없습니다!");
    }

    /// <summary>
    /// 벤치에 유닛을 추가합니다 (유닛 번호 포함)
    /// </summary>
    public Character AddUnitToBench(UnitData unitData, Color unitColor)
    {
        if (unitData == null) return null;
        if (benchParent == null || benchParent.childCount == 0) return null;
        
        // 왼쪽부터 비어있는 첫 번째 칸 찾기
        for (int i = 0; i < benchParent.childCount; i++)
        {
            Transform benchSlot = benchParent.GetChild(i);
            BoardCell benchCell = benchSlot.GetComponent<BoardCell>();
            
            if (benchCell != null && benchCell.CanPlaceCharacter())
            {
                // 캐릭터 생성
                GameObject characterObj = new GameObject($"Unit_{unitData.unitNumber}");
                characterObj.transform.position = benchSlot.position;
                characterObj.transform.localScale = Vector3.one * cellSize;
                
                // 스프라이트 렌더러 추가
                SpriteRenderer spriteRenderer = characterObj.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = CreateSquareSprite(unitColor);
                spriteRenderer.sortingOrder = 1;
                
                // Character 컴포넌트 추가
                Character character = characterObj.AddComponent<Character>();
                character.characterColor = unitColor;
                character.unitNumber = unitData.unitNumber;
                RunEncounterTracker.RegisterEncounter(unitData.unitNumber);
                
                // 칸에 배치
                benchCell.PlaceCharacter(character);
                return character;
            }
        }
        
        Debug.LogWarning("벤치에 빈 칸이 없습니다!");
        return null;
    }

    /// <summary>
    /// 레벨업 보상 등 — 오른쪽 열부터 지그재그 우선순위로 빈 보드 칸을 찾습니다.
    /// (가운데 행 → 왼쪽 열 위/아래 → 현재 열 맨 위/맨 아래 순, 남은 칸은 동일 행 우선으로 채움)
    /// </summary>
    public BoardCell FindAutoPlacementCell()
    {
        EnsureBoardParent();
        if (boardParent == null) return null;

        List<(int row, int col)> order = BuildAutoPlacementZigzagOrder();
        for (int i = 0; i < order.Count; i++)
        {
            (int row, int col) = order[i];
            BoardCell cell = GetBoardCell(row, col);
            if (cell != null && cell.CanPlaceCharacter())
            {
                return cell;
            }
        }

        return null;
    }

    List<(int row, int col)> BuildAutoPlacementZigzagOrder()
    {
        int cols = Mathf.Max(1, boardColumns);
        int rows = Mathf.Max(1, boardRows);
        int center = rows / 2;
        int up = center - 1;
        int down = center + 1;
        int top = 0;
        int bottom = rows - 1;

        var order = new List<(int row, int col)>(cols * rows);
        var used = new HashSet<long>();

        void TryAdd(int col, int row)
        {
            if (col < 0 || col >= cols || row < 0 || row >= rows) return;
            long key = ((long)col << 32) | (uint)row;
            if (!used.Add(key)) return;
            order.Add((row, col));
        }

        for (int col = cols - 1; col >= 0; col--)
        {
            TryAdd(col, center);
            if (col > 0)
            {
                TryAdd(col - 1, up);
                TryAdd(col - 1, down);
            }
            TryAdd(col, top);
            TryAdd(col, bottom);
        }

        int[] rowSweep = { center, up, down, top, bottom };
        for (int col = cols - 1; col >= 0; col--)
        {
            for (int i = 0; i < rowSweep.Length; i++)
            {
                TryAdd(col, rowSweep[i]);
            }
        }

        return order;
    }

    public bool HasEmptyBoardCell() => FindAutoPlacementCell() != null;

    public bool CanAcceptNewBoardUnit()
    {
        if (!HasEmptyBoardCell()) return false;
        return CountBoardUnits() < GetMaxBoardUnits();
    }

    /// <summary>
    /// 유닛을 보드 우선순위 칸에 생성·배치합니다.
    /// </summary>
    public Character AddUnitToBoardAuto(UnitData unitData, Color unitColor)
    {
        if (unitData == null) return null;
        if (!CanAcceptNewBoardUnit()) return null;

        BoardCell cell = FindAutoPlacementCell();
        if (cell == null) return null;

        Character character = CreateUnitCharacter(unitData, unitColor, cell.transform.position);
        if (character == null) return null;

        if (!cell.PlaceCharacter(character))
        {
            Destroy(character.gameObject);
            return null;
        }

        character.SnapToAssignedCell();
        return character;
    }

    BoardCell GetBoardCell(int row, int col)
    {
        EnsureBoardParent();
        if (boardParent == null) return null;

        Transform slot = boardParent.Find($"BoardCell_{row}_{col}");
        return slot != null ? slot.GetComponent<BoardCell>() : null;
    }

    void EnsureBoardParent()
    {
        if (boardParent != null) return;
        boardParent = transform.Find("Board");
    }

    /// <summary>레벨과 무관하게 보드에 배치 가능한 최대 유닛 수.</summary>
    public const int MaxBoardUnitCap = 8;

    int GetMaxBoardUnits()
    {
        int level = GameManager.Instance != null ? GameManager.Instance.playerLevel : 1;
        return Mathf.Min(Mathf.Max(2 + (level - 1), 2), MaxBoardUnitCap);
    }

    int CountBoardUnits()
    {
        BoardCell[] cells = FindObjectsByType<BoardCell>(FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            BoardCell cell = cells[i];
            if (cell != null && cell.isBoardCell && cell.isOccupied)
            {
                count++;
            }
        }
        return count;
    }

    Character CreateUnitCharacter(UnitData unitData, Color unitColor, Vector3 position)
    {
        GameObject characterObj = new GameObject($"Unit_{unitData.unitNumber}");
        characterObj.transform.position = position;
        characterObj.transform.localScale = Vector3.one * cellSize;

        SpriteRenderer spriteRenderer = characterObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateSquareSprite(unitColor);
        spriteRenderer.sortingOrder = 1;

        Character character = characterObj.AddComponent<Character>();
        character.characterColor = unitColor;
        character.unitNumber = unitData.unitNumber;
        RunEncounterTracker.RegisterEncounter(unitData.unitNumber);
        return character;
    }

    /// <summary>
    /// 캐릭터를 벤치로 이동합니다 (왼쪽부터 비어있는 첫 번째 칸)
    /// </summary>
    public bool TryMoveCharacterToBench(Character character)
    {
        if (character == null) return false;
        if (benchParent == null || benchParent.childCount == 0) return false;

        for (int i = 0; i < benchParent.childCount; i++)
        {
            Transform benchSlot = benchParent.GetChild(i);
            BoardCell benchCell = benchSlot.GetComponent<BoardCell>();

            if (benchCell != null && benchCell.CanPlaceCharacter())
            {
                benchCell.PlaceCharacter(character);
                character.SetCell(benchCell);
                character.transform.position = benchCell.transform.position;
                return true;
            }
        }

        Debug.LogWarning("벤치에 빈 칸이 없습니다!");
        return false;
    }
}

