using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class LegacyScrollBarNodeTests {
	[Fact]
	public void WheelEvent_RaisesLineDownWhenScrolledNegative() {
		var scrollBar = new LegacyScrollBarNode {
			Size = new Vector2(24f, 80f),
			Visible = true
		};
		var lineDownRequests = 0;
		scrollBar.LineDownRequested += _ => lineDownRequests++;
		scrollBar.SetVisible(true);
		scrollBar.SetScrollRange(10);
		scrollBar.SetViewRange(4);

		var pointerEvent = new UiPointerEvent(UiPointerEventKind.Wheel, Vector2.Zero, null, -1f, scrollBar);
		pointerEvent.RouteTo(scrollBar);

		scrollBar.OnPointerEvent(pointerEvent);

		Assert.Equal(1, lineDownRequests);
		Assert.True(pointerEvent.Handled);
	}

	[Fact]
	public void WheelEvent_RaisesLineUpWhenScrolledPositive() {
		var scrollBar = new LegacyScrollBarNode {
			Size = new Vector2(24f, 80f),
			Visible = true
		};
		var lineUpRequests = 0;
		scrollBar.LineUpRequested += _ => lineUpRequests++;
		scrollBar.SetVisible(true);
		scrollBar.SetScrollRange(10);
		scrollBar.SetViewRange(4);

		var pointerEvent = new UiPointerEvent(UiPointerEventKind.Wheel, Vector2.Zero, null, 1f, scrollBar);
		pointerEvent.RouteTo(scrollBar);

		scrollBar.OnPointerEvent(pointerEvent);

		Assert.Equal(1, lineUpRequests);
		Assert.True(pointerEvent.Handled);
	}
}
