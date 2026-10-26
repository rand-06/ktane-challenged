using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Challenged;
using UnityEngine;

using Rnd = UnityEngine.Random;

public abstract class ChallengedBase : MonoBehaviour
{
    public ChallengedScaffold ScaffoldPrefab;
    public KMColorblindMode ColorblindMode;

    public abstract string Name { get; }

    protected KMSelectable[] Buttons { get { return _scaffold.Buttons; } }

    private ChallengedScaffold _scaffold;
    private KMBombModule _module;
    private MeshRenderer[] _buttonRenderers;
    protected SquareColor[] _colors;
    private static readonly Dictionary<string, int> _moduleIdCounters = new Dictionary<string, int>();
    private int _moduleId;
    protected bool _isSolved;
    private bool _colorblind;

    private Coroutine _activeCoroutine;
    protected bool IsCoroutineActive { get { return _activeCoroutine != null; } }

    private static T[] newArray<T>(params T[] array) { return array; }
    private static readonly Color[] _lightColors = newArray<Color>(
        Color.black, Color.white, Color.red, new Color32(0x83, 0x83, 0xff, 0xff), Color.green, Color.yellow);

    private void Awake()
    {
        if (!_moduleIdCounters.ContainsKey(Name))
            _moduleIdCounters[Name] = 1;

        _moduleId = _moduleIdCounters[Name]++;
        _module = GetComponent<KMBombModule>();

        _scaffold = Instantiate(ScaffoldPrefab, transform);
        _buttonRenderers = _scaffold.Buttons.Select(b => b.GetComponent<MeshRenderer>()).ToArray();
        _colorblind = ColorblindMode.ColorblindModeActive;

        var moduleSelectable = GetComponent<KMSelectable>();
        foreach (var btn in _scaffold.Buttons)
            btn.Parent = moduleSelectable;
        moduleSelectable.Children = _scaffold.Buttons;
        moduleSelectable.UpdateChildren();
        for (int i = 0; i < _scaffold.Buttons.Length; i++)
            _scaffold.Buttons[i].OnInteract = MakeButtonHandler(i);
        SetAllButtonsBlack();
    }

    private void Start()
    {
        _colors =  new SquareColor[_buttonRenderers.Length];
        for (int i = 0; i < _scaffold.Lights.Length; i++)
            _scaffold.Lights[i].range = .1f * _module.transform.lossyScale.x;
        DoStart();
    }

    protected abstract void DoStart();

    private KMSelectable.OnInteractHandler MakeButtonHandler(int index)
    {
        return delegate
        {
            _scaffold.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _scaffold.Buttons[index].transform);
            _scaffold.Buttons[index].AddInteractionPunch();
            if (!_isSolved)
                ButtonPressed(index);
            return false;
        };
    }

    protected void Log(string format, params object[] args)
    {
        Debug.LogFormat(@"[{0} #{1}] {2}", Name, _moduleId, string.Format(format, args));
    }

    protected abstract void ButtonPressed(int index);

    protected void ModulePassed()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
        SetAllButtonsBlack();
        Log("Module solved.");
        _module.HandlePass();
        _isSolved = true;
    }

    protected void Strike()
    {
        _scaffold.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
        SetAllButtonsBlack();
    }

    protected void PlaySound(int index)
    {
        switch (_colors[index])
        {
            case SquareColor.Red:
            case SquareColor.Black:
            case SquareColor.White:
                _scaffold.Audio.PlaySoundAtTransform("redlight", _scaffold.Buttons[index].transform);
                break;
            case SquareColor.Blue:
                _scaffold.Audio.PlaySoundAtTransform("bluelight", _scaffold.Buttons[index].transform);
                break;
            case SquareColor.Green:
                _scaffold.Audio.PlaySoundAtTransform("greenlight", _scaffold.Buttons[index].transform);
                break;
            case SquareColor.Yellow:
                _scaffold.Audio.PlaySoundAtTransform("yellowlight", _scaffold.Buttons[index].transform);
                break;
        }
    }

    public void SetButtonBlack(int index)
    {
        _buttonRenderers[index].sharedMaterial = _scaffold.Materials[(int) SquareColor.Black];
        _scaffold.Lights[index].gameObject.SetActive(false);
    }

    public void SetAllButtonsBlack()
    {
        for (int i = 0; i < _buttonRenderers.Length; i++)
            SetButtonBlack(i);
    }

    public void SetButtonColor(int ix, SquareColor color)
    {
        if (color == SquareColor.Black)
            SetButtonBlack(ix);
        else
        {
            _buttonRenderers[ix].sharedMaterial = _colorblind ? _scaffold.MaterialsCB[(int) color] ?? _scaffold.Materials[(int) color] : _scaffold.Materials[(int) color];
            _scaffold.Lights[ix].color = _lightColors[(int) color];
            _scaffold.Lights[ix].gameObject.SetActive(true);
        }
    }

    public void StartSquareColorsCoroutine(SquareColor[] colors, SquaresToRecolor behaviour = SquaresToRecolor.All, bool delay = false, bool unshuffled = false)
    {
        var indexes = new List<int?>((behaviour == SquaresToRecolor.NonwhiteOnly
            ? Enumerable.Range(0, _buttonRenderers.Length).Where(ix => colors[ix] != SquareColor.White)
            : behaviour == SquaresToRecolor.NonblackOnly
            ? Enumerable.Range(0, _buttonRenderers.Length).Where(ix => colors[ix] != SquareColor.Black)
            : Enumerable.Range(0, _buttonRenderers.Length)).Select(i => (int?) i));
        if (!unshuffled)
            indexes.Shuffle();
        if (delay)
            indexes.Insert(0, null);
        StartSquareColorsCoroutine(colors, indexes.ToArray());
    }

    /// <summary>
    /// Starts a coroutine that re-colors some of the squares.
    /// </summary>
    /// <param name="colors">The colors to set the relevant squares to.</param>
    /// <param name="indexes">Specifies which squares to recolor and in which order. Insert a <c>null</c> value to add a delay.</param>
    public void StartSquareColorsCoroutine(SquareColor[] colors, int?[] indexes)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(SetSquareColorsCoroutine(colors, indexes));
    }

    private IEnumerator SetSquareColorsCoroutine(SquareColor[] colors, int?[] indexes)
    {
        foreach (var i in indexes)
        {
            if (i == null)
                yield return new WaitForSeconds(Rnd.Range(1.5f, 2f));
            else
            {
                SetButtonColor(i.Value, colors[i.Value]);
                yield return new WaitForSeconds(.03f);
            }
        }
        _activeCoroutine = null;
    }
    
    int sqrt(int val)
    {
        if (val<=0) return 0;
        int ans = 1;
        for (; ans * ans <= val; ans++) ;
        return ans - 1;
    }

#pragma warning disable 414
    protected readonly string TwitchHelpMessage = @"!{0} A1 A2 A3 B3 [specify column as letter, then row as number] | !{0} colorblind";
#pragma warning restore 414

    protected IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.Trim().Equals("colorblind", StringComparison.InvariantCultureIgnoreCase))
        {
            _colorblind = !_colorblind;
            StartSquareColorsCoroutine(_colors);
            yield return null;
            yield break;
        }

        var buttons = new List<KMSelectable>();
        foreach (var piece in command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (piece.Length != 2 || piece[0] < 'a' || piece[0] > 'a'+sqrt(Buttons.Length)-1 || piece[1] < '1' || piece[1] > '1'+sqrt(Buttons.Length)-1)
                yield break;
            buttons.Add(_scaffold.Buttons[(piece[0] - 'a') + +sqrt(Buttons.Length) * (piece[1] - '1')]);
        }

        yield return null;
        yield return "solve";
        yield return "strike";
        yield return buttons;
    }
}