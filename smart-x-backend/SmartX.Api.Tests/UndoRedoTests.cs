using SmartX.Api.Logic;
using SmartX.Api.Models;
using SmartX.Api.Models.Requests;
using SmartX.Api.Models.Stream;

namespace SmartX.Api.Tests;

/// <summary>The override history: a Stack for undo and a second Stack for redo.</summary>
public class UndoRedoTests
{
    private const string Operator = "tester";

    private static DeviceCommand Dispatch(SmartXCommandEngine engine, string nodeId, string parameters)
    {
        var (command, error) = engine.Dispatch(new DispatchCommandRequest
        {
            NodeId = nodeId,
            CommandType = CommandType.SetThreshold,
            Parameters = parameters,
            IssuedBy = Operator
        });

        Assert.Null(error);
        return command!;
    }

    [Fact]
    public void Undo_cancels_a_queued_override_and_moves_it_to_the_redo_stack()
    {
        var engine = EngineFixture.Create();
        var node = EngineFixture.ReachableDevices(engine, CommandType.SetThreshold).First().NodeId;
        var command = Dispatch(engine, node, "temp.max=31.5");

        var (result, error) = engine.UndoLastOverride(Operator, command.Id);

        Assert.Null(error);
        Assert.Equal(UndoOutcome.Cancelled, result!.Outcome);
        Assert.Equal(CommandStatus.Cancelled, command.Status);

        var history = engine.GetOverrideHistory();
        Assert.DoesNotContain(history.Undo, entry => entry.CommandId == command.Id);
        Assert.Equal(command.Id, history.Redo[0].CommandId);
    }

    [Fact]
    public void Undo_restores_the_previous_setting()
    {
        var engine = EngineFixture.Create();
        var node = EngineFixture.ReachableDevices(engine, CommandType.SetThreshold).First().NodeId;

        Dispatch(engine, node, "temp.max=31.5");
        var second = Dispatch(engine, node, "temp.max=33");
        Assert.Contains("31.5", engine.GetOverrideHistory().Undo[0].UndoDescription);

        engine.UndoLastOverride(Operator, second.Id);

        // The node is back at 31.5, so the plan for undoing the next change restores 31.5.
        Dispatch(engine, node, "temp.max=35");
        Assert.Contains("31.5", engine.GetOverrideHistory().Undo[0].UndoDescription);
    }

    [Fact]
    public void Undo_is_idempotent_when_the_same_entry_is_undone_twice()
    {
        var engine = EngineFixture.Create();
        var node = EngineFixture.ReachableDevices(engine, CommandType.SetThreshold).First().NodeId;
        var older = Dispatch(engine, node, "temp.max=30");
        var newer = Dispatch(engine, node, "temp.max=31");

        engine.UndoLastOverride(Operator, newer.Id);
        var (repeat, error) = engine.UndoLastOverride(Operator, newer.Id);

        Assert.Null(error);
        Assert.Equal(UndoOutcome.AlreadyUndone, repeat!.Outcome);

        // The older override was not touched by the repeated request.
        Assert.Equal(older.Id, engine.GetOverrideHistory().Undo[0].CommandId);
        Assert.Equal(CommandStatus.Queued, older.Status);
    }

    [Fact]
    public void Redo_reapplies_the_undone_override_and_is_idempotent()
    {
        var engine = EngineFixture.Create();
        var node = EngineFixture.ReachableDevices(engine, CommandType.SetThreshold).First().NodeId;
        var command = Dispatch(engine, node, "temp.max=31.5");
        engine.UndoLastOverride(Operator, command.Id);

        var (redo, error) = engine.RedoLastUndo(Operator, command.Id);

        Assert.Null(error);
        Assert.Equal(RedoOutcome.Redone, redo!.Outcome);
        Assert.Equal(command.Id, redo.Redone!.RedoOf);
        Assert.Equal("temp.max=31.5", redo.Command!.Parameters);

        var history = engine.GetOverrideHistory();
        Assert.Equal(redo.Command.Id, history.Undo[0].CommandId);
        Assert.Empty(history.Redo);

        var (repeat, repeatError) = engine.RedoLastUndo(Operator, command.Id);
        Assert.Null(repeatError);
        Assert.Equal(RedoOutcome.AlreadyRedone, repeat!.Outcome);
        Assert.Single(engine.GetOverrideHistory().Undo, entry => entry.RedoOf == command.Id);
    }

    [Fact]
    public void A_new_override_clears_the_redo_stack()
    {
        var engine = EngineFixture.Create();
        var node = EngineFixture.ReachableDevices(engine, CommandType.SetThreshold).First().NodeId;
        var command = Dispatch(engine, node, "temp.max=31.5");
        engine.UndoLastOverride(Operator, command.Id);
        Assert.Single(engine.GetOverrideHistory().Redo);

        Dispatch(engine, node, "temp.max=29");

        Assert.Empty(engine.GetOverrideHistory().Redo);
    }
}
