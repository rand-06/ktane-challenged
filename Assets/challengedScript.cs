using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class challengedScript : MonoBehaviour
{

	public List<KMSelectable> buttons;
	public List<MeshRenderer> buttonsMesh;
	public KMBombModule Module;
	public KMAudio Audio;
    public AudioClip stage;

	static int ModuleIdCounter;
	int ModuleId;

	private bool moduleSolved;

	private static readonly List<Color> colors = new List<Color>()
	{
		Color.red, Color.green, Color.blue, Color.yellow
	};

	private static readonly List<char> rules = new List<char>() { 'R', 'G', 'B', 'Y', 'O', 'C' };

	private static readonly List<List<int>> rows = new List<List<int>>()
	{
		new List<int> { 0, 1, 2 },
		new List<int> { 3, 4, 5 },
		new List<int> { 6, 7, 8 },
	};

	private static readonly List<List<int>> cols = new List<List<int>>()
	{
		new List<int> { 0, 3, 6 },
		new List<int> { 1, 4, 7 },
		new List<int> { 2, 5, 8 },
	};

	private char currentRule;
	private List<int> currentBoard;

	private bool TwitchShouldStop;

	void Awake()
	{
		ModuleId = ++ModuleIdCounter;
	}

	void log(string s)
	{
		Debug.LogFormat("[Challenged #{0}] " + s.Replace("\n", "\n[Challenged #{0}] "), ModuleId);
	}

	List<List<char>> generatedRules = new List<List<char>>();

	List<List<char>> generateRules()
	{
		List<List<char>> ans = new List<List<char>>();
		for (int i = 0; i < buttons.Count - 2; i++)
		{
			List<char> temp = rules.OrderBy(x => Rnd.value).ToList();
			ans.Add(temp);
		}

		ans.Add(Enumerable.Range(0, 6).Select(_ => 'O').ToList());
		log(ans.Select(l => l.Aggregate("", (a, b) => a + b)).Aggregate((a, b) => a + "\n" + b));
		return ans;
	}

	List<int> generateInitGrid()
	{
		int goalColor = Rnd.Range(0, 4);
		currentRule = rules[goalColor];
		List<int> ans2 = new List<int> { 0, 0, 1, 1, 2, 2, -1 };
		ans2.AddRange(Enumerable.Range(0, 2).Select(_ => Rnd.Range(0, 3)));
		ans2.Shuffle();
		List<int> colors2 = Enumerable.Range(0, 4).Where(x => x != goalColor).ToList().Shuffle();
		return ans2.Select(x => x == -1 ? goalColor : colors2[x]).ToList();
	}

	int availableRowCol(bool col)
	{
		for (int row = 0; row < rows.Count; row++)
			if ((col ? cols[row] : rows[row]).Any(index => currentBoard[index] != 4))
				return row;
		return 3;
	}

	void press(int index)
	{
		Audio.HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		buttons[index].AddInteractionPunch();
		if (moduleSolved) return;

		switch (currentRule)
		{
			case 'R':
				if (currentBoard[index] != 0)
				{
					strike();
					return;
				}

				currentBoard[index] = 4;
				buttonsMesh[index].material.color = Color.white;
				if (!currentBoard.Contains(0)) nextStage();
				break;
			case 'G':
				if (currentBoard[index] != 1)
				{
					strike();
					return;
				}

				currentBoard[index] = 4;
				buttonsMesh[index].material.color = Color.white;
				if (!currentBoard.Contains(1)) nextStage();
				break;
			case 'B':
				if (currentBoard[index] != 2)
				{
					strike();
					return;
				}

				currentBoard[index] = 4;
				buttonsMesh[index].material.color = Color.white;
				if (!currentBoard.Contains(2)) nextStage();
				break;
			case 'Y':
				if (currentBoard[index] != 3)
				{
					strike();
					return;
				}

				currentBoard[index] = 4;
				buttonsMesh[index].material.color = Color.white;
				if (!currentBoard.Contains(3)) nextStage();
				break;
			case 'O':
				if (!rows[availableRowCol(false)].Contains(index))
				{
					strike();
					return;
				}

				int oldAval = availableRowCol(false);
				currentBoard[index] = 4;
				buttonsMesh[index].material.color = Color.white;
				if (availableRowCol(false) != oldAval) nextStage();
				break;
			case 'C':
				if (!cols[availableRowCol(true)].Contains(index))
				{
					strike();
					return;
				}

				oldAval = availableRowCol(true);
				currentBoard[index] = 4;
				buttonsMesh[index].material.color = Color.white;
				if (availableRowCol(true) != oldAval) nextStage();
				break;
		}

	}

	void nextStage()
	{
		Audio.PlaySoundAtTransform(stage.name, transform);
		int whites = currentBoard.Count(x => x == 4);
		if (whites == 9)
		{
			solve();
			return;
		}

		currentRule = generatedRules[whites - 1][rules.IndexOf(currentRule)];
		if (rules.IndexOf(currentRule) < 4)
		{
			int intRule = rules.IndexOf(currentRule);
			currentBoard = currentBoard.Select(x => x == 4 ? 4 : -1).ToList();
			List<int> availableIndices =
				Enumerable.Range(0, currentBoard.Count).Where(x => currentBoard[x] != 4).ToList();
			currentBoard[availableIndices.PickRandom()] = intRule;
			currentBoard = currentBoard.Select(x => x == -1 ? Rnd.Range(0, 4) : x).ToList();
			colorSquares(currentBoard);
		}
		else
		{
			currentBoard = currentBoard.Select(x => x == 4 ? 4 : Rnd.Range(0, 4)).ToList();
			colorSquares(currentBoard);
		}
	}

	void strike()
	{
		TwitchShouldStop = true;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
		currentBoard = generateInitGrid();
		colorSquares(currentBoard);
	}

	void colorSquares(List<int> colorInts)
	{
		for (int i = 0; i < buttons.Count; i++)
		{
			switch (colorInts[i])
			{
				case -1: buttonsMesh[i].material.color = Color.gray; break;
				case 4: buttonsMesh[i].material.color = Color.white; break;
				default: buttonsMesh[i].material.color = colors[colorInts[i]]; break;
			}
		}
	}

	void solve()
	{
		Module.HandlePass();
		moduleSolved = true;
	}

	void Start()
	{
		for (int i = 0; i < buttons.Count; i++)
		{
			int ii = i;
			buttons[ii].OnInteract += delegate
			{
				press(ii);
				return false;
			};
		}

		generatedRules = generateRules();
		currentBoard = generateInitGrid();
		colorSquares(currentBoard);
	}

	private string TwitchHelpMessage = @"Use !{0} press 1 2 3 4 to press buttons 1-9 in reading order.";

	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		command = command.ToLowerInvariant();
		if (Regex.IsMatch(command, @"^\s*press (\d\s)*\d$"))
		{
			List<string> parameters = command.Split(' ').Skip(1).ToList();
			if (parameters.Any(x => x.Length > 1 || !(x[0] >= '1' && x[0] <= '9')))
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}
			List<int> buttonsToPress = parameters.Select(int.Parse).ToList();
			foreach (int b in buttonsToPress)
			{
				buttons[b-1].OnInteract();
				yield return "trycancel";
				yield return new WaitForSeconds(0.1f);
				if (TwitchShouldStop) yield break;
				TwitchShouldStop = false;
			}
		}
		else yield return "sendtochaterror Invalid command.";
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
		solve();
	}

}
