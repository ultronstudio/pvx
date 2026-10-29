using PVX.Cinematics;
using PVX.Dialogue;
using PVX.FirstPerson;
using PVX.Interaction.Stride;
using PVX.World;
using Stride.Engine.Design;
using Xunit;

namespace PVX.ProcessorExecutionModeTests;

/// <summary>
/// Locks in which PVX entity components' default processors must be
/// Runtime-only (<see cref="ExecutionMode.Runtime"/>).
/// </summary>
/// <remarks>
/// Every processor listed in <see cref="RuntimeOnlyComponents_PinExecutionModeToRuntime"/>
/// resolves <c>PVXRuntime</c>/<c>PVXGameplaySession</c>/<c>PVXInteractionFeature</c>,
/// or otherwise drives gameplay simulation (transform animation, physics
/// sync). <see cref="DefaultEntityComponentProcessorAttribute.ExecutionMode"/>
/// defaults to <see cref="ExecutionMode.All"/>, which also matches
/// GameStudio's scene editor (<c>ExecutionMode.Editor</c> — see
/// <c>EntityHierarchyEditorGame</c>): without an explicit <see cref="ExecutionMode.Runtime"/>
/// pin, the processor would be instantiated there too, with no PVX runtime
/// registered, either throwing immediately (FirstPerson/Interaction
/// controller/Carry/Cinematic all fail fast on a missing <c>PVXRuntime</c>)
/// or running gameplay-only transform/physics logic somewhere it was never
/// meant to run (Door/Drawer). This is a regression test for exactly that
/// failure mode, not a correctness test of each processor's own behavior
/// (see each module's own test project for that).
/// </remarks>
public class ProcessorExecutionModeTests
{
    [Theory]
    [InlineData(typeof(PVXFirstPersonControllerComponent))]
    [InlineData(typeof(PVXStrideInteractionControllerComponent))]
    [InlineData(typeof(PVXCarryComponent))]
    [InlineData(typeof(PVXCinematicComponent))]
    [InlineData(typeof(PVXDoorComponent))]
    [InlineData(typeof(PVXDrawerComponent))]
    public void RuntimeOnlyComponents_PinExecutionModeToRuntime(Type componentType)
    {
        Assert.Equal(ExecutionMode.Runtime, GetExecutionMode(componentType));
    }

    /// <summary>
    /// The other side of the same invariant: components whose processors
    /// have no PVX runtime dependency (and, for <see cref="PVXSwitchComponent"/>/<see cref="PVXNpcComponent"/>,
    /// whose "Starts..." initialization is actually useful to preview in
    /// the editor) must stay unrestricted — proving the Runtime pin above
    /// is a deliberate, audited choice per component, not something
    /// applied mechanically to every PVX processor.
    /// </summary>
    [Theory]
    [InlineData(typeof(PVXSwitchComponent))]
    [InlineData(typeof(PVXPickupComponent))]
    [InlineData(typeof(PVXNpcComponent))]
    [InlineData(typeof(PVXDialogueComponent))]
    public void EditorSafeComponents_DoNotRestrictExecutionMode(Type componentType)
    {
        Assert.Equal(ExecutionMode.All, GetExecutionMode(componentType));
    }

    private static ExecutionMode GetExecutionMode(Type componentType)
    {
        var attribute = componentType
            .GetCustomAttributes(typeof(DefaultEntityComponentProcessorAttribute), inherit: false)
            .Cast<DefaultEntityComponentProcessorAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        return attribute!.ExecutionMode;
    }
}
