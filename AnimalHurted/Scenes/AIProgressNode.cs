using Godot;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MonteCarlo;
using AnimalHurtedLib;
using AnimalHurtedLib.AI;

public partial class AIProgressNode : Node
{
    bool _abort;
    IOrderedEnumerable<MonteCarloTreeSearch.Node<GameAIPlayer, Move>> _result;

    public ProgressBar ProgressBar { get { return GetNode<ProgressBar>("ProgressBar"); } }

    [Signal]
    public delegate void ProgressSignalEventHandler(int numIterations);

    [Signal]
    public delegate void ProgressFinishedSignalEventHandler();

    public override void _Ready()
    {
        ProgressSignal += _signal_Progress;
        ProgressFinishedSignal += _signal_ProgressFinished;

        ProgressBar.MaxValue = AISingleton.AIMaxIterations;

        // with HardMode, we don't start thread until player 1 has finished building their deck
        if (AISingleton.Instance.HardMode)
            AISingleton.Instance.StartAIThread();

        AISingleton.Instance.SetAIDelegates(out bool aiFinished, out _result, AIProgress, AIFinished);
        if (aiFinished)
            EmitSignal("ProgressFinishedSignal");
    }

    // thread event
    void AIProgress(object sender, int iterationCount, out bool abort)
    {
        abort = _abort;
        // signal to main thread
        EmitSignal("ProgressSignal", iterationCount);
    }

    // thread event
    void AIFinished(object sender, IOrderedEnumerable<MonteCarloTreeSearch.Node<GameAIPlayer, Move>> result)
    {
        _result = result;

		// output the branch of the tree from the result selected
        /*int lineCount = 0;
        string output = string.Empty;
        var node = result.FirstOrDefault();
        while (node != null)
        {
            if (!string.IsNullOrEmpty(output))
                output += System.Environment.NewLine;
            output += $"{new string(' ', lineCount * 4)} {node.NumWins} '{node.State.CurrentPlayer.Player.Name}' {node.State.CurrentPlayer.Player.Game.Round}";
            lineCount++;
            node = node.Children.OrderByDescending(n => n.NumRuns).FirstOrDefault();
        }
        Debug.WriteLine(output);*/

        EmitSignal("ProgressFinishedSignal");
    }

    public void _signal_Progress(int numIterations)
    {
        ProgressBar.Value = numIterations;
    }

    public void _signal_ProgressFinished()
    {
        var move = _result.FirstOrDefault();
        move?.Action.ExecuteActions(GameSingleton.Instance.Game.Player2);
        BuildNode.StartBattle(this);
    }

    public void _on_ContinueButton_pressed()
    {
        _abort = true;
        GetNode<Button>("ContinueButton").Disabled = true;
    }
}
