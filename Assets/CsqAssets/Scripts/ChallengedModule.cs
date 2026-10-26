using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Challenged;
using UnityEngine;
using Rnd = UnityEngine.Random;


public class ChallengedModule : ChallengedBase
{
    public override string Name { get { return "Challenged"; } }

    // Contains the (seeded) rules
    private  List<List<object>> _table;

    private HashSet<int> _allowedPresses;
    private HashSet<int> _expectedPresses;
    private object _lastStage;
    private static readonly SquareColor[] _colorsInUse = new[] { SquareColor.Red, SquareColor.Blue, SquareColor.Green, SquareColor.Yellow };
    private static List<object> rules;

    int sqrt(int val)
    {
        if (val<=0) return 0;
        int ans = 1;
        for (; ans * ans <= val; ans++) ;
        return ans - 1;
    }
    
    List<List<object>> generateRules()
    {
        rules = _colorsInUse.Select(x => (object)x).ToList();
        rules.AddRange(new List<object> { true, false });
        List<List<object>> ans = new List<List<object>>();
        for (int i = 0; i < ScaffoldPrefab.Buttons.Length - 2; i++)
        {
            List<object> temp = rules.OrderBy(x => Rnd.value).ToList();
            ans.Add(temp);
        }

        ans.Add(Enumerable.Range(0, 6).Select(_ => (object)true).ToList());
        Log("#. R B G Y O C:");
        for (int i=0; i<ans.Count; i++)
        {
            Log(ans[i]
                    .Select(x =>
                        _colorsInUse.Select(y=>(object)y).Contains(x)?
                            x.ToString()[0].ToString():
                            (bool)x?"O":"C"
                    )
                    .Aggregate(i + 1 + ".", (a, b) => a + " " + b));
        }
        
        return ans;
    }

    protected override void DoStart()
    {
        _table = generateRules();
        SetInitialState();
    }

    private void SetInitialState()
    {
        tryAgain:
        var counts = new Dictionary<SquareColor, int>();
        foreach (var c in _colorsInUse)
            counts[c] = 1;
        var indexes = Enumerable.Range(0, ScaffoldPrefab.Buttons.Length).ToList().Shuffle().Take(_colorsInUse.Length).ToArray();
        for (var i = 0; i < _colorsInUse.Length; i++)
            _colors[indexes[i]] = _colorsInUse[i];
        for (int i = 0; i < ScaffoldPrefab.Buttons.Length; i++)
            if (!indexes.Contains(i))
            {
                _colors[i] = _colorsInUse.PickRandom();
                counts[_colors[i]]++;
            }
        var minCount = _colorsInUse.Min(c => counts[c]);
        if (counts.Count(kvp => kvp.Value == minCount) > 1)
            goto tryAgain;

        var minCountColor = _colorsInUse.First(c => counts[c] == minCount);

        _lastStage = minCountColor;
        _expectedPresses = new HashSet<int>();
        _allowedPresses = new HashSet<int>();
        for (int i = 0; i < ScaffoldPrefab.Buttons.Length; i++)
            if (_colors[i] == minCountColor)
            {
                _allowedPresses.Add(i);
                _expectedPresses.Add(i);
            }
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.NonwhiteOnly, delay: true);
        //LogDebug("Colors: {0}", _colors.JoinString(", "));
    }

    protected override void ButtonPressed(int index)
    {
        if (_isSolved)
            return;

        if (!_allowedPresses.Contains(index))
        {
            Log(@"Button #{0} ({1}) was incorrect at this time.", index, _colors[index]);
            Strike();
            SetInitialState();
        }
        else
        {
            PlaySound(index);
            _expectedPresses.Remove(index);
            _colors[index] = SquareColor.White;
            SetButtonColor(index, SquareColor.White);
            if (_expectedPresses.Count == 0)
            {
                var whiteCount = _colors.Count(c => c == SquareColor.White);
                if (whiteCount == ScaffoldPrefab.Buttons.Length)
                {
                    _expectedPresses = null;
                    _allowedPresses = null;
                    ModulePassed();
                }
                else
                {
                    _allowedPresses.Clear();

                    var nonWhite = Enumerable.Range(0, ScaffoldPrefab.Buttons.Length).Where(i => _colors[i] != SquareColor.White).ToArray();
                    foreach (var i in nonWhite)
                    {
                        SetButtonBlack(i);
                        _colors[i] = _colorsInUse.PickRandom();
                    }

                    // Move to next stage.
                    var columnIx = _lastStage.Equals(true) ? 4 : _lastStage.Equals(false) ? 5 : Array.IndexOf(_colorsInUse, (SquareColor) _lastStage);
                    var nextStage = _table[whiteCount - 1][columnIx];
                    Log("{0} lit: next stage is {1}.", whiteCount, nextStage.Equals(true) ? "Row" : nextStage.Equals(false) ? "Column" : ((SquareColor) nextStage).ToString());
                    if (nextStage.Equals(true))
                    {
                        // Row
                        var firstRow = Enumerable.Range(0, sqrt(ScaffoldPrefab.Buttons.Length)).First(row => Enumerable.Range(0, sqrt(ScaffoldPrefab.Buttons.Length)).Any(col => _colors[sqrt(ScaffoldPrefab.Buttons.Length) * row + col] != SquareColor.White));
                        for (int col = 0; col < sqrt(ScaffoldPrefab.Buttons.Length); col++)
                            if (_colors[sqrt(ScaffoldPrefab.Buttons.Length) * firstRow + col] != SquareColor.White)
                            {
                                _allowedPresses.Add(sqrt(ScaffoldPrefab.Buttons.Length) * firstRow + col);
                                _expectedPresses.Add(sqrt(ScaffoldPrefab.Buttons.Length) * firstRow + col);
                            }
                    }
                    else if (nextStage.Equals(false))
                    {
                        // Column
                        var firstCol = Enumerable.Range(0, sqrt(ScaffoldPrefab.Buttons.Length)).First(col => Enumerable.Range(0, sqrt(ScaffoldPrefab.Buttons.Length)).Any(row => _colors[sqrt(ScaffoldPrefab.Buttons.Length) * row + col] != SquareColor.White));
                        for (int row = 0; row < sqrt(ScaffoldPrefab.Buttons.Length); row++)
                            if (_colors[sqrt(ScaffoldPrefab.Buttons.Length) * row + firstCol] != SquareColor.White)
                            {
                                _allowedPresses.Add(sqrt(ScaffoldPrefab.Buttons.Length) * row + firstCol);
                                _expectedPresses.Add(sqrt(ScaffoldPrefab.Buttons.Length) * row + firstCol);
                            }
                    }
                    else
                    {
                        // A specific color
                        // Make sure at least one square has that color
                        var color = (SquareColor) nextStage;
                        _colors[nonWhite[Rnd.Range(0, nonWhite.Length)]] = color;
                        for (int i = 0; i < ScaffoldPrefab.Buttons.Length; i++)
                            if (_colors[i] == color)
                            {
                                _allowedPresses.Add(i);
                                _expectedPresses.Add(i);
                            }
                    }
                    _lastStage = nextStage;
                    StartSquareColorsCoroutine(_colors, SquaresToRecolor.NonwhiteOnly, delay: true);
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            Buttons[_expectedPresses.First()].OnInteract();
            yield return new WaitForSeconds(.1f);

            while (IsCoroutineActive)
                yield return true;
        }
    }
}
