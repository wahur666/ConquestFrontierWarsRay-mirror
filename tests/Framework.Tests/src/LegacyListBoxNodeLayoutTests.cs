using System.Numerics;
using System.Reflection;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class LegacyListBoxNodeLayoutTests {
	[Fact]
	public void ConfigureScrollBarLayout_PlacesScrollBarAtLaneStartInsideControl() {
		var listbox = new LegacyListBoxNode {
			Size = new Vector2(100f, 80f)
		};
		var scrollBar = CreateScrollBar();
		AttachScrollBar(listbox, scrollBar);
		SetPrivateField(listbox, "_textArea", new RECT { Left = 6, Top = 6, Right = 36, Bottom = 120 });

		InvokeConfigureScrollBarLayout(listbox);

		Assert.Equal(new Vector2(37f, 0f), scrollBar.Position);
		Assert.Equal(new Vector2(24f, 80f), scrollBar.Size);
	}

	[Fact]
	public void GetGlobalTextBounds_PreservesAuthoredContentWidth() {
		var listbox = new LegacyListBoxNode {
			Size = new Vector2(100f, 80f)
		};
		var scrollBar = CreateScrollBar();
		AttachScrollBar(listbox, scrollBar);
		SetPrivateField(listbox, "_textArea", new RECT { Left = 6, Top = 6, Right = 36, Bottom = 120 });

		var bounds = InvokeGetGlobalTextBounds(listbox, new Rectangle(0f, 0f, 100f, 80f));

		Assert.Equal(6f, bounds.X);
		Assert.Equal(31f, bounds.Width);
	}

	[Fact]
	public void ResolveConfiguredWidth_AddsScrollbarLaneToBaseWidth() {
		var listbox = new LegacyListBoxNode { };
		var scrollBar = CreateScrollBar();
		AttachScrollBar(listbox, scrollBar);
		SetPrivateField(listbox, "_textArea", new RECT { Left = 6, Top = 6, Right = 36, Bottom = 120 });

		var width = InvokeFloatMethod(listbox, "ResolveConfiguredWidth");

		Assert.Equal(50.2f, width);
	}

	[Fact]
	public void ResolveConfiguredHeight_UsesBaseHeight() {
		var listbox = new LegacyListBoxNode();
		SetPrivateField(listbox, "_textArea", new RECT { Left = 6, Top = 6, Right = 36, Bottom = 120 });

		var height = InvokeFloatMethod(listbox, "ResolveConfiguredHeight");

		Assert.Equal(121f, height);
	}

	private static void InvokeConfigureScrollBarLayout(LegacyListBoxNode listbox) {
		var method = typeof(LegacyListBoxNode).GetMethod("ConfigureScrollBarLayout", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		method!.Invoke(listbox, []);
	}

	private static Rectangle InvokeGetGlobalTextBounds(LegacyListBoxNode listbox, Rectangle bounds) {
		var method = typeof(LegacyListBoxNode).GetMethod("GetGlobalTextBounds", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		return Assert.IsType<Rectangle>(method!.Invoke(listbox, [bounds]));
	}

	private static float InvokeFloatMethod(LegacyListBoxNode listbox, string methodName) {
		var method = typeof(LegacyListBoxNode).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		return Assert.IsType<float>(method!.Invoke(listbox, []));
	}

	private static LegacyScrollBarNode CreateScrollBar() {
		var scrollBar = new LegacyScrollBarNode("ScrollBar") {
			Size = new Vector2(24f, 80f)
		};
		SetAutoProperty(scrollBar, "ButtonWidth", 24);
		SetPrivateField(scrollBar, "_scrollRange", 10);
		SetPrivateField(scrollBar, "_viewRange", 4);
		return scrollBar;
	}

	private static void AttachScrollBar(LegacyListBoxNode listbox, LegacyScrollBarNode scrollBar) {
		SetPrivateField(listbox, "_scrollBar", scrollBar);
		SetPrivateField(listbox, "_scrollBarRequested", true);
	}

	private static void SetAutoProperty<T>(object instance, string propertyName, T value) {
		var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(field);
		field!.SetValue(instance, value);
	}

	private static void SetPrivateField<T>(object instance, string fieldName, T value) {
		var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(field);
		field!.SetValue(instance, value);
	}
}
