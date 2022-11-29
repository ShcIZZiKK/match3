using System.Collections;
using UnityEngine;

public class Match3Node : MonoBehaviour {

	public SpriteRenderer sprite; // спрайт узла
	public GameObject highlight; // объект подсветки узла

	public SpriteRenderer spriteBody; // Объект тела тела

	public int id { get; set; }
	public bool ready { get; set; }
	public int x { get; set; }
	public int y { get; set; }
}
