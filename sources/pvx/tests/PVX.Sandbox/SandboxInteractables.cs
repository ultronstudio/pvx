using PVX.Interaction;

namespace PVX.Sandbox;

/// <summary>
/// Fake interaction source/target-discovery used by the sandbox demo in
/// place of a real first-person camera and physics raycast, neither of
/// which PVX.Interaction implements itself.
/// </summary>
internal sealed class SandboxInteractionSource : IPVXInteractionSource
{
}

internal sealed class SandboxTargetProvider : IPVXInteractionTargetProvider
{
    public PVXInteractionCandidate? Current { get; set; }

    public IReadOnlyList<PVXInteractionCandidate> GetCandidates(IPVXInteractionSource source) =>
        Current is { } candidate ? [candidate] : Array.Empty<PVXInteractionCandidate>();
}

/// <summary>
/// Minimal generic door: offers Open/Inspect while closed, Close/Inspect
/// while open. PVX.Interaction never knows about hinges or door state.
/// </summary>
internal sealed class SandboxDoor : IPVXInteractable
{
    private bool isOpen;

    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
    {
        var inspect = new PVXInteractionDescriptor(
            new PVXInteractionActionId("inspect"), new SandboxAction("Inspect door"), priority: 50);

        if (isOpen)
        {
            var close = new PVXInteractionDescriptor(
                new PVXInteractionActionId("close"),
                new SandboxAction("Close door", () => isOpen = false),
                priority: 100);
            return [close, inspect];
        }

        var open = new PVXInteractionDescriptor(
            new PVXInteractionActionId("open"),
            new SandboxAction("Open door", () => isOpen = true),
            priority: 100);
        return [open, inspect];
    }
}

/// <summary>
/// Generic hold-mode target (e.g. a fuse switch requiring sustained input).
/// </summary>
internal sealed class SandboxHoldObject : IPVXInteractable
{
    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context) =>
    [
        new PVXInteractionDescriptor(
            new PVXInteractionActionId("activate"),
            new SandboxAction("Activate"),
            priority: 100,
            mode: PVXInteractionMode.Hold,
            holdDuration: TimeSpan.FromSeconds(2))
    ];
}

/// <summary>
/// Ladder acceptance case: offers Climb Up/Climb Down depending on which
/// interaction point is focused, and only hands off to a fake traversal
/// handler. PVX.Interaction never implements the actual climbing.
/// </summary>
internal sealed class SandboxLadder : IPVXInteractable
{
    public static readonly PVXInteractionPoint BottomEntry = new("BottomEntry");
    public static readonly PVXInteractionPoint TopEntry = new("TopEntry");

    private readonly SandboxTraversalHandler traversal;

    public SandboxLadder(SandboxTraversalHandler traversal)
    {
        this.traversal = traversal;
    }

    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
    {
        if (context.Point == BottomEntry)
        {
            return
            [
                new PVXInteractionDescriptor(
                    new PVXInteractionActionId("climb_up"),
                    new SandboxAction("Climb Up", () => traversal.EnterLadder("Up")),
                    priority: 100)
            ];
        }

        if (context.Point == TopEntry)
        {
            return
            [
                new PVXInteractionDescriptor(
                    new PVXInteractionActionId("climb_down"),
                    new SandboxAction("Climb Down", () => traversal.EnterLadder("Down")),
                    priority: 100)
            ];
        }

        return Array.Empty<PVXInteractionDescriptor>();
    }
}

/// <summary>
/// Stand-in for a future PVX.Characters.Traversal module: records that a
/// ladder was entered without implementing any actual movement.
/// </summary>
internal sealed class SandboxTraversalHandler
{
    public void EnterLadder(string direction) =>
        Console.WriteLine($"[PVX Sandbox] Traversal handler: EnterLadder({direction})");
}

/// <summary>
/// Shared generic action behavior for all sandbox interactables: logs and
/// invokes an optional effect, never implementing any real mechanic.
/// </summary>
internal sealed class SandboxAction : IPVXInteractionAction
{
    private readonly string label;
    private readonly Action? onExecute;

    public SandboxAction(string label, Action? onExecute = null)
    {
        this.label = label;
        this.onExecute = onExecute;
    }

    public bool CanExecute(PVXInteractionContext context) => true;

    public void Begin(PVXInteractionContext context) =>
        Console.WriteLine($"[PVX Sandbox] Action begin: {label}");

    public bool Execute(PVXInteractionContext context)
    {
        Console.WriteLine($"[PVX Sandbox] Action execute: {label}");
        onExecute?.Invoke();
        return true;
    }

    public void Cancel(PVXInteractionContext context) =>
        Console.WriteLine($"[PVX Sandbox] Action cancel: {label}");
}
