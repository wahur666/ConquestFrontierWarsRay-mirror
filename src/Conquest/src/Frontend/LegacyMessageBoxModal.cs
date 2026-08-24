using System;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Frontend;

internal enum LegacyMessageBoxButtons {
	Ok,
	OkCancel
}

internal sealed class LegacyMessageBoxModal : LegacyModalNode {
	private readonly Action<bool> _completed;
	private readonly GT_MESSAGEBOX _messageBox;
	private readonly string _messageText;
	private readonly string _titleText;
	private readonly LegacyRcStringResolver _strings;
	private readonly UtfDbRepository _utfDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly LegacyMessageBoxButtons _buttonMode;
	private bool _awaitingEscapeRelease = true;

	public LegacyMessageBoxModal(
		GT_MESSAGEBOX messageBox,
		UtfDbRepository utfDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		string titleText,
		string messageText,
		LegacyMessageBoxButtons buttonMode,
		Action<bool> completed) : base("LegacyMessageBoxModal") {
		CloseOnEscape = false;
		_messageBox = messageBox;
		_utfDbRepository = utfDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_titleText = titleText;
		_messageText = messageText;
		_buttonMode = buttonMode;
		_completed = completed;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = new Vector2(92f, 145f);

		AddStaticNode("Background", _messageBox.Background);
		var title = AddStaticNode("Title", _messageBox.Title);
		var message = AddStaticNode("Message", _messageBox.Message);
		title.SetText(_titleText);
		message.SetText(_messageText);

		var ok = AddButtonNode("Ok", _buttonMode == LegacyMessageBoxButtons.Ok ? _messageBox.OkAlone : _messageBox.Ok);
		ok.Activated += _ => _completed(true);
		ok.SetKeyboardFocus(true);

		if (_buttonMode == LegacyMessageBoxButtons.OkCancel) {
			var cancel = AddButtonNode("Cancel", _messageBox.Cancel);
			cancel.Activated += _ => _completed(false);
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (_awaitingEscapeRelease) {
			_awaitingEscapeRelease = Input.IsActionPressed(InputManager.UiEscapeAction);
			return;
		}

		if (Input.IsActionJustPressed(InputManager.UiEscapeAction)) {
			_completed(false);
		}
	}

	private LegacyButtonNode AddButtonNode(string label, BUTTON_DATA data) {
		var archetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonType);
		var node = new LegacyButtonNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.ButtonText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.Text = text;
		}

		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyStaticNode AddStaticNode(string label, STATIC_DATA data) {
		var archetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", data.StaticType);
		var node = new LegacyStaticNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.StaticText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.SetText(text);
		}

		ContentRoot.AddChild(node);
		return node;
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _utfDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
		       ?? throw new InvalidOperationException(
			       $"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}
}
