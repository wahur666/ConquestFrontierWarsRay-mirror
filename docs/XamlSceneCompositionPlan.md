# XAML-Style Scene Composition Plan

## Problem

The current framework composes scenes imperatively in C# through `Node.AddChild(...)`
and property assignment. That works, but it mixes three concerns in one place:

1. scene structure
2. editor-facing layout data
3. gameplay logic and event wiring

For larger scenes, this becomes harder to read, harder to diff, and harder to
iterate on without touching logic code. The idea is to introduce a XAML-style
XML scene description that defines the node tree, then generate a partial C#
class that builds that tree and exposes named nodes to a hand-written partial
class containing game logic.

The target is not a general UI framework clone. The target is a practical scene
authoring layer for the current `Framework` node system.

## Assessment

### Short answer

Yes, the idea is feasible, and it fits the current framework better as a
**build-time code generation system** than as a runtime XML loader.

### Why it fits the current framework

The existing framework already has the right primitives:

- `Node` owns parent/child composition and lifecycle.
- `Node2D` and `Node3D` expose plain settable transform properties.
- `SceneTree` expects an ordinary root `Node` object.
- The framework is explicit and C#-first; there is no competing serialization or
  reflection-heavy scene system already in place.

That means XML can act as a declarative front-end that compiles into the same
imperative node construction the framework already uses.

### Why generated partial classes are the right shape

Generated partial classes preserve normal C# ergonomics:

- logic stays in source-controlled hand-written code
- scene structure stays in markup
- named nodes can become strongly typed fields or properties
- event hooks can compile into normal method calls
- the runtime does not need a fragile dynamic parser to construct every screen

This is close to the WPF/XAML model, but the implementation should stay much
smaller and stricter.

### Why a runtime XML loader is a weaker first step

A runtime loader looks simpler at first, but it creates avoidable problems:

- late failures instead of compile-time failures
- more reflection or string-based type lookup
- weaker IDE navigation
- harder refactoring of node names and event handlers
- more moving parts in game startup paths

For this framework, generated code is the cleaner default.

### Main constraints from the current framework

There are also real constraints:

1. `Node.Name` is constructor-only today.
   Generated code must pass node names through constructors instead of assigning
   them later.

2. Node creation is constructor-driven.
   The markup system should only target node types with simple, predictable
   constructors at first.

3. There is no existing source-generator project.
   This is feasible in .NET, but it is new infrastructure for the repo.

4. There is no general property conversion layer.
   The generator must own explicit conversions for types like `Vector2`,
   `Vector3`, `Quaternion`, colors, enums, and resources.

5. The framework currently favors explicit APIs over backend abstraction.
   The scene markup should follow that same philosophy instead of becoming a
   generic object graph serializer.

### Recommendation

Proceed, but keep the first version narrow:

- scene composition only
- no full data binding system
- no general-purpose dependency property system
- no runtime reflection-based loader as the primary path
- start with `Node`, `Node2D`, `Node3D`, `Control`, `PanelNode`, `TextNode`,
  `ButtonNode`, `Sprite`, `Camera3DNode`, `Socket3D`, `Hardpoint3D`, and
  `Light3D`

This should be treated as a **scene definition compiler**, not as “XAML for
everything”.

## Proposed Shape

### Authoring file

Use a dedicated XML extension such as `.scene.xml` or `.scene.xaml`. A separate
extension is preferable to avoid implying full WPF/XAML compatibility.

Example:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Scene
    Class="ConquestFrontierWarsRay.Game.Scenes.SkirmishHudScene"
    RootType="Node"
    xmlns:f="clr-namespace:ConquestFrontierWarsRay.Framework">
  <f:Node Name="Root">
    <f:Camera3DNode Name="MainCamera" Position="0,25,-40" />

    <f:Node2D Name="HudRoot">
      <f:PanelNode Name="TopBar" Position="24,24" Size="640,72">
        <f:TextNode Name="ResourceText" Text="Ore: 0" />
        <f:ButtonNode Name="PauseButton" Text="Pause" Clicked="OnPauseClicked" />
      </f:PanelNode>
    </f:Node2D>
  </f:Node>
</Scene>
```

This should stay deliberately conservative:

- one class per scene file
- one root node subtree
- explicit node names for anything the logic needs to reference
- explicit event handler names

### Generated output

The generator should emit a partial class that builds the declared node tree and
stores typed references to named nodes.

Example generated shape:

```csharp
namespace ConquestFrontierWarsRay.Game.Scenes;

public partial class SkirmishHudScene : Node {
    public Camera3DNode MainCamera { get; private set; } = null!;
    public Node2D HudRoot { get; private set; } = null!;
    public PanelNode TopBar { get; private set; } = null!;
    public TextNode ResourceText { get; private set; } = null!;
    public ButtonNode PauseButton { get; private set; } = null!;

    partial void OnBuildCompleted();

    public SkirmishHudScene() : base("Root") {
        BuildGeneratedScene();
        OnBuildCompleted();
    }

    private void BuildGeneratedScene() {
        MainCamera = AddChild(new Camera3DNode("MainCamera"));
        MainCamera.Position = new Vector3(0f, 25f, -40f);

        HudRoot = AddChild(new Node2D("HudRoot"));
        TopBar = HudRoot.AddChild(new PanelNode("TopBar"));
        TopBar.Position = new Vector2(24f, 24f);
        TopBar.Size = new Vector2(640f, 72f);

        ResourceText = TopBar.AddChild(new TextNode("ResourceText"));
        ResourceText.Text = "Ore: 0";

        PauseButton = TopBar.AddChild(new ButtonNode("PauseButton"));
        PauseButton.Text = "Pause";
        PauseButton.Clicked += OnPauseClicked;
    }
}
```

### Hand-written logic partial

```csharp
namespace ConquestFrontierWarsRay.Game.Scenes;

public partial class SkirmishHudScene {
    partial void OnBuildCompleted() {
        ResourceText.Text = "Ore: 500";
    }

    private void OnPauseClicked() {
        // game logic
    }
}
```

This split is the core of the idea. It keeps scene structure generated and
keeps gameplay logic hand-authored.

## Design Rules

To keep the system maintainable, the first version should follow these rules:

1. **No arbitrary object graph serialization.**
   Only support approved framework node types and approved property types.

2. **No reflection-driven event hookup in runtime code.**
   Resolve handlers at generation time by emitting direct method references.

3. **No hidden magic lifecycle.**
   Generated code should just call constructors, `AddChild`, property setters,
   and event subscriptions.

4. **No binding engine in v1.**
   State updates should still happen through normal C# logic.

5. **No content pipeline explosion.**
   Resource references should map to existing framework resource APIs, not
   invent a second asset system.

## Potential Execution

### Phase 1: Decide the contract

Define the smallest supported contract:

- file extension
- XML schema shape
- allowed framework node types
- allowed property types
- event syntax
- named-node exposure rules
- generated class naming rules

Deliverable:

- a short schema spec document
- 2 to 3 example scene files

### Phase 2: Add source-generator infrastructure

Create a dedicated generator project, for example:

- `src/Framework.SceneGenerator`

The generator should:

- discover `*.scene.xml` files via `AdditionalFiles`
- parse XML
- validate node types and attributes
- emit `.g.cs` partial classes
- report diagnostics with file/line context

Deliverable:

- generator project referenced by the app project that owns scenes
- one generated scene class compiling successfully

### Phase 3: Support a narrow node/property set

Start with a controlled matrix:

- nodes: `Node`, `Node2D`, `Node3D`, `Control`, `PanelNode`, `TextNode`,
  `ButtonNode`, `Sprite`, `Camera3DNode`
- properties:
  - `Position`
  - `Rotation`
  - `Scale`
  - `Text`
  - `Visible`
  - simple size/layout properties already present on control types

Avoid 3D lighting, resources, and hardpoints until the basic pipeline is
stable.

Deliverable:

- one 2D HUD scene
- one simple 3D scene root with camera
- tests for parse and generation diagnostics

### Phase 4: Wire events and named node access

Add:

- named node references as generated properties
- event hookup for simple patterns such as `Clicked="OnPauseClicked"`
- duplicate-name validation
- missing-handler diagnostics where practical

Deliverable:

- usable scene + logic partial workflow

### Phase 5: Add resource references

After the tree generator is stable, add framework-aware resource attributes such
as texture or audio references.

This should compile into explicit resource-loading code, not opaque runtime
lookups scattered across scene nodes.

Deliverable:

- `Sprite` scene authoring with texture resource references

### Phase 6: Add optional editor support

Only after the file format stabilizes:

- XML schema or completion hints
- preview/test app scene loader
- scene validation command in CI

## Risks

### Good risks

These are manageable:

- writing the source generator
- building explicit type converters
- keeping the schema small

### Bad risks

These are where the idea can go wrong:

1. trying to replicate full WPF/XAML semantics
2. adding binding, templating, styles, and dependency-property behavior too
   early
3. supporting every framework type before the contract is proven
4. mixing scene markup with general gameplay data serialization

If those happen, the system will become expensive before it proves value.

## Suggested First Slice

The most practical first slice is:

1. create one scene file for a non-trivial HUD or menu
2. generate a partial class for that file
3. keep all logic in the hand-written partial
4. instantiate the scene as a normal `SceneTree` root
5. verify that scene edits mostly avoid touching logic files

If that slice feels cleaner than the equivalent hand-written C# after one or
two real screens, the idea is worth continuing. If not, stop early and keep the
framework imperative.

## Final Recommendation

This is a good idea **if** the scope stays tight. For this framework, the right
implementation is:

- XML scene markup
- build-time source generation
- generated partial class for composition
- hand-written partial class for logic

It is technically feasible with the current framework because the framework is
already built around explicit node construction and recursive composition.

It is not a good idea if the goal is to import the full conceptual weight of
WPF/XAML. The practical version is a small, strongly typed scene compiler for
the existing node tree.
