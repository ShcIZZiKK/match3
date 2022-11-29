using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match3Control : MonoBehaviour {

	private enum Mode {MatchOnly, FreeMove};

	[SerializeField] 
	private Mode mode; // два режима перемещения, 'MatchOnly' означает, что передвинуть узел можно если произошло совпадение, иначе произойдет возврат
	[SerializeField] 
	private float speed = 5.5f; // скорость движения объектов
	[SerializeField] 
	private LayerMask layerMask; // маска узла (префаба)
	[SerializeField] 
	private Color[] color; // набор цветов/id
	[SerializeField] 
	private int gridWidth = 7; // ширина игрового поля
	[SerializeField] 
	private int gridHeight = 10; // высота игрового поля
	[SerializeField] 
	private Match3Node sampleObject; // образец узла (префаб)
	[SerializeField]
	private GameObject sampleObjectDeath; // образец узла при удалении (префаб)
	[SerializeField] 
	private float sampleSize = 1; // размер узла (ширина и высота)
	[SerializeField]
	private GameObject bgTile; // фон для ячейки

	[SerializeField] 
	private Sprite[] tilesBody; // набор спрайтов тела/id
	[SerializeField]
	private Sprite[] tilesEyes; // набор спрайтов Глаз/id
	[SerializeField]
	private Sprite bodyBomb; // спрайт бомбы
	[SerializeField]
	private int chanceBomb = 5; // шанс бомбы
	private int idBomb; // id бомбы
	[SerializeField]
	private GameObject objectBomb; // префаб бомбы

	private Match3Node[,] grid; //Сетка из объектов
	private Match3Node[] nodeArray; // Массив объектов
	private Vector3[,] position; // Позиция текущего объекта ??
	private Match3Node current, last; // Текущий выбранный объект и предыдущий
	private Vector3 currentPos, lastPos; // Позиция текущего объекта и предыдущего
	private List<Match3Node> lines; // Линия ??
	private bool isLines, isMove, isMode; // Если объекты выстроились в ряд | если двигается | режим?

	private AudioManager audioManager;
	private GameManager gameManager;

	[SerializeField]
	private Transform parentTileFX; // Родитель для бомб и мертых фруктов

	private List<GameObject> bombList = new List<GameObject>(); // список бомб
	private List<GameObject> deathTileList = new List<GameObject>(); // список мертвых фруктов

	private float timeToMultiplier = 3f;
	private float timeToMultiplierDisable;
	private float multiplier = 1;

	void Start()
	{
		audioManager = FindObjectOfType<AudioManager>();
		gameManager = FindObjectOfType<GameManager>();

		sampleSize = sampleObject.transform.localScale.y;
		idBomb = tilesBody.Length + 1;

		// создание игрового поля (2D массив) с заданными параметрами
		grid = Create2DGrid(sampleObject, gridWidth, gridHeight, sampleSize, bgTile, transform);
		// создаем бомбы и мертвые фрукты
		CreateFXGrid();

		// Заполняем поле контентом, пока не будет совпадений на линии 3+
		do
		{
			SetupField();
		}
		while (IsLine());
	}

	void Update()
	{
		DestroyLines();

		MoveNodes();

		if(Time.time > timeToMultiplierDisable)
		{
			gameManager.multiplierText.text = "1";
		}

		if (isLines || isMove) 
			return;

		if (last == null)
		{
			Control();
		}
		else
		{
			MoveCurrent();
		}
	}

	// уничтожаем совпадения с задержкой
	void DestroyLines()
	{
		if (!isLines) 
			return;

		for (int i = 0; i < lines.Count; i++)
		{
			if(lines[i].id == idBomb)
			{
				Boom(lines[i], lines[i + 2]);

				isMove = true;
				isLines = false;

				return;
			}
		}

		for (int i = 0; i < lines.Count; i++)
		{
			// Деактитивируем объекты в линии
			lines[i].gameObject.SetActive(false);
			// Очищаем в сетке позицию
			grid[lines[i].x, lines[i].y] = null;

			DeathTilePlay(lines[i].gameObject);
		}

		if(Time.time <= timeToMultiplierDisable)
		{
			multiplier += 1;
		}
		else
		{
			multiplier = 1;
		}

		timeToMultiplierDisable = Time.time + timeToMultiplier;

		gameManager.AddScore(5.0f, multiplier);


		isMove = true;
		isLines = false;
}

	void DeathTilePlay(GameObject tile)
	{
		Vector3 pos = tile.transform.position;

		for (int i = 0; i < deathTileList.Count; i++)
		{
			if (!deathTileList[i].gameObject.activeSelf)
			{
				deathTileList[i].transform.position = pos;
				deathTileList[i].gameObject.SetActive(true);

				Transform deathTileBody = deathTileList[i].GetComponent<Transform>().Find("Body");
				deathTileBody.GetComponent<SpriteRenderer>().sprite = tile.transform.Find("Body").GetComponent<SpriteRenderer>().sprite;
				float angel = deathTileList[i].GetComponent<Rigidbody2D>().rotation * Mathf.Deg2Rad;
				deathTileList[i].GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(-0.5f, 0.5f), 1) * 2f, ForceMode2D.Impulse);

				return;
			}
		}
	}

	// передвижение узлов и заполнение поля, после проверки совпадений
	void MoveNodes()
	{
		//Если сейчас происходят движения, выходим
		if (!isMove) 
			return;

		for (int y = 0; y < gridHeight; y++)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				if (grid[x, 0] == null)
				{
					bool check = true;

					for (int i = 0; i < gridWidth; i++)
					{
						//Если в самом верху больше не объектов, то записываем туда новый объект из освободившихся
						if (grid[i, 0] == null)
						{
							grid[i, 0] = GetFree(position[i, 0]);
							if(grid[i, 0].id == idBomb)
								grid[i, 0].GetComponent<Animator>().SetBool("IsBomb", true);
						}
					}

					for (int i = 0; i < nodeArray.Length; i++)
					{
						//Если какой-то объект не активнен
						if (!nodeArray[i].gameObject.activeSelf) 
							check = false;
					}

					if (check)
					{
						isMove = false;
						//Обновляем сетку
						GridUpdate();

						//Если собрана линия, обновляем счетчик времени и устаавлваем статус, что была собрана линию
						if (IsLine())
						{
							isLines = true;
							audioManager.PlaySoundClear();
						}
						else
						{
							isMode = false;
						}
					}
				}

				// Если в текущей позиции есть объект, а снизу нет, то двигаем его вниз
				if (grid[x, y] != null)
				{
					if (y + 1 < gridHeight && grid[x, y].gameObject.activeSelf && grid[x, y + 1] == null)
					{
						grid[x, y].transform.position = Vector3.MoveTowards(grid[x, y].transform.position, position[x, y + 1], speed * Time.deltaTime);

						if (grid[x, y].transform.position == position[x, y + 1])
						{
							grid[x, y + 1] = grid[x, y];
							grid[x, y] = null;
						}
					}
				}
			}
		}
	}

	// стартовые установки, подготовка игрового поля
	void SetupField() 
	{
		position = new Vector3[gridWidth, gridHeight];
		nodeArray = new Match3Node[gridWidth * gridHeight];

		int i = 0;
		int id = -1;
		int step = 0;

		for(int y = 0; y < gridHeight; y++)
		{
			for(int x = 0; x < gridWidth; x++)
			{
				if(chanceBomb >= Random.Range(1, 100))
				{
					grid[x, y].ready = false;
					grid[x, y].x = x;
					grid[x, y].y = y;
					grid[x, y].id = idBomb;
					grid[x, y].GetComponent<Animator>().SetBool("IsBomb", true);
					grid[x, y].transform.Find("Body").GetComponent<SpriteRenderer>().sprite = bodyBomb;
					grid[x, y].gameObject.SetActive(true);
					grid[x, y].highlight.SetActive(false);
					position[x, y] = grid[x, y].transform.position;
					nodeArray[i] = grid[x, y];
					i++;
				}
				else
				{
					int j = Random.Range(0, tilesBody.Length);
					if (id != j)
						id = j;
					else
						step++;

					if (step > 2)
					{
						step = 0;
						id = (id + 1 < tilesBody.Length - 1) ? id + 1 : id - 1;
					}

					grid[x, y].ready = false;
					grid[x, y].x = x;
					grid[x, y].y = y;
					grid[x, y].id = id;
					grid[x, y].GetComponent<Animator>().SetBool("IsBomb", false);
					grid[x, y].transform.Find("Body").GetComponent<SpriteRenderer>().sprite = tilesBody[id];
					grid[x, y].transform.Find("Body/Eyes").GetComponent<SpriteRenderer>().sprite = tilesEyes[id];
					grid[x, y].gameObject.SetActive(true);
					grid[x, y].highlight.SetActive(false);
					position[x, y] = grid[x, y].transform.position;
					nodeArray[i] = grid[x, y];
					i++;
				}
			}
		}

		current = null;
		last = null;
	}

	// возвращает неактивный узел
	Match3Node GetFree(Vector3 pos) 
	{
		for(int i = 0; i < nodeArray.Length; i++)
		{
			if(!nodeArray[i].gameObject.activeSelf)
			{
				if (chanceBomb >= Random.Range(1, 100))
				{
					nodeArray[i].id = idBomb;
					nodeArray[i].transform.Find("Body").GetComponent<SpriteRenderer>().sprite = bodyBomb;
					nodeArray[i].transform.Find("Body/Eyes").GetComponent<SpriteRenderer>().sprite = null;
					nodeArray[i].transform.Find("Body/Eyes").GetComponent<SpriteRenderer>().sprite = null;

				}
				else
				{
					int j = Random.Range(0, tilesBody.Length);
					nodeArray[i].id = j;
					nodeArray[i].transform.Find("Body").GetComponent<SpriteRenderer>().sprite = tilesBody[j];
					nodeArray[i].transform.Find("Body/Eyes").GetComponent<SpriteRenderer>().sprite = tilesEyes[j];
				}

				nodeArray[i].transform.position = pos;
				nodeArray[i].gameObject.SetActive(true);
				return nodeArray[i];
			}
		}

		return null;
	}

	// обновление игрового поля с помощью рейкаста
	void GridUpdate() 
	{
		for (int y = 0; y < gridHeight; y++)
		{
			for(int x = 0; x < gridWidth; x++)
			{
				RaycastHit2D hit = Physics2D.Raycast(position[x, y], Vector2.zero, Mathf.Infinity, layerMask);

				if(hit.transform != null)
				{
					grid[x, y] = hit.transform.GetComponent<Match3Node>();
					grid[x, y].ready = false;
					grid[x, y].x = x;
					grid[x, y].y = y;	
				}
			}
		}
	}

	// перемещение выделенного мышкой узла
	void MoveCurrent() 
	{
		current.transform.position = Vector3.MoveTowards(current.transform.position, lastPos, speed * Time.deltaTime);
		last.transform.position = Vector3.MoveTowards(last.transform.position, currentPos, speed * Time.deltaTime);

		if (current.transform.position == lastPos && last.transform.position == currentPos)
		{
			audioManager.PlaySoundSwipe();

			GridUpdate();

			if (mode == Mode.MatchOnly && isMode && !CheckNearNodes(current) && !CheckNearNodes(last))
			{
				if (current.id == idBomb || last.id == idBomb)
				{
					Boom(current, last);

					current = null;
					last = null;
				}
				else
				{
					currentPos = position[current.x, current.y];
					lastPos = position[last.x, last.y];
					isMode = false;
				}
				
				return;
			}
			else
			{
				isMode = false;
			}


			if (IsLine())
			{
				isLines = true;
				audioManager.PlaySoundClear();
			}

			current = null;
			last = null;
		}
	}

	//Производим взрыв при использовании бомбы
	void Boom(Match3Node currentNode, Match3Node lastNode)
	{
		int defaultMinX = 0;
		int defaultMinY = 0;
		int defaultMaxX = 0;
		int defaultMaxY = 0;

		if(currentNode.id == idBomb && lastNode.id == idBomb)
		{
			if(currentNode.x < lastNode.x)
			{
				defaultMinX = currentNode.x;
				defaultMaxX = lastNode.x;
			}
			else
			{
				defaultMinX = lastNode.x;
				defaultMaxX = currentNode.x;
			}

			if (currentNode.y < lastNode.y)
			{
				defaultMinY = currentNode.y;
				defaultMaxY = lastNode.y;
			}
			else
			{
				defaultMinY = lastNode.y;
				defaultMaxY = currentNode.y;
			}
		}
		else if(currentNode.id == idBomb)
		{
			defaultMinX = currentNode.x;
			defaultMinY = currentNode.y;
			defaultMaxX = currentNode.x;
			defaultMaxY = currentNode.y;
		}
		else {
			defaultMinX = lastNode.x;
			defaultMinY = lastNode.y;
			defaultMaxX = lastNode.x;
			defaultMaxY = lastNode.y;
		}

		int minX = (defaultMinX - 1) >= 0 ? (defaultMinX - 1) : 0;
		int maxX = (defaultMaxX + 1) <= gridWidth - 1 ? (defaultMaxX + 1) : gridWidth - 1;
		int minY = (defaultMinY - 1) >= 0 ? (defaultMinY - 1) : 0;
		int maxY = (defaultMaxY + 1) <= gridHeight - 1 ? (defaultMaxY + 1) : gridHeight - 1;

		GetFreeBomb(grid[(defaultMinX + defaultMaxX) / 2, (defaultMinY + defaultMaxY) / 2].transform.position);

		List<Match3Node> listDestroy  = new List<Match3Node>();

		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
			{
				if(grid[x, y].id != idBomb)
					listDestroy.Add(grid[x, y]);

				grid[x,y].gameObject.SetActive(false);
				grid[x, y] = null;
			}
		}

		if (Time.time <= timeToMultiplierDisable)
		{
			multiplier += 1;
		}
		else
		{
			multiplier = 1;
		}

		timeToMultiplierDisable = Time.time + timeToMultiplier;

		gameManager.AddScore(15.0f, multiplier);

		for (int i = 0; i < listDestroy.Count; i++)
		{
			DeathTilePlay(listDestroy[i].gameObject);
		}

		isMove = true;
		isLines = false;
	}

	// берем свободную бомбу
	void GetFreeBomb(Vector3 pos)
	{
		for (int i = 0; i < bombList.Count; i++)
		{
			if (!bombList[i].gameObject.activeSelf)
			{
				bombList[i].transform.position = pos;
				bombList[i].gameObject.SetActive(true);

				StartCoroutine(ReturnInListBomb(bombList[i].gameObject));

				return;
			}
		}
	}

	// деактивируем бомбу, после проигрыша анимации
	IEnumerator ReturnInListBomb(GameObject bomb)
	{
		yield return new WaitForSeconds(1f);

		bomb.SetActive(false);
	}

	// проверка, возможно-ли совпадение на текущем ходу
	bool CheckNearNodes(Match3Node node) 
	{
		if(node.x-2 >= 0) 
			if(grid[node.x-1, node.y].id == node.id && grid[node.x-2, node.y].id == node.id) 
				return true;

		if(node.y-2 >= 0) 
			if(grid[node.x, node.y-1].id == node.id && grid[node.x, node.y-2].id == node.id) 
				return true;

		if(node.x+2 < gridWidth) 
			if(grid[node.x+1, node.y].id == node.id && grid[node.x+2, node.y].id == node.id) 
				return true;

		if(node.y+2 < gridHeight) 
			if(grid[node.x, node.y+1].id == node.id && grid[node.x, node.y+2].id == node.id) 
				return true;

		if(node.x-1 >= 0 && node.x+1 < gridWidth) 
			if(grid[node.x-1, node.y].id == node.id && grid[node.x+1, node.y].id == node.id) 
				return true;

		if(node.y-1 >= 0 && node.y+1 < gridHeight) 
			if(grid[node.x, node.y-1].id == node.id && grid[node.x, node.y+1].id == node.id) 
				return true;

		return false;
	}

	// метка для узлов, которые находятся рядом с выбранным (чтобы нельзя было выбрать другие)
	void SetNode(Match3Node node, bool value) 
	{
		if(node == null) 
			return;

		if(node.x-1 >= 0) 
			grid[node.x-1, node.y].ready = value;

		if(node.y-1 >= 0) 
			grid[node.x, node.y-1].ready = value;

		if(node.x+1 < gridWidth) 
			grid[node.x+1, node.y].ready = value;

		if(node.y+1 < gridHeight) 
			grid[node.x, node.y+1].ready = value;
	}

	// управление ЛКМ
	void Control() 
	{
		if(Input.GetMouseButtonDown(0) && !isMode)
		{
			RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, Mathf.Infinity, layerMask);

			audioManager.PlaySoundClick();

			if (hit.transform != null && current == null)
			{
				current = hit.transform.GetComponent<Match3Node>();
				SetNode(current, true);
				current.highlight.SetActive(true);

				if(current.id != idBomb)
					hit.transform.GetComponent<Animator>().SetBool("Hover", true);
			}
			else if(hit.transform != null && current != null)
			{
				if (current.id != idBomb)
					current.GetComponent<Animator>().SetBool("Hover", false);

				last = hit.transform.GetComponent<Match3Node>();

				if(last != null && !last.ready)
				{
					current.highlight.SetActive(false);
					last.highlight.SetActive(true);
					SetNode(current, false);
					SetNode(last, true);
					current = last;
					last = null;
					return;
				}

				current.highlight.SetActive(false);
				currentPos = current.transform.position;
				lastPos = last.transform.position;
				isMode = true;
			}
		}
	}

	// поиск совпадений по горизонтали и вертикали
	bool IsLine() 
	{
		int j = -1;

		lines = new List<Match3Node>();

		for(int y = 0; y < gridHeight; y++)
		{
			for(int x = 0; x < gridWidth; x++)
			{
				if(x+2 < gridWidth && j < 0 && grid[x+1,y].id == grid[x,y].id && grid[x+2,y].id == grid[x,y].id)
				{
					j = grid[x,y].id;
				}

				if(j == grid[x,y].id)
				{
					lines.Add(grid[x,y]);
				}
				else
				{
					j = -1;
				}
			}

			j = -1;
		}

		j = -1;

		for(int y = 0; y < gridWidth; y++)
		{
			for(int x = 0; x < gridHeight; x++)
			{
				if(x+2 < gridHeight && j < 0 && grid[y,x+1].id == grid[y,x].id && grid[y,x+2].id == grid[y,x].id)
				{
					j = grid[y,x].id;
				}

				if(j == grid[y,x].id)
				{
					lines.Add(grid[y,x]);
				}
				else
				{
					j = -1;
				}
			}

			j = -1;
		}

		return (lines.Count > 0) ? true : false;
	}

	// функция создания 2D массива на основе шаблона
	private Match3Node[,] Create2DGrid(Match3Node sample, int width, int height, float size, GameObject bg, Transform parent)
	{
		Match3Node[,] field = new Match3Node[width, height];

		float posX = -size * width / 2f - size / 2f;
		float posY = size * height / 2f - size / 2f;

		float Xreset = posX;

		int z = 0;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				posX += size;
				field[x, y] = Instantiate(sample, parent) as Match3Node;
				field[x, y].transform.localPosition = new Vector3(posX, posY, 0);
				field[x, y].name = "Node-" + z;

				Instantiate(bg, new Vector3(posX, posY, 0), Quaternion.identity, parent);

				z++;
			}
			posY -= size;
			posX = Xreset;
		}

		return field;
	}

	private void CreateFXGrid()
	{
		int countFX = gridWidth * gridHeight;

		for(int i = 0; i < countFX; i++)
		{
			//Создаем бомбы и объекты фруктов при смерти
			GameObject bomb = Instantiate(objectBomb, new Vector3(0, 0, 0), Quaternion.identity, parentTileFX);
			GameObject deathTile = Instantiate(sampleObjectDeath, new Vector3(0, 0, 0), Quaternion.identity, parentTileFX);
			bomb.SetActive(false);
			deathTile.SetActive(false);

			bombList.Add(bomb);
			deathTileList.Add(deathTile);
		}
	} 
}